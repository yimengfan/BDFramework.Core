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

﻿using dnlib.DotNet;
using Obfuz.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using FileUtil = Obfuz.Utils.FileUtil;

namespace Obfuz.Unity
{

#if UNITY_2019_1_OR_NEWER
    public class ObfuscationProcess : IPostBuildPlayerScriptDLLs
    {
        public int callbackOrder => ObfuzSettings.Instance.buildPipelineSettings.obfuscationProcessCallbackOrder;

        public static event Action<ObfuscationBeginEventArgs> OnObfuscationBegin;

        public static event Action<ObfuscationEndEventArgs> OnObfuscationEnd;

        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
#if !UNITY_2022_1_OR_NEWER
            RunObfuscate(report.files);
#else
            RunObfuscate(report.GetFiles());
#endif
        }

        private static void BackupOriginalDlls(string srcDir, string dstDir, HashSet<string> dllNames)
        {
            FileUtil.RecreateDir(dstDir);
            foreach (string dllName in dllNames)
            {
                string srcFile = Path.Combine(srcDir, dllName + ".dll");
                string dstFile = Path.Combine(dstDir, dllName + ".dll");
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, dstFile, true);
                    Debug.Log($"BackupOriginalDll {srcFile} -> {dstFile}");
                }
            }
        }

        public static void ValidateReferences(string stagingAreaTempManagedDllDir, HashSet<string> assembliesToObfuscated, HashSet<string> obfuscationRelativeAssemblyNames)
        {
            var modCtx = ModuleDef.CreateModuleContext();
            var asmResolver = (AssemblyResolver)modCtx.AssemblyResolver;

            foreach (string assFile in Directory.GetFiles(stagingAreaTempManagedDllDir, "*.dll", SearchOption.AllDirectories))
            {
                ModuleDefMD mod = ModuleDefMD.Load(File.ReadAllBytes(assFile), modCtx);
                string modName = mod.Assembly.Name;
                foreach (AssemblyRef assRef in mod.GetAssemblyRefs())
                {
                    string refAssName = assRef.Name;
                    if (assembliesToObfuscated.Contains(refAssName) && !obfuscationRelativeAssemblyNames.Contains(modName))
                    {
                        throw new BuildFailedException($"assembly:{modName} references to obfuscated assembly:{refAssName}, but it's not been added to ObfuzSettings.AssemblySettings.NonObfuscatedButReferencingObfuscatedAssemblies.");
                    }
                }
                mod.Dispose();
            }
        }

        private static void RunObfuscate(BuildFile[] files)
        {
            ObfuzSettings settings = ObfuzSettings.Instance;
            if (!settings.buildPipelineSettings.enable)
            {
                Debug.Log("Obfuscation is disabled.");
                return;
            }

            Debug.Log("Obfuscation begin...");
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;

            var obfuscationRelativeAssemblyNames = new HashSet<string>(settings.assemblySettings.GetObfuscationRelativeAssemblyNames());
            string stagingAreaTempManagedDllDir = Path.GetDirectoryName(files.First(file => file.path.EndsWith(".dll")).path);
            string backupPlayerScriptAssembliesPath = settings.GetOriginalAssemblyBackupDir(buildTarget);
            BackupOriginalDlls(stagingAreaTempManagedDllDir, backupPlayerScriptAssembliesPath, obfuscationRelativeAssemblyNames);

            string applicationContentsPath = EditorApplication.applicationContentsPath;

            var obfuscatorBuilder = ObfuscatorBuilder.FromObfuzSettings(settings, buildTarget, false);

            var assemblySearchDirs = new List<string>
                {
                   stagingAreaTempManagedDllDir,
                };
            obfuscatorBuilder.InsertTopPriorityAssemblySearchPaths(assemblySearchDirs);

            ValidateReferences(stagingAreaTempManagedDllDir, new HashSet<string>(obfuscatorBuilder.CoreSettingsFacade.assembliesToObfuscate), obfuscationRelativeAssemblyNames);


            OnObfuscationBegin?.Invoke(new ObfuscationBeginEventArgs
            {
                scriptAssembliesPath = stagingAreaTempManagedDllDir,
                obfuscatedScriptAssembliesPath = obfuscatorBuilder.CoreSettingsFacade.obfuscatedAssemblyOutputPath,
            });
            bool succ = false;

            try
            {
                Obfuscator obfuz = obfuscatorBuilder.Build();
                obfuz.Run();

                foreach (var dllName in obfuscationRelativeAssemblyNames)
                {
                    string src = $"{obfuscatorBuilder.CoreSettingsFacade.obfuscatedAssemblyOutputPath}/{dllName}.dll";
                    string dst = $"{stagingAreaTempManagedDllDir}/{dllName}.dll";

                    if (!File.Exists(src))
                    {
                        Debug.LogWarning($"obfuscation assembly not found! skip copy. path:{src}");
                        continue;
                    }
                    File.Copy(src, dst, true);
                    Debug.Log($"obfuscate dll:{dst}");
                }
                succ = true;
            }
            catch (Exception e)
            {
                succ = false;
                Debug.LogException(e);
                Debug.LogError($"Obfuscation failed.");
            }
            OnObfuscationEnd?.Invoke(new ObfuscationEndEventArgs
            {
                success = succ,
                originalScriptAssembliesPath = backupPlayerScriptAssembliesPath,
                obfuscatedScriptAssembliesPath = stagingAreaTempManagedDllDir,
            });

            Debug.Log("Obfuscation end.");
        }
    }
#endif
}
