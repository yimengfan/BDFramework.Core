using System;
using System.IO;
using System.Reflection;
using HybridCLR;
using UnityEngine;


namespace BDFramework
{
    /// <summary>
    /// 脚本加载器
    /// </summary>
    static public class ScriptLoderAOT
    {
        /// <summary>
        /// aot patch路径
        /// </summary>
        static readonly public string HYCLR_AOT_PATCH_PATH = $"script/aot_patch";
        /// <summary>
        /// 热更dll定义
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix";
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
        static public void Load(string clientVersion)
        {
            if (Application.isEditor)
            {
                Debug.Log( "【AOT.load】Editor模式，DLL 加载已经加载!");
            }
            else
            {
                Debug.Log("【AOT.load】热更模式!");
                LoadHotfixDLL(clientVersion);
            }
        }


        /// <summary>
        /// 加载热更代码
        /// first =>  persistent/{platform}/{version_num}/xxxx
        /// second =>  streaming/{platform}/xxx
        /// </summary>
        /// <param name="source"></param>
        /// <param name="copyto"></param>
        /// <returns></returns>
        static public void LoadHotfixDLL(string clientVersion)
        {
            /// first =>  persistent/{platform}/{version_num}/xxxx
            /// second =>  streaming/{platform}/xxx
            var platform = GetPlatformLoadPath();
            var firstLoadDir = Path.Combine(Application.persistentDataPath, clientVersion, platform);
#if UNITY_ANDROID
            var secondLoadDir = platform; //BetterStreaming 加载
#else
            var secondLoadDir = Path.Combine(Application.streamingAssetsPath, platform);
#endif


            //--------------------元数据DLL加载------------------------
            //加载AOT Patcth 一定在母包Streaming内 ,没有 version 目录
            var aotPatchRoot = Path.Combine(GetPlatformLoadPath(), HYCLR_AOT_PATCH_PATH);
            Debug.Log($"---------------【AOT.Load】HYCLR执行, Dll路径:{aotPatchRoot}---------------");
            //
            var aotPatchDlls = BetterStreamingAssets.GetFiles(aotPatchRoot, "*" + HOT_DLL_EXTENSION);
            foreach (var path in aotPatchDlls)
            {
                Debug.Log("【AOT.Load】HCLR加载AOT Patch:" + path);
                var dllbytes = BetterStreamingAssets.ReadAllBytes(path);
                var err = RuntimeApi.LoadMetadataForAOTAssembly(dllbytes, HomologousImageMode.SuperSet);
                Debug.Log($"LoadMetadataForAOTAssembly:{path}. ret:{err}");
            }


            //---------------------热更DLL加载-多寻址------------------------
            var hotfixdllRootPath = Path.Combine(firstLoadDir, HOTFIX_DLL_PATH);
            Debug.Log($"【AOT.Load】HCLR执行, Dll路径:{hotfixdllRootPath}");
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
                    Debug.Log($"加载:{hotfixDll}");
                    Assembly.Load(dllBytes);
                }
            }
            else
            {
                //streaming加载


#if UNITY_ANDROID
                hotfixdllRootPath = Path.Combine(GetPlatformLoadPath(), HOTFIX_DLL_PATH);
                hotfixDlls = BetterStreamingAssets.GetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
#else
                hotfixdllRootPath = Path.Combine(secondLoadDir, HOTFIX_DLL_PATH);
                hotfixDlls = Directory.GetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
#endif
                Debug.Log($"【AOT.Load】重新寻址 HyCLR执行  Dll路径:{hotfixdllRootPath}");
                if (hotfixDlls == null || hotfixDlls.Length == 0)
                {
                    throw new Exception("【AOT.Load】HyCLR热更DLL不存在! 路径:" + hotfixdllRootPath);
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

                    Debug.Log($"【AOT.Load】StreamingAsset :{hotfixDll}");
                    Assembly.Load(dllBytes);
                }
            }
        }

        /// <summary>
        /// 从 BApplication 复制的
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static string GetPlatformLoadPath()
        {
            RuntimePlatform platform = RuntimePlatform.WindowsEditor;
#if UNITY_IOS
            platform = RuntimePlatform.IPhonePlayer;
#elif UNITY_ANDROID
                  platform = RuntimePlatform.Android;
#elif UNITY_STANDALONE_WIN
                  platform = RuntimePlatform.WindowsPlayer;
#elif UNITY_STANDALONE_OSX
                  platform = RuntimePlatform.OSXPlayer;
#endif
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "windows";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "osx";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                default:
                    return "editor";
            }

            return platform.ToString().Replace("Editor", "").ToLower();
        }
    }
}