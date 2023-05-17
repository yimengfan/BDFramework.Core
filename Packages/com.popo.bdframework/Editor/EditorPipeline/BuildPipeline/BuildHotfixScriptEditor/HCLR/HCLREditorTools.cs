using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using HybridCLR.Editor;
using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.MethodBridge;
using UnityEditor;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// HCLR 编辑器工具
    /// </summary>
    static public class HCLREditorTools
    {
        
        /// <summary>
        /// 在打包前执行
        /// </summary>
        /// <param name="target"></param>
        static public void PreBuild( BuildTarget target)
        {
            CompileDllCommand.CompileDll(target);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(target);
            // 生成裁剪后的aot dll
            StripAOTDllCommand.GenerateStripedAOTDlls(target,  BApplication.GetBuildTargetGroup(target));
            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            GenerateMethodBridge(target);
            ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
        }
        
        
        
        
        /// <summary>
        /// 桥接函数生成
        /// </summary>
        /// <param name="target"></param>
        public static void GenerateMethodBridge(BuildTarget target)
        {
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            using (AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames), hotUpdateDllNames))
            {
                var analyzer = new Analyzer(new Analyzer.Options
                {
                    MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration),
                    Collector = collector,
                });

                analyzer.Run();

                var tasks = new List<System.Threading.Tasks.Task>();
                string templateCode = File.ReadAllText($"{SettingsUtil.TemplatePathInPackage}/MethodBridgeStub.cpp");
                foreach (PlatformABI platform in Enum.GetValues(typeof(PlatformABI)))
                {
                    string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge_{platform}.cpp";
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        MethodBridgeGeneratorCommand.GenerateMethodBridgeCppFile(analyzer, platform, templateCode, outputFile);
                    }));
                }
                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            }

            MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }
    }
}