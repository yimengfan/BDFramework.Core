using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BDFramework.Asset;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.Sql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.AssetsManager
{
    /// <summary>
    /// 对应 AssetsVersionController.DevOps.cs 的纯逻辑测试。
    /// 这里不依赖真实下载，仅验证三段版控解析、package_build.info 合并和子包资源筛选。
    /// </summary>
    /// <remarks>
    /// 常规情况下通过 Unity Test Runner 执行；如果项目级初始化干扰了 <c>-runTests</c>，也可以改走 <c>RunBatchVerification()</c>。
    /// </remarks>
    public class AssetsVersionControllerDevOpsTest
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// 当项目级初始化会干扰 Unity 原生 -runTests 流程时，可以通过 -executeMethod 直接调用这组纯逻辑测试。
        /// </summary>
        public static void RunBatchVerification()
        {
            AssetsVersionControllerDevOpsBatchVerification.RunBatchVerification();
        }

        /// <summary>
        /// 在每个 NUnit 测试入口开始时输出统一的测试目的与实现手段日志。
        /// 这样无论是 Unity Test Runner 还是 TeamCity 收集控制台输出，都能直接看到当前测试想验证什么、通过什么方式验证。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应代码路径的行为与错误契约。",
                "执行显式测试入口并断言关键结果、错误信息与流程进度日志。");
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 验证共享版控文本会被稳定解析为 Code / AssetBundle / Table 三段版本号。
        /// </summary>
        [Test]
        public void TryParseFileServerVersionInfo_ParsesThreeSegments()
        {
            VerifyTryParseFileServerVersionInfoParsesThreeSegments();
        }

        /// <summary>
        /// 验证 TeamCity 期望版控与远端共享版控不一致时，会返回明确的组件级错误信息。
        /// </summary>
        [Test]
        public void ValidateExpectedFileServerVersionInfo_ReturnsErrorForMismatchedManifest()
        {
            VerifyValidateExpectedFileServerVersionInfoReturnsErrorForMismatchedManifest();
        }

        /// <summary>
        /// 验证三个组件各自的 package_build.info 字段会合并回统一运行时结构。
        /// </summary>
        [Test]
        public void MergeFileServerPackageBuildInfo_MergesComponentSpecificFields()
        {
            VerifyMergeFileServerPackageBuildInfoMergesComponentSpecificFields();
        }

        /// <summary>
        /// 验证组件根 package_build.info 仍是旧默认值时，会回退使用当前组件目录版本号补齐对应的三段字段。
        /// </summary>
        [Test]
        public void NormalizeFileServerComponentPackageBuildInfo_FallsBackToComponentVersionWhenSourceUsesNone()
        {
            VerifyNormalizeFileServerComponentPackageBuildInfoFallsBackToComponentVersionWhenSourceUsesNone();
        }

        /// <summary>
        /// 验证下载完成后本地 package_build.info 若没有回写成当前链路期望版本，会返回明确错误。
        /// </summary>
        [Test]
        public void ValidateFileServerPackageBuildInfo_ReturnsErrorForMismatchedComponentVersions()
        {
            VerifyValidateFileServerPackageBuildInfoReturnsErrorForMismatchedComponentVersions();
        }

        /// <summary>
        /// 验证子包配置可以同时挑出 AssetBundle、热更代码、表格和依赖配置文件。
        /// </summary>
        [Test]
        public void BuildFileServerSubPackageAssetItems_CollectsConfiguredAssetsAcrossComponents()
        {
            VerifyBuildFileServerSubPackageAssetItemsCollectsConfiguredAssetsAcrossComponents();
        }

        /// <summary>
        /// 验证文件服务器 assets.info 标准化时会剔除 package_build.info，并按资源 Id 稳定排序。
        /// </summary>
        [Test]
        public void NormalizeFileServerManagedAssetItems_FiltersPackageBuildInfoAndSortsById()
        {
            VerifyNormalizeFileServerManagedAssetItemsFiltersPackageBuildInfoAndSortsById();
        }

        /// <summary>
        /// 验证 CI BatchMode 严格远端验证会禁用组件元数据缓存回退，避免把远端异常短路成旧缓存命中。
        /// </summary>
        [Test]
        public void ShouldUseCachedFileServerComponentContextOnFailure_DisablesFallbackForStrictRemoteVerification()
        {
            VerifyShouldUseCachedFileServerComponentContextOnFailureDisablesFallbackForStrictRemoteVerification();
        }

        /// <summary>
        /// 验证文件服务器远端下载路径对 Code / AssetBundle 优先使用 HashName，对无 hash 资源回退到 LocalPath。
        /// </summary>
        [Test]
        public void BuildFileServerAssetRemoteRelativePath_PrefersHashNameForManagedAssets()
        {
            VerifyBuildFileServerAssetRemoteRelativePathPrefersHashNameForManagedAssets();
        }

        /// <summary>
        /// 验证 CI 批量验证会优先挑选真正代表 Code / AssetBundle / Table 三类 payload 的样本文件。
        /// </summary>
        [Test]
        public void FindFileServerRepresentativeAsset_PicksRealPayloadAssets()
        {
            VerifyFindFileServerRepresentativeAssetPicksRealPayloadAssets();
        }

        /// <summary>
        /// 验证 AssetBundle 本地打开校验样本会按 art_assets.info 中所有非空 AssetBundlePath 去重后输出。
        /// </summary>
        [Test]
        public void CollectFileServerAssetBundleValidationRelativePaths_DeduplicatesNonEmptyBundlePaths()
        {
            VerifyCollectFileServerAssetBundleValidationRelativePathsDeduplicatesNonEmptyBundlePaths();
        }

        /// <summary>
        /// 验证从 art_assets.info 文本直接恢复本地校验样本时，同样会按所有非空 AssetBundlePath 去重后输出。
        /// </summary>
        [Test]
        public void CollectFileServerAssetBundleValidationRelativePathsFromArtAssetsInfoContent_DeduplicatesNonEmptyBundlePaths()
        {
            VerifyCollectFileServerAssetBundleValidationRelativePathsFromArtAssetsInfoContentDeduplicatesNonEmptyBundlePaths();
        }

        /// <summary>
        /// 验证从 art_assets.info 文本恢复资产级本地加载校验项时，会保留每条可加载资产记录而不是只按 bundle 去重。
        /// </summary>
        [Test]
        public void CollectFileServerAssetBundleValidationEntriesFromArtAssetsInfoContent_KeepsLoadableAssetRows()
        {
            VerifyCollectFileServerAssetBundleValidationEntriesFromArtAssetsInfoContentKeepsLoadableAssetRows();
        }

        /// <summary>
        /// 验证批量校验会基于本地 art_assets.info 为缺失的 AssetBundle 生成补下载项，而不是只依赖 assets.info 里的原始清单。
        /// </summary>
        [Test]
        public void BuildMissingFileServerAssetBundleValidationDownloadItems_UsesArtAssetsInfoForMissingBundles()
        {
            VerifyBuildMissingFileServerAssetBundleValidationDownloadItemsUsesArtAssetsInfoForMissingBundles();
        }

        /// <summary>
        /// 验证 CI BatchMode 独立下载目录缺少 package_build.info 时，会返回空白结构而不是回退到旧版 ClientAssetsUtils 初始化。
        /// </summary>
        [Test]
        public void LoadLocalPackageBuildInfo_ReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing()
        {
            VerifyLoadLocalPackageBuildInfoReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing();
        }

        /// <summary>
        /// 验证 CI BatchMode 本地资源校验只依赖当前下载目录和 hash，不依赖 StreamingAssets 初始化。
        /// </summary>
        [Test]
        public void IsFileServerDownloadedAssetValid_ValidatesOnlyDownloadedPayloadFile()
        {
            VerifyIsFileServerDownloadedAssetValidValidatesOnlyDownloadedPayloadFile();
        }

        /// <summary>
        /// 验证代表性的热更代码资源在下载落地后，会经过真实程序集装载校验而不是只做 hash 检查。
        /// </summary>
        [Test]
        public void ValidateFileServerCodeRepresentativeLocalLoad_SucceedsForManagedAssemblyFile()
        {
            VerifyValidateFileServerCodeRepresentativeLocalLoadSucceedsForManagedAssemblyFile();
        }

        /// <summary>
        /// 验证代表性的 AssetBundle 资源在下载落地后，会经过真实本地打开校验。
        /// </summary>
        [Test]
        public void ValidateFileServerAssetBundleRepresentativeLocalLoad_SucceedsForBuiltBundle()
        {
            VerifyValidateFileServerAssetBundleRepresentativeLocalLoadSucceedsForBuiltBundle();
        }

        /// <summary>
        /// 验证 AssetBundle 资产级本地加载校验会按 art_assets.info 资产记录解析 loadPath，并完成真实 LoadAsset。
        /// </summary>
        [Test]
        public void ValidateFileServerAssetBundleLocalLoads_SucceedsForBuiltBundleAsset()
        {
            VerifyValidateFileServerAssetBundleLocalLoadsSucceedsForBuiltBundleAsset();
        }

        /// <summary>
        /// 验证代表性 AssetBundle 主线程投递 helper 在当前已经位于主线程时，会直接返回底层本地打开校验结果。
        /// </summary>
        [Test]
        public void ValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContext_ReturnsMissingFileErrorOnMainThread()
        {
            VerifyValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContextReturnsMissingFileErrorOnMainThread();
        }

        /// <summary>
        /// 验证代表性本地加载总入口在主线程收口后，会直接返回 AssetBundle 缺文件错误，而不是再次等待主线程派发超时。
        /// </summary>
        [Test]
        public void ValidateFileServerRepresentativeLocalLoads_ReturnsAssetBundleMissingFileErrorOnMainThread()
        {
            VerifyValidateFileServerRepresentativeLocalLoadsReturnsAssetBundleMissingFileErrorOnMainThread();
        }

        /// <summary>
        /// 验证代表性的表格资源在下载落地后，会经过真实 SQLite 只读打开校验。
        /// </summary>
        [Test]
        public void ValidateFileServerTableRepresentativeLocalLoad_SucceedsForReadableSqlite()
        {
            VerifyValidateFileServerTableRepresentativeLocalLoadSucceedsForReadableSqlite();
        }

        /// <summary>
        /// 验证文件服务器请求超时包装器在任务按时完成时会直接返回原始结果。
        /// </summary>
        [Test]
        public void AwaitFileServerRequestWithTimeout_ReturnsResultWhenTaskCompletesInTime()
        {
            VerifyAwaitFileServerRequestWithTimeoutReturnsResultWhenTaskCompletesInTime();
        }

        /// <summary>
        /// 验证文件服务器请求超时包装器在任务长时间无响应时会抛出明确超时并触发取消回调。
        /// </summary>
        [Test]
        public void AwaitFileServerRequestWithTimeout_ThrowsTimeoutWhenTaskDoesNotFinish()
        {
            VerifyAwaitFileServerRequestWithTimeoutThrowsTimeoutWhenTaskDoesNotFinish();
        }

        /// <summary>
        /// 验证 CI BatchMode 文件服务器进度日志会稳定带出阶段名、序号、总量和目标路径。
        /// </summary>
        [Test]
        public void FormatFileServerBatchProgressMessage_IncludesStageCountsAndTarget()
        {
            VerifyFormatFileServerBatchProgressMessageIncludesStageCountsAndTarget();
        }

        /// <summary>
        /// 验证 CI BatchMode 文件服务器进度日志在缺省输入下仍会回退成稳定占位文本，避免 TeamCity 日志缺字段。
        /// </summary>
        [Test]
        public void FormatFileServerBatchProgressMessage_UsesFallbackForMissingValues()
        {
            VerifyFormatFileServerBatchProgressMessageUsesFallbackForMissingValues();
        }

        /// <summary>
        /// 以纯异常校验方式验证共享版控解析结果，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyTryParseFileServerVersionInfoParsesThreeSegments()
        {
            var parsed = AssetsVersionControllerDevOpsPureLogic.TryParseFileServerVersionInfo("101.202.303",
                out var versionInfo);

            EnsureTrue(parsed, "TryParseFileServerVersionInfo 应返回 true。");
            EnsureEqual("101", versionInfo?.CodeVersion, "CodeVersion 不匹配。");
            EnsureEqual("202", versionInfo?.AssetBundleVersion, "AssetBundleVersion 不匹配。");
            EnsureEqual("303", versionInfo?.TableVersion, "TableVersion 不匹配。");
            EnsureEqual("101.202.303", versionInfo?.RawValue, "RawValue 不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证共享版控不一致时的错误信息，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyValidateExpectedFileServerVersionInfoReturnsErrorForMismatchedManifest()
        {
            var expected = new AssetsVersionController.FileServerVersionInfo()
            {
                CodeVersion = "501",
                AssetBundleVersion = "502",
                TableVersion = "503",
            };
            var actual = new AssetsVersionController.FileServerVersionInfo()
            {
                CodeVersion = "501",
                AssetBundleVersion = "999",
                TableVersion = "503",
            };

            var error = AssetsVersionControllerDevOpsPureLogic.ValidateExpectedFileServerVersionInfo(expected, actual);

            EnsureEqual("文件服务器 AssetBundle 版控不匹配 expected=502 actual=999", error,
                "共享版控不一致错误信息不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证 package_build.info 合并逻辑，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyMergeFileServerPackageBuildInfoMergesComponentSpecificFields()
        {
            var baseInfo = new ClientPackageBuildInfo()
            {
                Version = "0.1.0",
                BasePckScriptSVCVersion = "base",
                HotfixScriptSVCVersion = "old-hotfix",
                AssetBundleSVCVersion = "old-ab",
                TableSVCVersion = "old-table",
                BuildTime = 1,
            };
            var codeInfo = new ClientPackageBuildInfo()
            {
                HotfixScriptSVCVersion = "new-hotfix",
                BuildTime = 10,
            };
            var assetBundleInfo = new ClientPackageBuildInfo()
            {
                AssetBundleSVCVersion = "new-ab",
                BuildTime = 20,
            };
            var tableInfo = new ClientPackageBuildInfo()
            {
                TableSVCVersion = "new-table",
                BuildTime = 30,
            };

            var merged = AssetsVersionControllerDevOpsPureLogic.MergeFileServerPackageBuildInfo(baseInfo, codeInfo,
                assetBundleInfo, tableInfo);

            EnsureEqual("0.1.0", merged.Version, "Version 合并结果不匹配。");
            EnsureEqual("base", merged.BasePckScriptSVCVersion, "BasePckScriptSVCVersion 合并结果不匹配。");
            EnsureEqual("new-hotfix", merged.HotfixScriptSVCVersion,
                "HotfixScriptSVCVersion 合并结果不匹配。");
            EnsureEqual("new-ab", merged.AssetBundleSVCVersion, "AssetBundleSVCVersion 合并结果不匹配。");
            EnsureEqual("new-table", merged.TableSVCVersion, "TableSVCVersion 合并结果不匹配。");
            EnsureEqual(30L, merged.BuildTime, "BuildTime 合并结果不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证组件 package_build.info 版本兜底逻辑，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyNormalizeFileServerComponentPackageBuildInfoFallsBackToComponentVersionWhenSourceUsesNone()
        {
            var codeInfo = new ClientPackageBuildInfo()
            {
                Version = "0.1.0",
                HotfixScriptSVCVersion = "none",
            };
            var assetBundleInfo = new ClientPackageBuildInfo()
            {
                Version = "0.1.0",
                AssetBundleSVCVersion = string.Empty,
            };
            var tableInfo = new ClientPackageBuildInfo()
            {
                Version = "0.0.2",
                TableSVCVersion = "none",
            };
            var existingCodeInfo = new ClientPackageBuildInfo()
            {
                HotfixScriptSVCVersion = "legacy-keep",
            };

            var normalizedCodeInfo = AssetsVersionController.NormalizeFileServerComponentPackageBuildInfo(
                AssetsVersionController.FileServerComponentKind.Code, "30", codeInfo);
            var normalizedAssetBundleInfo = AssetsVersionController.NormalizeFileServerComponentPackageBuildInfo(
                AssetsVersionController.FileServerComponentKind.AssetBundle, "32", assetBundleInfo);
            var normalizedTableInfo = AssetsVersionController.NormalizeFileServerComponentPackageBuildInfo(
                AssetsVersionController.FileServerComponentKind.Table, "27", tableInfo);
            var preservedCodeInfo = AssetsVersionController.NormalizeFileServerComponentPackageBuildInfo(
                AssetsVersionController.FileServerComponentKind.Code, "30", existingCodeInfo);

            EnsureEqual("30", normalizedCodeInfo.HotfixScriptSVCVersion,
                "Code 组件版本兜底结果不匹配。");
            EnsureEqual("32", normalizedAssetBundleInfo.AssetBundleSVCVersion,
                "AssetBundle 组件版本兜底结果不匹配。");
            EnsureEqual("27", normalizedTableInfo.TableSVCVersion,
                "Table 组件版本兜底结果不匹配。");
            EnsureEqual("legacy-keep", preservedCodeInfo.HotfixScriptSVCVersion,
                "已有非空 Code 组件版本时不应被兜底逻辑覆盖。");
            EnsureEqual("none", codeInfo.HotfixScriptSVCVersion,
                "组件版本兜底不应原地修改输入对象。");
        }

        /// <summary>
        /// 以纯异常校验方式验证本地 package_build.info 版本错误信息，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyValidateFileServerPackageBuildInfoReturnsErrorForMismatchedComponentVersions()
        {
            var expected = new AssetsVersionController.FileServerVersionInfo()
            {
                CodeVersion = "601",
                AssetBundleVersion = "602",
                TableVersion = "603",
            };
            var actual = new ClientPackageBuildInfo()
            {
                HotfixScriptSVCVersion = "601",
                AssetBundleSVCVersion = "700",
                TableSVCVersion = "603",
            };

            var error = AssetsVersionControllerDevOpsPureLogic.ValidateFileServerPackageBuildInfo(actual, expected);

            EnsureEqual("本地 package_build.info AssetBundle 版本不匹配 expected=602 actual=700", error,
                "package_build.info 错误信息不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证子包资源筛选结果，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyBuildFileServerSubPackageAssetItemsCollectsConfiguredAssetsAcrossComponents()
        {
            var subPackageConfig = new SubPackageConfigItem()
            {
                PackageName = "demo",
                ArtAssetsIdList = new List<int>() {1},
                HotfixCodePathList = new List<string>() {"script/hotfix/Game.dll.bytes"},
                TablePathList = new List<string>() {"local.db"},
                ConfAndInfoList = new List<string>() {"art_assets/art_assets.info"},
            };
            var assetBundleAssets = new List<AssetItem>()
            {
                new AssetItem() {Id = 1, LocalPath = "art_assets/demo.bundle", HashName = "ab-hash"},
                new AssetItem() {Id = 10001, LocalPath = "art_assets/art_assets.info", HashName = "conf-hash"},
            };
            var codeAssets = new List<AssetItem>()
            {
                new AssetItem() {Id = 20001, LocalPath = "script/hotfix/Game.dll.bytes", HashName = "dll-hash"},
            };
            var tableAssets = new List<AssetItem>()
            {
                new AssetItem() {Id = 30001, LocalPath = "local.db", HashName = string.Empty},
            };

            var selected = AssetsVersionControllerDevOpsPureLogic.BuildFileServerSubPackageAssetItems(subPackageConfig,
                assetBundleAssets, codeAssets, tableAssets);

            EnsureEqual(4L, selected.Count, "子包筛选资源数量不匹配。");
            EnsureTrue(selected.Exists(item => item.LocalPath == "art_assets/demo.bundle"),
                "缺少 art_assets/demo.bundle。");
            EnsureTrue(selected.Exists(item => item.LocalPath == "script/hotfix/Game.dll.bytes"),
                "缺少 script/hotfix/Game.dll.bytes。");
            EnsureTrue(selected.Exists(item => item.LocalPath == "local.db"), "缺少 local.db。");
            EnsureTrue(selected.Exists(item => item.LocalPath == "art_assets/art_assets.info"),
                "缺少 art_assets/art_assets.info。");
        }

        /// <summary>
        /// 以纯异常校验方式验证文件服务器 assets.info 标准化结果，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyNormalizeFileServerManagedAssetItemsFiltersPackageBuildInfoAndSortsById()
        {
            var normalized = AssetsVersionController.NormalizeFileServerManagedAssetItems(new List<AssetItem>()
            {
                new AssetItem() {Id = 30, LocalPath = "PACKAGE_BUILD.INFO", HashName = "pkg"},
                new AssetItem() {Id = 20, LocalPath = "art_assets/demo.bundle", HashName = "ab"},
                new AssetItem() {Id = 10, LocalPath = "script/hotfix/Game.dll.bytes", HashName = "dll"},
            });

            EnsureEqual(2L, normalized.Count, "标准化后资源数量不匹配。");
            EnsureEqual("script/hotfix/Game.dll.bytes", normalized[0].LocalPath,
                "标准化后第一个资源顺序不匹配。");
            EnsureEqual("art_assets/demo.bundle", normalized[1].LocalPath,
                "标准化后第二个资源顺序不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证严格远端验证会禁用组件元数据缓存回退，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyShouldUseCachedFileServerComponentContextOnFailureDisablesFallbackForStrictRemoteVerification()
        {
            EnsureTrue(!AssetsVersionController.ShouldUseCachedFileServerComponentContextOnFailure(true, true),
                "严格远端验证时不应允许组件元数据缓存回退。");
            EnsureTrue(AssetsVersionController.ShouldUseCachedFileServerComponentContextOnFailure(true, false),
                "非严格远端验证时应允许组件元数据缓存回退。");
            EnsureTrue(!AssetsVersionController.ShouldUseCachedFileServerComponentContextOnFailure(false, false),
                "显式禁用缓存回退时不应允许组件元数据缓存回退。");
        }

        /// <summary>
        /// 以纯异常校验方式验证文件服务器远端下载路径选择逻辑，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyBuildFileServerAssetRemoteRelativePathPrefersHashNameForManagedAssets()
        {
            var hashedAsset = new AssetItem()
            {
                Id = 801,
                LocalPath = "script/hotfix/Game.dll.bytes",
                HashName = "dll-hash",
            };
            var plainAsset = new AssetItem()
            {
                Id = 802,
                LocalPath = "local.db",
                HashName = string.Empty,
            };

            EnsureEqual("dll-hash", AssetsVersionController.BuildFileServerAssetRemoteRelativePath(hashedAsset),
                "存在 HashName 时应优先使用 HashName 作为远端下载路径。");
            EnsureEqual("local.db", AssetsVersionController.BuildFileServerAssetRemoteRelativePath(plainAsset),
                "缺少 HashName 时应回退到 LocalPath 作为远端下载路径。");
        }

        /// <summary>
        /// 以纯异常校验方式验证代表性资源挑选结果，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyFindFileServerRepresentativeAssetPicksRealPayloadAssets()
        {
            var codeItems = new List<AssetItem>()
            {
                new AssetItem() {Id = 1, LocalPath = "package_build.info", HashName = "pkg"},
                new AssetItem() {Id = 2, LocalPath = "script/hotfix/Game.dll.bytes", HashName = "dll"},
            };
            var assetBundleItems = new List<AssetItem>()
            {
                new AssetItem() {Id = 3, LocalPath = "art_assets/art_assets.info", HashName = "info"},
                new AssetItem() {Id = 4, LocalPath = "art_assets/hero.bundle", HashName = "ab"},
            };
            var tableItems = new List<AssetItem>()
            {
                new AssetItem() {Id = 5, LocalPath = "server_data/server.db", HashName = string.Empty},
                new AssetItem() {Id = 6, LocalPath = "local.db", HashName = string.Empty},
            };

            var codeAsset = AssetsVersionControllerDevOpsPureLogic.FindFileServerRepresentativeAsset(
                AssetsVersionController.FileServerComponentKind.Code, codeItems);
            var assetBundleAsset = AssetsVersionControllerDevOpsPureLogic.FindFileServerRepresentativeAsset(
                AssetsVersionController.FileServerComponentKind.AssetBundle, assetBundleItems);
            var tableAsset = AssetsVersionControllerDevOpsPureLogic.FindFileServerRepresentativeAsset(
                AssetsVersionController.FileServerComponentKind.Table, tableItems);

            EnsureEqual("script/hotfix/Game.dll.bytes", codeAsset?.LocalPath,
                "Code 代表性资源选择结果不匹配。");
            EnsureEqual("art_assets/hero.bundle", assetBundleAsset?.LocalPath,
                "AssetBundle 代表性资源选择结果不匹配。");
            EnsureEqual("local.db", tableAsset?.LocalPath, "Table 代表性资源选择结果不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证 art_assets.info 驱动的 AssetBundle 本地校验路径提取结果，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyCollectFileServerAssetBundleValidationRelativePathsDeduplicatesNonEmptyBundlePaths()
        {
            var validationRelativePaths = AssetsVersionController.CollectFileServerAssetBundleValidationRelativePaths(
                new List<AssetBundleItem>()
                {
                    new AssetBundleItem(1, "Assets/Hero.prefab", "hero.bundle", 0, string.Empty, 0),
                    new AssetBundleItem(2, "Assets/HeroVariant.prefab", "hero.bundle", 0, string.Empty, 0),
                    new AssetBundleItem(3, string.Empty, "shared.bundle", 0, string.Empty, 0),
                    new AssetBundleItem(4, "Assets/Config.asset", string.Empty, 0, string.Empty, 0),
                });

            EnsureEqual(2L, validationRelativePaths.Count, "AssetBundle 本地校验路径数量不匹配。");
            EnsureEqual("art_assets/hero.bundle", validationRelativePaths[0],
                "第一个 AssetBundle 本地校验路径不匹配。");
            EnsureEqual("art_assets/shared.bundle", validationRelativePaths[1],
                "第二个 AssetBundle 本地校验路径不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证从 art_assets.info 文本直接恢复 AssetBundle 本地校验路径时，也会按非空 bundle 去重。
        /// </summary>
        internal static void VerifyCollectFileServerAssetBundleValidationRelativePathsFromArtAssetsInfoContentDeduplicatesNonEmptyBundlePaths()
        {
            var artAssetsInfoContent = string.Join("\n", new[]
            {
                "Id,AssetType,LoadPath,GUID,AssetBundleLoadType,AssetBundlePath,Hash,AssetsPackSourceHash,Mix,DependAssetIds",
                "1,0,Assets/Hero.prefab,,0,hero.bundle,,,0,",
                "2,0,Assets/HeroVariant.prefab,,0,hero.bundle,,,0,",
                "3,0,,,0,shared.bundle,,,0,",
                "4,0,Assets/Config.asset,,0,,,,0,",
            });

            var validationRelativePaths = AssetsVersionControllerDevOpsPureLogic
                .CollectFileServerAssetBundleValidationRelativePathsFromArtAssetsInfoContent(artAssetsInfoContent);

            EnsureEqual(2L, validationRelativePaths.Count, "从 art_assets.info 文本恢复的 AssetBundle 本地校验路径数量不匹配。");
            EnsureEqual("art_assets/hero.bundle", validationRelativePaths[0],
                "从 art_assets.info 文本恢复的第一个 AssetBundle 本地校验路径不匹配。");
            EnsureEqual("art_assets/shared.bundle", validationRelativePaths[1],
                "从 art_assets.info 文本恢复的第二个 AssetBundle 本地校验路径不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证资产级本地加载样本恢复逻辑会保留每条可加载资产记录，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyCollectFileServerAssetBundleValidationEntriesFromArtAssetsInfoContentKeepsLoadableAssetRows()
        {
            var artAssetsInfoContent = string.Join("\n", new[]
            {
                "Id,AssetType,LoadPath,GUID,AssetBundleLoadType,AssetBundlePath,Hash,AssetsPackSourceHash,Mix,DependAssetIds",
                "1,0,Assets/Hero.prefab,guid-hero,0,hero.bundle,hero-hash,,0,",
                "2,0,Assets/HeroVariant.prefab,guid-hero-variant,0,hero.bundle,hero-hash,,0,",
                "3,0,,,0,hero.bundle,hero-hash,,0,",
                "4,0,Assets/Shared.prefab,,0,shared.bundle,shared-hash,,0,",
            });

            var validationEntries = AssetsVersionControllerDevOpsPureLogic
                .CollectFileServerAssetBundleValidationEntriesFromArtAssetsInfoContent(artAssetsInfoContent);

            EnsureEqual(3L, validationEntries.Count, "资产级加载样本数量不匹配。");
            EnsureEqual("Assets/Hero.prefab", validationEntries[0].AssetDisplayPath,
                "第一个资产级加载样本显示路径不匹配。");
            EnsureEqual("guid-hero", validationEntries[0].AssetGuid,
                "第一个资产级加载样本 GUID 不匹配。");
            EnsureEqual("art_assets/hero.bundle", validationEntries[0].AssetBundleRelativePath,
                "第一个资产级加载样本 bundle 相对路径不匹配。");
            EnsureEqual("Assets/HeroVariant.prefab", validationEntries[1].AssetDisplayPath,
                "第二个资产级加载样本显示路径不匹配。");
            EnsureEqual("Assets/Shared.prefab", validationEntries[2].AssetDisplayPath,
                "第三个资产级加载样本显示路径不匹配。");
            EnsureEqual("art_assets/shared.bundle", validationEntries[2].AssetBundleRelativePath,
                "第三个资产级加载样本 bundle 相对路径不匹配。");
        }

        /// <summary>
        /// 以纯异常校验方式验证批量校验的补下载 helper 会根据 art_assets.info 中缺失的 bundle 生成远端下载项。
        /// </summary>
        internal static void VerifyBuildMissingFileServerAssetBundleValidationDownloadItemsUsesArtAssetsInfoForMissingBundles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, BResources.ART_ASSET_ROOT_PATH));

            try
            {
                var artAssetsInfoContent = string.Join("\n", new[]
                {
                    "Id,AssetType,LoadPath,GUID,AssetBundleLoadType,AssetBundlePath,Hash,AssetsPackSourceHash,Mix,DependAssetIds",
                    "1,0,Assets/Hero.prefab,,0,hero.bundle,hero-hash,,0,",
                    "2,0,Assets/HeroVariant.prefab,,0,hero.bundle,hero-hash,,0,",
                    "3,0,,,0,shared.bundle,shared-hash,,0,",
                    "4,0,,,0,ghost.bundle,ghost-hash,,0,",
                });
                File.WriteAllText(Path.Combine(tempDir, BResources.ART_ASSET_INFO_PATH), artAssetsInfoContent);

                var downloadItems = AssetsVersionController.BuildMissingFileServerAssetBundleValidationDownloadItems(
                    "http://files.example.com",
                    "android",
                    tempDir,
                    new AssetsVersionController.FileServerComponentContext()
                    {
                        ComponentKind = AssetsVersionController.FileServerComponentKind.AssetBundle,
                        Version = "45",
                        AssetItems = new List<AssetItem>()
                        {
                            new AssetItem() {Id = 1, LocalPath = "art_assets/hero.bundle", HashName = "hero-hash"},
                            new AssetItem() {Id = 2, LocalPath = "art_assets/shared.bundle", HashName = "shared-hash"},
                            new AssetItem() {Id = 3, LocalPath = "art_assets/art_assets.info", HashName = "info-hash"},
                        },
                    });

                EnsureEqual(2L, downloadItems.Count, "补下载 AssetBundle 数量不匹配。");
                EnsureEqual("art_assets/hero.bundle", downloadItems[0].AssetItem.LocalPath,
                    "第一个补下载 AssetBundle 本地路径不匹配。");
                EnsureEqual("hero-hash", downloadItems[0].AssetItem.HashName,
                    "第一个补下载 AssetBundle Hash 不匹配。");
                EnsureEqual("art_assets/shared.bundle", downloadItems[1].AssetItem.LocalPath,
                    "第二个补下载 AssetBundle 本地路径不匹配。");
                EnsureEqual("shared-hash", downloadItems[1].AssetItem.HashName,
                    "第二个补下载 AssetBundle Hash 不匹配。");
                EnsureTrue(downloadItems[0].RemoteUrl.EndsWith("/files/ClientRes_Assetbundle_android/45/hero-hash"),
                    "第一个补下载 AssetBundle 远端路径不匹配。");
                EnsureTrue(downloadItems[1].RemoteUrl.EndsWith("/files/ClientRes_Assetbundle_android/45/shared-hash"),
                    "第二个补下载 AssetBundle 远端路径不匹配。");
                EnsureTrue(downloadItems[0].FinalLocalPath.Replace("\\", "/").EndsWith("/art_assets/hero.bundle"),
                    "第一个补下载 AssetBundle 本地落盘路径不匹配。");
                EnsureTrue(downloadItems[1].FinalLocalPath.Replace("\\", "/").EndsWith("/art_assets/shared.bundle"),
                    "第二个补下载 AssetBundle 本地落盘路径不匹配。");
                EnsureTrue(downloadItems[0].RequireHashValidation, "第一个补下载 AssetBundle 应启用 hash 校验。");
                EnsureTrue(downloadItems[1].RequireHashValidation, "第二个补下载 AssetBundle 应启用 hash 校验。");
                EnsureTrue(downloadItems.TrueForAll(item => item.AssetItem.LocalPath != "art_assets/ghost.bundle"),
                    "未出现在受管清单里的 AssetBundle 不应进入补下载列表。");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证缺少本地 package_build.info 时的无回退读取结果，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyLoadLocalPackageBuildInfoReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var packageBuildInfo = AssetsVersionController.LoadLocalPackageBuildInfo(tempDir, false);

                EnsureEqual("0.0.0", packageBuildInfo.Version, "缺少本地 package_build.info 时 Version 默认值不匹配。");
                EnsureEqual("none", packageBuildInfo.HotfixScriptSVCVersion,
                    "缺少本地 package_build.info 时 HotfixScriptSVCVersion 默认值不匹配。");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证 CI 独立下载目录下的 hash 校验逻辑，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyIsFileServerDownloadedAssetValidValidatesOnlyDownloadedPayloadFile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            var assetRelativePath = "script/hotfix/Game.dll.bytes";
            var assetAbsolutePath = Path.Combine(tempDir, "script", "hotfix", "Game.dll.bytes");
            Directory.CreateDirectory(Path.GetDirectoryName(assetAbsolutePath) ?? tempDir);
            File.WriteAllBytes(assetAbsolutePath, Encoding.UTF8.GetBytes("verify-payload"));

            try
            {
                var hash = FileHelper.GetMurmurHash3(assetAbsolutePath);
                var assetItem = new AssetItem()
                {
                    Id = 701,
                    LocalPath = assetRelativePath,
                    HashName = hash,
                };

                EnsureTrue(AssetsVersionController.IsFileServerDownloadedAssetValid(tempDir, assetItem),
                    "CI 本地下载目录 hash 校验应该通过。");

                assetItem.HashName = "invalid-hash";
                EnsureTrue(!AssetsVersionController.IsFileServerDownloadedAssetValid(tempDir, assetItem),
                    "CI 本地下载目录 hash 校验应该拒绝错误 hash。");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证热更代码代表性资源的真实本地装载，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyValidateFileServerCodeRepresentativeLocalLoadSucceedsForManagedAssemblyFile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var assemblyPath = typeof(AssetsVersionControllerDevOpsTest).Assembly.Location;
            var localCodePath = Path.Combine(tempDir, "Assembly-CSharp-firstpass.zlua.bytes");
            File.Copy(assemblyPath, localCodePath, true);

            try
            {
                var error = AssetsVersionController.ValidateFileServerCodeRepresentativeLocalLoad(localCodePath);

                EnsureEqual<string>(null, error, "热更代码代表性资源本地装载校验不应失败。");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证 AssetBundle 代表性资源的真实本地打开，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyValidateFileServerAssetBundleRepresentativeLocalLoadSucceedsForBuiltBundle()
        {
            var assetRootRelativePath = $"Assets/__FileServerVerifyTemp/{Guid.NewGuid():N}";
            var assetFileRelativePath = $"{assetRootRelativePath}/verify_asset.txt";
            var assetRootAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetRootRelativePath);
            var assetFileAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetFileRelativePath);
            var outputRoot = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            var bundleName = $"file-server-verify-{Guid.NewGuid():N}";
            var bundlePath = Path.Combine(outputRoot, bundleName);

            Directory.CreateDirectory(assetRootAbsolutePath);
            Directory.CreateDirectory(outputRoot);
            File.WriteAllText(assetFileAbsolutePath, "verify-bundle-payload", Encoding.UTF8);

            try
            {
                AssetDatabase.ImportAsset(assetFileRelativePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(assetFileRelativePath);
                if (importer == null)
                {
                    throw new InvalidOperationException($"无法获取测试资源导入器 path={assetFileRelativePath}");
                }

                importer.assetBundleName = bundleName;
                importer.SaveAndReimport();

                var buildManifest = BuildPipeline.BuildAssetBundles(
                    outputRoot,
                    BuildAssetBundleOptions.None,
                    ResolveHostAssetBundleBuildTarget());
                EnsureTrue(buildManifest != null, "测试 AssetBundle 构建结果不应为 null。");
                EnsureTrue(File.Exists(bundlePath), "测试 AssetBundle 输出文件不存在。");

                var error = AssetsVersionController.ValidateFileServerAssetBundleRepresentativeLocalLoad(bundlePath);

                EnsureEqual<string>(null, error, "AssetBundle 代表性资源本地打开校验不应失败。");
            }
            finally
            {
                AssetDatabase.RemoveAssetBundleName(bundleName, true);
                AssetDatabase.DeleteAsset(assetRootRelativePath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (Directory.Exists(outputRoot))
                {
                    Directory.Delete(outputRoot, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证资产级本地加载校验会根据 art_assets.info 的单资产记录执行真实 LoadAsset，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyValidateFileServerAssetBundleLocalLoadsSucceedsForBuiltBundleAsset()
        {
            var assetRootRelativePath = $"Assets/__FileServerVerifyTemp/{Guid.NewGuid():N}";
            var assetFileRelativePath = $"{assetRootRelativePath}/verify_asset.txt";
            var assetRootAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetRootRelativePath);
            var assetFileAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetFileRelativePath);
            var outputRoot = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            var bundleName = $"file-server-verify-{Guid.NewGuid():N}";
            var bundlePath = Path.Combine(outputRoot, bundleName);

            Directory.CreateDirectory(assetRootAbsolutePath);
            Directory.CreateDirectory(outputRoot);
            File.WriteAllText(assetFileAbsolutePath, "verify-bundle-payload", Encoding.UTF8);

            try
            {
                AssetDatabase.ImportAsset(assetFileRelativePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(assetFileRelativePath);
                if (importer == null)
                {
                    throw new InvalidOperationException($"无法获取测试资源导入器 path={assetFileRelativePath}");
                }

                importer.assetBundleName = bundleName;
                importer.SaveAndReimport();

                var buildManifest = BuildPipeline.BuildAssetBundles(
                    outputRoot,
                    BuildAssetBundleOptions.None,
                    ResolveHostAssetBundleBuildTarget());
                EnsureTrue(buildManifest != null, "测试 AssetBundle 构建结果不应为 null。");
                EnsureTrue(File.Exists(bundlePath), "测试 AssetBundle 输出文件不存在。");

                var validationEntries = new List<AssetsVersionController.FileServerAssetBundleValidationEntry>()
                {
                    new AssetsVersionController.FileServerAssetBundleValidationEntry()
                    {
                        AssetId = 1,
                        AssetDisplayPath = assetFileRelativePath,
                        AssetLoadPath = assetFileRelativePath,
                        AssetBundleRelativePath = $"art_assets/{bundleName}",
                        AssetBundleLocalPath = bundlePath,
                    },
                };

                var error = AssetsVersionController.ValidateFileServerAssetBundleLocalLoads(validationEntries);

                EnsureEqual<string>(null, error, "AssetBundle 资产级本地加载校验不应失败。");
            }
            finally
            {
                AssetDatabase.RemoveAssetBundleName(bundleName, true);
                AssetDatabase.DeleteAsset(assetRootRelativePath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (Directory.Exists(outputRoot))
                {
                    Directory.Delete(outputRoot, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证代表性 AssetBundle 主线程投递 helper 在主线程直跑场景下不会吞掉底层错误信息。
        /// </summary>
        internal static void VerifyValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContextReturnsMissingFileErrorOnMainThread()
        {
            var missingBundlePath = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"), "missing.bundle");

            var error = AssetsVersionController
                .ValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContext(missingBundlePath)
                .GetAwaiter()
                .GetResult();

            EnsureTrue(error != null && error.Contains($"path={missingBundlePath}"),
                "代表性 AssetBundle 主线程投递 helper 应保留原始缺文件错误。");
        }

        /// <summary>
        /// 以主线程直接调用方式验证代表性本地加载总入口会先通过第一个真实资产记录，
        /// 再在第二个缺失 bundle 的资产记录上返回底层错误，从而证明 AssetBundle 校验会按 art_assets.info 资产列表逐个遍历。
        /// 该校验覆盖 CI batchmode 外层同步桥接在后台阶段结束后回到主线程执行的最终路径。
        /// </summary>
        internal static void VerifyValidateFileServerRepresentativeLocalLoadsReturnsAssetBundleMissingFileErrorOnMainThread()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var localCodePath = Path.Combine(tempDir, "Assembly-CSharp-firstpass.zlua.bytes");
            var sourceAssemblyPath = typeof(AssetsVersionControllerDevOpsTest).Assembly.Location;
            var assetRootRelativePath = $"Assets/__FileServerVerifyTemp/{Guid.NewGuid():N}";
            var assetFileRelativePath = $"{assetRootRelativePath}/verify_asset.txt";
            var assetRootAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetRootRelativePath);
            var assetFileAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetFileRelativePath);
            var outputRoot = Path.Combine(tempDir, "bundle-output");
            var bundleName = $"file-server-verify-{Guid.NewGuid():N}";
            var firstBundlePath = Path.Combine(outputRoot, bundleName);
            var missingBundlePath = Path.Combine(tempDir, "missing.bundle");

            try
            {
                File.Copy(sourceAssemblyPath, localCodePath, true);
                Directory.CreateDirectory(assetRootAbsolutePath);
                Directory.CreateDirectory(outputRoot);
                File.WriteAllText(assetFileAbsolutePath, "verify-bundle-payload", Encoding.UTF8);

                AssetDatabase.ImportAsset(assetFileRelativePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(assetFileRelativePath);
                if (importer == null)
                {
                    throw new InvalidOperationException($"无法获取测试资源导入器 path={assetFileRelativePath}");
                }

                importer.assetBundleName = bundleName;
                importer.SaveAndReimport();

                var buildManifest = BuildPipeline.BuildAssetBundles(
                    outputRoot,
                    BuildAssetBundleOptions.None,
                    ResolveHostAssetBundleBuildTarget());
                EnsureTrue(buildManifest != null, "测试 AssetBundle 构建结果不应为 null。");
                EnsureTrue(File.Exists(firstBundlePath), "测试 AssetBundle 输出文件不存在。");

                var controller = new AssetsVersionController();
                var validationEntries = new List<AssetsVersionController.FileServerAssetBundleValidationEntry>()
                {
                    new AssetsVersionController.FileServerAssetBundleValidationEntry()
                    {
                        AssetId = 1,
                        AssetDisplayPath = assetFileRelativePath,
                        AssetLoadPath = assetFileRelativePath,
                        AssetBundleRelativePath = $"art_assets/{bundleName}",
                        AssetBundleLocalPath = firstBundlePath,
                    },
                    new AssetsVersionController.FileServerAssetBundleValidationEntry()
                    {
                        AssetId = 2,
                        AssetDisplayPath = "Assets/Missing.prefab",
                        AssetLoadPath = "Assets/Missing.prefab",
                        AssetBundleRelativePath = "art_assets/missing.bundle",
                        AssetBundleLocalPath = missingBundlePath,
                    },
                };
                var error = controller
                    .ValidateFileServerRepresentativeLocalLoads(localCodePath,
                        validationEntries,
                        Path.Combine(tempDir, "unused.db"))
                    .GetAwaiter()
                    .GetResult();

                EnsureTrue(error != null && error.Contains($"bundle={missingBundlePath}"),
                    "主线程收口后的代表性本地加载应保留 AssetBundle 缺 bundle 错误。");
                EnsureTrue(!error.Contains("切换 Unity 主线程超时"),
                    "主线程收口后的代表性本地加载不应再返回主线程派发超时。");
            }
            finally
            {
                AssetDatabase.RemoveAssetBundleName(bundleName, true);
                AssetDatabase.DeleteAsset(assetRootRelativePath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证表格代表性资源的真实 SQLite 打开，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyValidateFileServerTableRepresentativeLocalLoadSucceedsForReadableSqlite()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFramework", "AssetsVersionControllerDevOpsTest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "local.db");
            var oldPassword = SqliteLoder.password;
            SqliteLoder.Password = string.Empty;

            try
            {
                var createConnection = SqliteLoder.LoadDBReadWriteCreate(dbPath, false);
                EnsureTrue(createConnection != null, "测试 SQLite 数据库创建结果不应为 null。");
                SqliteLoder.Close("local");

                var error = AssetsVersionController.ValidateFileServerTableRepresentativeLocalLoad(dbPath);

                EnsureEqual<string>(null, error, "表格代表性资源本地打开校验不应失败。");
            }
            finally
            {
                SqliteLoder.Close("local");
                SqliteLoder.Password = oldPassword;
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 以纯异常校验方式验证请求超时包装器的成功路径，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyAwaitFileServerRequestWithTimeoutReturnsResultWhenTaskCompletesInTime()
        {
            var cancelInvoked = false;
            var result = AssetsVersionController.AwaitFileServerRequestWithTimeout(
                Task.FromResult("ok"),
                TimeSpan.FromMilliseconds(20),
                "http://127.0.0.1/success",
                () => cancelInvoked = true).GetAwaiter().GetResult();

            EnsureEqual("ok", result, "超时包装器成功路径应返回原始结果。");
            EnsureTrue(!cancelInvoked, "超时包装器成功路径不应触发取消回调。");
        }

        /// <summary>
        /// 以纯异常校验方式验证请求超时包装器的超时路径，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyAwaitFileServerRequestWithTimeoutThrowsTimeoutWhenTaskDoesNotFinish()
        {
            var neverCompletes = new TaskCompletionSource<string>();
            var cancelInvoked = false;

            try
            {
                AssetsVersionController.AwaitFileServerRequestWithTimeout(
                    neverCompletes.Task,
                    TimeSpan.FromMilliseconds(20),
                    "http://127.0.0.1/slow",
                    () => cancelInvoked = true).GetAwaiter().GetResult();
                throw new InvalidOperationException("长时间无响应的文件服务器请求应触发 TimeoutException。");
            }
            catch (TimeoutException exception)
            {
                EnsureTrue(exception.Message.Contains("http://127.0.0.1/slow"),
                    "超时异常应包含请求地址，方便 CI 直接定位问题资源。");
            }

            EnsureTrue(cancelInvoked, "超时路径应触发取消回调，避免底层请求继续悬挂。");
        }

        /// <summary>
        /// 以纯异常校验方式验证 CI BatchMode 文件服务器进度日志格式，供 TeamCity 阶段排障复用。
        /// </summary>
        internal static void VerifyFormatFileServerBatchProgressMessageIncludesStageCountsAndTarget()
        {
            var message = AssetsVersionController.FormatFileServerBatchProgressMessage(
                "校验资源",
                7,
                61,
                "hotfix/code.dll",
                "component=Code");

            EnsureEqual("校验资源 progress=7/61 target=hotfix/code.dll component=Code", message,
                "进度日志格式应稳定输出阶段、序号、路径和补充信息。");
        }

        /// <summary>
        /// 以纯异常校验方式验证 CI BatchMode 文件服务器进度日志的缺省占位文案。
        /// </summary>
        internal static void VerifyFormatFileServerBatchProgressMessageUsesFallbackForMissingValues()
        {
            var message = AssetsVersionController.FormatFileServerBatchProgressMessage(null, -3, -9, null);

            EnsureEqual("unknown progress=0/0 target=<none>", message,
                "进度日志缺省输入时应回退成稳定占位文本。");
        }

        /// <summary>
        /// 解析当前编辑器宿主能直接本地打开的 AssetBundle 构建目标。
        /// 测试只关心 bundle 是否可被当前 Unity 进程打开，因此固定映射到宿主编辑器平台，避免用 Android/iOS 产物做本地打开校验时出现平台不匹配假失败。
        /// </summary>
        private static BuildTarget ResolveHostAssetBundleBuildTarget()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    return BuildTarget.StandaloneOSX;
                case RuntimePlatform.WindowsEditor:
                    return BuildTarget.StandaloneWindows64;
                case RuntimePlatform.LinuxEditor:
                    return BuildTarget.StandaloneLinux64;
                default:
                    throw new InvalidOperationException($"不支持的编辑器宿主平台:{Application.platform}");
            }
        }

        /// <summary>
        /// 验证 global_version.info JSON 数组可以按平台提取 version_num 并解析为三段版控。
        /// </summary>
        [Test]
        public void TryParseGlobalVersionInfoJson_ExtractsPlatformVersionNum()
        {
            VerifyTryParseGlobalVersionInfoJsonExtractsPlatformVersionNum();
        }

        /// <summary>
        /// 以纯异常校验方式验证 global_version.info JSON 解析，供 batchmode 路径复用。
        /// </summary>
        internal static void VerifyTryParseGlobalVersionInfoJsonExtractsPlatformVersionNum()
        {
            var json = "[{\"key\":\"default\",\"platform\":\"android\",\"version_num\":\"20.22.17\",\"game_server_ip\":\"127.0.0.1\"}]";

            var parsed = AssetsVersionControllerDevOpsPureLogic.TryParseGlobalVersionInfoJson(json, "android",
                out var versionInfo);
            EnsureTrue(parsed, "TryParseGlobalVersionInfoJson(android) 应返回 true。");
            EnsureEqual("20", versionInfo?.CodeVersion, "CodeVersion 不匹配。");
            EnsureEqual("22", versionInfo?.AssetBundleVersion, "AssetBundleVersion 不匹配。");
            EnsureEqual("17", versionInfo?.TableVersion, "TableVersion 不匹配。");
            EnsureEqual("20.22.17", versionInfo?.RawValue, "RawValue 不匹配。");

            // 非 android 平台应返回 false
            var parsedIos = AssetsVersionControllerDevOpsPureLogic.TryParseGlobalVersionInfoJson(json, "ios",
                out var iosVersionInfo);
            EnsureTrue(!parsedIos, "TryParseGlobalVersionInfoJson(ios) 在只有 android 条目时应返回 false。");
            EnsureTrue(iosVersionInfo == null, "ios 解析失败时 versionInfo 应为 null。");

            // 多条目：验证 ios 也能匹配
            var multiJson = "[{\"key\":\"default\",\"platform\":\"android\",\"version_num\":\"20.22.17\",\"game_server_ip\":\"127.0.0.1\"},"
                            + "{\"key\":\"default\",\"platform\":\"ios\",\"version_num\":\"30.40.50\",\"game_server_ip\":\"127.0.0.1\"}]";
            var parsedMulti = AssetsVersionControllerDevOpsPureLogic.TryParseGlobalVersionInfoJson(multiJson, "ios",
                out var iosMultiInfo);
            EnsureTrue(parsedMulti, "TryParseGlobalVersionInfoJson(ios) 多条目应返回 true。");
            EnsureEqual("30", iosMultiInfo?.CodeVersion, "ios CodeVersion 不匹配。");
            EnsureEqual("40", iosMultiInfo?.AssetBundleVersion, "ios AssetBundleVersion 不匹配。");
            EnsureEqual("50", iosMultiInfo?.TableVersion, "ios TableVersion 不匹配。");
        }

        /// <summary>
        /// 批验证私有断言：期望值不一致时抛出显式异常，避免依赖 NUnit 约束对象。
        /// </summary>
        internal static void EnsureEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"{message} expected={expected} actual={actual}");
            }
        }

        /// <summary>
        /// 批验证私有断言：条件不满足时抛出显式异常，避免依赖 NUnit 断言入口。
        /// </summary>
        internal static void EnsureTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    /// <summary>
    /// AssetsVersionController 文件服务器协议的独立 batch 验证入口。
    /// 该类型不承载 NUnit 测试声明，只负责把纯逻辑校验按 batchmode 需要的报告和退出码组织起来，
    /// 供本地验证与 CI 在 `-executeMethod` 场景下稳定调用。
    /// </summary>
    /// <remarks>
    /// Unity Test Runner 仍通过 <see cref="AssetsVersionControllerDevOpsTest"/> 执行同一组断言；
    /// batchmode 则走这个更薄的协调器，避免测试宿主差异影响本地前置校验。
    /// </remarks>
    public static class AssetsVersionControllerDevOpsBatchVerification
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// 入口会顺序执行所有纯逻辑校验，生成 Library 报告并通过退出码反馈整体结果。
        /// </summary>
        public static void RunBatchVerification()
        {
            // Phase 1: 顺序执行纯逻辑校验，并把每个步骤的结果写入统一报告。
            AssetsVersionControllerDevOpsTest.LogTestPurposeAndMeans(
                nameof(AssetsVersionControllerDevOpsBatchVerification),
                "验证 AssetsVersionController 文件服务器 BatchMode 纯逻辑路径在离线环境下的关键契约。",
                "顺序执行纯逻辑验证入口、写出批验证报告，并用显式退出码反馈结果。");
            Debug.Log("[测试进度] suite=AssetsVersionControllerDevOpsBatchVerification stage=start");
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, Action Action)[]
            {
                (nameof(AssetsVersionControllerDevOpsTest.TryParseGlobalVersionInfoJson_ExtractsPlatformVersionNum),
                    AssetsVersionControllerDevOpsTest.VerifyTryParseGlobalVersionInfoJsonExtractsPlatformVersionNum),
                (nameof(AssetsVersionControllerDevOpsTest.TryParseFileServerVersionInfo_ParsesThreeSegments),
                    AssetsVersionControllerDevOpsTest.VerifyTryParseFileServerVersionInfoParsesThreeSegments),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateExpectedFileServerVersionInfo_ReturnsErrorForMismatchedManifest),
                    AssetsVersionControllerDevOpsTest.VerifyValidateExpectedFileServerVersionInfoReturnsErrorForMismatchedManifest),
                (nameof(AssetsVersionControllerDevOpsTest.MergeFileServerPackageBuildInfo_MergesComponentSpecificFields),
                    AssetsVersionControllerDevOpsTest.VerifyMergeFileServerPackageBuildInfoMergesComponentSpecificFields),
                (nameof(AssetsVersionControllerDevOpsTest.NormalizeFileServerComponentPackageBuildInfo_FallsBackToComponentVersionWhenSourceUsesNone),
                    AssetsVersionControllerDevOpsTest.VerifyNormalizeFileServerComponentPackageBuildInfoFallsBackToComponentVersionWhenSourceUsesNone),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerPackageBuildInfo_ReturnsErrorForMismatchedComponentVersions),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerPackageBuildInfoReturnsErrorForMismatchedComponentVersions),
                (nameof(AssetsVersionControllerDevOpsTest.BuildFileServerSubPackageAssetItems_CollectsConfiguredAssetsAcrossComponents),
                    AssetsVersionControllerDevOpsTest.VerifyBuildFileServerSubPackageAssetItemsCollectsConfiguredAssetsAcrossComponents),
                (nameof(AssetsVersionControllerDevOpsTest.NormalizeFileServerManagedAssetItems_FiltersPackageBuildInfoAndSortsById),
                    AssetsVersionControllerDevOpsTest.VerifyNormalizeFileServerManagedAssetItemsFiltersPackageBuildInfoAndSortsById),
                (nameof(AssetsVersionControllerDevOpsTest.ShouldUseCachedFileServerComponentContextOnFailure_DisablesFallbackForStrictRemoteVerification),
                    AssetsVersionControllerDevOpsTest.VerifyShouldUseCachedFileServerComponentContextOnFailureDisablesFallbackForStrictRemoteVerification),
                (nameof(AssetsVersionControllerDevOpsTest.BuildFileServerAssetRemoteRelativePath_PrefersHashNameForManagedAssets),
                    AssetsVersionControllerDevOpsTest.VerifyBuildFileServerAssetRemoteRelativePathPrefersHashNameForManagedAssets),
                (nameof(AssetsVersionControllerDevOpsTest.FindFileServerRepresentativeAsset_PicksRealPayloadAssets),
                    AssetsVersionControllerDevOpsTest.VerifyFindFileServerRepresentativeAssetPicksRealPayloadAssets),
                (nameof(AssetsVersionControllerDevOpsTest.CollectFileServerAssetBundleValidationRelativePaths_DeduplicatesNonEmptyBundlePaths),
                    AssetsVersionControllerDevOpsTest.VerifyCollectFileServerAssetBundleValidationRelativePathsDeduplicatesNonEmptyBundlePaths),
                (nameof(AssetsVersionControllerDevOpsTest.CollectFileServerAssetBundleValidationRelativePathsFromArtAssetsInfoContent_DeduplicatesNonEmptyBundlePaths),
                    AssetsVersionControllerDevOpsTest.VerifyCollectFileServerAssetBundleValidationRelativePathsFromArtAssetsInfoContentDeduplicatesNonEmptyBundlePaths),
                (nameof(AssetsVersionControllerDevOpsTest.CollectFileServerAssetBundleValidationEntriesFromArtAssetsInfoContent_KeepsLoadableAssetRows),
                    AssetsVersionControllerDevOpsTest.VerifyCollectFileServerAssetBundleValidationEntriesFromArtAssetsInfoContentKeepsLoadableAssetRows),
                (nameof(AssetsVersionControllerDevOpsTest.BuildMissingFileServerAssetBundleValidationDownloadItems_UsesArtAssetsInfoForMissingBundles),
                    AssetsVersionControllerDevOpsTest.VerifyBuildMissingFileServerAssetBundleValidationDownloadItemsUsesArtAssetsInfoForMissingBundles),
                (nameof(AssetsVersionControllerDevOpsTest.LoadLocalPackageBuildInfo_ReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing),
                    AssetsVersionControllerDevOpsTest.VerifyLoadLocalPackageBuildInfoReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing),
                (nameof(AssetsVersionControllerDevOpsTest.IsFileServerDownloadedAssetValid_ValidatesOnlyDownloadedPayloadFile),
                    AssetsVersionControllerDevOpsTest.VerifyIsFileServerDownloadedAssetValidValidatesOnlyDownloadedPayloadFile),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerCodeRepresentativeLocalLoad_SucceedsForManagedAssemblyFile),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerCodeRepresentativeLocalLoadSucceedsForManagedAssemblyFile),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerAssetBundleRepresentativeLocalLoad_SucceedsForBuiltBundle),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerAssetBundleRepresentativeLocalLoadSucceedsForBuiltBundle),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerAssetBundleLocalLoads_SucceedsForBuiltBundleAsset),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerAssetBundleLocalLoadsSucceedsForBuiltBundleAsset),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContext_ReturnsMissingFileErrorOnMainThread),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContextReturnsMissingFileErrorOnMainThread),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerRepresentativeLocalLoads_ReturnsAssetBundleMissingFileErrorOnMainThread),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerRepresentativeLocalLoadsReturnsAssetBundleMissingFileErrorOnMainThread),
                (nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerTableRepresentativeLocalLoad_SucceedsForReadableSqlite),
                    AssetsVersionControllerDevOpsTest.VerifyValidateFileServerTableRepresentativeLocalLoadSucceedsForReadableSqlite),
                (nameof(AssetsVersionControllerDevOpsTest.AwaitFileServerRequestWithTimeout_ReturnsResultWhenTaskCompletesInTime),
                    AssetsVersionControllerDevOpsTest.VerifyAwaitFileServerRequestWithTimeoutReturnsResultWhenTaskCompletesInTime),
                (nameof(AssetsVersionControllerDevOpsTest.AwaitFileServerRequestWithTimeout_ThrowsTimeoutWhenTaskDoesNotFinish),
                    AssetsVersionControllerDevOpsTest.VerifyAwaitFileServerRequestWithTimeoutThrowsTimeoutWhenTaskDoesNotFinish),
                (nameof(AssetsVersionControllerDevOpsTest.FormatFileServerBatchProgressMessage_IncludesStageCountsAndTarget),
                    AssetsVersionControllerDevOpsTest.VerifyFormatFileServerBatchProgressMessageIncludesStageCountsAndTarget),
                (nameof(AssetsVersionControllerDevOpsTest.FormatFileServerBatchProgressMessage_UsesFallbackForMissingValues),
                    AssetsVersionControllerDevOpsTest.VerifyFormatFileServerBatchProgressMessageUsesFallbackForMissingValues),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                RunCheck(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
            }

            // Phase 2: 把批验证结果写到 Library，方便 CI 和本地 batchmode 直接收集报告。
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "AssetsVersionControllerDevOpsBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            // Phase 3: 返回显式退出码，让 batchmode 可以直接据此判断验证是否通过。
            if (failedCount > 0)
            {
                Debug.LogError(
                    $"AssetsVersionController DevOps standalone batch verification failed. Report: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(
                $"AssetsVersionController DevOps standalone batch verification passed. Report: {outputPath}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 执行单个纯逻辑校验并把结果写入 batch 验证报告。
        /// </summary>
        private static void RunCheck(int currentIndex, int totalCount, string checkName, Action checkAction,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            AssetsVersionControllerDevOpsTest.LogTestPurposeAndMeans(checkName,
                $"验证 {checkName} 对应代码路径的行为与错误契约。",
                "直接调用纯逻辑验证入口并断言返回结果、错误信息与流程进度日志。");
            Debug.Log($"[测试进度] suite=AssetsVersionControllerDevOpsBatchVerification current={currentIndex}/{totalCount} name={checkName}");
            try
            {
                checkAction();
                reportBuilder.AppendLine($"PASS {checkName}");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {checkName}");
                reportBuilder.AppendLine(exception.ToString());
            }
        }
    }
}