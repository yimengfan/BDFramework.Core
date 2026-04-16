using System;
using System.IO;
using System.Text;
using BDFramework.RuntimeTests.ApiTest;
using BDFramework.RuntimeTests.ApiTest.AssetsManager;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
        private readonly BResourcesApiTest runtimeTest = new BResourcesApiTest();

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
            runtimeTest.SetUp(ResolveCurrentTestName(nameof(BResourcesTest)));
        }

        /// <summary>
        /// 获取当前测试名；当批验证路径没有有效 NUnit 上下文时回退到指定名称。
        /// Resolve the current test name and fall back to the provided name when the batch path has no valid NUnit context.
        /// </summary>
        internal static string ResolveCurrentTestName(string fallbackName)
        {
            try
            {
                var testName = TestContext.CurrentContext?.Test?.Name;
                return string.IsNullOrEmpty(testName) ? fallbackName : testName;
            }
            catch
            {
                return fallbackName;
            }
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// Emit a unified test-start log that always includes the purpose and means.
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            ApiTestLog.LogTestPurposeAndMeans(testName, purpose, means);
        }

        /// <summary>
        /// 验证服务器版控文件路径会稳定拼接平台目录和固定文件名。
        /// Verify that the server version-info path consistently appends the platform directory and fixed file name.
        /// </summary>
        [Test]
        public void GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName()
        {
            runtimeTest.GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName();
        }

        /// <summary>
        /// 验证资源信息路径的两个重载会分别走根目录和平台目录规则。
        /// Verify that the two resource-info path overloads follow the root-only and platform-directory rules respectively.
        /// </summary>
        [Test]
        public void GetAssetsInfoPath_OverloadsUseExpectedRules()
        {
            runtimeTest.GetAssetsInfoPath_OverloadsUseExpectedRules();
        }

        /// <summary>
        /// 验证旧版分包命名会直接按原文件名拼接，保持兼容路径不被二次格式化。
        /// Verify that legacy sub-package names are appended directly without secondary formatting so compatibility paths remain stable.
        /// </summary>
        [Test]
        public void GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged()
        {
            runtimeTest.GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged();
        }

        /// <summary>
        /// 验证新版分包名会被格式化为 server_assets_subpack_{name}.info 规则。
        /// Verify that modern sub-package names are formatted into the server_assets_subpack_{name}.info rule.
        /// </summary>
        [Test]
        public void GetAssetsSubPackageInfoPath_FormatsModernSubPackageName()
        {
            runtimeTest.GetAssetsSubPackageInfoPath_FormatsModernSubPackageName();
        }

        /// <summary>
        /// 验证资源组缓存会保留写入顺序，并且清理后读取为空。
        /// Verify that the asset-group cache preserves insertion order and returns empty after cleanup.
        /// </summary>
        [Test]
        public void AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries()
        {
            runtimeTest.AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries();
        }

        /// <summary>
        /// 验证空资源列表异步加载会直接回调空结果，并且不要求事先初始化 ResLoader。
        /// Verify that async loading with an empty asset list immediately returns an empty result and does not require ResLoader initialization.
        /// </summary>
        [Test]
        public void AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader()
        {
            runtimeTest.AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader();
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
                    () => ExecuteWithSetUp(
                        testInstance.SetUp,
                        testInstance.GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName)
                ),
                (
                    nameof(BResourcesTest.GetAssetsInfoPath_OverloadsUseExpectedRules),
                    () => ExecuteWithSetUp(
                        testInstance.SetUp,
                        testInstance.GetAssetsInfoPath_OverloadsUseExpectedRules)
                ),
                (
                    nameof(BResourcesTest.GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged),
                    () => ExecuteWithSetUp(
                        testInstance.SetUp,
                        testInstance.GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged)
                ),
                (
                    nameof(BResourcesTest.GetAssetsSubPackageInfoPath_FormatsModernSubPackageName),
                    () => ExecuteWithSetUp(
                        testInstance.SetUp,
                        testInstance.GetAssetsSubPackageInfoPath_FormatsModernSubPackageName)
                ),
                (
                    nameof(BResourcesTest.AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries),
                    () => ExecuteWithSetUp(
                        testInstance.SetUp,
                        testInstance.AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries)
                ),
                (
                    nameof(BResourcesTest.AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader),
                    () => ExecuteWithSetUp(
                        testInstance.SetUp,
                        testInstance.AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader)
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
        /// 先执行测试级 SetUp，再执行实际断言。
        /// Run the test-level SetUp first and then execute the actual assertion.
        /// </summary>
        private static void ExecuteWithSetUp(Action setUp, Action action)
        {
            setUp();
            action();
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