using System;
using System.IO;
using System.Reflection;
using HybridCLR;
using UnityEngine;


namespace BDFramework
{
    /// <summary>
    /// AOT 启动阶段的程序集加载器。
    /// 该类负责在 Player 环境先装载 HybridCLR AOT 补充元数据，再按 persistent -&gt; streaming 的顺序加载热更程序集。
    /// </summary>
    /// <remarks>
    /// 典型调用点是启动场景里的 <c>BDLauncher.Start()</c>；它只负责装载程序集，不负责资源和管理器初始化。
    /// </remarks>
    static public class ScriptLoderAOT
    {
        /// <summary>
        /// HybridCLR AOT 补充元数据目录。
        /// 该目录由构建流程写入母包，运行时启动阶段在真正加载热更程序集前消费它。
        /// </summary>
        static readonly public string HYCLR_AOT_PATCH_PATH = $"script/aot_patch";
        /// <summary>
        /// 热更程序集目录。
        /// 构建流程会把热更 DLL 输出到该目录；运行时优先从 persistent 读取，缺失时再回退到 StreamingAssets。
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix";
        /// <summary>
        /// 热更程序集在包内使用的文件扩展名。
        /// </summary>
        static readonly public string HOT_DLL_EXTENSION = ".zlua.bytes";


        /// <summary>
        /// AOT 启动阶段的程序集加载总入口。
        /// </summary>
        /// <param name="clientVersion">当前母包版本号，用于定位 persistent 下的热更 DLL 目录。</param>
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
        /// 在 Player 环境加载 HybridCLR 元数据和热更程序集。
        /// </summary>
        /// <param name="clientVersion">当前母包版本号，用于解析 persistent 目录下的热更资源根。</param>
        static public void LoadHotfixDLL(string clientVersion)
        {
            // Phase 1: 先计算 persistent 与 StreamingAssets 的双寻址根目录。
            var platform = GetPlatformLoadPath();
            var firstLoadDir = Path.Combine(Application.persistentDataPath, clientVersion, platform);
#if UNITY_ANDROID
            var secondLoadDir = platform; //BetterStreaming 加载
#else
            var secondLoadDir = Path.Combine(Application.streamingAssetsPath, platform);
#endif


            // Phase 2: AOT 补充元数据始终从母包 StreamingAssets 读取，不走版本目录。
            var aotPatchRoot = Path.Combine(GetPlatformLoadPath(), HYCLR_AOT_PATCH_PATH);
            Debug.Log($"---------------【AOT.Load】HYCLR执行, Dll路径:{aotPatchRoot}---------------");

            var aotPatchDlls = BetterStreamingAssets.GetFiles(aotPatchRoot, "*" + HOT_DLL_EXTENSION);
            foreach (var path in aotPatchDlls)
            {
                Debug.Log("【AOT.Load】HCLR加载AOT Patch:" + path);
                var dllbytes = BetterStreamingAssets.ReadAllBytes(path);
                var err = RuntimeApi.LoadMetadataForAOTAssembly(dllbytes, HomologousImageMode.SuperSet);
                Debug.Log($"LoadMetadataForAOTAssembly:{path}. ret:{err}");
            }


            // Phase 3: 优先加载 persistent 中已经下载完成的热更 DLL，保证版本控制生效。
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
                // Phase 4: 若 persistent 不存在热更 DLL，则回退到母包 StreamingAssets 里的默认程序集。


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
        /// 解析当前运行平台在包体资源目录里的标准路径名。
        /// </summary>
        /// <returns>例如 <c>android</c>、<c>ios</c>、<c>windows</c> 或 <c>osx</c>。</returns>
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