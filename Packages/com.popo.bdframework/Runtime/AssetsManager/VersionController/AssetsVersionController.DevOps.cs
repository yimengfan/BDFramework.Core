using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr.V2;
using BDFramework.Sql;
using Cysharp.Threading.Tasks;
using LitJson;
using ServiceStack.Text;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 文件服务器资源版控扩展。
    /// 流程固定为：读取共享三段版控文件、按 Code/AssetBundle/Table 分别求差异、下载差异资源、
    /// 合并本地元数据并清理旧资源，最后沿用老入口的状态回调返回结果。
    /// </summary>
    /// <remarks>
    /// 这套逻辑当前通过 <c>WithDevOps</c> 后缀的显式入口暴露，不会被旧的 <c>UpdateAssets</c> / <c>GetServerSubPackageInfos</c> 隐式访问。
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
        public sealed class FileServerVersionInfo
        {
            public string CodeVersion { get; set; } = string.Empty;

            public string AssetBundleVersion { get; set; } = string.Empty;

            public string TableVersion { get; set; } = string.Empty;

            public string RawValue => AssetsVersionControllerDevOpsPureLogic.FormatFileServerVersionInfo(this);

            public bool HasAnyVersion =>
                !string.IsNullOrEmpty(CodeVersion) || !string.IsNullOrEmpty(AssetBundleVersion) ||
                !string.IsNullOrEmpty(TableVersion);

            internal string GetVersion(FileServerComponentKind componentKind)
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
        /// AssetBundle 组件如果不再显式上传 package_build.info，会在这里用当前组件版本生成可合并的占位元数据。
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
        /// CI BatchMode 文件服务器验证请求。
        /// 该请求聚合 TeamCity 透传的文件服务器地址与三段期望版本号，供批处理入口统一校验版控与下载结果。
        /// </summary>
        /// <example>
        /// TeamCity 平台验证任务会把 BuildCode / BuildAssetbundle / BuildTable 的 build.number 组装成期望值，
        /// 再通过 PublishPipeLineCI 的批处理入口转成这个请求对象执行真实下载验证。
        /// </example>
        public sealed class FileServerBatchVerificationRequest
        {
            /// <summary>
            /// CI BatchMode 验证的目标运行时平台。
            /// 在 Windows 编辑器下执行 -buildTarget Android 验证时，BApplication.RuntimePlatform 返回 WindowsEditor，
            /// 而实际需要校验的目标平台由 CLI 参数或 TeamCity 任务决定，因此必须显式传入。
            /// </summary>
            public RuntimePlatform TargetPlatform { get; set; } = RuntimePlatform.WindowsEditor;

            public string ServerUrl { get; set; } = string.Empty;

            public FileServerVersionInfo ExpectedVersionInfo { get; set; } = new FileServerVersionInfo();

            public bool ResetLocalStateBeforeVerify { get; set; } = true;
        }

        /// <summary>
        /// CI BatchMode 文件服务器验证结果。
        /// 结果会记录本次实际解析到的三段版控、代表性资源落地路径和最终失败原因，便于 TeamCity 日志直接定位问题。
        /// </summary>
        public sealed class FileServerBatchVerificationResult
        {
            public RuntimePlatform Platform { get; set; }

            public string PlatformPath { get; set; } = string.Empty;

            public string FirstLoadDir { get; set; } = string.Empty;

            public string ExpectedVersion { get; set; } = string.Empty;

            public string ActualVersion { get; set; } = string.Empty;

            public bool UsedLocalFallbackVersion { get; set; }

            public string CodeAssetLocalPath { get; set; } = string.Empty;

            public string AssetBundleAssetLocalPath { get; set; } = string.Empty;

            /// <summary>
            /// 按 art_assets.info 解析出的去重 AssetBundle 本地校验路径集合。
            /// 这里保留完整列表，供 CI BatchMode 在主线程上逐个执行 LoadFromFile 校验。
            /// </summary>
            public List<string> AssetBundleAssetLocalPaths { get; set; } = new List<string>();

            public string TableAssetLocalPath { get; set; } = string.Empty;

            public ClientPackageBuildInfo PackageBuildInfo { get; set; } = new ClientPackageBuildInfo();

            public string Error { get; set; } = string.Empty;

            public bool IsSuccess => string.IsNullOrEmpty(Error);
        }

        /// <summary>
        /// 远端共享版控入口文件。
        /// 文件内容为 JSON 数组，每条记录包含 key, platform, version_num, game_server_ip。
        /// 运行时先读取这份全局指针文件，按当前平台提取 version_num（格式为 code.assetbundle.table），
        /// 再决定三个组件分别去哪一个构建号目录下载。
        /// </summary>
        private const string FileServerVersionManifestFileName = "global_version.info";

        /// <summary>
        /// 本地文件服务器缓存目录。
        /// 用来保存上次成功读取的组件元数据，支持断网回退和减少重复拉取 assets.info。
        /// </summary>
        private const string FileServerCacheFolderName = "version_cache";

        /// <summary>
        /// CI BatchMode 独立下载根目录。
        /// 该目录只服务于文件服务器真实下载验证，避免为了准备旧版 FIRST/SECOND_LOAD_DIR 提前触发
        /// <see cref="ClientAssetsUtils"/> 的 BetterStreamingAssets 初始化。
        /// </summary>
        private const string FileServerBatchVerificationRootFolderName = "file_server_verify";

        /// <summary>
        /// 文件服务器协议使用的 package_build.info 文件名。
        /// 这里保持为局部常量，避免 CI 验证路径因为访问 <see cref="ClientAssetsUtils"/> 的静态字段而触发类型初始化。
        /// </summary>
        private const string FileServerPackageBuildInfoPath = "package_build.info";

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

        /// <summary>
        /// 文件服务器文本元数据请求的单次超时时间。
        /// 避免 global_version.info / assets.info 等文本请求在 CI 上无限等待。
        /// </summary>
        private static readonly TimeSpan FileServerTextRequestTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 文件服务器二进制资源下载的单次超时时间。
        /// 避免某个资源连接挂起时把整个 TeamCity BatchMode 长时间卡住。
        /// </summary>
        private static readonly TimeSpan FileServerAssetRequestTimeout = TimeSpan.FromSeconds(180);

        private static readonly FileServerComponentKind[] FileServerManagedComponents =
        {
            FileServerComponentKind.Code,
            FileServerComponentKind.AssetBundle,
            FileServerComponentKind.Table,
        };

        /// <summary>
        /// 通过进程命令行判断当前是否处于 Unity BatchMode。
        /// 这里不能访问 <see cref="Application.isBatchMode"/>，因为文件服务器批验证会在线程池里运行。
        /// </summary>
        private static bool IsUnityBatchModeProcess()
        {
            return (Environment.CommandLine ?? string.Empty).IndexOf("-batchmode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 统一输出文件服务器流程日志。
        /// BatchMode 下会额外镜像到 Unity 控制台，确保 TeamCity 可以直接看到关键阶段、警告和错误。
        /// </summary>
        private static void LogFileServerFlow(string message, Color color)
        {
            BDebug.Log(LogTag, $"[FileServer] {message}", color);
            if (!IsUnityBatchModeProcess())
            {
                return;
            }

            var formattedMessage = $"[CI][FileServer] {message}";
            if (color == Color.red)
            {
                Debug.LogError(formattedMessage);
                return;
            }

            if (color == Color.yellow)
            {
                Debug.LogWarning(formattedMessage);
                return;
            }

            Debug.Log(formattedMessage);
        }

        /// <summary>
        /// 生成 CI BatchMode 文件服务器阶段进度日志，统一输出阶段名、序号和当前目标路径，避免 TeamCity 上只看到“开始了但不知道卡在哪”。
        /// </summary>
        internal static string FormatFileServerBatchProgressMessage(string stageName,
            int currentIndex,
            int totalCount,
            string currentTargetPath,
            string extraInfo = null)
        {
            var normalizedStageName = string.IsNullOrWhiteSpace(stageName) ? "unknown" : stageName.Trim();
            var normalizedIndex = currentIndex < 0 ? 0 : currentIndex;
            var normalizedTotal = totalCount < 0 ? 0 : totalCount;
            var normalizedTargetPath = string.IsNullOrWhiteSpace(currentTargetPath)
                ? "<none>"
                : currentTargetPath.Trim();
            var normalizedExtraInfo = string.IsNullOrWhiteSpace(extraInfo) ? string.Empty : $" {extraInfo.Trim()}";
            return $"{normalizedStageName} progress={normalizedIndex}/{normalizedTotal} target={normalizedTargetPath}{normalizedExtraInfo}";
        }

        /// <summary>
        /// 输出 CI BatchMode 细粒度阶段进度日志。
        /// 统一通过同一格式输出，便于 TeamCity 日志检索与人工定位当前卡住的资源或阶段。
        /// </summary>
        private static void LogFileServerBatchProgress(string stageName,
            int currentIndex,
            int totalCount,
            string currentTargetPath,
            Color color,
            string extraInfo = null)
        {
            if (!IsUnityBatchModeProcess())
            {
                return;
            }

            LogFileServerFlow(
                FormatFileServerBatchProgressMessage(stageName, currentIndex, totalCount, currentTargetPath,
                    extraInfo),
                color);
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
        /// 显式文件服务器 BatchMode 验证入口（DevOps 模式）。
        /// 该同步桥接 API 供 Editor/CI 在 batchmode 下直接调用，内部会把真实下载验证放到线程池执行，
        /// 避免 Unity 主线程同步等待异步流程时被同步上下文卡住。
        /// </summary>
        /// <example>
        /// TeamCity 的 Unity BatchMode 入口可以显式调用：
        /// <code>
        /// var result = BResources.VerifyFileServerAssetsForBatchModeWithDevOps(
        ///     new AssetsVersionController.FileServerBatchVerificationRequest()
        ///     {
        ///         ServerUrl = serverUrl,
        ///         ExpectedVersionInfo = new AssetsVersionController.FileServerVersionInfo()
        ///         {
        ///             CodeVersion = codeBuild,
        ///             AssetBundleVersion = assetbundleBuild,
        ///             TableVersion = tableBuild,
        ///         },
        ///     });
        /// </code>
        /// </example>
        public FileServerBatchVerificationResult VerifyFileServerAssetsForBatchModeWithDevOps(
            FileServerBatchVerificationRequest request)
        {
            BDebug.EnableLog(LogTag);
            try
            {
                var result = Task.Run(() => VerifyFileServerAssetsForBatchMode(request)).GetAwaiter().GetResult();
                if (!result.IsSuccess)
                {
                    return result;
                }

                return FinalizeFileServerBatchVerificationOnMainThread(request, result);
            }
            catch (Exception exception)
            {
                LogFileServerFlow($"CI BatchMode 文件服务器验证异常 err={exception}", Color.red);
                return new FileServerBatchVerificationResult()
                {
                    ExpectedVersion = request?.ExpectedVersionInfo?.RawValue ?? string.Empty,
                    Error = $"文件服务器批量验证异常:{exception.Message}",
                };
            }
        }

        /// <summary>
        /// 在同步 batchmode 桥接入口里收口所有必须运行在 Unity 主线程上的最终校验。
        /// 前半段真实下载和全量 hash 校验仍然保留在线程池中执行，等后台阶段完成后再回到当前主线程执行
        /// 代表性 AssetBundle 本地打开和 package_build.info 终检，避免主线程被 <see cref="Task.Run(Func{Task})"/> 阻塞时再去反向投递导致超时。
        /// </summary>
        private FileServerBatchVerificationResult FinalizeFileServerBatchVerificationOnMainThread(
            FileServerBatchVerificationRequest request,
            FileServerBatchVerificationResult result)
        {
            LogFileServerFlow("文件服务器后台下载与全量校验完成，开始主线程代表性本地加载校验。", Color.green);

            var representativeLoadError = ValidateFileServerRepresentativeLocalLoads(
                    result.CodeAssetLocalPath,
                    result.AssetBundleAssetLocalPaths,
                    result.TableAssetLocalPath)
                .GetAwaiter()
                .GetResult();
            if (!string.IsNullOrEmpty(representativeLoadError))
            {
                result.Error = representativeLoadError;
                return result;
            }

            result.PackageBuildInfo = LoadLocalPackageBuildInfo(result.FirstLoadDir, false);
            var packageBuildInfoError = ValidateFileServerPackageBuildInfo(
                result.PackageBuildInfo,
                request?.ExpectedVersionInfo ?? new FileServerVersionInfo());
            if (!string.IsNullOrEmpty(packageBuildInfoError))
            {
                result.Error = packageBuildInfoError;
                return result;
            }

            LogFileServerFlow(
                $"CI BatchMode 文件服务器验证完成 version={result.ActualVersion} code={result.CodeAssetLocalPath} abFirst={result.AssetBundleAssetLocalPath} abCount={result.AssetBundleAssetLocalPaths.Count} table={result.TableAssetLocalPath}",
                Color.green);
            return result;
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
                "当前服务器未提供文件服务器版控入口: global_version.info");
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
            onError?.Invoke("当前服务器未提供文件服务器版控入口: global_version.info");
        }

        /// <summary>
        /// 解析共享三段版控文件 <c>code.assetbundle.table</c>。
        /// </summary>
        internal static bool TryParseFileServerVersionInfo(string content, out FileServerVersionInfo versionInfo)
        {
            return AssetsVersionControllerDevOpsPureLogic.TryParseFileServerVersionInfo(content, out versionInfo);
        }

        /// <summary>
        /// 从 global_version.info JSON 中按平台提取三段版控。
        /// </summary>
        internal static bool TryParseGlobalVersionInfoJson(string content, string platform,
            out FileServerVersionInfo versionInfo)
        {
            return AssetsVersionControllerDevOpsPureLogic.TryParseGlobalVersionInfoJson(content, platform,
                out versionInfo);
        }

        /// <summary>
        /// 把三段版控对象序列化回共享文本格式。
        /// </summary>
        internal static string FormatFileServerVersionInfo(FileServerVersionInfo versionInfo)
        {
            return AssetsVersionControllerDevOpsPureLogic.FormatFileServerVersionInfo(versionInfo);
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
            return AssetsVersionControllerDevOpsPureLogic.MergeFileServerPackageBuildInfo(baseInfo, codeInfo,
                assetBundleInfo, tableInfo);
        }

        /// <summary>
        /// 规范化单个组件携带的 <c>package_build.info</c>。
        /// 文件服务器协议的权威组件版本来自共享版控和组件目录名，因此当显式元数据仍保留旧默认值 <c>none</c> 时，
        /// 这里会用当前组件版本兜底，避免最终回写的本地 <c>package_build.info</c> 丢失三段版本号。
        /// </summary>
        internal static ClientPackageBuildInfo NormalizeFileServerComponentPackageBuildInfo(
            FileServerComponentKind componentKind,
            string componentVersion,
            ClientPackageBuildInfo sourceInfo)
        {
            return AssetsVersionControllerDevOpsPureLogic.NormalizeFileServerComponentPackageBuildInfo(componentKind,
                componentVersion, sourceInfo);
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
            return AssetsVersionControllerDevOpsPureLogic.BuildFileServerSubPackageAssetItems(subPackageConfig,
                assetBundleAssets, codeAssets, tableAssets);
        }

        /// <summary>
        /// 校验 TeamCity 当前链路期望的共享三段版本号是否与远端 <c>global_version.info</c> 中当前平台的 <c>version_num</c> 一致。
        /// </summary>
        internal static string ValidateExpectedFileServerVersionInfo(
            FileServerVersionInfo expectedVersionInfo,
            FileServerVersionInfo actualVersionInfo)
        {
            return AssetsVersionControllerDevOpsPureLogic.ValidateExpectedFileServerVersionInfo(expectedVersionInfo,
                actualVersionInfo);
        }

        /// <summary>
        /// 校验下载完成后本地重建的 <c>package_build.info</c> 是否已经回写为当前 TeamCity 链路期望的三段版本号。
        /// </summary>
        internal static string ValidateFileServerPackageBuildInfo(
            ClientPackageBuildInfo packageBuildInfo,
            FileServerVersionInfo expectedVersionInfo)
        {
            return AssetsVersionControllerDevOpsPureLogic.ValidateFileServerPackageBuildInfo(packageBuildInfo,
                expectedVersionInfo);
        }

        /// <summary>
        /// 从单个组件的资源清单里挑出最能代表“这类资源真的可用”的验证样本。
        /// Code 优先挑脚本热更 payload，AssetBundle 优先挑真实 art_assets bundle，Table 优先挑 local.db。
        /// </summary>
        internal static AssetItem FindFileServerRepresentativeAsset(
            FileServerComponentKind componentKind,
            List<AssetItem> assetItems)
        {
            return AssetsVersionControllerDevOpsPureLogic.FindFileServerRepresentativeAsset(componentKind, assetItems);
        }

        /// <summary>
        /// 从 art_assets.info 的解析结果里提取去重后的 AssetBundle 相对路径，供 CI BatchMode 做逐个 LoadFromFile 校验。
        /// </summary>
        internal static List<string> CollectFileServerAssetBundleValidationRelativePaths(
            IEnumerable<AssetBundleItem> assetBundleItems)
        {
            return AssetsVersionControllerDevOpsPureLogic.CollectFileServerAssetBundleValidationRelativePaths(
                assetBundleItems);
        }

        /// <summary>
        /// 从当前本地下载目录的 art_assets.info 解析出需要逐个本地打开校验的 AssetBundle 绝对路径。
        /// 这里故意依赖运行时同源的 AssetBundleConfigLoader，避免 CI 校验和正式加载路径对配置解析出现分叉。
        /// </summary>
        private static List<string> ResolveFileServerAssetBundleValidationLocalPaths(string firstLoadDir)
        {
            if (string.IsNullOrEmpty(firstLoadDir) || !Directory.Exists(firstLoadDir))
            {
                return new List<string>();
            }

            var configLoader = new AssetBundleConfigLoader();
            configLoader.Load(firstLoadDir);
            return CollectFileServerAssetBundleValidationRelativePaths(configLoader.AssetbundleItemList)
                .Select(relativePath => IPath.Combine(firstLoadDir, relativePath))
                .ToList();
        }

        /// <summary>
        /// CI BatchMode 的显式文件服务器下载验证入口。
        /// 这条路径会强制读取远端共享版控、重置本地缓存、真实下载 Code/AssetBundle/Table 三类差异资源，
        /// 再校验本地元数据和代表性样本文件，确保 TeamCity 验证不是被旧缓存或母包残留短路。
        /// </summary>
        internal async Task<FileServerBatchVerificationResult> VerifyFileServerAssetsForBatchMode(
            FileServerBatchVerificationRequest request)
        {
            var result = new FileServerBatchVerificationResult();
            if (request == null)
            {
                result.Error = "文件服务器批量验证请求为空。";
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.ServerUrl))
            {
                result.Error = "文件服务器批量验证缺少 serverUrl。";
                return result;
            }

            LogFileServerFlow($"开始 CI BatchMode 文件服务器验证 url={request.ServerUrl} targetPlatform={request.TargetPlatform}", Color.cyan);

            // Phase 1: 先解析共享版控，并把 TeamCity 期望版本号与远端 version.info 对齐。
            // 使用请求中显式指定的目标平台，而不是 BApplication.RuntimePlatform（在 Windows 编辑器下不会因 -buildTarget 改变）。
            var resolveResult = await ResolveFileServerVersionInfo(request.ServerUrl, request.TargetPlatform, useIsolatedLocalLoadDir: true);
            result.Platform = resolveResult.Platform;
            result.PlatformPath = resolveResult.PlatformPath;
            result.FirstLoadDir = resolveResult.FirstLoadDir;
            result.ExpectedVersion = request.ExpectedVersionInfo?.RawValue ?? string.Empty;
            result.ActualVersion = resolveResult.VersionInfo?.RawValue ?? string.Empty;
            result.UsedLocalFallbackVersion = resolveResult.UsedLocalFallbackVersion;

            if (!resolveResult.IsFileServerProtocol)
            {
                result.Error = "当前服务器未提供文件服务器共享版控入口。";
                return result;
            }

            if (!string.IsNullOrEmpty(resolveResult.Error))
            {
                result.Error = resolveResult.Error;
                return result;
            }

            if (resolveResult.UsedLocalFallbackVersion)
            {
                result.Error = "文件服务器共享版控回退到了本地缓存，本次没有执行真实远端验证。";
                return result;
            }

            var versionError = ValidateExpectedFileServerVersionInfo(request.ExpectedVersionInfo,
                resolveResult.VersionInfo);
            if (!string.IsNullOrEmpty(versionError))
            {
                result.Error = versionError;
                return result;
            }

            // Phase 2: 验证任务必须强制清空本地下载态，避免被历史 persistent/缓存短路成“假下载”。
            if (request.ResetLocalStateBeforeVerify)
            {
                LogFileServerFlow($"重置文件服务器本地下载目录 dir={resolveResult.FirstLoadDir}", Color.yellow);
                ResetFileServerLocalState(resolveResult.FirstLoadDir);
                resolveResult.CacheDir = IPath.Combine(resolveResult.FirstLoadDir, FileServerCacheFolderName);
                Directory.CreateDirectory(resolveResult.CacheDir);
                resolveResult.State = new FileServerVersionControllerState();
            }

            // Phase 3: 对 Code / AssetBundle / Table 分别读取远端上下文，并先挑出 Code / Table 的代表性验证样本。
            var componentContexts = new Dictionary<FileServerComponentKind, FileServerComponentContext>();
            var allowComponentContextCacheFallback = ShouldUseCachedFileServerComponentContextOnFailure(
                useCacheOnFailure: true, strictRemoteVerification: true);
            foreach (var componentKind in FileServerManagedComponents)
            {
                var remoteVersion = resolveResult.VersionInfo.GetVersion(componentKind);
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    continue;
                }

                var loadContextResult = await LoadFileServerComponentContext(request.ServerUrl,
                    resolveResult.PlatformPath, componentKind, remoteVersion, false,
                    allowComponentContextCacheFallback);
                if (!string.IsNullOrEmpty(loadContextResult.Item1))
                {
                    result.Error = loadContextResult.Item1;
                    return result;
                }

                componentContexts[componentKind] = loadContextResult.Item2;
                SaveFileServerComponentCache(resolveResult.CacheDir, loadContextResult.Item2);
                LogFileServerFlow(
                    $"组件上下文加载完成 component={componentKind} version={remoteVersion} assetCount={loadContextResult.Item2.AssetItems.Count}",
                    Color.green);
            }

            if (!componentContexts.TryGetValue(FileServerComponentKind.Code, out var codeContext))
            {
                result.Error = "文件服务器验证缺少热更代码组件上下文。";
                return result;
            }

            if (!componentContexts.TryGetValue(FileServerComponentKind.AssetBundle, out var assetBundleContext))
            {
                result.Error = "文件服务器验证缺少 AssetBundle 组件上下文。";
                return result;
            }

            if (!componentContexts.TryGetValue(FileServerComponentKind.Table, out var tableContext))
            {
                result.Error = "文件服务器验证缺少表格组件上下文。";
                return result;
            }

            var codeAsset = FindFileServerRepresentativeAsset(FileServerComponentKind.Code, codeContext.AssetItems);
            var tableAsset = FindFileServerRepresentativeAsset(FileServerComponentKind.Table, tableContext.AssetItems);
            if (codeAsset == null || tableAsset == null)
            {
                result.Error = "文件服务器验证缺少 Code / Table 的代表性资源样本。";
                return result;
            }

            LogFileServerFlow(
                $"代表性资源样本已选中 code={codeAsset.LocalPath} assetBundle=defer-to-art_assets.info table={tableAsset.LocalPath}",
                Color.green);

            result.CodeAssetLocalPath = IPath.Combine(resolveResult.FirstLoadDir, codeAsset.LocalPath);
            result.TableAssetLocalPath = IPath.Combine(resolveResult.FirstLoadDir, tableAsset.LocalPath);

            // Phase 4: 强制按远端结果重建差异列表并执行真实下载，再重建本地 package_build.info / assets.info。
            var downloadItems = new List<FileServerDownloadItem>();
            foreach (var componentPair in componentContexts)
            {
                downloadItems.AddRange(BuildFileServerDownloadItems(request.ServerUrl, resolveResult.PlatformPath,
                    resolveResult.FirstLoadDir, componentPair.Key, componentPair.Value, false, null, resolveResult,
                    true));
            }

            LogFileServerFlow($"文件服务器批量验证开始真实下载 count={downloadItems.Count}", Color.cyan);
            var downloadResult = await DownloadFileServerAssets(downloadItems, null);
            if (downloadResult.Item1.Count > 0)
            {
                result.Error = $"文件服务器批量验证存在下载失败资源 count={downloadResult.Item1.Count}";
                return result;
            }

            LogFileServerFlow($"开始重建文件服务器本地元数据 componentCount={componentContexts.Count}", Color.cyan);
            RebuildFileServerLocalMetadata(resolveResult.FirstLoadDir, resolveResult.CacheDir, componentContexts,
                allowBasePackageFallback: false);
            foreach (var componentPair in componentContexts)
            {
                SetFileServerStateComponentVersion(resolveResult.State, componentPair.Key, componentPair.Value.Version);
                resolveResult.State.ManagedFiles[GetFileServerComponentKey(componentPair.Key)] =
                    GetFileServerManagedFiles(componentPair.Value, false, null);
            }

            resolveResult.State.InstalledVersion = resolveResult.VersionInfo.RawValue;
            resolveResult.State.LastKnownRemoteVersion = resolveResult.VersionInfo.RawValue;
            LogFileServerFlow($"开始回写文件服务器本地状态 installedVersion={resolveResult.State.InstalledVersion}", Color.cyan);
            SaveFileServerState(resolveResult.CacheDir, resolveResult.State);
            LogFileServerFlow($"文件服务器本地状态回写完成 managedComponentCount={resolveResult.State.ManagedFiles.Count}", Color.green);

            // Phase 5: 先做全量 hash/存在性校验，再确认 package_build.info 已经落成当前链路的三段版本号。
            var validateError = await ValidateFileServerManagedAssets(resolveResult.FirstLoadDir,
                resolveResult.Platform, componentContexts, false, null, null,
                preferLocalDownloadedAssetsOnly: true);
            if (!string.IsNullOrEmpty(validateError))
            {
                result.Error = validateError;
                return result;
            }

            var assetBundleAssetLocalPaths = ResolveFileServerAssetBundleValidationLocalPaths(resolveResult.FirstLoadDir);
            if (assetBundleAssetLocalPaths.Count <= 0)
            {
                result.Error = "文件服务器验证未能从 art_assets.info 解析出任何 AssetBundle 加载样本。";
                return result;
            }

            result.AssetBundleAssetLocalPaths = assetBundleAssetLocalPaths;
            result.AssetBundleAssetLocalPath = assetBundleAssetLocalPaths[0];
            LogFileServerFlow(
                $"AssetBundle 本地加载样本已根据 art_assets.info 解析 count={assetBundleAssetLocalPaths.Count} first={result.AssetBundleAssetLocalPath}",
                Color.green);

            LogFileServerFlow("文件服务器全量资源校验通过，等待主线程执行代表性本地加载校验。", Color.green);
            return result;
        }

        /// <summary>
        /// 尝试执行文件服务器协议总入口。
        /// 该方法会先解析共享版控文件，再根据是否请求子包把流程分发到全量或子包协调方法。
        /// </summary>
        /// <returns><c>false</c> 表示当前服务器未启用文件服务器协议；<c>true</c> 表示已消费该请求并返回了结果。</returns>
        private async Task<bool> TryStartFileServerVersionControl(
            UpdateMode updateMode,
            string serverUrl,
            string subPackageName,
            Action<AssetItem, List<AssetItem>> onDownloadProccess,
            Action<RetStatus, string> onTaskEndCallback)
        {
            // Phase 1: 读取共享版控入口，并根据远端结果或本地回退版本建立本次运行时上下文。
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

            // Phase 2: 共享版控解析成功后，再按“全量 / 子包”两种入口分派到各自协调流程。
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

        /// <summary>
        /// 尝试读取文件服务器协议下的子包配置列表。
        /// 它会先解析共享版控，再从 Code / AssetBundle 组件里加载缓存或远端子包配置。
        /// </summary>
        /// <returns><c>false</c> 表示服务器未启用文件服务器协议；<c>true</c> 表示已经返回成功或失败结果。</returns>
        private async Task<bool> TryGetFileServerSubPackageInfos(
            string serverUrl,
            Action<Dictionary<string, string>> callback,
            Action<string> onError)
        {
            // Phase 1: 共享版控入口决定当前应该读取哪一个构建号下的子包配置。
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

            // Phase 2: 从组件上下文里取出子包配置，并统一映射回旧入口仍使用的 server_assets_subpack_xxx.info 名称。
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

        /// <summary>
        /// 执行文件服务器协议的全量资源版控流程。
        /// 主要阶段包括：组件元数据准备、差异资源计算、下载、package_build.info 合并、旧资源清理和最终校验。
        /// </summary>
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

            // Phase 1: 非修复模式下，如果三段版本完全一致，就直接结束，不重复计算差异资源。
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

            // Phase 2: 为 Code / AssetBundle / Table 逐个准备远端上下文，并缓存本地元数据副本。
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
            // Phase 3: 下载差异资源，随后合并 package_build.info、清理旧文件并执行最终一致性校验。
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

        /// <summary>
        /// 执行文件服务器协议的子包资源版控流程。
        /// 它只筛选子包声明里真正需要的 Code / AssetBundle / Table 资源，并把结果落盘到旧入口仍消费的子包配置文件。
        /// </summary>
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

            // Phase 1: 先读取子包配置定义，并把请求名规范化为统一比较格式。
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

            // Phase 2: 准备三类组件上下文，再按子包声明从全量清单里筛出真正需要的资源。
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

            // Phase 3: 下载子包差异资源，回写旧入口消费的子包文件，并同步本地状态缓存。
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

        /// <summary>
        /// 解析文件服务器共享版控入口，并在远端不可用时回退到本地缓存状态。
        /// </summary>
        private async Task<FileServerResolveResult> ResolveFileServerVersionInfo(string serverUrl,
            RuntimePlatform? targetPlatform = null,
            bool useIsolatedLocalLoadDir = false)
        {
            // CI BatchMode 验证需要显式传入目标平台；未指定时回退到 BApplication.RuntimePlatform（编辑器本机行为）。
            var runtimePlatform = targetPlatform ?? BApplication.RuntimePlatform;
            var platformPath = BApplication.GetPlatformLoadPath(runtimePlatform);
            var firstLoadDir = EnsureFileServerLocalLoadDir(runtimePlatform, useIsolatedLocalLoadDir).Item1;
            var cacheDir = IPath.Combine(firstLoadDir, FileServerCacheFolderName);
            Directory.CreateDirectory(cacheDir);

            var state = LoadFileServerState(cacheDir);
            var versionUrl = BuildFileServerVersionManifestUrl(serverUrl);

            // Phase 1: 优先请求远端共享版控文件，确认当前服务器是否启用了文件服务器协议。
            LogFileServerFlow($"开始请求共享版控文件 url={versionUrl}", Color.cyan);

            var manifestDownloadResult = await DownloadTextWithRetry(versionUrl);
            if (manifestDownloadResult.Item1)
            {
                // 解析 global_version.info JSON，按当前平台提取 version_num 再拆分为三段版控。
                if (!AssetsVersionControllerDevOpsPureLogic.TryParseGlobalVersionInfoJson(
                        manifestDownloadResult.Item3, platformPath, out var remoteVersionInfo))
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

            // Phase 2: 远端请求失败时，若本地缓存里有上次成功三段版本，就按本地临时版本回退。
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

            // Phase 3: 既没有远端入口，也没有本地可用回退版本时，返回“当前服务器未启用新协议”或直接失败。
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

        /// <summary>
        /// 读取文件服务器协议里的子包配置列表。
        /// 如果远端读取失败且允许使用缓存，就回退到本地缓存的 assets_subpack.info 文本。
        /// </summary>
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

        /// <summary>
        /// 为不再显式上传 package_build.info 的组件生成最小可合并元数据。
        /// 这里用组件版本回填对应字段，保证最终回写到本地的 package_build.info 仍能通过三段版控校验。
        /// </summary>
        private static void PopulateSyntheticFileServerComponentPackageBuildInfo(FileServerComponentContext context)
        {
            if (context == null)
            {
                return;
            }

            context.PackageBuildInfo = NormalizeFileServerComponentPackageBuildInfo(context.ComponentKind,
                context.Version,
                new ClientPackageBuildInfo());
            context.PackageBuildContent = JsonMapper.ToJson(context.PackageBuildInfo);
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

            var packageBuildDownload = await DownloadTextWithRetry(CombineUrl(remoteRoot,
                FileServerPackageBuildInfoPath));
            if (!packageBuildDownload.Item1)
            {
                if (componentKind == FileServerComponentKind.AssetBundle)
                {
                    PopulateSyntheticFileServerComponentPackageBuildInfo(context);
                    LogFileServerFlow(
                        $"文件服务器组件元数据缺少 package_build.info，使用构建号生成占位元数据 component={componentKind} version={componentVersion}",
                        Color.yellow);
                }
                else
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
            }

            else
            {
                try
                {
                    context.PackageBuildContent = packageBuildDownload.Item3;
                    context.PackageBuildInfo = NormalizeFileServerComponentPackageBuildInfo(componentKind,
                        componentVersion,
                        JsonMapper.ToObject<ClientPackageBuildInfo>(packageBuildDownload.Item3)
                        ?? new ClientPackageBuildInfo());
                }
                catch (Exception e)
                {
                    if (componentKind == FileServerComponentKind.AssetBundle)
                    {
                        PopulateSyntheticFileServerComponentPackageBuildInfo(context);
                        LogFileServerFlow(
                            $"文件服务器组件元数据解析失败，使用构建号生成占位元数据 component={componentKind} version={componentVersion} err={e.Message}",
                            Color.yellow);
                    }
                    else
                    {
                        return new Tuple<string, FileServerComponentContext>($"文件服务器解析 package_build.info 失败:{e.Message}",
                            null);
                    }
                }
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
                    context.AssetItems = NormalizeFileServerManagedAssetItems(assetItems);
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

        /// <summary>
        /// 并发下载文件服务器差异资源，并把失败清单与是否需要重启的结果统一返回。
        /// </summary>
        private async Task<Tuple<List<FileServerDownloadItem>, bool>> DownloadFileServerAssets(
            List<FileServerDownloadItem> downloadItems,
            Action<AssetItem, List<AssetItem>> onDownloadProccess)
        {
            var failedItems = new ConcurrentBag<FileServerDownloadItem>();
            var downloadedItems = new ConcurrentBag<FileServerDownloadItem>();
            var processedItemCount = 0;
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
                            var completedCount = Interlocked.Increment(ref processedItemCount);
                            LogFileServerBatchProgress("下载资源",
                                completedCount,
                                downloadItems.Count,
                                item.AssetItem?.LocalPath,
                                Color.cyan,
                                $"component={item.ComponentKind}");
                            if (IsRestartRequiredAsset(item.AssetItem))
                            {
                                Interlocked.Exchange(ref needRestart, 1);
                            }
                        }
                        else
                        {
                            failedItems.Add(item);
                            var completedCount = Interlocked.Increment(ref processedItemCount);
                            LogFileServerBatchProgress("下载资源失败",
                                completedCount,
                                downloadItems.Count,
                                item.AssetItem?.LocalPath,
                                Color.red,
                                $"component={item.ComponentKind}");
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

        /// <summary>
        /// 下载单个文件服务器资源，并在 hash 校验通过后以原子替换方式落到本地。
        /// </summary>
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
                        bytes = await AwaitFileServerRequestWithTimeout(
                            client.DownloadDataTaskAsync(downloadItem.RemoteUrl),
                            FileServerAssetRequestTimeout,
                            downloadItem.RemoteUrl,
                            client.CancelAsync);
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
                    LogFileServerFlow(
                        $"文件服务器资源下载重试 attempt={retryIndex + 1}/{RETRY_COUNT} url={downloadItem.RemoteUrl} err={err}",
                        retryIndex == RETRY_COUNT - 1 ? Color.red : Color.yellow);
                }
            }

            if (!string.IsNullOrEmpty(err) || bytes == null)
            {
                BDebug.LogError($"文件服务器资源下载失败:{downloadItem.RemoteUrl} err:{err}");
                return false;
            }

            if (onDownloadProccess != null)
            {
                await UniTask.SwitchToMainThread();
                onDownloadProccess.Invoke(downloadItem.AssetItem, allDownloadAssets);
                await UniTask.SwitchToThreadPool();
            }

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

        /// <summary>
        /// 为单个文件服务器网络请求施加显式超时，并在超时后执行调用方提供的取消动作。
        /// 这样 CI 遇到半开连接或无响应 socket 时，会以明确异常结束，而不是无限挂起。
        /// 同时避免在 Unity 主线程用同步等待结果时捕获编辑器上下文，导致超时分支续体无法恢复。
        /// </summary>
        internal static async Task<T> AwaitFileServerRequestWithTimeout<T>(
            Task<T> requestTask,
            TimeSpan timeout,
            string requestUrl,
            Action onTimeout = null)
        {
            if (requestTask == null)
            {
                throw new ArgumentNullException(nameof(requestTask));
            }

            if (timeout <= TimeSpan.Zero)
            {
                return await requestTask.ConfigureAwait(false);
            }

            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(requestTask, timeoutTask).ConfigureAwait(false);
            if (completedTask == requestTask)
            {
                return await requestTask.ConfigureAwait(false);
            }

            try
            {
                onTimeout?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】【FileServer】请求超时后的取消回调失败 url={requestUrl} err={exception.Message}");
            }

            throw new TimeoutException(
                $"文件服务器请求超时 timeout={timeout.TotalSeconds:0}s url={requestUrl}");
        }

        /// <summary>
        /// 根据组件上下文生成本次需要下载的文件服务器资源清单。
        /// Code / AssetBundle 远端目录使用 hash 文件名存储 payload，所以这里要把远端下载路径和本地落盘路径显式区分开。
        /// </summary>
        private List<FileServerDownloadItem> BuildFileServerDownloadItems(
            string serverUrl,
            string platformPath,
            string firstLoadDir,
            FileServerComponentKind componentKind,
            FileServerComponentContext componentContext,
            bool isSubPackageMode,
            List<AssetItem> selectedAssets,
            FileServerResolveResult resolveResult,
            bool forceRemoteDownload = false)
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
                if (forceRemoteDownload
                    || !ClientAssetsUtils.IsExsitAssetWithCheckHash(resolveResult.Platform, assetItem.LocalPath,
                        assetItem.HashName))
                {
                    downloadItems.Add(new FileServerDownloadItem()
                    {
                        ComponentKind = componentKind,
                        AssetItem = assetItem,
                        FinalLocalPath = IPath.Combine(firstLoadDir, assetItem.LocalPath),
                        RemoteUrl = CombineUrl(remoteRoot, BuildFileServerAssetRemoteRelativePath(assetItem)),
                        RequireHashValidation = true,
                    });
                }
            }

            return downloadItems;
        }

        /// <summary>
        /// 生成文件服务器协议里单个资源的远端相对路径。
        /// Code / AssetBundle 上传到文件服务器时使用 <c>HashName</c> 作为远端文件名，只有没有 hash 的资源才直接使用 <c>LocalPath</c>。
        /// </summary>
        internal static string BuildFileServerAssetRemoteRelativePath(AssetItem assetItem)
        {
            if (assetItem == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(assetItem.HashName))
            {
                return assetItem.HashName;
            }

            return assetItem.LocalPath ?? string.Empty;
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
            Action<RetStatus, string> onTaskEndCallback,
            bool preferLocalDownloadedAssetsOnly = false)
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

            var targetAssetList = targetAssets.ToList();
            LogFileServerFlow(
                $"开始文件服务器资源校验 total={targetAssetList.Count} subPackageMode={isSubPackageMode} localDownloadedOnly={preferLocalDownloadedAssetsOnly}",
                Color.cyan);

            var validatedCount = 0;

            foreach (var assetItem in targetAssetList)
            {
                validatedCount++;
                LogFileServerBatchProgress("校验资源", validatedCount, targetAssetList.Count, assetItem.LocalPath,
                    Color.cyan);

                var isValid = true;
                if (string.IsNullOrEmpty(assetItem.HashName))
                {
                    var localPath = IPath.Combine(firstLoadDir, assetItem.LocalPath);
                    isValid = File.Exists(localPath);
                }
                else if (preferLocalDownloadedAssetsOnly)
                {
                    isValid = IsFileServerDownloadedAssetValid(firstLoadDir, assetItem);
                }
                else
                {
                    isValid = ClientAssetsUtils.IsExsitAssetWithCheckHash(platform, assetItem.LocalPath,
                        assetItem.HashName);
                }

                if (onTaskEndCallback != null)
                {
                    await UniTask.SwitchToMainThread();
                    onTaskEndCallback.Invoke(RetStatus.Checkassets, assetItem.LocalPath);
                    await UniTask.SwitchToThreadPool();
                }
                if (!isValid)
                {
                    errors.Add(assetItem.LocalPath);
                    LogFileServerFlow($"文件服务器资源校验命中失败 target={assetItem.LocalPath}", Color.red);
                }
            }

            if (errors.Count == 0)
            {
                LogFileServerFlow($"文件服务器资源校验完成 total={targetAssetList.Count}", Color.green);
                return null;
            }

            LogFileServerFlow($"文件服务器资源校验失败 failedCount={errors.Count}", Color.red);
            return "文件服务器资源校验失败:\n" + string.Join("\n", errors);
        }

        /// <summary>
        /// 对下载完成后的三类代表性资源执行真实本地打开校验。
        /// Code 走程序集装载探测，AssetBundle 走 <see cref="AssetBundle.LoadFromFile(string)"/>，Table 走 SQLite 只读打开，
        /// 这样可以把“文件存在但本地无法打开”的问题和纯下载/hash 问题区分开。
        /// </summary>
        internal async Task<string> ValidateFileServerRepresentativeLocalLoads(
            string codeAssetLocalPath,
            IReadOnlyList<string> assetBundleAssetLocalPaths,
            string tableAssetLocalPath)
        {
            var firstAssetBundleLocalPath = assetBundleAssetLocalPaths != null && assetBundleAssetLocalPaths.Count > 0
                ? assetBundleAssetLocalPaths[0]
                : string.Empty;
            var assetBundleCount = assetBundleAssetLocalPaths?.Count ?? 0;

            LogFileServerBatchProgress("代表性本地加载", 1, 3, codeAssetLocalPath, Color.cyan, "component=Code");
            var codeLoadError = ValidateFileServerCodeRepresentativeLocalLoad(codeAssetLocalPath);
            if (!string.IsNullOrEmpty(codeLoadError))
            {
                return codeLoadError;
            }

            LogFileServerFlow($"代表性本地加载通过 component=Code target={codeAssetLocalPath}", Color.green);

            LogFileServerBatchProgress("代表性本地加载", 2, 3, firstAssetBundleLocalPath, Color.cyan,
                $"component=AssetBundle bundleCount={assetBundleCount}");
            var assetBundleLoadError =
                await ValidateFileServerAssetBundleLocalLoadsOnUnityContext(assetBundleAssetLocalPaths);
            if (!string.IsNullOrEmpty(assetBundleLoadError))
            {
                return assetBundleLoadError;
            }

            LogFileServerFlow(
                $"代表性本地加载通过 component=AssetBundle count={assetBundleCount} first={firstAssetBundleLocalPath}",
                Color.green);

            LogFileServerBatchProgress("代表性本地加载", 3, 3, tableAssetLocalPath, Color.cyan, "component=Table");
            var tableLoadError = ValidateFileServerTableRepresentativeLocalLoad(tableAssetLocalPath);
            if (!string.IsNullOrEmpty(tableLoadError))
            {
                return tableLoadError;
            }

            LogFileServerFlow($"代表性本地加载通过 component=Table target={tableAssetLocalPath}", Color.green);
            LogFileServerFlow("代表性本地加载校验全部完成。", Color.green);

            return null;
        }

        /// <summary>
        /// 校验代表性的热更代码 payload 是否能按运行时同源逻辑完成程序集装载。
        /// 这里只做一次最小化 <see cref="Assembly.Load(byte[])"/> 探测，不执行任何业务入口，用于识别“下载成功但 DLL 字节不可装载”的情况。
        /// </summary>
        internal static string ValidateFileServerCodeRepresentativeLocalLoad(string codeAssetLocalPath)
        {
            if (string.IsNullOrEmpty(codeAssetLocalPath))
            {
                return "文件服务器热更代码本地加载校验缺少代表性资源路径。";
            }

            if (!File.Exists(codeAssetLocalPath))
            {
                return $"文件服务器热更代码本地加载校验缺少文件 path={codeAssetLocalPath}";
            }

            try
            {
                var assembly = Assembly.Load(File.ReadAllBytes(codeAssetLocalPath));
                if (assembly == null)
                {
                    return $"文件服务器热更代码本地加载校验失败 path={codeAssetLocalPath} err=Assembly.Load 返回 null";
                }

                return null;
            }
            catch (Exception exception)
            {
                return $"文件服务器热更代码本地加载校验失败 path={codeAssetLocalPath} err={exception.Message}";
            }
        }

        /// <summary>
        /// 在 CI batchmode 里把基于 art_assets.info 解析出的所有 AssetBundle 本地打开校验一次性投递到 Unity 同步上下文执行。
        /// 这样可以确保 bundle 遍历顺序稳定，同时避免后台线程直接触发 UnityEngine 资源 API。
        /// </summary>
        internal static async Task<string> ValidateFileServerAssetBundleLocalLoadsOnUnityContext(
            IReadOnlyList<string> assetBundleAssetLocalPaths)
        {
            var firstAssetBundleLocalPath = assetBundleAssetLocalPaths != null && assetBundleAssetLocalPaths.Count > 0
                ? assetBundleAssetLocalPaths[0]
                : string.Empty;
            var assetBundleCount = assetBundleAssetLocalPaths?.Count ?? 0;

            if (PlayerLoopHelper.IsMainThread)
            {
                LogFileServerFlow(
                    $"mainThreadDispatch status=already-main-thread component=AssetBundle firstTarget={firstAssetBundleLocalPath} count={assetBundleCount}",
                    Color.cyan);
                return ValidateFileServerAssetBundleLocalLoads(assetBundleAssetLocalPaths);
            }

            var unitySynchronizationContext = PlayerLoopHelper.UnitySynchronizationContext;
            if (unitySynchronizationContext == null)
            {
                LogFileServerFlow(
                    $"mainThreadDispatch status=missing-sync-context component=AssetBundle firstTarget={firstAssetBundleLocalPath} count={assetBundleCount}",
                    Color.red);
                return $"文件服务器 AssetBundle 本地加载校验失败 path={firstAssetBundleLocalPath} err=UnitySynchronizationContext 为空";
            }

            LogFileServerFlow(
                $"mainThreadDispatch status=queued component=AssetBundle firstTarget={firstAssetBundleLocalPath} count={assetBundleCount}",
                Color.cyan);

            var resultTaskCompletionSource = new TaskCompletionSource<string>();
            unitySynchronizationContext.Post(_ =>
            {
                try
                {
                    resultTaskCompletionSource.TrySetResult(
                        ValidateFileServerAssetBundleLocalLoads(assetBundleAssetLocalPaths));
                }
                catch (Exception exception)
                {
                    resultTaskCompletionSource.TrySetResult(
                        $"文件服务器 AssetBundle 本地加载校验失败 path={firstAssetBundleLocalPath} err={exception.Message}");
                }
            }, null);

            var completedTask = await Task.WhenAny(resultTaskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            if (completedTask != resultTaskCompletionSource.Task)
            {
                LogFileServerFlow(
                    $"mainThreadDispatch status=timeout component=AssetBundle firstTarget={firstAssetBundleLocalPath} count={assetBundleCount} timeoutSeconds=15",
                    Color.red);
                return $"文件服务器 AssetBundle 本地加载校验失败 path={firstAssetBundleLocalPath} err=切换 Unity 主线程超时";
            }

            LogFileServerFlow(
                $"mainThreadDispatch status=entered component=AssetBundle firstTarget={firstAssetBundleLocalPath} count={assetBundleCount}",
                Color.cyan);
            return await resultTaskCompletionSource.Task;
        }

        /// <summary>
        /// 按 art_assets.info 中所有非空 AssetBundlePath 去重后的顺序逐个执行本地打开校验。
        /// 每个 bundle 都沿用单文件校验 helper，从而把错误精确定位到具体的下载文件。
        /// </summary>
        internal static string ValidateFileServerAssetBundleLocalLoads(
            IReadOnlyList<string> assetBundleAssetLocalPaths)
        {
            if (assetBundleAssetLocalPaths == null || assetBundleAssetLocalPaths.Count <= 0)
            {
                return "文件服务器 AssetBundle 本地加载校验缺少基于 art_assets.info 的资源路径。";
            }

            var normalizedLocalPaths = assetBundleAssetLocalPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (normalizedLocalPaths.Count <= 0)
            {
                return "文件服务器 AssetBundle 本地加载校验缺少基于 art_assets.info 的资源路径。";
            }

            for (var index = 0; index < normalizedLocalPaths.Count; index++)
            {
                var assetBundleAssetLocalPath = normalizedLocalPaths[index];
                LogFileServerBatchProgress("AssetBundle逐个本地加载", index + 1, normalizedLocalPaths.Count,
                    assetBundleAssetLocalPath, Color.cyan, "component=AssetBundle");
                var loadError = ValidateFileServerAssetBundleRepresentativeLocalLoad(assetBundleAssetLocalPath);
                if (!string.IsNullOrEmpty(loadError))
                {
                    return loadError;
                }
            }

            return null;
        }

        /// <summary>
        /// 在 CI batchmode 里把代表性 AssetBundle 本地打开显式投递到 Unity 同步上下文执行。
        /// 这样即使主线程派发异常或长期无响应，也会返回明确错误，而不是在 <see cref="UniTask.SwitchToMainThread()"/> 上静默挂住。
        /// </summary>
        internal static async Task<string> ValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContext(
            string assetBundleAssetLocalPath)
        {
            if (PlayerLoopHelper.IsMainThread)
            {
                LogFileServerFlow(
                    $"mainThreadDispatch status=already-main-thread component=AssetBundle target={assetBundleAssetLocalPath}",
                    Color.cyan);
                return ValidateFileServerAssetBundleRepresentativeLocalLoad(assetBundleAssetLocalPath);
            }

            var unitySynchronizationContext = PlayerLoopHelper.UnitySynchronizationContext;
            if (unitySynchronizationContext == null)
            {
                LogFileServerFlow(
                    $"mainThreadDispatch status=missing-sync-context component=AssetBundle target={assetBundleAssetLocalPath}",
                    Color.red);
                return $"文件服务器 AssetBundle 本地加载校验失败 path={assetBundleAssetLocalPath} err=UnitySynchronizationContext 为空";
            }

            LogFileServerFlow(
                $"mainThreadDispatch status=queued component=AssetBundle target={assetBundleAssetLocalPath}",
                Color.cyan);

            var resultTaskCompletionSource = new TaskCompletionSource<string>();
            unitySynchronizationContext.Post(_ =>
            {
                try
                {
                    resultTaskCompletionSource.TrySetResult(
                        ValidateFileServerAssetBundleRepresentativeLocalLoad(assetBundleAssetLocalPath));
                }
                catch (Exception exception)
                {
                    resultTaskCompletionSource.TrySetResult(
                        $"文件服务器 AssetBundle 本地加载校验失败 path={assetBundleAssetLocalPath} err={exception.Message}");
                }
            }, null);

            var completedTask = await Task.WhenAny(resultTaskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            if (completedTask != resultTaskCompletionSource.Task)
            {
                LogFileServerFlow(
                    $"mainThreadDispatch status=timeout component=AssetBundle target={assetBundleAssetLocalPath} timeoutSeconds=15",
                    Color.red);
                return $"文件服务器 AssetBundle 本地加载校验失败 path={assetBundleAssetLocalPath} err=切换 Unity 主线程超时";
            }

            LogFileServerFlow(
                $"mainThreadDispatch status=entered component=AssetBundle target={assetBundleAssetLocalPath}",
                Color.cyan);
            return await resultTaskCompletionSource.Task;
        }

        /// <summary>
        /// 校验代表性的 AssetBundle payload 是否能在本地通过 <see cref="AssetBundle.LoadFromFile(string)"/> 打开。
        /// 调用方需要确保此 helper 运行在 Unity 主线程，避免把 UnityEngine 资源打开操作放到线程池执行。
        /// </summary>
        internal static string ValidateFileServerAssetBundleRepresentativeLocalLoad(string assetBundleAssetLocalPath)
        {
            if (string.IsNullOrEmpty(assetBundleAssetLocalPath))
            {
                return "文件服务器 AssetBundle 本地加载校验缺少代表性资源路径。";
            }

            if (!File.Exists(assetBundleAssetLocalPath))
            {
                return $"文件服务器 AssetBundle 本地加载校验缺少文件 path={assetBundleAssetLocalPath}";
            }

            AssetBundle assetBundle = null;
            try
            {
                assetBundle = AssetBundle.LoadFromFile(assetBundleAssetLocalPath);
                if (assetBundle == null)
                {
                    return $"文件服务器 AssetBundle 本地加载校验失败 path={assetBundleAssetLocalPath} err=AssetBundle.LoadFromFile 返回 null";
                }

                return null;
            }
            catch (Exception exception)
            {
                return $"文件服务器 AssetBundle 本地加载校验失败 path={assetBundleAssetLocalPath} err={exception.Message}";
            }
            finally
            {
                assetBundle?.Unload(true);
            }
        }

        /// <summary>
        /// 校验代表性的 SQLite payload 是否能按运行时只读方式在本地打开。
        /// 这里沿用 <see cref="SqliteLoder.LoadDBReadOnly(string)"/>，从而覆盖密码、驱动和文件头三类真实打开问题。
        /// </summary>
        internal static string ValidateFileServerTableRepresentativeLocalLoad(string tableAssetLocalPath)
        {
            if (string.IsNullOrEmpty(tableAssetLocalPath))
            {
                return "文件服务器表格本地加载校验缺少代表性资源路径。";
            }

            if (!File.Exists(tableAssetLocalPath))
            {
                return $"文件服务器表格本地加载校验缺少文件 path={tableAssetLocalPath}";
            }

            var dbName = Path.GetFileNameWithoutExtension(tableAssetLocalPath);
            try
            {
                var connection = SqliteLoder.LoadDBReadOnly(tableAssetLocalPath);
                if (connection == null)
                {
                    return $"文件服务器表格本地加载校验失败 path={tableAssetLocalPath} err=SqliteLoder.LoadDBReadOnly 返回 null";
                }

                return null;
            }
            catch (Exception exception)
            {
                return $"文件服务器表格本地加载校验失败 path={tableAssetLocalPath} err={exception.Message}";
            }
            finally
            {
                if (!string.IsNullOrEmpty(dbName))
                {
                    SqliteLoder.Close(dbName);
                }
            }
        }

        private void RebuildFileServerLocalMetadata(string firstLoadDir,
            string cacheDir,
            Dictionary<FileServerComponentKind, FileServerComponentContext> componentContexts,
            bool allowBasePackageFallback = true)
        {
            LogFileServerFlow(
                $"开始重建本地元数据 componentCount={componentContexts.Count} allowBasePackageFallback={allowBasePackageFallback}",
                Color.cyan);
            var mergedAssets = componentContexts.Values
                .Where(context => context.ComponentKind != FileServerComponentKind.Table)
                .SelectMany(context => context.AssetItems);
            var normalizedAssets = NormalizeFileServerManagedAssetItems(mergedAssets);
            if (normalizedAssets.Count > 0)
            {
                LogFileServerFlow($"写入本地 assets.info assetCount={normalizedAssets.Count}", Color.cyan);
                FileHelper.WriteAllText(IPath.Combine(firstLoadDir, BResources.ASSETS_INFO_PATH),
                    CsvSerializer.SerializeToString(normalizedAssets));
            }

            var localPackageBuildInfo = LoadLocalPackageBuildInfo(firstLoadDir, allowBasePackageFallback);
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
            LogFileServerFlow("写入本地 package_build.info。", Color.cyan);
            FileHelper.WriteAllText(IPath.Combine(firstLoadDir, FileServerPackageBuildInfoPath),
                JsonMapper.ToJson(mergedPackageBuildInfo));

            var subPackagePath = GetFileServerSubPackageCachePath(cacheDir);
            if (File.Exists(subPackagePath))
            {
                LogFileServerFlow("回写本地 assets_sub_package 配置缓存。", Color.cyan);
                FileHelper.WriteAllText(IPath.Combine(firstLoadDir, BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH),
                    File.ReadAllText(subPackagePath));
            }

            LogFileServerFlow($"本地元数据重建完成 mergedAssets={normalizedAssets.Count} hasSubPackageCache={File.Exists(subPackagePath)}",
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

        /// <summary>
        /// 为文件服务器协议准备本地加载目录。
        /// 常规运行沿用 <see cref="ClientAssetsUtils"/> 的版本目录语义；CI BatchMode 验证则切到独立目录，
        /// 避免在纯下载验证前触发 BetterStreamingAssets 初始化。
        /// </summary>
        private static Tuple<string, string> EnsureFileServerLocalLoadDir(RuntimePlatform runtimePlatform,
            bool useIsolatedLocalLoadDir = false)
        {
            if (useIsolatedLocalLoadDir)
            {
                var platformPath = BApplication.GetPlatformLoadPath(runtimePlatform);
                var isolatedFirstLoadDir = IPath.Combine(BApplication.persistentDataPath,
                    FileServerBatchVerificationRootFolderName, platformPath);
                var isolatedSecondLoadDir = IPath.Combine(BApplication.streamingAssetsPath, platformPath);
                Directory.CreateDirectory(isolatedFirstLoadDir);
                return new Tuple<string, string>(isolatedFirstLoadDir, isolatedSecondLoadDir);
            }

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

        /// <summary>
        /// 校验文件服务器验证链刚刚下载到本地的单个资源。
        /// 该 helper 只看当前验证目录下的实际文件和 hash，不访问 StreamingAssets，也不依赖 ClientAssetsUtils 初始化。
        /// </summary>
        internal static bool IsFileServerDownloadedAssetValid(string firstLoadDir, AssetItem assetItem)
        {
            if (string.IsNullOrEmpty(firstLoadDir) || assetItem == null || string.IsNullOrEmpty(assetItem.LocalPath))
            {
                return false;
            }

            var localPath = IPath.Combine(firstLoadDir, assetItem.LocalPath);
            if (!File.Exists(localPath))
            {
                return false;
            }

            if (string.IsNullOrEmpty(assetItem.HashName))
            {
                return true;
            }

            var hash = FileHelper.GetMurmurHash3(localPath);
            return string.Equals(hash, assetItem.HashName, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildFileServerVersionManifestUrl(string serverUrl)
        {
            // Artifact file server exposes downloads under /files/{path}; the manifest
            // is uploaded by Python as global_version.info at the root of the store,
            // so the download URL is serverUrl/files/global_version.info.
            return CombineUrl(CombineUrl(serverUrl, "files"), FileServerVersionManifestFileName);
        }

        /// <summary>
        /// 拼接文件服务器下载根路径。Artifact file server 所有下载走 /files/{path} 路由，
        /// 所以统一在 serverUrl 后追加 files 前缀再接具体文件路径。
        /// </summary>
        private static string BuildFileServerDownloadRoot(string serverUrl)
        {
            return CombineUrl(serverUrl, "files");
        }

        private static string BuildFileServerComponentRemoteRoot(string serverUrl, string platformPath,
            FileServerComponentKind componentKind,
            string componentVersion)
        {
            var downloadRoot = BuildFileServerDownloadRoot(serverUrl);
            switch (componentKind)
            {
                case FileServerComponentKind.Code:
                    return CombineUrl(downloadRoot, $"ClientRes_Code_{platformPath}", componentVersion);
                case FileServerComponentKind.AssetBundle:
                    return CombineUrl(downloadRoot, $"ClientRes_Assetbundle_{platformPath}", componentVersion);
                case FileServerComponentKind.Table:
                    return CombineUrl(downloadRoot, "ClientRes_Table", componentVersion);
                default:
                    return downloadRoot;
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
                context.PackageBuildInfo = NormalizeFileServerComponentPackageBuildInfo(componentKind,
                    componentVersion,
                    JsonMapper.ToObject<ClientPackageBuildInfo>(context.PackageBuildContent)
                    ?? new ClientPackageBuildInfo());
                if (componentKind != FileServerComponentKind.Table)
                {
                    context.AssetsInfoContent = File.ReadAllText(GetFileServerAssetsInfoCachePath(cacheDir, componentKind));
                    var assetItems = CsvSerializer.DeserializeFromString<List<AssetItem>>(context.AssetsInfoContent)
                                     ?? new List<AssetItem>();
                    context.AssetItems = NormalizeFileServerManagedAssetItems(assetItems);
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

        /// <summary>
        /// 标准化文件服务器协议管理的资源列表。
        /// 该 helper 统一过滤 <c>package_build.info</c> 元数据占位项，并保持既有的去重与按 Id 排序规则，
        /// 避免不同下载/缓存入口分别维护相同逻辑。
        /// </summary>
        internal static List<AssetItem> NormalizeFileServerManagedAssetItems(IEnumerable<AssetItem> assetItems)
        {
            return (assetItems ?? Enumerable.Empty<AssetItem>())
                .Where(item => !string.Equals(item.LocalPath, FileServerPackageBuildInfoPath,
                    StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(item => item.Id)
                .ToList();
        }

        /// <summary>
        /// 决定远端组件元数据读取失败时是否允许回退到本地缓存。
        /// CI BatchMode 严格远端验证必须禁用该回退，避免把真实远端问题伪装成缓存命中，
        /// 同时避免再次走到旧版 <see cref="ClientAssetsUtils"/> 初始化路径。
        /// </summary>
        internal static bool ShouldUseCachedFileServerComponentContextOnFailure(bool useCacheOnFailure,
            bool strictRemoteVerification)
        {
            return useCacheOnFailure && !strictRemoteVerification;
        }

        /// <summary>
        /// 读取当前本地下载目录里的 package_build.info。
        /// 对于 CI BatchMode 验证，可关闭母包回退，避免只为了拿默认版本而触发 ClientAssetsUtils 的静态初始化。
        /// </summary>
        internal static ClientPackageBuildInfo LoadLocalPackageBuildInfo(string firstLoadDir,
            bool allowBasePackageFallback = true)
        {
            var localPackageBuildPath = IPath.Combine(firstLoadDir, FileServerPackageBuildInfoPath);
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

            if (!allowBasePackageFallback)
            {
                return new ClientPackageBuildInfo();
            }

            try
            {
                return ClientAssetsUtils.GetBasePackBuildInfo() ?? new ClientPackageBuildInfo();
            }
            catch
            {
                return new ClientPackageBuildInfo();
            }
        }

        /// <summary>
        /// 重置文件服务器验证使用的 persistent 下载目录。
        /// 只在 CI 批量验证入口使用，目的是强制触发真实下载，避免历史文件或母包残留让验证变成假阳性。
        /// </summary>
        private static void ResetFileServerLocalState(string firstLoadDir)
        {
            if (Directory.Exists(firstLoadDir))
            {
                Directory.Delete(firstLoadDir, true);
            }

            Directory.CreateDirectory(firstLoadDir);
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

        /// <summary>
        /// 以有限重试和显式超时读取文件服务器文本元数据。
        /// 404 会被单独标记成“不存在”，其余异常会保留最后一次错误文本返回给调用方。
        /// </summary>
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
                        var content = await AwaitFileServerRequestWithTimeout(
                            client.DownloadStringTaskAsync(url),
                            FileServerTextRequestTimeout,
                            url,
                            client.CancelAsync);
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

                if (!string.IsNullOrEmpty(error) && !isNotFound)
                {
                    LogFileServerFlow(
                        $"文件服务器文本请求重试 attempt={retryIndex + 1}/{RETRY_COUNT} url={url} err={error}",
                        retryIndex == RETRY_COUNT - 1 ? Color.red : Color.yellow);
                }
            }

            return new Tuple<bool, bool, string, string>(false, isNotFound, null, error);
        }
    }
}