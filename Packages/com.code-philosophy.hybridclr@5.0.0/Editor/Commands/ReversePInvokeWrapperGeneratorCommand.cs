using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Link;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.ReversePInvokeWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{

    public static class ReversePInvokeWrapperGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/ReversePInvokeWrapper", priority = 103)]

        public static void CompileAndGenerateReversePInvokeWrapper()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);
            GenerateReversePInvokeWrapper(target);
        }

        public static void GenerateReversePInvokeWrapper(BuildTarget target)
        {
            List<string> hotUpdateDlls = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            using (var cache = new AssemblyCache(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDlls)))
            {
                var analyzer = new ReversePInvokeWrap.Analyzer(cache, hotUpdateDlls);
                analyzer.Run();

                string outputFile = $"{SettingsUtil.GeneratedCppDir}/ReversePInvokeMethodStub.cpp";

                List<ABIReversePInvokeMethodInfo> methods = analyzer.BuildABIMethods();
                Debug.Log($"GenerateReversePInvokeWrapper. wraperCount:{methods.Sum(m => m.Count)} output:{outputFile}");
                var generator = new Generator();
                generator.Generate(methods, outputFile);
                Debug.LogFormat("[ReversePInvokeWrapperGeneratorCommand] output:{0}", outputFile);
            }
            MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }
    }
}
