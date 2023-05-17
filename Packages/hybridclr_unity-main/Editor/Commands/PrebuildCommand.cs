using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace HybridCLR.Editor.Commands
{
    public static class PrebuildCommand
    {
        /// <summary>
        /// 按照必要的顺序，执行所有生成操作，适合打包前操作
        /// </summary>
        [MenuItem("HybridCLR/Generate/All-Android", priority = 200)]
        public static void GenerateAll_Android()
        {
            GenerateAll(BuildTarget.Android);
        }
        /// <summary>
        /// 按照必要的顺序，执行所有生成操作，适合打包前操作
        /// </summary>
        [MenuItem("HybridCLR/Generate/All-iOS", priority = 201)]
        public static void GenerateAll_iOS()
        {
            GenerateAll(BuildTarget.iOS);
        }
        [MenuItem("HybridCLR/Generate/All-Win64", priority = 202)]
        public static void GenerateAll_Win64()
        {
            GenerateAll(BuildTarget.StandaloneWindows64);
        }
        
        
        /// <summary>
        /// 生成所有的dll，link.xml，桥接函数，反射函数，AOT引用
        /// </summary>
        /// <param name="bt"></param>
        public static void GenerateAll(BuildTarget bt)
        {
            CompileDllCommand.CompileDll(bt);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(bt);
            // 生成裁剪后的aot dll
            StripAOTDllCommand.GenerateStripedAOTDlls(bt, EditorUserBuildSettings.selectedBuildTargetGroup);
            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridge(bt);
            ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(bt);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(bt);
        }
    }
}
