using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using Cysharp.Threading.Tasks;
using LitJson;
using ServiceStack.Text;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 文件服务器资源版控扩展（DevOps 模式）。
    /// 流程固定为：读取共享三段版控文件、按 Code/AssetBundle/Table 分别求差异、下载差异资源、
    /// 合并本地元数据并清理旧资源，最后沿用老入口的状态回调返回结果。
    /// </summary>
    /// <remarks>
    /// 这套逻辑通过显式文件服务器模式入口调用，不会被旧的 <c>UpdateAssets</c> / <c>GetServerSubPackageInfos</c> 隐式访问。
    /// 因此旧协议与文件服务器协议可以隔离维护，但都沿用相同的下载回调与结果回调约定。
    /// </remarks>
    /// <example>
    /// 业务层如果希望走文件服务器三段版控，应显式调用：
    /// <code>
    /// BResources.StartAssetsVersionControlWithDevOps(
    ///     UpdateMode.CompareWithRepairCoreAssets,
    ///     serverUrl,
    ///     onTaskEndCallback: OnUpdateFinished);
    /// </code>
    /// </example>
    public partial class AssetsVersionController
    {
        /// <summary>
        /// 文件服务器三段版控里的组件类型。
        /// </summary>
        internal enum FileServerComponentKind
        {
            Code,
            AssetBundle,
            Table,
        }

        /// <summary>
        /// 文件服务器共享版控文件的解析结果，对应 <c>code.assetbundle.table</c>。
        /// </summary>
        internal sealed class FileServerVersionInfo
        {
            public string CodeVersion { get; set; } = string.Empty;

            public string AssetBundleVersion { get; set; } = string.Empty;

            public string TableVersion { get; set; } = string.Empty;

            public string RawValue => FormatFileServerVersionInfo(this);

            public bool HasAnyVersion =>
                !string.IsNullOrEmpty(CodeVersion) || !string.IsNullOrEmpty(AssetBundleVersion) ||
                !string.IsNullOrEmpty(TableVersion);

            public string GetVersion(FileServerComponentKind componentKind)
            {
                switch (componentKind)
                {
                    case FileServerComponentKind.Code:
                        return CodeVersion;
                    case FileServerComponentKind.AssetBundle:
                        return AssetBundleVersion;
                    case FileServerComponentKind.Table:
                        return TableVersion;
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// 本地持久化的文件服务器版控状态。
        /// 用于断网时回退到上次成功记录，并跟踪各组件及子包的已安装版本。
        /// </summary>
        internal sealed class FileServerVersionControllerState
        {
            public string InstalledVersion { get; set; } = string.Empty;

            public string LastKnownRemoteVersion { get; set; } = string.Empty;

            public Dictionary<string, string> InstalledComponentVersions { get; set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, List<string>> ManagedFiles { get; set; } =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, string> InstalledSubPackageVersions { get; set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, List<string>> SubPackageManagedFiles { get; set; } =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 单个组件的远端上下文，包含下载判定所需的 assets.info、package_build.info 和分包配置。
        /// </summary>
        internal sealed class FileServerComponentContext
        {
            public FileServerComponentKind ComponentKind { get; set; }

            public string Version { get; set; } = string.Empty;

            public List<AssetItem> AssetItems { get; set; } = new List<AssetItem>();

            public ClientPackageBuildInfo PackageBuildInfo { get; set; } = new ClientPackageBuildInfo();

            public string AssetsInfoContent { get; set; } = string.Empty;

            public string PackageBuildContent { get; set; } = string.Empty;

            public string SubPackageContent { get; set; } = string.Empty;

            public List<SubPackageConfigItem> SubPackageConfigs { get; set; } = new List<SubPackageConfigItem>();
        }

        /// <summary>
        /// 单个待下载资源的落地信息。
        /// </summary>
        internal sealed class FileServerDownloadItem
        {
            public FileServerComponentKind ComponentKind { get; set; }

            public AssetItem AssetItem { get; set; }

            public string RemoteUrl { get; set; } = string.Empty;

            public string FinalLocalPath { get; set; } = string.Empty;

            public bool RequireHashValidation { get; set; }
        }

        /// <summary>
        /// 一次文件服务器版控解析后的运行期上下文。
        /// </summary>
        private sealed class FileServerResolveResult
        {
            public bool IsFileServerProtocol { get; set; }

            public string Error { get; set; }

            public RuntimePlatform Platform { get; set; }

            public string PlatformPath { get; set; } = string.Empty;

            public string FirstLoadDir { get; set; } = string.Empty;

            public string CacheDir { get; set; } = string.Empty;

            public FileServerVersionInfo VersionInfo { get; set; } = new FileServerVersionInfo();

            public FileServerVersionControllerState State { get; set; } = new FileServerVersionControllerState();

            public bool UsedLocalFallbackVersion { get; set; }
        }

        /// <summary>
        /// 远端共享版控入口文件。
        /// 文件内容固定为 code.assetbundle.table，运行时先读取它，再决定三个组件分别去哪一个构建号目录下载。
        /// </summary>
        private const string FileServerVersionManifestFileName = "version.info";

        /// <summary>
        /// 远端共享版控入口目录模板。
        /// 例如 iOS 平台会映射到 clientRes_ios/version.info。
        /// </summary>
        private const string FileServerVersionManifestRootFormat = "clientRes_{0}";

        /// <summary>
        /// 本地文件服务器缓存目录。
        /// 用来保存上次成功读取的组件元数据，支持断网回退和减少重复拉取 assets.info。
        /// </summary>
        private const string FileServerCacheFolderName = "version_cache";

        /// <summary>
        /// 本地文件服务器状态文件。
        /// 记录已安装三段版本号、各组件版本、子包版本和受本协议管理的文件列表。
        /// </summary>
        private const string FileServerStateFileName = "client_res_state.json";

        /// <summary>
        /// 组件级 package_build.info 缓存模板。
        /// Code / AssetBundle / Table 会各自缓存一份，最终再合并回运行时消费的 package_build.info。
        /// </summary>
        private const string FileServerPackageBuildInfoFileFormat = "{0}_package_build.info";

        /// <summary>
        /// 组件级 assets.info 缓存模板。
        /// 仅 Code / AssetBundle 使用，Table 组件没有独立的 assets.info 文件。
        /// </summary>
        private const string FileServerAssetsInfoFileFormat = "{0}_assets.info";

        /// <summary>
        /// 分包配置缓存文件。
        /// 对应远端的 assets_subpack.info，本地会缓存它用于子包查询和断网回退。
        /// </summary>
        private const string FileServerSubPackageCacheFileName = "assets_subpack.info";

        private static readonly FileServerComponentKind[] FileServerManagedComponents =
        {
            FileServerComponentKind.Code,
            FileServerComponentKind.AssetBundle,
            FileServerComponentKind.Table,
        };

        private static void LogFileServerFlow(string message, Color color)
        {
            BDebug.Log(LogTag, $"[FileServer] {message}", color);
        }

        /// <summary>
        /// 显式文件服务器下载入口（DevOps 模式）。
        /// 用户选择这套 API 后，不再访问旧版 server_assets_version.info 流程。
        /// </summary>
        public void UpdateAssetsWithDevOps(
            UpdateMode updateMode,
            string serverConfigUrl,
            string assetsPackageName = "",
            Action<AssetItem, List<AssetItem>> onDownloadProccess = null,
            Action<RetStatus, string> onTaskEndCallback = null)
        {
            BDebug.EnableLog(LogTag);
            LogFileServerFlow($"进入文件服务器下载入口 mode={updateMode} package={(string.IsNullOrEmpty(assetsPackageName) ? "<all>" : assetsPackageName)} url={serverConfigUrl}",
                Color.cyan);
            UniTask.RunOnThreadPool(() =>
            {
                StartFileServerVersionControl(updateMode, serverConfigUrl, assetsPackageName, onDownloadProccess,
                    onTaskEndCallback);
            });
        }

        /// <summary>
        /// 显式文件服务器子包信息入口（DevOps 模式）。
        /// </summary>
        public void GetServerSubPackageInfosWithDevOps(
            string serverUrl,
            Action<Dictionary<string, string>> callback,
            Action<string> onError = null)
        {
            LogFileServerFlow($"进入文件服务器子包查询入口 url={serverUrl}", Color.cyan);
            UniTask.RunOnThreadPool(() =>
            {
                GetFileServerSubPackageInfosOnly(serverUrl, callback, onError);
            });
        }

        /// <summary>
        /// 文件服务器模式专用下载入口，不做旧协议回退。
        /// </summary>
        private async Task StartFileServerVersionControl(
            UpdateMode updateMode,
            string serverUrl,
            string subPackageName,
            Action<AssetItem, List<AssetItem>> onDownloadProccess,
            Action<RetStatus, string> onTaskEndCallback)
        {
            LogFileServerFlow($"开始解析文件服务器版控 mode={updateMode} package={(string.IsNullOrEmpty(subPackageName) ? "<all>" : subPackageName)}",
                Color.cyan);
            if (await TryStartFileServerVersionControl(updateMode, serverUrl, subPackageName, onDownloadProccess,
                    onTaskEndCallback))
            {
                return;
            }

            LogFileServerFlow("当前服务器不存在文件服务器版控入口，终止新协议流程。", Color.yellow);
            await UniTask.SwitchToMainThread();
            onTaskEndCallback?.Invoke(RetStatus.Error,
                "当前服务器未提供文件服务器版控入口: clientRes_{platform}/version.info");
        }

        /// <summary>
        /// 文件服务器模式专用子包查询入口，不做旧协议回退。
        /// </summary>
        private async Task GetFileServerSubPackageInfosOnly(
            string serverUrl,
            Action<Dictionary<string, string>> callback,
            Action<string> onError = null)
        {
            LogFileServerFlow("开始读取文件服务器子包版控信息。", Color.cyan);
            if (await TryGetFileServerSubPackageInfos(serverUrl, callback, onError))
            {
                return;
            }

            LogFileServerFlow("当前服务器不存在文件服务器子包版控入口，终止新协议查询。", Color.yellow);
            await UniTask.SwitchToMainThread();
            onError?.Invoke("当前服务器未提供文件服务器版控入口: clientRes_{platform}/version.info");
        }

        /// <summary>
        /// 解析共享三段版控文件 <c>code.assetbundle.table</c>。
        /// </summary>
        internal static bool TryParseFileServerVersionInfo(string content, out FileServerVersionInfo versionInfo)
        {
            versionInfo = null;
            var normalized = (content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            var segments = normalized.Split('.');
            if (segments.Length != 3)
            {
                return false;
            }

            segments = segments.Select(item => item.Trim()).ToArray();
            if (segments.Any(string.IsNullOrEmpty) || segments.Any(item => item.Contains(".")))
            {
                return false;
            }

            versionInfo = new FileServerVersionInfo()
            {
                CodeVersion = segments[0],
                AssetBundleVersion = segments[1],
                TableVersion = segments[2],
            };
            return true;
        }

        /// <summary>
        /// 把三段版控对象序列化回共享文本格式。
        /// </summary>
        internal static string FormatFileServerVersionInfo(FileServerVersionInfo versionInfo)
        {
            if (versionInfo == null)
            {
                return string.Empty;
            }

            return $"{versionInfo.CodeVersion}.{versionInfo.AssetBundleVersion}.{versionInfo.TableVersion}";
        }

        /// <summary>
        /// 子包名兼容层：同时接受裸包名与旧协议里的 server_assets_subpack_xxx.info。
        /// </summary>
        internal static string NormalizeFileServerSubPackageName(string subPackageName)
        {
            var normalized = (subPackageName ?? string.Empty).Trim();
            const string prefix = "server_assets_subpack_";
            const string suffix = ".info";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length);
            }

            if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - suffix.Length);
            }

            return normalized;
        }

        /// <summary>
        /// 统一生成旧入口仍然使用的子包配置文件名。
        /// </summary>
        internal static string GetFileServerSubPackageFileName(string packageName)
        {
            return $"server_assets_subpack_{packageName}.info";
        }

        /// <summary>
        /// 合并三个组件各自携带的 package_build.info，避免新协议落地时互相覆盖。
        /// </summary>
        internal static ClientPackageBuildInfo MergeFileServerPackageBuildInfo(
            ClientPackageBuildInfo baseInfo,
            ClientPackageBuildInfo codeInfo,
            ClientPackageBuildInfo assetBundleInfo,
            ClientPackageBuildInfo tableInfo)
        {
            var mergedInfo = new ClientPackageBuildInfo();
            if (baseInfo != null)
            {
                mergedInfo.BuildTime = baseInfo.BuildTime;
                mergedInfo.Version = baseInfo.Version;
                mergedInfo.BasePckScriptSVCVersion = baseInfo.BasePckScriptSVCVersion;
                mergedInfo.HotfixScriptSVCVersion = baseInfo.HotfixScriptSVCVersion;
                mergedInfo.AssetBundleSVCVersion = baseInfo.AssetBundleSVCVersion;
                mergedInfo.TableSVCVersion = baseInfo.TableSVCVersion;
            }

            ApplyFileServerPackageBuildInfo(mergedInfo, codeInfo);
            ApplyFileServerPackageBuildInfo(mergedInfo, assetBundleInfo);
            ApplyFileServerPackageBuildInfo(mergedInfo, tableInfo);

            return mergedInfo;
        }

        /// <summary>
        /// 根据分包配置，从 Code / AssetBundle / Table 三类资源中筛出本次子包真正需要下载的资源。
        /// </summary>
        internal static List<AssetItem> BuildFileServerSubPackageAssetItems(
            SubPackageConfigItem subPackageConfig,
            List<AssetItem> assetBundleAssets,
            List<AssetItem> codeAssets,
            List<AssetItem> tableAssets)
        {
            var selectedAssets = new List<AssetItem>();
            if (subPackageConfig == null)
            {
                return selectedAssets;
            }

            if (assetBundleAssets != null)
            {
                foreach (var artAssetId in subPackageConfig.ArtAssetsIdList)
                {
                    var found = assetBundleAssets.FirstOrDefault(item => item.Id == artAssetId);
                    if (found != null)
                    {
                        selectedAssets.Add(found);
                    }
                }
            }

            if (codeAssets != null)
            {
                foreach (var hotfixPath in subPackageConfig.HotfixCodePathList)
                {
                    var found = codeAssets.FirstOrDefault(item => item.LocalPath == hotfixPath);
                    if (found != null)
                    {
                        selectedAssets.Add(found);
                    }
                }
            }

            if (tableAssets != null)
            {
                foreach (var tablePath in subPackageConfig.TablePathList)
                {
                    var found = tableAssets.FirstOrDefault(item => item.LocalPath == tablePath);
                    if (found != null)
                    {
                        selectedAssets.Add(found);
                    }
                }
            }

            foreach (var confName in subPackageConfig.ConfAndInfoList)
            {
                var found = assetBundleAssets?.FirstOrDefault(item => item.LocalPath == confName)
                            ?? codeAssets?.FirstOrDefault(item => item.LocalPath == confName)
                            ?? tableAssets?.FirstOrDefault(item => item.LocalPath == confName);
                if (found != null)
                {
                    selectedAssets.Add(found);
                }
            }

            return selectedAssets.Distinct().OrderBy(item => item.Id).ToList();
        }

        private async Task<bool> TryStartFileServerVersionControl(
            UpdateMode updateMode,
            string serverUrl,
            string subPackageName,
            Action<AssetItem, List<AssetItem>> onDownloadProccess,
            Action<RetStatus, string> onTaskEndCallback)
        {
            var resolveResult = await ResolveFileServerVersionInfo(serverUrl);
            if (!resolveResult.IsFileServerProtocol)
            {
                LogFileServerFlow("未检测到文件服务器共享版控文件。", Color.yellow);
                return false;
            }

            if (!string.IsNullOrEmpty(resolveResult.Error))
            {
                LogFileServerFlow($"文件服务器版控解析失败 err={resolveResult.Error}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, resolveResult.Error);
                return true;
            }

            LogFileServerFlow($"文件服务器版控解析成功 version={resolveResult.VersionInfo.RawValue} fallback={resolveResult.UsedLocalFallbackVersion}",
                resolveResult.UsedLocalFallbackVersion ? Color.yellow : Color.green);

            if (string.IsNullOrEmpty(subPackageName))
            {
                await ExecuteFileServerFullVersionControl(resolveResult, updateMode, serverUrl, onDownloadProccess,
                    onTaskEndCallback);
            }
            else
            {
                await ExecuteFileServerSubPackageVersionControl(resolveResult, updateMode, serverUrl, subPackageName,
                    onDownloadProccess, onTaskEndCallback);
            }

            return true;
        }

        private async Task<bool> TryGetFileServerSubPackageInfos(
            string serverUrl,
            Action<Dictionary<string, string>> callback,
            Action<string> onError)
        {
            var resolveResult = await ResolveFileServerVersionInfo(serverUrl);
            if (!resolveResult.IsFileServerProtocol)
            {
                LogFileServerFlow("文件服务器子包查询未检测到文件服务器共享版控文件。", Color.yellow);
                return false;
            }

            if (!string.IsNullOrEmpty(resolveResult.Error))
            {
                LogFileServerFlow($"文件服务器子包版控解析失败 err={resolveResult.Error}", Color.red);
                await UniTask.SwitchToMainThread();
                onError?.Invoke(resolveResult.Error);
                return true;
            }

            var loadResult = await LoadFileServerSubPackageConfigs(serverUrl, resolveResult, true);
            if (!string.IsNullOrEmpty(loadResult.Item1))
            {
                LogFileServerFlow($"读取文件服务器子包配置失败 err={loadResult.Item1}", Color.red);
                await UniTask.SwitchToMainThread();
                onError?.Invoke(loadResult.Item1);
                return true;
            }

            var serverSubPackages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var config in loadResult.Item2)
            {
                serverSubPackages[GetFileServerSubPackageFileName(config.PackageName)] =
                    resolveResult.VersionInfo.RawValue;
            }

            LogFileServerFlow($"文件服务器子包配置读取完成 count={serverSubPackages.Count}", Color.green);
            await UniTask.SwitchToMainThread();
            callback?.Invoke(serverSubPackages);
            return true;
        }

        private async Task ExecuteFileServerFullVersionControl(
            FileServerResolveResult resolveResult,
            UpdateMode updateMode,
            string serverUrl,
            Action<AssetItem, List<AssetItem>> onDownloadProccess,
            Action<RetStatus, string> onTaskEndCallback)
        {
            var state = resolveResult.State;
            var firstLoadDir = resolveResult.FirstLoadDir;
            var platform = resolveResult.Platform;
            var platformPath = resolveResult.PlatformPath;
            var remoteVersionInfo = resolveResult.VersionInfo;

            LogFileServerFlow($"开始全量文件服务器版控 local={state.InstalledVersion} remote={remoteVersionInfo.RawValue} mode={updateMode}",
                Color.cyan);

            if (updateMode != UpdateMode.RepairFull && state.InstalledVersion == remoteVersionInfo.RawValue)
            {
                BDebug.Log(LogTag,
                    resolveResult.UsedLocalFallbackVersion
                        ? "文件服务器版控服务不可用，使用本地临时版本号且当前已是最新版本。"
                        : "文件服务器三段版本一致，无需下载。",
                    Color.yellow);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Success, null);
                return;
            }

            var componentContexts = new Dictionary<FileServerComponentKind, FileServerComponentContext>();
            foreach (var componentKind in FileServerManagedComponents)
            {
                var remoteVersion = remoteVersionInfo.GetVersion(componentKind);
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    continue;
                }

                var localVersion = GetFileServerStateComponentVersion(state, componentKind);
                var shouldFetchRemote = updateMode == UpdateMode.RepairFull
                                        || !string.Equals(localVersion, remoteVersion,
                                            StringComparison.OrdinalIgnoreCase)
                                        || !HasFileServerComponentCache(resolveResult.CacheDir, componentKind);
                FileServerComponentContext context;
                if (shouldFetchRemote)
                {
                    var remoteContextResult = await LoadFileServerComponentContext(serverUrl, platformPath, componentKind,
                        remoteVersion, false, true);
                    if (!string.IsNullOrEmpty(remoteContextResult.Item1))
                    {
                        await UniTask.SwitchToMainThread();
                        onTaskEndCallback?.Invoke(RetStatus.Error, remoteContextResult.Item1);
                        return;
                    }

                    context = remoteContextResult.Item2;
                    SaveFileServerComponentCache(resolveResult.CacheDir, context);
                }
                else
                {
                    context = LoadCachedFileServerComponentContext(resolveResult.CacheDir, componentKind, remoteVersion);
                    if (context == null)
                    {
                        var remoteContextResult = await LoadFileServerComponentContext(serverUrl, platformPath,
                            componentKind, remoteVersion, false, true);
                        if (!string.IsNullOrEmpty(remoteContextResult.Item1))
                        {
                            await UniTask.SwitchToMainThread();
                            onTaskEndCallback?.Invoke(RetStatus.Error, remoteContextResult.Item1);
                            return;
                        }

                        context = remoteContextResult.Item2;
                        SaveFileServerComponentCache(resolveResult.CacheDir, context);
                    }
                }

                componentContexts[componentKind] = context;
            }

            LogFileServerFlow($"组件元数据准备完成 count={componentContexts.Count}", Color.green);

            var downloadItems = new List<FileServerDownloadItem>();
            foreach (var componentPair in componentContexts)
            {
                var componentKind = componentPair.Key;
                var context = componentPair.Value;
                var localVersion = GetFileServerStateComponentVersion(state, componentKind);
                var requireRepair = updateMode == UpdateMode.RepairFull;
                var versionChanged = !string.Equals(localVersion, context.Version, StringComparison.OrdinalIgnoreCase);
                if (!requireRepair && !versionChanged)
                {
                    continue;
                }

                downloadItems.AddRange(BuildFileServerDownloadItems(serverUrl, platformPath, firstLoadDir, componentKind,
                    context, false, null, resolveResult));
            }

            LogFileServerFlow($"差异资源计算完成 downloadCount={downloadItems.Count}", Color.green);
            var downloadResult = await DownloadFileServerAssets(downloadItems, onDownloadProccess);
            if (downloadResult.Item1.Count > 0)
            {
                LogFileServerFlow($"文件服务器全量下载失败 failedCount={downloadResult.Item1.Count}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, "文件服务器存在未完成下载的资源!");
                return;
            }

            RebuildFileServerLocalMetadata(firstLoadDir, resolveResult.CacheDir, componentContexts);

            foreach (var componentPair in componentContexts)
            {
                var deletedFiles = CleanupFileServerRemovedFiles(resolveResult, componentPair.Key,
                    GetFileServerManagedFiles(componentPair.Value, false, null));
                foreach (var deletedFile in deletedFiles)
                {
                    await UniTask.SwitchToMainThread();
                    onTaskEndCallback?.Invoke(RetStatus.DeleteOldAssets, deletedFile);
                    await UniTask.SwitchToThreadPool();
                }

                SetFileServerStateComponentVersion(state, componentPair.Key, componentPair.Value.Version);
                state.ManagedFiles[GetFileServerComponentKey(componentPair.Key)] =
                    GetFileServerManagedFiles(componentPair.Value, false, null);
            }

            state.InstalledVersion = remoteVersionInfo.RawValue;
            state.LastKnownRemoteVersion = remoteVersionInfo.RawValue;
            SaveFileServerState(resolveResult.CacheDir, state);

            // 4. 最终校验仍沿用旧版控的回调语义，避免更新页需要区分两套协议。
            var validateError = await ValidateFileServerManagedAssets(firstLoadDir, platform, componentContexts, false,
                null, onTaskEndCallback);
            if (!string.IsNullOrEmpty(validateError))
            {
                LogFileServerFlow($"文件服务器全量校验失败 err={validateError}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, validateError);
                return;
            }

            LogFileServerFlow($"文件服务器全量流程完成 needRestart={downloadResult.Item2}", Color.green);
            await UniTask.SwitchToMainThread();
            onTaskEndCallback?.Invoke(downloadResult.Item2 ? RetStatus.SuccessNeedRestart : RetStatus.Success, null);
        }

        private async Task ExecuteFileServerSubPackageVersionControl(
            FileServerResolveResult resolveResult,
            UpdateMode updateMode,
            string serverUrl,
            string requestSubPackageName,
            Action<AssetItem, List<AssetItem>> onDownloadProccess,
            Action<RetStatus, string> onTaskEndCallback)
        {
            var normalizedPackageName = NormalizeFileServerSubPackageName(requestSubPackageName);
            LogFileServerFlow($"开始子包文件服务器版控 request={requestSubPackageName} normalized={normalizedPackageName} mode={updateMode}",
                Color.cyan);
            var subPackageConfigsResult = await LoadFileServerSubPackageConfigs(serverUrl, resolveResult, true);
            if (!string.IsNullOrEmpty(subPackageConfigsResult.Item1))
            {
                LogFileServerFlow($"读取文件服务器子包配置失败 err={subPackageConfigsResult.Item1}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, subPackageConfigsResult.Item1);
                return;
            }

            var subPackageConfig = subPackageConfigsResult.Item2.FirstOrDefault(config =>
                string.Equals(config.PackageName, normalizedPackageName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetFileServerSubPackageFileName(config.PackageName), requestSubPackageName,
                    StringComparison.OrdinalIgnoreCase));
            if (subPackageConfig == null)
            {
                LogFileServerFlow($"文件服务器子包不存在 request={requestSubPackageName}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, $"文件服务器不存在子包配置:{requestSubPackageName}");
                return;
            }

            var subPackageFileName = GetFileServerSubPackageFileName(subPackageConfig.PackageName);
            if (updateMode != UpdateMode.RepairFull
                && resolveResult.State.InstalledSubPackageVersions.TryGetValue(subPackageFileName, out var localSubPackageVersion)
                && string.Equals(localSubPackageVersion, resolveResult.VersionInfo.RawValue, StringComparison.OrdinalIgnoreCase))
            {
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Success, null);
                return;
            }

            var componentContexts = new Dictionary<FileServerComponentKind, FileServerComponentContext>();
            foreach (var componentKind in FileServerManagedComponents)
            {
                var remoteVersion = resolveResult.VersionInfo.GetVersion(componentKind);
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    continue;
                }

                var loadContextResult = await LoadFileServerComponentContext(serverUrl, resolveResult.PlatformPath,
                    componentKind, remoteVersion, true, true);
                if (!string.IsNullOrEmpty(loadContextResult.Item1))
                {
                    await UniTask.SwitchToMainThread();
                    onTaskEndCallback?.Invoke(RetStatus.Error, loadContextResult.Item1);
                    return;
                }

                componentContexts[componentKind] = loadContextResult.Item2;
                SaveFileServerComponentCache(resolveResult.CacheDir, loadContextResult.Item2);
            }

            var tableAssets = BuildFileServerTableAssetItems(resolveResult.VersionInfo.TableVersion);
            var selectedAssets = BuildFileServerSubPackageAssetItems(subPackageConfig,
                componentContexts.TryGetValue(FileServerComponentKind.AssetBundle, out var abContext)
                    ? abContext.AssetItems
                    : null,
                componentContexts.TryGetValue(FileServerComponentKind.Code, out var codeContext)
                    ? codeContext.AssetItems
                    : null,
                tableAssets.Select(item => item.AssetItem).ToList());

            LogFileServerFlow($"子包资源筛选完成 package={subPackageConfig.PackageName} assetCount={selectedAssets.Count}", Color.green);

            var downloadItems = new List<FileServerDownloadItem>();
            if (abContext != null)
            {
                downloadItems.AddRange(BuildFileServerDownloadItems(serverUrl, resolveResult.PlatformPath,
                    resolveResult.FirstLoadDir, FileServerComponentKind.AssetBundle, abContext, true, selectedAssets,
                    resolveResult));
            }

            if (codeContext != null)
            {
                downloadItems.AddRange(BuildFileServerDownloadItems(serverUrl, resolveResult.PlatformPath,
                    resolveResult.FirstLoadDir, FileServerComponentKind.Code, codeContext, true, selectedAssets,
                    resolveResult));
            }

            downloadItems.AddRange(tableAssets.Where(item =>
                    selectedAssets.Any(asset => asset.LocalPath == item.AssetItem.LocalPath))
                .Select(item => BuildFileServerTableDownloadItem(serverUrl, resolveResult.PlatformPath,
                    resolveResult.FirstLoadDir, resolveResult.VersionInfo.TableVersion, item)));

            LogFileServerFlow($"子包差异资源计算完成 package={subPackageConfig.PackageName} downloadCount={downloadItems.Count}", Color.green);

            var downloadResult = await DownloadFileServerAssets(downloadItems, onDownloadProccess);
            if (downloadResult.Item1.Count > 0)
            {
                LogFileServerFlow($"文件服务器子包下载失败 package={subPackageConfig.PackageName} failedCount={downloadResult.Item1.Count}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, "文件服务器子包资源下载失败!");
                return;
            }

            var subPackageInfoPath = BResources.GetAssetsSubPackageInfoPath(BApplication.persistentDataPath,
                resolveResult.Platform, subPackageFileName);
            FileHelper.WriteAllText(subPackageInfoPath, CsvSerializer.SerializeToString(selectedAssets));

            resolveResult.State.InstalledSubPackageVersions[subPackageFileName] = resolveResult.VersionInfo.RawValue;
            resolveResult.State.SubPackageManagedFiles[subPackageFileName] = selectedAssets.Select(item => item.LocalPath)
                .Distinct()
                .ToList();
            resolveResult.State.LastKnownRemoteVersion = resolveResult.VersionInfo.RawValue;
            SaveFileServerState(resolveResult.CacheDir, resolveResult.State);

            var validateError = await ValidateFileServerManagedAssets(resolveResult.FirstLoadDir, resolveResult.Platform,
                componentContexts, true, selectedAssets, onTaskEndCallback);
            if (!string.IsNullOrEmpty(validateError))
            {
                LogFileServerFlow($"文件服务器子包校验失败 package={subPackageConfig.PackageName} err={validateError}", Color.red);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, validateError);
                return;
            }

            LogFileServerFlow($"文件服务器子包流程完成 package={subPackageConfig.PackageName} needRestart={downloadResult.Item2}", Color.green);
            await UniTask.SwitchToMainThread();
            onTaskEndCallback?.Invoke(downloadResult.Item2 ? RetStatus.SuccessNeedRestart : RetStatus.Success, null);
        }

        private async Task<FileServerResolveResult> ResolveFileServerVersionInfo(string serverUrl)
        {
            var runtimePlatform = BApplication.RuntimePlatform;
            var platformPath = BApplication.GetPlatformLoadPath(runtimePlatform);
            var firstLoadDir = EnsureFileServerLocalLoadDir(runtimePlatform).Item1;
            var cacheDir = IPath.Combine(firstLoadDir, FileServerCacheFolderName);
            Directory.CreateDirectory(cacheDir);

            var state = LoadFileServerState(cacheDir);
            var versionUrl = BuildFileServerVersionManifestUrl(serverUrl, platformPath);

            LogFileServerFlow($"开始请求共享版控文件 url={versionUrl}", Color.cyan);

            var manifestDownloadResult = await DownloadTextWithRetry(versionUrl);
            if (manifestDownloadResult.Item1)
            {
                if (!TryParseFileServerVersionInfo(manifestDownloadResult.Item3, out var remoteVersionInfo))
                {
                    return new FileServerResolveResult()
                    {
                        IsFileServerProtocol = true,
                        Error = $"文件服务器版控文件格式错误:{versionUrl}",
                        Platform = runtimePlatform,
                        PlatformPath = platformPath,
                        FirstLoadDir = firstLoadDir,
                        CacheDir = cacheDir,
                        State = state,
                    };
                }

                state.LastKnownRemoteVersion = remoteVersionInfo.RawValue;
                SaveFileServerState(cacheDir, state);
                LogFileServerFlow($"共享版控文件读取成功 remoteVersion={remoteVersionInfo.RawValue}", Color.green);
                return new FileServerResolveResult()
                {
                    IsFileServerProtocol = true,
                    Platform = runtimePlatform,
                    PlatformPath = platformPath,
                    FirstLoadDir = firstLoadDir,
                    CacheDir = cacheDir,
                    State = state,
                    VersionInfo = remoteVersionInfo,
                };
            }

            if (!string.IsNullOrEmpty(state.LastKnownRemoteVersion)
                && TryParseFileServerVersionInfo(state.LastKnownRemoteVersion, out var localFallbackVersionInfo))
            {
                LogFileServerFlow($"共享版控请求失败，使用本地临时版本 fallbackVersion={localFallbackVersionInfo.RawValue}", Color.yellow);
                return new FileServerResolveResult()
                {
                    IsFileServerProtocol = true,
                    Platform = runtimePlatform,
                    PlatformPath = platformPath,
                    FirstLoadDir = firstLoadDir,
                    CacheDir = cacheDir,
                    State = state,
                    VersionInfo = localFallbackVersionInfo,
                    UsedLocalFallbackVersion = true,
                };
            }

            if (manifestDownloadResult.Item2)
            {
                LogFileServerFlow("共享版控文件不存在，当前服务器未启用文件服务器协议。", Color.yellow);
                return new FileServerResolveResult()
                {
                    IsFileServerProtocol = false,
                    Platform = runtimePlatform,
                    PlatformPath = platformPath,
                    FirstLoadDir = firstLoadDir,
                    CacheDir = cacheDir,
                    State = state,
                };
            }

            LogFileServerFlow($"共享版控请求失败且不存在本地回退版本 err={manifestDownloadResult.Item4}", Color.red);
            return new FileServerResolveResult()
            {
                IsFileServerProtocol = false,
                Platform = runtimePlatform,
                PlatformPath = platformPath,
                FirstLoadDir = firstLoadDir,
                CacheDir = cacheDir,
                State = state,
            };
        }

        private async Task<Tuple<string, List<SubPackageConfigItem>>> LoadFileServerSubPackageConfigs(
            string serverUrl,
            FileServerResolveResult resolveResult,
            bool useCacheOnFailure)
        {
            var candidateKinds = new[] {FileServerComponentKind.AssetBundle, FileServerComponentKind.Code};
            foreach (var componentKind in candidateKinds)
            {
                var componentVersion = resolveResult.VersionInfo.GetVersion(componentKind);
                if (string.IsNullOrEmpty(componentVersion))
                {
                    continue;
                }

                var remoteRoot = BuildFileServerComponentRemoteRoot(serverUrl, resolveResult.PlatformPath, componentKind,
                    componentVersion);
                var configUrl = CombineUrl(remoteRoot, BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH);
                var downloadResult = await DownloadTextWithRetry(configUrl);
                if (downloadResult.Item1)
                {
                    try
                    {
                        var configs = CsvSerializer.DeserializeFromString<List<SubPackageConfigItem>>(downloadResult.Item3)
                                      ?? new List<SubPackageConfigItem>();
                        SaveFileServerSubPackageCache(resolveResult.CacheDir, downloadResult.Item3);
                        return new Tuple<string, List<SubPackageConfigItem>>(null, configs);
                    }
                    catch (Exception e)
                    {
                        return new Tuple<string, List<SubPackageConfigItem>>($"文件服务器分包配置解析失败:{e.Message}",
                            new List<SubPackageConfigItem>());
                    }
                }

                if (downloadResult.Item2)
                {
                    continue;
                }
            }

            if (useCacheOnFailure)
            {
                var cachePath = GetFileServerSubPackageCachePath(resolveResult.CacheDir);
                if (File.Exists(cachePath))
                {
                    try
                    {
                        var content = File.ReadAllText(cachePath);
                        var configs = CsvSerializer.DeserializeFromString<List<SubPackageConfigItem>>(content)
                                      ?? new List<SubPackageConfigItem>();
                        return new Tuple<string, List<SubPackageConfigItem>>(null, configs);
                    }
                    catch (Exception e)
                    {
                        return new Tuple<string, List<SubPackageConfigItem>>($"文件服务器本地分包缓存解析失败:{e.Message}",
                            new List<SubPackageConfigItem>());
                    }
                }
            }

            return new Tuple<string, List<SubPackageConfigItem>>(null, new List<SubPackageConfigItem>());
        }

        private async Task<Tuple<string, FileServerComponentContext>> LoadFileServerComponentContext(
            string serverUrl,
            string platformPath,
            FileServerComponentKind componentKind,
            string componentVersion,
            bool includeSubPackageConfig,
            bool useCacheOnFailure)
        {
            var remoteRoot = BuildFileServerComponentRemoteRoot(serverUrl, platformPath, componentKind, componentVersion);
            var context = new FileServerComponentContext()
            {
                ComponentKind = componentKind,
                Version = componentVersion,
            };

            var packageBuildDownload = await DownloadTextWithRetry(CombineUrl(remoteRoot, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH));
            if (!packageBuildDownload.Item1)
            {
                if (useCacheOnFailure)
                {
                    var cachedContext = LoadCachedFileServerComponentContext(GetFileServerSharedCacheDir(platformPath),
                        componentKind, componentVersion);
                    if (cachedContext != null)
                    {
                        return new Tuple<string, FileServerComponentContext>(null, cachedContext);
                    }
                }

                return new Tuple<string, FileServerComponentContext>($"文件服务器下载 package_build.info 失败:{remoteRoot}",
                    null);
            }

            try
            {
                context.PackageBuildContent = packageBuildDownload.Item3;
                context.PackageBuildInfo = JsonMapper.ToObject<ClientPackageBuildInfo>(packageBuildDownload.Item3)
                                           ?? new ClientPackageBuildInfo();
            }
            catch (Exception e)
            {
                return new Tuple<string, FileServerComponentContext>($"文件服务器解析 package_build.info 失败:{e.Message}",
                    null);
            }

            if (componentKind != FileServerComponentKind.Table)
            {
                var assetsInfoDownload = await DownloadTextWithRetry(CombineUrl(remoteRoot, BResources.ASSETS_INFO_PATH));
                if (!assetsInfoDownload.Item1)
                {
                    return new Tuple<string, FileServerComponentContext>($"文件服务器下载 assets.info 失败:{remoteRoot}",
                        null);
                }

                try
                {
                    context.AssetsInfoContent = assetsInfoDownload.Item3;
                    var assetItems = CsvSerializer.DeserializeFromString<List<AssetItem>>(assetsInfoDownload.Item3)
                                     ?? new List<AssetItem>();
                    context.AssetItems = assetItems.Where(item =>
                            !string.Equals(item.LocalPath, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH,
                                StringComparison.OrdinalIgnoreCase))
                        .Distinct()
                        .OrderBy(item => item.Id)
                        .ToList();
                }
                catch (Exception e)
                {
                    return new Tuple<string, FileServerComponentContext>($"文件服务器解析 assets.info 失败:{e.Message}",
                        null);
                }

                if (includeSubPackageConfig)
                {
                    var subPackageDownload = await DownloadTextWithRetry(CombineUrl(remoteRoot,
                        BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH));
                    if (subPackageDownload.Item1)
                    {
                        context.SubPackageContent = subPackageDownload.Item3;
                        try
                        {
                            context.SubPackageConfigs =
                                CsvSerializer.DeserializeFromString<List<SubPackageConfigItem>>(subPackageDownload.Item3)
                                ?? new List<SubPackageConfigItem>();
                        }
                        catch (Exception e)
                        {
                            return new Tuple<string, FileServerComponentContext>($"文件服务器解析 assets_subpack.info 失败:{e.Message}",
                                null);
                        }
                    }
                }
            }
            else
            {
                context.AssetItems = BuildFileServerTableAssetItems(componentVersion).Select(item => item.AssetItem)
                    .ToList();
            }

            return new Tuple<string, FileServerComponentContext>(null, context);
        }

        private async Task<Tuple<List<FileServerDownloadItem>, bool>> DownloadFileServerAssets(
            List<FileServerDownloadItem> downloadItems,
            Action<AssetItem, List<AssetItem>> onDownloadProccess)
        {
            var failedItems = new ConcurrentBag<FileServerDownloadItem>();
            var downloadedItems = new ConcurrentBag<FileServerDownloadItem>();
            var needRestart = 0;
            if (downloadItems.Count == 0)
            {
                LogFileServerFlow("没有需要下载的差异资源。", Color.green);
                return new Tuple<List<FileServerDownloadItem>, bool>(new List<FileServerDownloadItem>(), false);
            }

            var downloadAssets = downloadItems.Select(item => item.AssetItem).ToList();
            LogFileServerFlow($"开始并发下载资源 total={downloadItems.Count} concurrency={CalculateFileServerDownloadConcurrency()}", Color.cyan);
            using (var semaphore = new SemaphoreSlim(CalculateFileServerDownloadConcurrency()))
            {
                var downloadTasks = downloadItems.Select(async item =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var downloadSucceeded = await DownloadFileServerAsset(item, downloadAssets, onDownloadProccess);
                        if (downloadSucceeded)
                        {
                            downloadedItems.Add(item);
                            if (IsRestartRequiredAsset(item.AssetItem))
                            {
                                Interlocked.Exchange(ref needRestart, 1);
                            }
                        }
                        else
                        {
                            failedItems.Add(item);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                await UniTask.WhenAll(downloadTasks);
            }

            LogFileServerFlow($"资源下载阶段结束 failedCount={failedItems.Count} needRestart={needRestart == 1}",
                failedItems.Count > 0 ? Color.red : Color.green);
            return new Tuple<List<FileServerDownloadItem>, bool>(failedItems.ToList(), needRestart == 1);
        }

        private async Task<bool> DownloadFileServerAsset(FileServerDownloadItem downloadItem,
            List<AssetItem> allDownloadAssets,
            Action<AssetItem, List<AssetItem>> onDownloadProccess)
        {
            byte[] bytes = null;
            string err = null;
            for (int retryIndex = 0; retryIndex < RETRY_COUNT; retryIndex++)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        bytes = await client.DownloadDataTaskAsync(downloadItem.RemoteUrl);
                    }

                    if (downloadItem.RequireHashValidation && !string.IsNullOrEmpty(downloadItem.AssetItem.HashName))
                    {
                        var hash = FileHelper.GetMurmurHash3(bytes);
                        if (!string.Equals(hash, downloadItem.AssetItem.HashName, StringComparison.OrdinalIgnoreCase))
                        {
                            err = $"文件服务器资源 hash 校验失败:{downloadItem.AssetItem.LocalPath}";
                            bytes = null;
                            continue;
                        }
                    }

                    err = null;
                    break;
                }
                catch (Exception e)
                {
                    err = e.Message;
                }
            }

            if (!string.IsNullOrEmpty(err) || bytes == null)
            {
                BDebug.LogError($"文件服务器资源下载失败:{downloadItem.RemoteUrl} err:{err}");
                return false;
            }

            await UniTask.SwitchToMainThread();
            onDownloadProccess?.Invoke(downloadItem.AssetItem, allDownloadAssets);
            await UniTask.SwitchToThreadPool();

            var localDir = Path.GetDirectoryName(downloadItem.FinalLocalPath);
            if (!string.IsNullOrEmpty(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            var tempDownloadPath = downloadItem.FinalLocalPath + ".download";
            if (File.Exists(tempDownloadPath))
            {
                File.Delete(tempDownloadPath);
            }

            FileHelper.WriteAllBytes(tempDownloadPath, bytes);
            if (File.Exists(downloadItem.FinalLocalPath))
            {
                File.Delete(downloadItem.FinalLocalPath);
            }

            FileHelper.Move(tempDownloadPath, downloadItem.FinalLocalPath);
            return true;
        }

        private List<FileServerDownloadItem> BuildFileServerDownloadItems(
            string serverUrl,
            string platformPath,
            string firstLoadDir,
            FileServerComponentKind componentKind,
            FileServerComponentContext componentContext,
            bool isSubPackageMode,
            List<AssetItem> selectedAssets,
            FileServerResolveResult resolveResult)
        {
            var downloadItems = new List<FileServerDownloadItem>();
            if (componentContext == null)
            {
                return downloadItems;
            }

            var remoteRoot = BuildFileServerComponentRemoteRoot(serverUrl, platformPath, componentKind,
                componentContext.Version);
            if (componentKind == FileServerComponentKind.Table)
            {
                var tableAssets = BuildFileServerTableAssetItems(componentContext.Version);
                if (isSubPackageMode && selectedAssets != null)
                {
                    var selectedSet = new HashSet<string>(selectedAssets.Select(item => item.LocalPath),
                        StringComparer.OrdinalIgnoreCase);
                    tableAssets = tableAssets.Where(item => selectedSet.Contains(item.AssetItem.LocalPath)).ToList();
                }

                foreach (var tableAsset in tableAssets)
                {
                    downloadItems.Add(BuildFileServerTableDownloadItem(serverUrl, platformPath, firstLoadDir,
                        componentContext.Version, tableAsset));
                }

                return downloadItems;
            }

            var targetAssets = componentContext.AssetItems;
            if (isSubPackageMode && selectedAssets != null)
            {
                var selectedPathSet = new HashSet<string>(selectedAssets.Select(item => item.LocalPath),
                    StringComparer.OrdinalIgnoreCase);
                targetAssets = targetAssets.Where(item => selectedPathSet.Contains(item.LocalPath)).ToList();
            }

            foreach (var assetItem in targetAssets)
            {
                if (!ClientAssetsUtils.IsExsitAssetWithCheckHash(resolveResult.Platform, assetItem.LocalPath,
                        assetItem.HashName))
                {
                    downloadItems.Add(new FileServerDownloadItem()
                    {
                        ComponentKind = componentKind,
                        AssetItem = assetItem,
                        FinalLocalPath = IPath.Combine(firstLoadDir, assetItem.LocalPath),
                        RemoteUrl = CombineUrl(remoteRoot, assetItem.LocalPath),
                        RequireHashValidation = true,
                    });
                }
            }

            return downloadItems;
        }

        private List<(AssetItem AssetItem, string RemoteRelativePath)> BuildFileServerTableAssetItems(string tableVersion)
        {
            var tableItems = new List<(AssetItem AssetItem, string RemoteRelativePath)>();
            if (string.IsNullOrEmpty(tableVersion))
            {
                return tableItems;
            }

            tableItems.Add((new AssetItem() {Id = 2000001, LocalPath = SqliteLoder.LOCAL_DB_PATH, FileSize = 0},
                "client.db"));
            tableItems.Add((new AssetItem() {Id = 2000002, LocalPath = IPath.Combine("server_data", SqliteLoder.SERVER_DB_PATH), FileSize = 0},
                SqliteLoder.SERVER_DB_PATH));
            return tableItems;
        }

        private FileServerDownloadItem BuildFileServerTableDownloadItem(string serverUrl,
            string platformPath,
            string firstLoadDir,
            string tableVersion,
            (AssetItem AssetItem, string RemoteRelativePath) tableItem)
        {
            return new FileServerDownloadItem()
            {
                ComponentKind = FileServerComponentKind.Table,
                AssetItem = tableItem.AssetItem,
                FinalLocalPath = IPath.Combine(firstLoadDir, tableItem.AssetItem.LocalPath),
                RemoteUrl = CombineUrl(BuildFileServerComponentRemoteRoot(serverUrl, platformPath,
                    FileServerComponentKind.Table, tableVersion), tableItem.RemoteRelativePath),
                RequireHashValidation = false,
            };
        }

        private async Task<string> ValidateFileServerManagedAssets(string firstLoadDir,
            RuntimePlatform platform,
            Dictionary<FileServerComponentKind, FileServerComponentContext> componentContexts,
            bool isSubPackageMode,
            List<AssetItem> selectedAssets,
            Action<RetStatus, string> onTaskEndCallback)
        {
            var errors = new List<string>();
            IEnumerable<AssetItem> targetAssets;
            if (isSubPackageMode && selectedAssets != null)
            {
                targetAssets = selectedAssets;
            }
            else
            {
                targetAssets = componentContexts.Values.SelectMany(item => item.AssetItems).Distinct().ToList();
            }

            foreach (var assetItem in targetAssets)
            {
                var isValid = true;
                if (string.IsNullOrEmpty(assetItem.HashName))
                {
                    var localPath = IPath.Combine(firstLoadDir, assetItem.LocalPath);
                    isValid = File.Exists(localPath);
                }
                else
                {
                    isValid = ClientAssetsUtils.IsExsitAssetWithCheckHash(platform, assetItem.LocalPath,
                        assetItem.HashName);
                }

                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Checkassets, assetItem.LocalPath);
                await UniTask.SwitchToThreadPool();
                if (!isValid)
                {
                    errors.Add(assetItem.LocalPath);
                }
            }

            if (errors.Count == 0)
            {
                return null;
            }

            return "文件服务器资源校验失败:\n" + string.Join("\n", errors);
        }

        private void RebuildFileServerLocalMetadata(string firstLoadDir,
            string cacheDir,
            Dictionary<FileServerComponentKind, FileServerComponentContext> componentContexts)
        {
            var mergedAssets = componentContexts.Values
                .Where(context => context.ComponentKind != FileServerComponentKind.Table)
                .SelectMany(context => context.AssetItems)
                .Where(item =>
                    !string.Equals(item.LocalPath, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH,
                        StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(item => item.Id)
                .ToList();
            if (mergedAssets.Count > 0)
            {
                FileHelper.WriteAllText(IPath.Combine(firstLoadDir, BResources.ASSETS_INFO_PATH),
                    CsvSerializer.SerializeToString(mergedAssets));
            }

            var localPackageBuildInfo = LoadLocalPackageBuildInfo(firstLoadDir);
            var mergedPackageBuildInfo = MergeFileServerPackageBuildInfo(localPackageBuildInfo,
                componentContexts.TryGetValue(FileServerComponentKind.Code, out var codeContext)
                    ? codeContext.PackageBuildInfo
                    : null,
                componentContexts.TryGetValue(FileServerComponentKind.AssetBundle, out var abContext)
                    ? abContext.PackageBuildInfo
                    : null,
                componentContexts.TryGetValue(FileServerComponentKind.Table, out var tableContext)
                    ? tableContext.PackageBuildInfo
                    : null);
            FileHelper.WriteAllText(IPath.Combine(firstLoadDir, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH),
                JsonMapper.ToJson(mergedPackageBuildInfo));

            var subPackagePath = GetFileServerSubPackageCachePath(cacheDir);
            if (File.Exists(subPackagePath))
            {
                FileHelper.WriteAllText(IPath.Combine(firstLoadDir, BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH),
                    File.ReadAllText(subPackagePath));
            }

            LogFileServerFlow($"本地元数据重建完成 mergedAssets={mergedAssets.Count} hasSubPackageCache={File.Exists(subPackagePath)}",
                Color.green);
        }

        private List<string> CleanupFileServerRemovedFiles(FileServerResolveResult resolveResult,
            FileServerComponentKind componentKind,
            List<string> newManagedFiles)
        {
            var deletedFiles = new List<string>();
            var state = resolveResult.State;
            var componentKey = GetFileServerComponentKey(componentKind);
            if (!state.ManagedFiles.TryGetValue(componentKey, out var oldManagedFiles) || oldManagedFiles == null)
            {
                return deletedFiles;
            }

            var newManagedSet = new HashSet<string>(newManagedFiles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var relativePath in oldManagedFiles)
            {
                if (newManagedSet.Contains(relativePath))
                {
                    continue;
                }

                var absolutePath = IPath.Combine(resolveResult.FirstLoadDir, relativePath);
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                    BDebug.Log(LogTag, $"文件服务器删除过期资源:{relativePath}");
                    deletedFiles.Add(relativePath);
                }
            }

            return deletedFiles;
        }

        private static void ApplyFileServerPackageBuildInfo(ClientPackageBuildInfo mergedInfo,
            ClientPackageBuildInfo sourceInfo)
        {
            if (mergedInfo == null || sourceInfo == null)
            {
                return;
            }

            if (sourceInfo.BuildTime > mergedInfo.BuildTime)
            {
                mergedInfo.BuildTime = sourceInfo.BuildTime;
            }

            if (string.IsNullOrEmpty(mergedInfo.Version) && !string.IsNullOrEmpty(sourceInfo.Version))
            {
                mergedInfo.Version = sourceInfo.Version;
            }

            if (!string.IsNullOrEmpty(sourceInfo.BasePckScriptSVCVersion)
                && !string.Equals(sourceInfo.BasePckScriptSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.BasePckScriptSVCVersion = sourceInfo.BasePckScriptSVCVersion;
            }

            if (!string.IsNullOrEmpty(sourceInfo.HotfixScriptSVCVersion)
                && !string.Equals(sourceInfo.HotfixScriptSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.HotfixScriptSVCVersion = sourceInfo.HotfixScriptSVCVersion;
            }

            if (!string.IsNullOrEmpty(sourceInfo.AssetBundleSVCVersion)
                && !string.Equals(sourceInfo.AssetBundleSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.AssetBundleSVCVersion = sourceInfo.AssetBundleSVCVersion;
            }

            if (!string.IsNullOrEmpty(sourceInfo.TableSVCVersion)
                && !string.Equals(sourceInfo.TableSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.TableSVCVersion = sourceInfo.TableSVCVersion;
            }
        }

        private static Tuple<string, string> EnsureFileServerLocalLoadDir(RuntimePlatform runtimePlatform)
        {
            if (string.IsNullOrEmpty(ClientAssetsUtils.FIRST_LOAD_DIR)
                || string.IsNullOrEmpty(ClientAssetsUtils.SECOND_LOAD_DIR))
            {
                var basePackageBuildInfo = ClientAssetsUtils.GetBasePackBuildInfo();
                var baseVersion = basePackageBuildInfo?.Version;
                if (string.IsNullOrEmpty(baseVersion))
                {
                    baseVersion = "0.0.0";
                }

                ClientAssetsUtils.GetMultiAssetsLoadPath(runtimePlatform, baseVersion);
            }

            var firstLoadDir = ClientAssetsUtils.FIRST_LOAD_DIR;
            var secondLoadDir = ClientAssetsUtils.SECOND_LOAD_DIR;
            if (string.IsNullOrEmpty(firstLoadDir))
            {
                firstLoadDir = IPath.Combine(BApplication.persistentDataPath, "0.0.0",
                    BApplication.GetPlatformLoadPath(runtimePlatform));
                ClientAssetsUtils.FIRST_LOAD_DIR = firstLoadDir;
            }

            if (string.IsNullOrEmpty(secondLoadDir))
            {
                secondLoadDir = IPath.Combine(BApplication.streamingAssetsPath,
                    BApplication.GetPlatformLoadPath(runtimePlatform));
                ClientAssetsUtils.SECOND_LOAD_DIR = secondLoadDir;
            }

            Directory.CreateDirectory(firstLoadDir);
            return new Tuple<string, string>(firstLoadDir, secondLoadDir);
        }

        private static string BuildFileServerVersionManifestUrl(string serverUrl, string platformPath)
        {
            return CombineUrl(serverUrl, string.Format(FileServerVersionManifestRootFormat, platformPath),
                FileServerVersionManifestFileName);
        }

        private static string BuildFileServerComponentRemoteRoot(string serverUrl, string platformPath,
            FileServerComponentKind componentKind,
            string componentVersion)
        {
            switch (componentKind)
            {
                case FileServerComponentKind.Code:
                    return CombineUrl(serverUrl, $"ClientRes_Code_{platformPath}", componentVersion);
                case FileServerComponentKind.AssetBundle:
                    return CombineUrl(serverUrl, $"ClientRes_Assetbundle_{platformPath}", componentVersion);
                case FileServerComponentKind.Table:
                    return CombineUrl(serverUrl, "ClientRes_Table", componentVersion);
                default:
                    return serverUrl;
            }
        }

        private static string CombineUrl(string root, params string[] segments)
        {
            var result = (root ?? string.Empty).TrimEnd('/');
            foreach (var segment in segments)
            {
                var normalized = (segment ?? string.Empty).Replace("\\", "/").Trim('/');
                if (string.IsNullOrEmpty(normalized))
                {
                    continue;
                }

                result = string.IsNullOrEmpty(result) ? normalized : result + "/" + normalized;
            }

            return result;
        }

        private static int CalculateFileServerDownloadConcurrency()
        {
            return Math.Min(Math.Max(Environment.ProcessorCount / 2, 2), 4);
        }

        private static string GetFileServerComponentKey(FileServerComponentKind componentKind)
        {
            switch (componentKind)
            {
                case FileServerComponentKind.Code:
                    return "code";
                case FileServerComponentKind.AssetBundle:
                    return "assetbundle";
                case FileServerComponentKind.Table:
                    return "table";
                default:
                    return componentKind.ToString().ToLowerInvariant();
            }
        }

        private static string GetFileServerStatePath(string cacheDir)
        {
            return IPath.Combine(cacheDir, FileServerStateFileName);
        }

        private static string GetFileServerPackageBuildCachePath(string cacheDir, FileServerComponentKind componentKind)
        {
            return IPath.Combine(cacheDir, string.Format(FileServerPackageBuildInfoFileFormat,
                GetFileServerComponentKey(componentKind)));
        }

        private static string GetFileServerAssetsInfoCachePath(string cacheDir, FileServerComponentKind componentKind)
        {
            return IPath.Combine(cacheDir,
                string.Format(FileServerAssetsInfoFileFormat, GetFileServerComponentKey(componentKind)));
        }

        private static string GetFileServerSubPackageCachePath(string cacheDir)
        {
            return IPath.Combine(cacheDir, FileServerSubPackageCacheFileName);
        }

        private static string GetFileServerSharedCacheDir(string platformPath)
        {
            var loadDir = EnsureFileServerLocalLoadDir(BApplication.RuntimePlatform).Item1;
            return IPath.Combine(loadDir, FileServerCacheFolderName);
        }

        private static bool HasFileServerComponentCache(string cacheDir, FileServerComponentKind componentKind)
        {
            if (componentKind == FileServerComponentKind.Table)
            {
                return File.Exists(GetFileServerPackageBuildCachePath(cacheDir, componentKind));
            }

            return File.Exists(GetFileServerPackageBuildCachePath(cacheDir, componentKind))
                   && File.Exists(GetFileServerAssetsInfoCachePath(cacheDir, componentKind));
        }

        private static FileServerVersionControllerState LoadFileServerState(string cacheDir)
        {
            var statePath = GetFileServerStatePath(cacheDir);
            if (!File.Exists(statePath))
            {
                return new FileServerVersionControllerState();
            }

            try
            {
                return JsonMapper.ToObject<FileServerVersionControllerState>(File.ReadAllText(statePath))
                       ?? new FileServerVersionControllerState();
            }
            catch
            {
                return new FileServerVersionControllerState();
            }
        }

        private static void SaveFileServerState(string cacheDir, FileServerVersionControllerState state)
        {
            FileHelper.WriteAllText(GetFileServerStatePath(cacheDir), JsonMapper.ToJson(state));
        }

        private static void SaveFileServerSubPackageCache(string cacheDir, string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                FileHelper.WriteAllText(GetFileServerSubPackageCachePath(cacheDir), content);
            }
        }

        private static void SaveFileServerComponentCache(string cacheDir, FileServerComponentContext context)
        {
            if (context == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(context.PackageBuildContent))
            {
                FileHelper.WriteAllText(GetFileServerPackageBuildCachePath(cacheDir, context.ComponentKind),
                    context.PackageBuildContent);
            }

            if (!string.IsNullOrEmpty(context.AssetsInfoContent))
            {
                FileHelper.WriteAllText(GetFileServerAssetsInfoCachePath(cacheDir, context.ComponentKind),
                    context.AssetsInfoContent);
            }

            if (!string.IsNullOrEmpty(context.SubPackageContent))
            {
                SaveFileServerSubPackageCache(cacheDir, context.SubPackageContent);
            }
        }

        private static FileServerComponentContext LoadCachedFileServerComponentContext(string cacheDir,
            FileServerComponentKind componentKind,
            string componentVersion)
        {
            if (!HasFileServerComponentCache(cacheDir, componentKind))
            {
                return null;
            }

            try
            {
                var context = new FileServerComponentContext()
                {
                    ComponentKind = componentKind,
                    Version = componentVersion,
                    PackageBuildContent = File.ReadAllText(GetFileServerPackageBuildCachePath(cacheDir, componentKind)),
                };
                context.PackageBuildInfo = JsonMapper.ToObject<ClientPackageBuildInfo>(context.PackageBuildContent)
                                           ?? new ClientPackageBuildInfo();
                if (componentKind != FileServerComponentKind.Table)
                {
                    context.AssetsInfoContent = File.ReadAllText(GetFileServerAssetsInfoCachePath(cacheDir, componentKind));
                    var assetItems = CsvSerializer.DeserializeFromString<List<AssetItem>>(context.AssetsInfoContent)
                                     ?? new List<AssetItem>();
                    context.AssetItems = assetItems.Where(item =>
                            !string.Equals(item.LocalPath, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH,
                                StringComparison.OrdinalIgnoreCase))
                        .Distinct()
                        .OrderBy(item => item.Id)
                        .ToList();
                    var subPackageCachePath = GetFileServerSubPackageCachePath(cacheDir);
                    if (File.Exists(subPackageCachePath))
                    {
                        context.SubPackageContent = File.ReadAllText(subPackageCachePath);
                        context.SubPackageConfigs =
                            CsvSerializer.DeserializeFromString<List<SubPackageConfigItem>>(context.SubPackageContent)
                            ?? new List<SubPackageConfigItem>();
                    }
                }

                return context;
            }
            catch
            {
                return null;
            }
        }

        private static ClientPackageBuildInfo LoadLocalPackageBuildInfo(string firstLoadDir)
        {
            var localPackageBuildPath = IPath.Combine(firstLoadDir, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH);
            if (File.Exists(localPackageBuildPath))
            {
                try
                {
                    return JsonMapper.ToObject<ClientPackageBuildInfo>(File.ReadAllText(localPackageBuildPath))
                           ?? new ClientPackageBuildInfo();
                }
                catch
                {
                }
            }

            return ClientAssetsUtils.GetBasePackBuildInfo() ?? new ClientPackageBuildInfo();
        }

        private static string GetFileServerStateComponentVersion(FileServerVersionControllerState state,
            FileServerComponentKind componentKind)
        {
            if (state == null)
            {
                return string.Empty;
            }

            state.InstalledComponentVersions.TryGetValue(GetFileServerComponentKey(componentKind), out var version);
            return version ?? string.Empty;
        }

        private static void SetFileServerStateComponentVersion(FileServerVersionControllerState state,
            FileServerComponentKind componentKind,
            string version)
        {
            if (state == null)
            {
                return;
            }

            state.InstalledComponentVersions[GetFileServerComponentKey(componentKind)] = version ?? string.Empty;
        }

        private static List<string> GetFileServerManagedFiles(FileServerComponentContext context,
            bool isSubPackageMode,
            List<AssetItem> selectedAssets)
        {
            IEnumerable<AssetItem> targetAssets = context?.AssetItems ?? new List<AssetItem>();
            if (isSubPackageMode && selectedAssets != null)
            {
                var selectedSet = new HashSet<string>(selectedAssets.Select(item => item.LocalPath),
                    StringComparer.OrdinalIgnoreCase);
                targetAssets = targetAssets.Where(item => selectedSet.Contains(item.LocalPath));
            }

            return targetAssets.Select(item => item.LocalPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<Tuple<bool, bool, string, string>> DownloadTextWithRetry(string url)
        {
            string error = null;
            bool isNotFound = false;
            for (var retryIndex = 0; retryIndex < RETRY_COUNT; retryIndex++)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var content = await client.DownloadStringTaskAsync(url);
                        return new Tuple<bool, bool, string, string>(true, false, content, null);
                    }
                }
                catch (WebException e)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response?.StatusCode == HttpStatusCode.NotFound)
                    {
                        isNotFound = true;
                        error = e.Message;
                        break;
                    }

                    error = e.Message;
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            }

            return new Tuple<bool, bool, string, string>(false, isNotFound, null, error);
        }
    }
}