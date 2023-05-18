using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// HCLR 编辑器工具
    /// </summary>
    static public class HCLREditorTools
    {
        
        /// <summary>
        /// 测试
        /// </summary>
        [MenuItem("xxx")]
        static public void Test()
        {
            PreBuild(BuildTarget.Android);
        }
        
        /// <summary>
        /// 在打包前执行
        /// </summary>
        /// <param name="target"></param>
        static public void PreBuild( BuildTarget target)
        {

            SetBDFramework2HCLRConfig();
            //编译补充元数据的DLL
            CompileDllCommand.CompileDll(target);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(target);
            // 生成裁剪后的aot dll
            StripAOTDllCommand.GenerateStripedAOTDlls(target,  BApplication.GetBuildTargetGroup(target));
            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
            ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
        }

        /// <summary>
        /// 设置HCLR配置
        /// </summary>
        static public void SetBDFramework2HCLRConfig()
        {
            //HCLR Setting
            //BD的hotfix dll
            {
                var list = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies);
                if (!list.Contains(ScriptLoder.HOTFIX_DEFINE))
                {
                    list.Add(ScriptLoder.HOTFIX_DEFINE);
                    HybridCLRSettings.Instance.hotUpdateAssemblies = list.ToArray();
                }
            }
            //BD框架的搜索目录
            {
                var list = new List<string>( HybridCLRSettings.Instance.externalHotUpdateAssembliyDirs);
                foreach (var platform in BApplication.SupportPlatform)
                {
                    var scriptPath = IPath.Combine( BApplication.GetPlatformDevOpsPublishAssetsPath(platform), ScriptLoder.SCRIPT_FOLDER_PATH);
                    scriptPath = IPath.ReplaceBackSlash(scriptPath);
                    if (!list.Contains(scriptPath))
                    {
                        list.Add(scriptPath);
                    }
                }
                
                HybridCLRSettings.Instance.externalHotUpdateAssembliyDirs = list.ToArray();
            }
            
            //Patch AOT
            {
                string[] aotAssemblies = new string[] { "mscorlib", "System", "System.Core" };
                var list = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies);
                foreach (var aotAssembly in aotAssemblies)
                {
                    if (!list.Contains(aotAssembly))
                    {
                        list.Add(aotAssembly);
                    }
                }
                HybridCLRSettings.Instance.patchAOTAssemblies = list.ToArray();
            } 
            HybridCLRSettings.Save();

        }
        
    }
}