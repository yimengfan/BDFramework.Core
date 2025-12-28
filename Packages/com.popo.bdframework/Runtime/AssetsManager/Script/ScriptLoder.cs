using System;
using System.IO;
using System.Reflection;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using UnityEngine;
using HybridCLR;


namespace BDFramework
{
    /// <summary>
    /// 脚本加载器
    /// </summary>
    static public class ScriptLoder
    {
        private static readonly string Tag = "ScriptLoder";

        /// <summary>
        /// aot patch路径
        /// </summary>
        static readonly public string HYCLR_AOT_PATCH_PATH = $"script/aot_patch_dll";

        /// <summary>
        /// 热更dll定义
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix_dll";

        /// <summary>
        /// 热更代码后缀
        /// </summary>
        static readonly public string HOT_DLL_EXTENSION = ".zlua.bytes";


        /// <summary>
        /// 脚本加载入口
        /// </summary>
        /// <param name="loadPathType"></param>
        /// <param name="runMode"></param>
        /// <param name="mainProjectTypes">UPM隔离了dll,需要手动传入</param>
        static public void Init(AssetLoadPathType loadPathType, string firstDir, string secondDir)
        {
            BDebug.EnableLog(Tag);
            // CLRBindAction = clrBindingAction;

            if (loadPathType == AssetLoadPathType.Editor)
            {
                BDebug.Log(Tag, "Editor(非热更)模式!");
            }
            else
            {
                BDebug.Log(Tag, "热更模式!");
                LoadHotfixDLL(firstDir, secondDir);
            }

            //---------------------启动------------------------
            Debug.Log("---------------------启动 逻辑------------------------");
            var type = typeof(ScriptLoder).Assembly.GetType("BDLauncherBridge");
            var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
            var startFunc = (Action) Delegate.CreateDelegate(typeof(Action), method);
            try
            {
                //开始
                startFunc();
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (var exc in e.LoaderExceptions)
                {
                    Debug.LogError(exc);
                }
            }
        }


        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="source"></param>
        /// <param name="copyto"></param>
        /// <returns></returns>
        static public void LoadHotfixDLL(string firstDir, string secondDir)
        {

            //--------------------元数据DLL加载------------------------
            //加载AOT,AOT Pacth 一定在母包Streaming内
            if (!Application.isEditor)
            {
                var aotPatchRoot = Path.Combine(BApplication.GetRuntimePlatformPath(), HYCLR_AOT_PATCH_PATH);
                BDebug.Log($"---------------【ScriptLoder】HCLR执行, Dll路径:{aotPatchRoot}---------------", Color.red);
                //
                var aotPatchDlls = BetterStreamingAssets.GetFiles(aotPatchRoot, "*" + "*" + HOT_DLL_EXTENSION);
                foreach (var path in aotPatchDlls)
                {
                    BDebug.Log("【ScriptLoder】HCLR加载AOT Patch:" + path, Color.yellow);
                    var dllbytes = BetterStreamingAssets.ReadAllBytes(path);
                    var err = RuntimeApi.LoadMetadataForAOTAssembly(dllbytes, HomologousImageMode.SuperSet);
                    Debug.Log($"LoadMetadataForAOTAssembly:{path}. ret:{err}");
                }
            }
            //---------------------热更DLL加载-多寻址------------------------
            var hotfixdllRootPath = Path.Combine(firstDir, HOTFIX_DLL_PATH);
            BDebug.Log($"【ScriptLoder】HCLR执行, Dll路径:{hotfixdllRootPath}", Color.red);
            string[] hotfixDlls = null;
            if (Directory.Exists(hotfixdllRootPath))
            {
                hotfixDlls = Directory.GetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
            }

            if (hotfixDlls != null && hotfixDlls.Length > 0)
            {
                foreach (var hotfixDll in hotfixDlls)
                {
                    var dllBytes = File.ReadAllBytes(hotfixDll);
                    BDebug.Log($"【ScriptLoder】 " + hotfixDll, Color.yellow);
                    Assembly.Load(dllBytes);
                }
            }
            else
            {
                //streaming加载


#if UNITY_ANDROID
                hotfixdllRootPath = Path.Combine(BApplication.GetRuntimePlatformPath(), HYCLR_AOT_PATCH_PATH);
                hotfixDlls = BetterStreamingAssets.GetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
#else
                hotfixdllRootPath = Path.Combine(secondDir, HYCLR_AOT_PATCH_PATH);
                hotfixDlls = Directory.GetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
#endif
                BDebug.Log($"【ScriptLoder】重新寻址 HyCLR执行  Dll路径:{hotfixdllRootPath}", Color.red);
                if (hotfixDlls == null || hotfixDlls.Length == 0)
                {
                    throw new Exception("【ScriptLoder】HyCLR热更DLL不存在! 路径:" + hotfixdllRootPath);
                }

                //
                foreach (var hotfixDll in hotfixDlls)
                {
                    byte[] dllBytes;
#if UNITY_ANDROID
                     dllBytes = BetterStreamingAssets.ReadAllBytes(hotfixDll);
#else
                    dllBytes = File.ReadAllBytes(hotfixDll);
#endif

                    BDebug.Log($"【ScriptLoder】StreamingAsset :{hotfixDll}", Color.yellow);
                    Assembly.Load(dllBytes);
                }
            }
        }


        /// <summary>
        /// 获取当前本地DLL
        /// </summary>
        static public string GetLocalDLLPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BApplication.GetPlatformLoadPath(platform), HOTFIX_DLL_PATH);
        }
    }
}
