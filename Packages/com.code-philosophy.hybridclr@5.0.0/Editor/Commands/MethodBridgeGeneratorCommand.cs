using HybridCLR.Editor;
using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.MethodBridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    using Analyzer = HybridCLR.Editor.MethodBridge.Analyzer;
    public class MethodBridgeGeneratorCommand
    {

        public static void CleanIl2CppBuildCache()
        {
            string il2cppBuildCachePath = SettingsUtil.Il2CppBuildCacheDir;
            if (!Directory.Exists(il2cppBuildCachePath))
            {
                return;
            }
            Debug.Log($"clean il2cpp build cache:{il2cppBuildCachePath}");
            Directory.Delete(il2cppBuildCachePath, true);
        }

        private static void GenerateMethodBridgeCppFile(Analyzer analyzer, string outputFile)
        {
            string templateCode = File.ReadAllText(outputFile, Encoding.UTF8);
            var g = new Generator(new Generator.Options()
            {
                TemplateCode = templateCode,
                OutputFile = outputFile,
                GenericMethods = analyzer.GenericMethods,
            });

            g.PrepareMethods();
            g.Generate();
            Debug.LogFormat("[MethodBridgeGeneratorCommand] output:{0}", outputFile);
        }

        [MenuItem("HybridCLR/Generate/MethodBridge", priority = 101)]
        public static void CompileAndGenerateMethodBridge()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            GenerateMethodBridge(target);
        }

        public static void GenerateMethodBridge(BuildTarget target)
        {
            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            List<string> aotAssemblyNames = Directory.Exists(aotDllDir) ?
                Directory.GetFiles(aotDllDir, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToList()
                : new List<string>();
            if (aotAssemblyNames.Count == 0)
            {
                throw new Exception($"no aot assembly found. please run `HybridCLR/Generate/All` or `HybridCLR/Generate/AotDlls` to generate aot dlls before runing `HybridCLR/Generate/MethodBridge`");
            }
            using (AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateAOTAssemblyResolver(target), aotAssemblyNames))
            {
                var analyzer = new Analyzer(new Analyzer.Options
                {
                    MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration),
                    Collector = collector,
                });

                analyzer.Run();
                string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge.cpp";
                GenerateMethodBridgeCppFile(analyzer, outputFile);
            }

            CleanIl2CppBuildCache();
        }
    }
}
