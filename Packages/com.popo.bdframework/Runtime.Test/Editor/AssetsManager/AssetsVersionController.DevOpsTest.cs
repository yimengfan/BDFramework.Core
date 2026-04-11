using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BDFramework.Asset;
using BDFramework.ResourceMgr;
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
            const int totalCheckCount = 11;

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