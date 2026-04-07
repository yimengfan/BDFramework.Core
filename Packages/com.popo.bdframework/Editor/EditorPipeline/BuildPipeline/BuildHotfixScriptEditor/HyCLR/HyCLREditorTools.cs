
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.Tools;
using BDFramework.Sql;
using Cysharp.Threading.Tasks;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using LitJson;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// HCLR 编辑器工具
    /// </summary>
    static public class HyCLREditorTools
    {

        [MenuItem("BDFrameWork工具箱/Test/HyCLREditorTools.BuildHotfixDLL")]
        static public void BuildHotfixDLL_Test()
        {
            BuildHotfixDLL(BApplication.DevOpsPublishAssetsPath, BuildTarget.StandaloneWindows64);
        }

        [MenuItem("BDFrameWork工具箱/Test/HyCLREditorTools.PreBuild")]
        static public void PreBuild_Test()
        {
            PreBuild(BuildTarget.StandaloneWindows64);
        }

        
        /// <summary>
        /// 在打包前执行
        /// </summary>
        /// <param name="target"></param>
        static public void PreBuild(BuildTarget target)
        {
            if (HybridCLRSettings.Instance == null)
            {
                throw new Exception("请先生成HCLR Setting!");
            }


            if (target != EditorUserBuildSettings.activeBuildTarget)
            {
                Debug.Log($"[HCLR] 当前平台 {EditorUserBuildSettings.activeBuildTarget} 与目标平台 {target} 不一致，开始切换目标平台。");
                var switched = BDEditorApplication.SwitchToBuildTarget(target);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (!switched || target != EditorUserBuildSettings.activeBuildTarget)
                {
                    throw new Exception("请将平台切换至目标平台:" + target.ToString());
                }
            }


            var tag = $"[HCLR] PreBuild for {target}";
            BDebug.LogWatchBegin(tag);
            {
                BDebug.Log("[HCLR]start:", Color.green);
                SetBDFramework2HCLRConfig();
                //安装华佗
                var installer = new HybridCLR.Editor.Installer.InstallerController();
                if (!installer.HasInstalledHybridCLR())
                {
                    BDebug.Log("[HCLR]开始安装华佗...", Color.magenta);
                    installer.InstallDefaultHybridCLR();
                }
                else
                {
                    BDebug.Log("[HCLR]华佗已经安装,跳过", Color.green);
                }

                //编译补充元数据的DLL
                PrebuildCommand.GenerateAll();
                //拷贝用于补充泛型
                var sourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
                CopyAOTMetadataDLL(sourceDir,Application.streamingAssetsPath, target);
            }
            BDebug.LogWatchEnd(tag);
        }

  
        /// <summary>
        /// 编译热更dll
        /// </summary>

        static public void BuildHotfixDLL(string outputDir, BuildTarget target)
        {
            bool isBuildSuccess = false;
            void WatchLog(string condition, string stackTrace, LogType type) {
                if (type == LogType.Error && condition.Contains("Fail")) {
                    isBuildSuccess = false;
                    Debug.LogError(stackTrace);
                }
            }
            isBuildSuccess = true;
            //核心构建逻辑
            string tmpOutputPath = IPath.ReplaceBackSlash(Path.GetFullPath(IPath.Combine("HybridCLRData", "out_hotfixdlls_temp")));
            if (Directory.Exists(tmpOutputPath))
            {
                Directory.Delete(tmpOutputPath,true);
            }

            Directory.CreateDirectory(tmpOutputPath);
            
            //
            Application.logMessageReceived += WatchLog;
            CompileDllCommand.CompileDll(tmpOutputPath,target,false);
            Application.logMessageReceived -= WatchLog;
            if (!isBuildSuccess) {
                throw new Exception("build hotfix_dll failed");
            }

            CopyHotfixDLLs(tmpOutputPath, outputDir,target);
   
            
        }
        
        /// <summary>
        /// 拷贝热更dll
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destDir"></param>
       static public void CopyHotfixDLLs(string sourceDir, string destDir,BuildTarget target)
       {
           sourceDir = IPath.ReplaceBackSlash(sourceDir);
           var destDLLRootDir = IPath.Combine(BApplication.GetPlatformLoadPath(destDir,target), HotfixScriptLoder.HOTFIX_DLL_PATH);
            if (Directory.Exists(destDLLRootDir))
            {
                Directory.Delete(destDLLRootDir,true);
            }
            Directory.CreateDirectory(destDLLRootDir);
         
            //
            var hotfixDlls = HybridCLRSettings.Instance.hotUpdateAssemblies;
            foreach (var hd in hotfixDlls)
            {
                var source = IPath.Combine(sourceDir, hd + ".dll");
                var destPath = IPath.Combine(destDLLRootDir, hd + HotfixScriptLoder.HOT_DLL_EXTENSION);

                if (!File.Exists(source))
                {
                   Debug.LogError("不存在热更代码:" + source);
                     continue;
                }
                
                BDebug.Log($"{source} => {destPath}", Color.yellow);
                FileHelper.Copy(source, destPath, true);
                if(File.Exists(destPath))
                {
                    BDebug.Log("热更打包成功 =>" + destPath, Color.green);
                }
                else
                {
                    Debug.LogError("热更打包失败 =>" + destPath);
                }
            }
        }


        /// <summary>
        /// 拷贝补充元数据的dll到hotfix目录
        /// </summary>
        /// <param name="destDir"></param>
        /// <param name="target"></param>
        static public void CopyAOTMetadataDLL(string sourceDir,string destDir, BuildTarget target)
        {
            //从aot目录拷贝补充元数据的dll到hotfix目录
            var aotDLLs = HybridCLRSettings.Instance.patchAOTAssemblies;
            var destPlatformDir = BApplication.GetPlatformLoadPath(destDir, target);
            foreach (var aotDll in aotDLLs)
            {
                var sourceDllPath = IPath.Combine(sourceDir, aotDll + ".dll");
                var destPath = IPath.Combine(destPlatformDir, HotfixScriptLoder.HYCLR_AOT_PATCH_PATH, aotDll + HotfixScriptLoder.HOT_DLL_EXTENSION);
                if (File.Exists(sourceDllPath))
                {
                    FileHelper.Copy(sourceDllPath, destPath, true);
                    Debug.Log($"<color=green>[HyCLR]拷贝 AOT Patch dll:{aotDll} </color>");
                }
                else
                {
                    throw new Exception("不存在aot dll：" + sourceDllPath);
                }
            }
        }
        
        /// <summary>
        /// 设置HCLR配置
        /// </summary>
        static public void SetBDFramework2HCLRConfig()
        {
            //HCLR Setting
            //BD的hotfix dll
            {
                var list = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies ?? Array.Empty<string>());

                string[] hotfixAssemblies = new string[]
                {
                    "Assembly-CSharp",
                    "Assembly-CSharp-firstpass",
                    "BDFramework.Core"
                };

                foreach (var hotfix in hotfixAssemblies)
                {
                    if (!list.Contains(hotfix))
                    {
                        list.Add(hotfix);
                    }
                }

                HybridCLRSettings.Instance.hotUpdateAssemblies = list.ToArray();
            }


            //Patch AOT
            {
                string[] aotAssemblies = new string[] { "mscorlib", "System", "System.Core" };
                var list = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies ?? Array.Empty<string>());
                foreach (var aotAssembly in aotAssemblies)
                {
                    if (!list.Contains(aotAssembly))
                    {
                        list.Add(aotAssembly);
                    }
                }

                HybridCLRSettings.Instance.patchAOTAssemblies = list.ToArray();
            }
            
            //保存
            HybridCLRSettings.Save();
        }





        

        
        /// <summary>
        /// 获取hotfix dll路径
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        static public string[] GetHotfixDLLPaths()
        {
            List<string> retlist = new List<string>();
            foreach (var hotfix in   HybridCLRSettings.Instance.hotUpdateAssemblies)
            {
                var path = $"{HotfixScriptLoder.HOTFIX_DLL_PATH}/{hotfix}.dll.bytes";
                retlist.Add(path);
            }
            return retlist.ToArray();
        }
    }
}