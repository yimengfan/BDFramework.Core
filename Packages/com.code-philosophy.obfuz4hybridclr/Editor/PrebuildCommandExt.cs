// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using HybridCLR.Editor.Commands;
using HybridCLR.Editor;
using Obfuz.Settings;
using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;
using System.IO;
using HybridCLR.Editor.Link;
using HybridCLR.Editor.Meta;
using UnityEditor.Build;
using HybridCLR.Editor.Installer;
using HybridCLR.Editor.MethodBridge;
using System.Linq;
using Analyzer = HybridCLR.Editor.MethodBridge.Analyzer;
using HybridCLR.Editor.Settings;
using Obfuz.Utils;
using FileUtil = Obfuz.Utils.FileUtil;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;
using CombinedAssemblyResolver = HybridCLR.Editor.Meta.CombinedAssemblyResolver;
using MetaUtil = HybridCLR.Editor.Meta.MetaUtil;
using AssemblyCache = HybridCLR.Editor.Meta.AssemblyCache;
using HybridCLR.Editor.AOT;
using Analyzer2 = HybridCLR.Editor.AOT.Analyzer;

namespace Obfuz4HybridCLR
{
    public static class PrebuildCommandExt
    {
        public static string GetObfuscatedHotUpdateAssemblyOutputPath(BuildTarget target)
        {
            return $"{ObfuzSettings.Instance.ObfuzRootDir}/{target}/ObfuscatedHotUpdateAssemblies";
        }


        [MenuItem("HybridCLR/ObfuzExtension/GenerateAll")]
        public static void GenerateAll()
        {
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException($"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            GeneratePolymorphicCodesWhenEnable();
            LinkGeneratorCommand.GenerateLinkXml(target);
            StripAOTDllCommand.GenerateStripedAOTDlls(target);

            string obfuscatedHotUpdateDllPath = GetObfuscatedHotUpdateAssemblyOutputPath(target);
            ObfuscateUtil.ObfuscateHotUpdateAssemblies(target, obfuscatedHotUpdateDllPath);
            GenerateMethodBridgeAndReversePInvokeWrapper(target, obfuscatedHotUpdateDllPath);
            GenerateAOTGenericReference(target, obfuscatedHotUpdateDllPath);
        }

        [MenuItem("HybridCLR/ObfuzExtension/CompileAndObfuscateDll")]
        public static void CompileAndObfuscateDll()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);

            string obfuscatedHotUpdateDllPath = GetObfuscatedHotUpdateAssemblyOutputPath(target);
            ObfuscateUtil.ObfuscateHotUpdateAssemblies(target, obfuscatedHotUpdateDllPath);
        }

        [MenuItem("HybridCLR/ObfuzExtension/GeneratePolymorphicCodes")]
        public static void GeneratePolymorphicCodes()
        {
            ObfuscateUtil.GeneratePolymorphicCodes($"{SettingsUtil.LocalIl2CppDir}/libil2cpp");
        }

        private static void GeneratePolymorphicCodesWhenEnable()
        {
            PolymorphicDllSettings settings = ObfuzSettings.Instance.polymorphicDllSettings;
            if (!settings.enable)
            {
                UnityEngine.Debug.LogWarning("Polymorphic code generation is disabled.");
                return;
            }
            GeneratePolymorphicCodes();
        }

        public static IAssemblyResolver CreateObfuscatedHotUpdateAssemblyResolver(BuildTarget target, List<string> obfuscatedHotUpdateAssemblies, string obfuscatedHotUpdateDllPath)
        {
            return new FixedSetAssemblyResolver(obfuscatedHotUpdateDllPath, obfuscatedHotUpdateAssemblies);
        }

        public static IAssemblyResolver CreateObfuscatedHotUpdateAndAOTAssemblyResolver(BuildTarget target, List<string> hotUpdateAssemblies, List<string> assembliesToObfuscate, string obfuscatedHotUpdateDllPath)
        {
            return new CombinedAssemblyResolver(
                CreateObfuscatedHotUpdateAssemblyResolver(target, hotUpdateAssemblies.Intersect(assembliesToObfuscate).ToList(), obfuscatedHotUpdateDllPath),
                MetaUtil.CreateHotUpdateAssemblyResolver(target, hotUpdateAssemblies.Except(assembliesToObfuscate).ToList()),
                MetaUtil.CreateAOTAssemblyResolver(target)
                );
        }

        public static void GenerateMethodBridgeAndReversePInvokeWrapper(BuildTarget target, string obfuscatedHotUpdateDllPath)
        {
            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            List<string> aotAssemblyNames = Directory.Exists(aotDllDir) ?
                Directory.GetFiles(aotDllDir, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToList()
                : new List<string>();
            if (aotAssemblyNames.Count == 0)
            {
                throw new Exception($"no aot assembly found. please run `HybridCLR/Generate/All` or `HybridCLR/Generate/AotDlls` to generate aot dlls before runing `HybridCLR/Generate/MethodBridge`");
            }
            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateAOTAssemblyResolver(target), aotAssemblyNames);

            var methodBridgeAnalyzer = new Analyzer(new Analyzer.Options
            {
                MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration),
                Collector = collector,
            });

            methodBridgeAnalyzer.Run();

            List<string> hotUpdateDlls = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            var cache = new AssemblyCache(CreateObfuscatedHotUpdateAndAOTAssemblyResolver(target, hotUpdateDlls, ObfuzSettings.Instance.assemblySettings.GetAssembliesToObfuscate(), obfuscatedHotUpdateDllPath));

            var reversePInvokeAnalyzer = new MonoPInvokeCallbackAnalyzer(cache, hotUpdateDlls);
            reversePInvokeAnalyzer.Run();

            var calliAnalyzer = new CalliAnalyzer(cache, hotUpdateDlls);
            calliAnalyzer.Run();
            var pinvokeAnalyzer = new PInvokeAnalyzer(cache, hotUpdateDlls);
            pinvokeAnalyzer.Run();
            var callPInvokeMethodSignatures = pinvokeAnalyzer.PInvokeMethodSignatures;

            string templateFile = $"{SettingsUtil.TemplatePathInPackage}/MethodBridge.cpp.tpl";
            string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge.cpp";

            var callNativeMethodSignatures = calliAnalyzer.CalliMethodSignatures.Concat(pinvokeAnalyzer.PInvokeMethodSignatures).ToList();

            var generateMethodBridgeMethod = typeof(MethodBridgeGeneratorCommand).GetMethod("GenerateMethodBridgeCppFile", BindingFlags.NonPublic | BindingFlags.Static);
            generateMethodBridgeMethod.Invoke(null, new object[] { methodBridgeAnalyzer.GenericMethods, reversePInvokeAnalyzer.ReversePInvokeMethods, callNativeMethodSignatures, templateFile, outputFile });

            MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }

        public static void GenerateAOTGenericReference(BuildTarget target, string obfuscatedHotUpdateDllPath)
        {
            var gs = SettingsUtil.HybridCLRSettings;
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(
                CreateObfuscatedHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames, ObfuzSettings.Instance.assemblySettings.GetAssembliesToObfuscate(), obfuscatedHotUpdateDllPath), hotUpdateDllNames);
            var analyzer = new Analyzer2(new Analyzer2.Options
            {
                MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
                Collector = collector,
            });

            analyzer.Run();

            var writer = new GenericReferenceWriter();
            writer.Write(analyzer.AotGenericTypes.ToList(), analyzer.AotGenericMethods.ToList(), $"{Application.dataPath}/{gs.outputAOTGenericReferenceFile}");
            AssetDatabase.Refresh();
        }
    }
}
