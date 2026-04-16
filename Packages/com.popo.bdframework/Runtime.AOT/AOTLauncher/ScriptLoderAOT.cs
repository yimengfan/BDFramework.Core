using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HybridCLR;
using UnityEngine;


namespace BDFramework
{
    /// <summary>
    /// AOT 启动阶段的程序集加载器。
    /// Assembly loader for the AOT startup phase.
    /// 该类负责在 Player 环境先装载 HybridCLR AOT 补充元数据，再按 persistent -&gt; streaming 的顺序加载热更程序集。
    /// This type loads HybridCLR supplemental AOT metadata first in player environments and then loads hotfix assemblies in persistent -&gt; streaming fallback order.
    /// </summary>
    /// <remarks>
    /// 典型调用点是启动场景里的 <c>BDLauncher.Start()</c>；它只负责装载程序集，不负责资源和管理器初始化。
    /// The typical caller is <c>BDLauncher.Start()</c> in the startup scene; it only handles assembly loading and does not initialize resources or managers.
    /// </remarks>
    static public class ScriptLoderAOT
    {
        /// <summary>
        /// HybridCLR AOT 补充元数据目录。
        /// 该目录由构建流程写入母包，运行时启动阶段在真正加载热更程序集前消费它。
        /// HybridCLR supplemental metadata directory.
        /// The build pipeline writes this directory into the base package, and the runtime startup flow consumes it before loading hotfix assemblies.
        /// </summary>
        static readonly public string HYCLR_AOT_PATCH_PATH = $"script/aot_patch";
        /// <summary>
        /// 热更程序集目录。
        /// 构建流程会把热更 DLL 输出到该目录；运行时优先从 persistent 读取，缺失时再回退到 StreamingAssets。
        /// Hotfix assembly directory.
        /// The build pipeline outputs hotfix DLLs into this directory, and runtime prefers the persistent copy before falling back to StreamingAssets.
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix";
        /// <summary>
        /// 热更程序集在包内使用的文件扩展名。
        /// File extension used by packaged hotfix assemblies.
        /// </summary>
        static readonly public string HOT_DLL_EXTENSION = ".zlua.bytes";
        /// <summary>
        /// 标记当前进程是否已经初始化 BetterStreamingAssets 索引。
        /// Marks whether the current process has already initialized the BetterStreamingAssets index.
        /// </summary>
        static private bool hasInitializedBetterStreamingAssets;


        /// <summary>
        /// AOT 启动阶段的程序集加载总入口。
        /// Entry point for assembly loading during the AOT startup phase.
        /// </summary>
        /// <param name="clientVersion">当前母包版本号，用于定位 persistent 下的热更 DLL 目录。</param>
        /// <param name="clientVersion">Current base-package version used to locate the hotfix DLL directory under persistent storage.</param>
        static public void Load(string clientVersion)
        {
            if (Application.isEditor)
            {
                Debug.Log( "【AOT.load】Editor模式，DLL加载已经 默认加载!");
            }
            else
            {
                Debug.Log("【AOT.load】真机热更模式，开始加载!");
                LoadHotfixDLL(clientVersion);
            }
        }


        /// <summary>
        /// 在 Player 环境加载 HybridCLR 元数据和热更程序集。
        /// Load HybridCLR metadata and hotfix assemblies in player environments.
        /// </summary>
        /// <param name="clientVersion">当前母包版本号，用于解析 persistent 目录下的热更资源根。</param>
        /// <param name="clientVersion">Current base-package version used to resolve the hotfix root under persistent storage.</param>
        static public void LoadHotfixDLL(string clientVersion)
        {
            // 阶段 1：先计算 persistent 与 StreamingAssets 的双寻址根目录。
            // Phase 1: Calculate the dual-address roots for persistent storage and StreamingAssets first.
            var platform = GetPlatformLoadPath();
            var firstLoadDir = Path.Combine(Application.persistentDataPath, clientVersion, platform);
#if UNITY_ANDROID
            var secondLoadDir = platform; //BetterStreaming 加载
#else
            var secondLoadDir = Path.Combine(Application.streamingAssetsPath, platform);
#endif


            // 阶段 2：AOT 补充元数据始终从母包 StreamingAssets 读取，不走版本目录。
            // Phase 2: Supplemental AOT metadata is always read from base-package StreamingAssets instead of the versioned directory.
            var aotPatchRoot = Path.Combine(GetPlatformLoadPath(), HYCLR_AOT_PATCH_PATH);
            Debug.Log($"---------------【AOT.Load】HYCLR执行, Dll路径:{aotPatchRoot}---------------");

            var aotPatchDlls = GetStreamingAssetFiles(aotPatchRoot, "*" + HOT_DLL_EXTENSION);
            foreach (var path in aotPatchDlls)
            {
                Debug.Log("【AOT.Load】HCLR加载AOT Patch:" + path);
                var dllbytes = BetterStreamingAssets.ReadAllBytes(path);
                var err = RuntimeApi.LoadMetadataForAOTAssembly(dllbytes, HomologousImageMode.SuperSet);
                Debug.Log($"LoadMetadataForAOTAssembly:{path}. ret:{err}");
            }


            // 阶段 3：优先加载 persistent 中已经下载完成的热更 DLL，保证版本控制生效。
            // Phase 3: Prefer hotfix DLLs already downloaded into persistent storage so version control stays effective.
            var hotfixdllRootPath = Path.Combine(firstLoadDir, HOTFIX_DLL_PATH);
            Debug.Log($"【AOT.Load】HCLR执行, Dll路径:{hotfixdllRootPath}");
            string[] hotfixDlls = null;
            if (Directory.Exists(hotfixdllRootPath))
            {
                hotfixDlls = Directory.GetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
            }

            if (hotfixDlls != null && hotfixDlls.Length > 0)
            {
                LoadHotfixAssemblies(
                    hotfixDlls,
                    File.ReadAllBytes,
                    (hotfixDll, dllBytes) =>
                    {
                        Debug.Log($"加载:{hotfixDll}");
                        Assembly.Load(dllBytes);
                    });
            }
            else
            {
                // 阶段 4：若 persistent 不存在热更 DLL，则回退到母包 StreamingAssets 里的默认程序集。
                // Phase 4: If persistent storage does not contain hotfix DLLs, fall back to the default assemblies inside base-package StreamingAssets.


#if UNITY_ANDROID
                hotfixdllRootPath = Path.Combine(GetPlatformLoadPath(), HOTFIX_DLL_PATH);
                hotfixDlls = GetStreamingAssetFiles(hotfixdllRootPath, "*" + HOT_DLL_EXTENSION);
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
                LoadHotfixAssemblies(
                    hotfixDlls,
                    hotfixDll =>
                    {
#if UNITY_ANDROID
                        return BetterStreamingAssets.ReadAllBytes(hotfixDll);
#else
                        return File.ReadAllBytes(hotfixDll);
#endif
                    },
                    (hotfixDll, dllBytes) =>
                    {
                        Debug.Log($"【AOT.Load】StreamingAsset :{hotfixDll}");
                        Assembly.Load(dllBytes);
                    });
            }
        }

        /// <summary>
        /// 在热更程序集存在依赖关系时，按“可成功装载即移除”的方式重试整个队列。
        /// Retry the whole hotfix-assembly queue by removing assemblies as soon as they load successfully.
        /// 这样可以兼容文件系统枚举顺序与程序集依赖顺序不一致的场景，例如 <c>Assembly-CSharp</c> 先被枚举到、
        /// 但它实际依赖稍后才会装载的 <c>BDFramework.Core</c> 时，会在下一轮自动重试而不是直接终止启动。
        /// This keeps startup resilient when file-system enumeration order differs from assembly dependency order, for example when <c>Assembly-CSharp</c>
        /// is discovered first but actually depends on <c>BDFramework.Core</c> being loaded later, in which case the next round retries automatically instead of aborting startup immediately.
        /// </summary>
        /// <param name="hotfixDlls">待装载的热更程序集路径列表。</param>
        /// <param name="hotfixDlls">Paths of the hotfix assemblies that still need to be loaded.</param>
        /// <param name="readDllBytes">读取程序集字节的委托。</param>
        /// <param name="readDllBytes">Delegate used to read assembly bytes.</param>
        /// <param name="loadHotfixAssembly">真正执行程序集装载的委托。</param>
        /// <param name="loadHotfixAssembly">Delegate that performs the actual assembly load.</param>
        static private void LoadHotfixAssemblies(
            string[] hotfixDlls,
            Func<string, byte[]> readDllBytes,
            Action<string, byte[]> loadHotfixAssembly)
        {
            var pendingHotfixDlls = new List<string>(hotfixDlls ?? Array.Empty<string>());
            var hotfixDllBytesMap = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var hotfixDll in pendingHotfixDlls)
            {
                if (!hotfixDllBytesMap.ContainsKey(hotfixDll))
                {
                    hotfixDllBytesMap.Add(hotfixDll, readDllBytes(hotfixDll));
                }
            }

            Exception lastDeferredException = null;
            while (pendingHotfixDlls.Count > 0)
            {
                var hasLoadedAssemblyInCurrentPass = false;
                for (var index = 0; index < pendingHotfixDlls.Count;)
                {
                    var hotfixDll = pendingHotfixDlls[index];
                    try
                    {
                        loadHotfixAssembly(hotfixDll, hotfixDllBytesMap[hotfixDll]);
                        pendingHotfixDlls.RemoveAt(index);
                        hasLoadedAssemblyInCurrentPass = true;
                        lastDeferredException = null;
                    }
                    catch (Exception exception) when (IsDeferredHotfixLoadException(exception))
                    {
                        Debug.LogWarning($"【AOT.Load】热更程序集依赖未就绪，稍后重试:{hotfixDll} err={exception.Message}");
                        lastDeferredException = exception;
                        index++;
                    }
                }

                if (!hasLoadedAssemblyInCurrentPass)
                {
                    throw new Exception(
                        $"【AOT.Load】存在无法解析依赖的热更程序集:{string.Join(",", pendingHotfixDlls)}",
                        lastDeferredException);
                }
            }
        }

        /// <summary>
        /// 判断异常是否属于“依赖尚未装载，可在后续轮次重试”的热更装载失败。
        /// Determine whether an exception represents a dependency-not-ready hotfix-load failure that can be retried in a later pass.
        /// </summary>
        /// <param name="exception">当前装载失败抛出的异常。</param>
        /// <param name="exception">Exception thrown by the current load attempt.</param>
        /// <returns>若属于可延后重试的依赖异常则返回 true。</returns>
        /// <returns>Returns true when the exception is a dependency-related failure that should be retried later.</returns>
        static private bool IsDeferredHotfixLoadException(Exception exception)
        {
            return exception is TypeLoadException
                   || exception is FileNotFoundException
                   || exception is FileLoadException
                   || exception is ReflectionTypeLoadException;
        }

        /// <summary>
        /// 读取 StreamingAssets 目录中的程序集文件，并把可选目录缺失统一收敛为空集合。
        /// Read assembly files from a StreamingAssets directory and normalize a missing optional directory into an empty set.
        /// </summary>
        /// <param name="directoryPath">要读取的 StreamingAssets 相对目录。</param>
        /// <param name="directoryPath">Relative StreamingAssets directory to inspect.</param>
        /// <param name="searchPattern">匹配程序集文件的搜索模式。</param>
        /// <param name="searchPattern">Search pattern used to match assembly files.</param>
        static private string[] GetStreamingAssetFiles(string directoryPath, string searchPattern)
        {
            return GetStreamingAssetFiles(
                directoryPath,
                searchPattern,
                EnsureBetterStreamingAssetsInitialized,
                BetterStreamingAssets.DirectoryExists,
                BetterStreamingAssets.GetFiles);
        }

        /// <summary>
        /// 读取 StreamingAssets 目录文件的可测辅助方法。
        /// Testable helper for reading files from a StreamingAssets directory.
        /// 该方法把初始化、目录探测和文件枚举拆成委托，便于在 Runtime.Test 中验证启动顺序与缺目录回退契约。
        /// This method splits initialization, directory probing, and file enumeration into delegates so Runtime.Test can verify the startup order and missing-directory fallback contract.
        /// </summary>
        /// <param name="directoryPath">要读取的 StreamingAssets 相对目录。</param>
        /// <param name="directoryPath">Relative StreamingAssets directory to inspect.</param>
        /// <param name="searchPattern">匹配程序集文件的搜索模式。</param>
        /// <param name="searchPattern">Search pattern used to match assembly files.</param>
        /// <param name="initializeStreamingAssets">用于准备 BetterStreamingAssets 索引的初始化委托。</param>
        /// <param name="initializeStreamingAssets">Initialization delegate that prepares the BetterStreamingAssets index.</param>
        /// <param name="directoryExists">用于探测目录存在性的委托。</param>
        /// <param name="directoryExists">Delegate used to probe whether the directory exists.</param>
        /// <param name="getFiles">用于枚举目录中文件的委托。</param>
        /// <param name="getFiles">Delegate used to enumerate files in the directory.</param>
        static private string[] GetStreamingAssetFiles(
            string directoryPath,
            string searchPattern,
            Action initializeStreamingAssets,
            Func<string, bool> directoryExists,
            Func<string, string, string[]> getFiles)
        {
            initializeStreamingAssets?.Invoke();

            if (!directoryExists(directoryPath))
            {
                Debug.Log($"【AOT.Load】StreamingAssets目录不存在，跳过可选目录:{directoryPath}");
                return Array.Empty<string>();
            }

            return getFiles(directoryPath, searchPattern) ?? Array.Empty<string>();
        }

        /// <summary>
        /// 确保 BetterStreamingAssets 在首次读取前完成索引初始化。
        /// Ensure that BetterStreamingAssets finishes index initialization before the first read.
        /// </summary>
        static private void EnsureBetterStreamingAssetsInitialized()
        {
            if (hasInitializedBetterStreamingAssets)
            {
                return;
            }

            Debug.Log("【AOT.Load】初始化 BetterStreamingAssets 索引");
            BetterStreamingAssets.Initialize();
            hasInitializedBetterStreamingAssets = true;
        }

        /// <summary>
        /// 解析当前运行平台在包体资源目录里的标准路径名。
        /// Resolve the standard packaged-resource directory name for the current runtime platform.
        /// </summary>
        /// <returns>例如 <c>android</c>、<c>ios</c>、<c>windows</c> 或 <c>osx</c>。</returns>
        /// <returns>For example <c>android</c>, <c>ios</c>, <c>windows</c>, or <c>osx</c>.</returns>
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