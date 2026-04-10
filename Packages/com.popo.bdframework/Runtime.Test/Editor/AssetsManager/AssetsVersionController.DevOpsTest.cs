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
    public class AssetsVersionControllerDevOpsTest
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// 当项目级初始化会干扰 Unity 原生 -runTests 流程时，可以通过 -executeMethod 直接调用这组纯逻辑测试。
        /// </summary>
        public static void RunBatchVerification()
        {
            var testInstance = new AssetsVersionControllerDevOpsTest();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;

            Execute(nameof(TryParseFileServerVersionInfo_ParsesThreeSegments),
                testInstance.TryParseFileServerVersionInfo_ParsesThreeSegments, reportBuilder, ref failedCount);
            Execute(nameof(MergeFileServerPackageBuildInfo_MergesComponentSpecificFields),
                testInstance.MergeFileServerPackageBuildInfo_MergesComponentSpecificFields, reportBuilder,
                ref failedCount);
            Execute(nameof(BuildFileServerSubPackageAssetItems_CollectsConfiguredAssetsAcrossComponents),
                testInstance.BuildFileServerSubPackageAssetItems_CollectsConfiguredAssetsAcrossComponents,
                reportBuilder, ref failedCount);

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "AssetsVersionControllerDevOpsBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total=3 passed={3 - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                Debug.LogError($"AssetsVersionController DevOps batch verification failed. Report: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"AssetsVersionController DevOps batch verification passed. Report: {outputPath}");
            EditorApplication.Exit(0);
        }

        [Test]
        public void TryParseFileServerVersionInfo_ParsesThreeSegments()
        {
            var parsed = AssetsVersionController.TryParseFileServerVersionInfo("101.202.303", out var versionInfo);

            Assert.That(parsed, Is.True);
            Assert.That(versionInfo.CodeVersion, Is.EqualTo("101"));
            Assert.That(versionInfo.AssetBundleVersion, Is.EqualTo("202"));
            Assert.That(versionInfo.TableVersion, Is.EqualTo("303"));
            Assert.That(versionInfo.RawValue, Is.EqualTo("101.202.303"));
        }

        [Test]
        public void MergeFileServerPackageBuildInfo_MergesComponentSpecificFields()
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

            var merged = AssetsVersionController.MergeFileServerPackageBuildInfo(baseInfo, codeInfo, assetBundleInfo,
                tableInfo);

            Assert.That(merged.Version, Is.EqualTo("0.1.0"));
            Assert.That(merged.BasePckScriptSVCVersion, Is.EqualTo("base"));
            Assert.That(merged.HotfixScriptSVCVersion, Is.EqualTo("new-hotfix"));
            Assert.That(merged.AssetBundleSVCVersion, Is.EqualTo("new-ab"));
            Assert.That(merged.TableSVCVersion, Is.EqualTo("new-table"));
            Assert.That(merged.BuildTime, Is.EqualTo(30));
        }

        [Test]
        public void BuildFileServerSubPackageAssetItems_CollectsConfiguredAssetsAcrossComponents()
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

            var selected = AssetsVersionController.BuildFileServerSubPackageAssetItems(subPackageConfig,
                assetBundleAssets, codeAssets, tableAssets);

            Assert.That(selected, Has.Count.EqualTo(4));
            Assert.That(selected.Exists(item => item.LocalPath == "art_assets/demo.bundle"), Is.True);
            Assert.That(selected.Exists(item => item.LocalPath == "script/hotfix/Game.dll.bytes"), Is.True);
            Assert.That(selected.Exists(item => item.LocalPath == "local.db"), Is.True);
            Assert.That(selected.Exists(item => item.LocalPath == "art_assets/art_assets.info"), Is.True);
        }

        private static void Execute(string testName, Action testAction, StringBuilder reportBuilder, ref int failedCount)
        {
            try
            {
                testAction();
                reportBuilder.AppendLine($"PASS {testName}");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
            }
        }
    }
}