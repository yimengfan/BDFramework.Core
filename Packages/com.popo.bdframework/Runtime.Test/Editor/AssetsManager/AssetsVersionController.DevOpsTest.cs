using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BDFramework.Asset;
using BDFramework.ResourceMgr;
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
        /// 验证代表性 AssetBundle 主线程投递 helper 在当前已经位于主线程时，会直接返回底层本地打开校验结果。
        /// </summary>
        [Test]
        public void ValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContext_ReturnsMissingFileErrorOnMainThread()
        {
            VerifyValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContextReturnsMissingFileErrorOnMainThread();
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
        /// 入口会顺序执行六组纯逻辑校验，生成 Library 报告并通过退出码反馈整体结果。
        /// </summary>
        public static void RunBatchVerification()
        {
            // Phase 1: 顺序执行纯逻辑校验，并把每个步骤的结果写入统一报告。
            Debug.Log("AssetsVersionController DevOps standalone batch verification starting.");
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            const int totalCheckCount = 21;

            RunCheck(nameof(AssetsVersionControllerDevOpsTest.TryParseGlobalVersionInfoJson_ExtractsPlatformVersionNum),
                AssetsVersionControllerDevOpsTest.VerifyTryParseGlobalVersionInfoJsonExtractsPlatformVersionNum,
                reportBuilder, ref failedCount);
            RunCheck(nameof(AssetsVersionControllerDevOpsTest.TryParseFileServerVersionInfo_ParsesThreeSegments),
                AssetsVersionControllerDevOpsTest.VerifyTryParseFileServerVersionInfoParsesThreeSegments,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ValidateExpectedFileServerVersionInfo_ReturnsErrorForMismatchedManifest),
                AssetsVersionControllerDevOpsTest.VerifyValidateExpectedFileServerVersionInfoReturnsErrorForMismatchedManifest,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.MergeFileServerPackageBuildInfo_MergesComponentSpecificFields),
                AssetsVersionControllerDevOpsTest.VerifyMergeFileServerPackageBuildInfoMergesComponentSpecificFields,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.NormalizeFileServerComponentPackageBuildInfo_FallsBackToComponentVersionWhenSourceUsesNone),
                AssetsVersionControllerDevOpsTest.VerifyNormalizeFileServerComponentPackageBuildInfoFallsBackToComponentVersionWhenSourceUsesNone,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerPackageBuildInfo_ReturnsErrorForMismatchedComponentVersions),
                AssetsVersionControllerDevOpsTest.VerifyValidateFileServerPackageBuildInfoReturnsErrorForMismatchedComponentVersions,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.BuildFileServerSubPackageAssetItems_CollectsConfiguredAssetsAcrossComponents),
                AssetsVersionControllerDevOpsTest.VerifyBuildFileServerSubPackageAssetItemsCollectsConfiguredAssetsAcrossComponents,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.NormalizeFileServerManagedAssetItems_FiltersPackageBuildInfoAndSortsById),
                AssetsVersionControllerDevOpsTest.VerifyNormalizeFileServerManagedAssetItemsFiltersPackageBuildInfoAndSortsById,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ShouldUseCachedFileServerComponentContextOnFailure_DisablesFallbackForStrictRemoteVerification),
                AssetsVersionControllerDevOpsTest.VerifyShouldUseCachedFileServerComponentContextOnFailureDisablesFallbackForStrictRemoteVerification,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.BuildFileServerAssetRemoteRelativePath_PrefersHashNameForManagedAssets),
                AssetsVersionControllerDevOpsTest.VerifyBuildFileServerAssetRemoteRelativePathPrefersHashNameForManagedAssets,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.FindFileServerRepresentativeAsset_PicksRealPayloadAssets),
                AssetsVersionControllerDevOpsTest.VerifyFindFileServerRepresentativeAssetPicksRealPayloadAssets,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.LoadLocalPackageBuildInfo_ReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing),
                AssetsVersionControllerDevOpsTest.VerifyLoadLocalPackageBuildInfoReturnsEmptyInfoWhenFallbackDisabledAndLocalFileMissing,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.IsFileServerDownloadedAssetValid_ValidatesOnlyDownloadedPayloadFile),
                AssetsVersionControllerDevOpsTest.VerifyIsFileServerDownloadedAssetValidValidatesOnlyDownloadedPayloadFile,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerCodeRepresentativeLocalLoad_SucceedsForManagedAssemblyFile),
                AssetsVersionControllerDevOpsTest.VerifyValidateFileServerCodeRepresentativeLocalLoadSucceedsForManagedAssemblyFile,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerAssetBundleRepresentativeLocalLoad_SucceedsForBuiltBundle),
                AssetsVersionControllerDevOpsTest.VerifyValidateFileServerAssetBundleRepresentativeLocalLoadSucceedsForBuiltBundle,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContext_ReturnsMissingFileErrorOnMainThread),
                AssetsVersionControllerDevOpsTest.VerifyValidateFileServerAssetBundleRepresentativeLocalLoadOnUnityContextReturnsMissingFileErrorOnMainThread,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.ValidateFileServerTableRepresentativeLocalLoad_SucceedsForReadableSqlite),
                AssetsVersionControllerDevOpsTest.VerifyValidateFileServerTableRepresentativeLocalLoadSucceedsForReadableSqlite,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.AwaitFileServerRequestWithTimeout_ReturnsResultWhenTaskCompletesInTime),
                AssetsVersionControllerDevOpsTest.VerifyAwaitFileServerRequestWithTimeoutReturnsResultWhenTaskCompletesInTime,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.AwaitFileServerRequestWithTimeout_ThrowsTimeoutWhenTaskDoesNotFinish),
                AssetsVersionControllerDevOpsTest.VerifyAwaitFileServerRequestWithTimeoutThrowsTimeoutWhenTaskDoesNotFinish,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.FormatFileServerBatchProgressMessage_IncludesStageCountsAndTarget),
                AssetsVersionControllerDevOpsTest.VerifyFormatFileServerBatchProgressMessageIncludesStageCountsAndTarget,
                reportBuilder, ref failedCount);
            RunCheck(
                nameof(AssetsVersionControllerDevOpsTest.FormatFileServerBatchProgressMessage_UsesFallbackForMissingValues),
                AssetsVersionControllerDevOpsTest.VerifyFormatFileServerBatchProgressMessageUsesFallbackForMissingValues,
                reportBuilder, ref failedCount);

            // Phase 2: 把批验证结果写到 Library，方便 CI 和本地 batchmode 直接收集报告。
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "AssetsVersionControllerDevOpsBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={totalCheckCount} passed={totalCheckCount - failedCount} failed={failedCount}{Environment.NewLine}");
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
        private static void RunCheck(string checkName, Action checkAction, StringBuilder reportBuilder,
            ref int failedCount)
        {
            Debug.Log($"AssetsVersionController DevOps standalone batch verification running {checkName}");
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