using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    using Analyzer = HybridCLR.Editor.AOT.Analyzer;
    public static class AOTReferenceGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/AOTGenericReference", priority = 102)]
        public static void CompileAndGenerateAOTGenericReference()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);
            GenerateAOTGenericReference(target);
        }

        public static void GenerateAOTGenericReference(BuildTarget target)
        {
            var gs = SettingsUtil.HybridCLRSettings;
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            using (AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames), hotUpdateDllNames))
            {
                var analyzer = new Analyzer(new Analyzer.Options
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
}
