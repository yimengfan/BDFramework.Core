#if ENABLE_HYCLR
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

        //
        /// <summary>
        /// 在打包前执行
        /// </summary>
        /// <param name="target"></param>
        static public void PreBuild(BuildTarget target, string assetsOutputDir)
        {
            if (HybridCLRSettings.Instance == null)
            {
                throw new Exception("请先生成HCLR Setting!");
            }


            if (target != EditorUserBuildSettings.activeBuildTarget)
            {
                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.Android:
                    {
                        BDEditorApplication.SwitchToAndroid();
                    }
                        break;
                    case BuildTarget.iOS:
                    {
                        BDEditorApplication.SwitchToiOS();
                    }
                        break;
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                    {
                        BDEditorApplication.SwitchToWindows();
                    }
                        break;
                    case BuildTarget.StandaloneOSX:
                    {
                        BDEditorApplication.SwitchToMacOSX();
                    }
                        break;
                }


                throw new Exception("请将平台切换至目标平台:" + target.ToString());
            }


            Stopwatch sw = new Stopwatch();
            sw.Start();
            {
                Debug.Log("<color=green>[HCLR]start:</color>");
                // SetBDFramework2HCLRConfig();
                //安装华佗
                var installer = new HybridCLR.Editor.Installer.InstallerController();
                if (!installer.HasInstalledHybridCLR())
                {
                    installer.InstallDefaultHybridCLR();
                }

                //编译补充元数据的DLL
                PrebuildCommand.GenerateAll();
                //拷贝用于补充泛型
                CopyAOTMetadataDLL(target, assetsOutputDir);
            }
            sw.Stop();
            Debug.Log($"<color=red>[HCLR]end! 耗时:{sw.ElapsedMilliseconds} ms.</color>");
// #if UNITY_EDITOR_OSX
//             BuildLibIl2cppForIOS();
// #endif
        }

        [MenuItem("HybridCLR/BuildHotfixDLL_test")]
        static public void BuildHotfixDLL_Test()
        {
            BuildHotfixDLL("", BuildTarget.StandaloneWindows64);
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
            string tmpOutputPath = Path.GetFullPath("HybridCLRData\\HotUpdateAssets_temp");
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
            //
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir,true);
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
                var list = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies);

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

            //保存
            HybridCLRSettings.Save();
        }



        /// <summary>
        /// 拷贝补充元数据的dll到hotfix目录
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assetsOutputDir"></param>
        static public void CopyCompileDll(BuildTarget target, string assetsOutputDir)
        {
            //从aot目录拷贝补充元数据的dll到hotfix目录
            var patchAOTDll = HybridCLRSettings.Instance.patchAOTAssemblies;
            assetsOutputDir = IPath.Combine(assetsOutputDir, BApplication.GetPlatformPath(target));
            foreach (var dll in patchAOTDll)
            {
                var sourceDllPath = IPath.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), dll + ".dll");
                var destPath = IPath.Combine(assetsOutputDir, ScriptLoder.HCLR_AOT_PATCH_PATH, dll + ".dll");
                if (File.Exists(sourceDllPath))
                {
                    FileHelper.Copy(sourceDllPath, destPath, true);
                    Debug.Log($"<color=green>[HCLR]拷贝AOT Patch dll:{dll} </color>");
                }
            }
        }

        

        /// <summary>
        /// 拷贝补充元数据的dll到hotfix目录
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assetsOutputDir"></param>
        static public void CopyAOTMetadataDLL(BuildTarget target, string assetsOutputDir)
        {
            //从aot目录拷贝补充元数据的dll到hotfix目录
            var patchAOTDll = HybridCLRSettings.Instance.patchAOTAssemblies;
            assetsOutputDir = IPath.Combine(assetsOutputDir, BApplication.GetPlatformPath(target));
            foreach (var dll in patchAOTDll)
            {
                var sourceDllPath = IPath.Combine(SettingsUtil.GetAssembliesPostIl2CppStripDir(target), dll + ".dll");
                var destPath = IPath.Combine(assetsOutputDir, ScriptLoder.HCLR_AOT_PATCH_PATH, dll + ".dll");
                if (File.Exists(sourceDllPath))
                {
                    FileHelper.Copy(sourceDllPath, destPath, true);
                    Debug.Log($"<color=green>[HyCLR]拷贝 AOT Patch dll:{dll} </color>");
                }
            }
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
                var path = $"{ScriptLoder.HOTFIX_DLL_PATH}/{hotfix}.dll.bytes";
                retlist.Add(path);
            }
            return retlist.ToArray();
        }
    }
}
#endif