using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.EditorTest.AssetsManager
{
    /// <summary>
    /// 覆盖 BResources 核心静态契约的编辑器测试。
    /// Editor tests covering the core static contracts of BResources.
    /// 这些断言只验证资源路径拼接、资源组缓存与空列表异步加载保护，不依赖真实资源文件或 AssetBundle 环境，
    /// 以便在 BatchMode 下快速锁定资源主链路的基础回归。
    /// These assertions only verify resource-path composition, asset-group caching, and the empty-list async-load guard without depending on real resource files or an AssetBundle environment,
    /// which keeps foundational regressions in the resource mainline local and fast in BatchMode.
    /// </summary>
    public class BResourcesTest
    {
        private readonly List<string> trackedGroups = new List<string>();

        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// Explicit verification entry exposed for batchmode.
        /// 当 Unity 原生 <c>-runTests</c> 结果导出不稳定时，可以通过这个入口直接执行 BResources 的核心契约断言。
        /// When Unity native <c>-runTests</c> result export is unreliable, this entry can directly execute the core BResources contract assertions.
        /// </summary>
        public static void RunBatchVerification()
        {
            BResourcesBatchVerification.RunBatchVerification();
        }

        /// <summary>
        /// 在每个测试开始时输出统一的测试目的与实现手段日志。
        /// Emit a unified purpose-and-means log at the start of each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            string testName;
            try
            {
                testName = TestContext.CurrentContext?.Test?.Name;
            }
            catch
            {
                testName = null;
            }

            LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(BResourcesTest) : testName,
                "验证 BResources 的核心静态契约不会因为资源链路调整而回归。",
                "直接调用 BResources 的路径与缓存辅助入口，并断言返回值、兼容分支和空列表保护结果。"
            );
        }

        /// <summary>
        /// 在每个测试结束后清理本次创建的资源组缓存，避免静态状态污染后续断言。
        /// Clear the asset-group cache created by the current test after each run so static state does not leak into later assertions.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var groupName in trackedGroups)
            {
                BResources.ClearAssetGroup(groupName);
            }

            trackedGroups.Clear();
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// Emit a unified test-start log that always includes the purpose and means.
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 验证服务器版控文件路径会稳定拼接平台目录和固定文件名。
        /// Verify that the server version-info path consistently appends the platform directory and fixed file name.
        /// </summary>
        [Test]
        public void GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName()
        {
            var rootPath = Path.Combine("Root", "Server");
            var expected = Path.Combine(rootPath, BApplication.GetPlatformLoadPath(RuntimePlatform.Android),
                BResources.SERVER_ASSETS_VERSION_INFO_PATH);

            Assert.That(BResources.GetServerAssetsVersionInfoPath(rootPath, RuntimePlatform.Android),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// 验证资源信息路径的两个重载会分别走根目录和平台目录规则。
        /// Verify that the two resource-info path overloads follow the root-only and platform-directory rules respectively.
        /// </summary>
        [Test]
        public void GetAssetsInfoPath_OverloadsUseExpectedRules()
        {
            var rootPath = Path.Combine("Root", "Client");

            Assert.That(BResources.GetAssetsInfoPath(rootPath),
                Is.EqualTo(Path.Combine(rootPath, BResources.ASSETS_INFO_PATH)));
            Assert.That(BResources.GetAssetsInfoPath(rootPath, RuntimePlatform.WindowsPlayer),
                Is.EqualTo(Path.Combine(rootPath, BApplication.GetPlatformLoadPath(RuntimePlatform.WindowsPlayer),
                    BResources.ASSETS_INFO_PATH)));
        }

        /// <summary>
        /// 验证旧版分包命名会直接按原文件名拼接，保持兼容路径不被二次格式化。
        /// Verify that legacy sub-package names are appended directly without secondary formatting so compatibility paths remain stable.
        /// </summary>
        [Test]
        public void GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged()
        {
            var rootPath = Path.Combine("Root", "Client");
            const string legacyName = "ServerAssetsSubPackage_demo.info";
            var expected = Path.Combine(rootPath, BApplication.GetPlatformLoadPath(RuntimePlatform.Android), legacyName);

            Assert.That(BResources.GetAssetsSubPackageInfoPath(rootPath, RuntimePlatform.Android, legacyName),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// 验证新版分包名会被格式化为 server_assets_subpack_{name}.info 规则。
        /// Verify that modern sub-package names are formatted into the server_assets_subpack_{name}.info rule.
        /// </summary>
        [Test]
        public void GetAssetsSubPackageInfoPath_FormatsModernSubPackageName()
        {
            var rootPath = Path.Combine("Root", "Client");
            var expected = Path.Combine(rootPath, BApplication.GetPlatformLoadPath(RuntimePlatform.IPhonePlayer),
                string.Format(BResources.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH, "demo"));

            Assert.That(BResources.GetAssetsSubPackageInfoPath(rootPath, RuntimePlatform.IPhonePlayer, "demo"),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// 验证资源组缓存会保留写入顺序，并且清理后读取为空。
        /// Verify that the asset-group cache preserves insertion order and returns empty after cleanup.
        /// </summary>
        [Test]
        public void AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries()
        {
            var groupName = $"BResourcesTestGroup_{Guid.NewGuid():N}";
            trackedGroups.Add(groupName);

            BResources.AddAssetsPathToGroup(groupName, "a.prefab", "b.mat");
            BResources.AddAssetsPathToGroup(groupName, "c.png");

            Assert.That(BResources.GetAssetsPathByGroup(groupName),
                Is.EqualTo(new[] { "a.prefab", "b.mat", "c.png" }));

            BResources.ClearAssetGroup(groupName);
            trackedGroups.Remove(groupName);

            Assert.That(BResources.GetAssetsPathByGroup(groupName), Is.Empty);
        }

        /// <summary>
        /// 验证空资源列表异步加载会直接回调空结果，并且不要求事先初始化 ResLoader。
        /// Verify that async loading with an empty asset list immediately returns an empty result and does not require ResLoader initialization.
        /// </summary>
        [Test]
        public void AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader()
        {
            IDictionary<string, Object> callbackResult = null;
            var callbackInvoked = false;

            var ids = BResources.AsyncLoad(new List<string>(), onLoadEnd: result =>
            {
                callbackInvoked = true;
                callbackResult = result;
            });

            Assert.That(ids, Is.Empty);
            Assert.That(callbackInvoked, Is.True);
            Assert.That(callbackResult, Is.Not.Null);
            Assert.That(callbackResult.Count, Is.EqualTo(0));
        }
    }

    /// <summary>
    /// BResources 测试的显式批验证入口。
    /// Explicit batch verification entry for the BResources tests.
    /// 该入口顺序执行资源路径、资源组缓存和空列表异步加载相关断言，写出稳定的 Library 报告，
    /// 供本地与 CI 在 <c>-executeMethod</c> 场景下稳定调用。
    /// This entry executes the resource-path, asset-group cache, and empty-list async-load assertions sequentially, writes a stable Library report,
    /// and can be called reliably by local runs and CI through <c>-executeMethod</c>.
    /// </summary>
    internal static class BResourcesBatchVerification
    {
        /// <summary>
        /// 顺序执行 BResources 相关断言，并生成批验证报告。
        /// Execute the BResources-related assertions sequentially and generate a batch verification report.
        /// </summary>
        internal static void RunBatchVerification()
        {
            BResourcesTest.LogTestPurposeAndMeans(
                nameof(BResourcesBatchVerification),
                "验证 BResources 的资源路径、资源组缓存和空列表异步加载保护保持稳定。",
                "顺序执行现有 BResources 测试断言，输出批验证报告，并使用显式退出码反馈结果。"
            );
            Debug.Log("[测试进度] suite=BResourcesBatchVerification stage=start");

            var testInstance = new BResourcesTest();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, Action Action)[]
            {
                (
                    nameof(BResourcesTest.GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName),
                    () => ExecuteWithFixture(
                        testInstance.SetUp,
                        testInstance.GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName,
                        testInstance.TearDown)
                ),
                (
                    nameof(BResourcesTest.GetAssetsInfoPath_OverloadsUseExpectedRules),
                    () => ExecuteWithFixture(
                        testInstance.SetUp,
                        testInstance.GetAssetsInfoPath_OverloadsUseExpectedRules,
                        testInstance.TearDown)
                ),
                (
                    nameof(BResourcesTest.GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged),
                    () => ExecuteWithFixture(
                        testInstance.SetUp,
                        testInstance.GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged,
                        testInstance.TearDown)
                ),
                (
                    nameof(BResourcesTest.GetAssetsSubPackageInfoPath_FormatsModernSubPackageName),
                    () => ExecuteWithFixture(
                        testInstance.SetUp,
                        testInstance.GetAssetsSubPackageInfoPath_FormatsModernSubPackageName,
                        testInstance.TearDown)
                ),
                (
                    nameof(BResourcesTest.AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries),
                    () => ExecuteWithFixture(
                        testInstance.SetUp,
                        testInstance.AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries,
                        testInstance.TearDown)
                ),
                (
                    nameof(BResourcesTest.AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader),
                    () => ExecuteWithFixture(
                        testInstance.SetUp,
                        testInstance.AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader,
                        testInstance.TearDown)
                ),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                Execute(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "BResourcesBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                Debug.LogError($"BResources 批验证失败，请查看报告: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"BResources 批验证通过，报告: {outputPath}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 先执行测试夹具 SetUp，再执行断言，并确保 TearDown 一定会运行。
        /// Run the fixture SetUp first, then the assertion, and ensure TearDown always runs.
        /// </summary>
        private static void ExecuteWithFixture(Action setUp, Action action, Action tearDown)
        {
            setUp();
            try
            {
                action();
            }
            finally
            {
                tearDown();
            }
        }

        /// <summary>
        /// 执行单个 BResources 断言并把结果写入统一批验证报告。
        /// Execute a single BResources assertion and append the result to the shared batch verification report.
        /// </summary>
        private static void Execute(
            int currentIndex,
            int totalCount,
            string testName,
            Action action,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            Debug.Log($"[测试进度] suite=BResourcesBatchVerification current={currentIndex}/{totalCount} name={testName}");
            try
            {
                action();
                reportBuilder.AppendLine($"PASS {testName}");
                Debug.Log($"[测试进度] suite=BResourcesBatchVerification current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                Debug.LogError($"[测试进度] suite=BResourcesBatchVerification current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}