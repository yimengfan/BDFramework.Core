using System;
using System.IO;
using System.Text;
using BDFramework.RuntimeTests.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// 验证启动器关键反射契约与启动顺序约束的编辑器测试。
    /// Editor tests that verify key launcher reflection contracts and startup-order constraints.
    /// 这些断言只检查启动器依赖的类型、方法和执行顺序声明，不真正执行热更启动流程，
    /// 以便在本地和 BatchMode 下快速锁定启动链路回归。
    /// These assertions only check the types, methods, and execution-order declarations required by the launcher without executing the real hotfix startup flow,
    /// which keeps startup regressions local in both local runs and BatchMode.
    /// </summary>
    public class BdLauncherTest
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// Explicit verification entry exposed for batchmode.
        /// 当 Unity 原生 <c>-runTests</c> 结果导出不稳定时，可以通过这个入口直接执行启动器相关断言。
        /// When Unity native <c>-runTests</c> result export is unreliable, this entry can execute the launcher-related assertions directly.
        /// </summary>
        public static void RunBatchVerification()
        {
            BdLauncherBatchVerification.RunBatchVerification();
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
                string.IsNullOrEmpty(testName) ? nameof(BdLauncherTest) : testName,
                "验证启动器反射契约与默认启动顺序不会发生回归。",
                "通过反射读取启动入口方法、执行顺序特性和 E2E 自动检测方法声明，并断言其契约保持稳定。"
            );
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// Emit a unified test-start log that always includes the purpose and means.
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            FrameworkContractAssertions.LogTestPurposeAndMeans(testName, purpose, means);
        }

        /// <summary>
        /// 验证热更脚本加载入口仍然是可发现的静态方法。
        /// Verify that the hotfix script loading entry is still a discoverable static method.
        /// </summary>
        [Test]
        public void FindScriptLoderInitMethod_ShouldResolveStaticMethod()
        {
            FrameworkContractAssertions.VerifyScriptLoaderInitMethodCanBeResolved();
        }

        /// <summary>
        /// 验证启动器声明了极小的默认执行顺序，确保场景生命周期尽量先于普通脚本触发。
        /// Verify that the launcher declares the minimum default execution order so scene startup runs as early as possible.
        /// </summary>
        [Test]
        public void BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder()
        {
            FrameworkContractAssertions.VerifyLauncherDefaultExecutionOrder();
        }

        /// <summary>
        /// 验证 AOT 阶段的 E2E 自动检测入口显式依赖编译期 DEBUG 条件裁剪。
        /// Verify that the AOT-stage E2E auto-detection entry explicitly depends on compile-time DEBUG conditional stripping.
        /// 这样 Release 非调试环境不会把 E2E 自动检测入口一起发布，而 Debug 链路仍可继续验证自动发现行为。
        /// This keeps the E2E auto-detection entry out of Release non-debug builds while preserving automatic-discovery validation on Debug paths.
        /// </summary>
        [Test]
        public void TryStartE2EFramework_ShouldUseConditionalDebugAttribute()
        {
            FrameworkContractAssertions.VerifyTryStartE2EFrameworkUsesConditionalDebugAttribute();
        }
    }

    /// <summary>
    /// 启动器测试的显式批验证入口。
    /// Explicit batch verification entry for the launcher tests.
    /// 该入口顺序执行现有启动器断言、写出稳定的 Library 报告，并在非交互环境中通过显式退出码反馈结果。
    /// This entry executes the existing launcher assertions sequentially, writes a stable Library report, and reports results through an explicit exit code in non-interactive environments.
    /// </summary>
    internal static class BdLauncherBatchVerification
    {
        /// <summary>
        /// 顺序执行启动器相关断言，并生成批验证报告。
        /// Execute launcher-related assertions sequentially and generate a batch verification report.
        /// </summary>
        internal static void RunBatchVerification()
        {
            BdLauncherTest.LogTestPurposeAndMeans(
                nameof(BdLauncherBatchVerification),
                "验证启动器反射契约、默认执行顺序与 E2E 自动检测入口保持稳定。",
                "顺序执行启动器现有 NUnit 断言，写出批验证报告，并使用显式退出码反馈结果。"
            );
            UnityDebug.Log("[测试进度] suite=BdLauncherBatchVerification stage=start");

            var testInstance = new BdLauncherTest();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, Action Action)[]
            {
                (
                    nameof(BdLauncherTest.FindScriptLoderInitMethod_ShouldResolveStaticMethod),
                    () => ExecuteWithSetUp(testInstance.SetUp, testInstance.FindScriptLoderInitMethod_ShouldResolveStaticMethod)
                ),
                (
                    nameof(BdLauncherTest.BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder),
                    () => ExecuteWithSetUp(testInstance.SetUp, testInstance.BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder)
                ),
                (
                    nameof(BdLauncherTest.TryStartE2EFramework_ShouldUseConditionalDebugAttribute),
                    () => ExecuteWithSetUp(testInstance.SetUp, testInstance.TryStartE2EFramework_ShouldUseConditionalDebugAttribute)
                ),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                Execute(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "BDLauncherBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                UnityDebug.LogError($"BDLauncher 批验证失败，请查看报告: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            UnityDebug.Log($"BDLauncher 批验证通过，报告: {outputPath}");
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
        /// 执行单个启动器断言并把结果写入统一批验证报告。
        /// Execute a single launcher assertion and append the result to the shared batch verification report.
        /// </summary>
        private static void Execute(
            int currentIndex,
            int totalCount,
            string testName,
            Action action,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            UnityDebug.Log($"[测试进度] suite=BdLauncherBatchVerification current={currentIndex}/{totalCount} name={testName}");
            try
            {
                action();
                reportBuilder.AppendLine($"PASS {testName}");
                UnityDebug.Log($"[测试进度] suite=BdLauncherBatchVerification current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                UnityDebug.LogError($"[测试进度] suite=BdLauncherBatchVerification current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}
