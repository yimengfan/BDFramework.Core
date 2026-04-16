using System;
using System.IO;
using System.Text;
using BDFramework.RuntimeTests.ApiTest;
using UnityEditor;
using UnityDebug = UnityEngine.Debug;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// 启动器 Runtime API 套件的编辑器 BatchMode 桥接器。
    /// Editor BatchMode bridge for the launcher runtime API suite.
    /// 该桥接器不再承载编辑器侧 NUnit 测试所有权；它只在本地验证脚本需要 <c>-executeMethod</c> 入口时，
    /// 顺序调用 Runtime.Test/Runtime/APITest 下的启动器、AOT 启动与热更依赖重试断言并写出稳定报告。
    /// This bridge no longer owns editor-side NUnit tests; it only provides a stable <c>-executeMethod</c> entrypoint for local verification scripts,
    /// sequentially invoking the launcher, AOT-startup, and hotfix-dependency-retry assertions under Runtime.Test/Runtime/APITest and writing a stable report.
    /// </summary>
    public static class BdLauncherBatchBridge
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// Explicit verification entry exposed for batchmode.
        /// 当本地脚本需要显式 BatchMode 入口时，可以通过这个桥接器直接执行 Runtime 启动器断言。
        /// When local scripts need an explicit BatchMode entrypoint, this bridge can execute the runtime launcher assertions directly.
        /// </summary>
        public static void RunBatchVerification()
        {
            ApiTestLog.LogTestPurposeAndMeans(
                nameof(BdLauncherBatchBridge),
                "验证启动器反射契约、AOT 启动 StreamingAssets 与热更依赖重试规则、默认执行顺序与 E2E 自动检测入口保持稳定。",
                "顺序执行 Runtime APITest 启动器、AOT 启动与热更依赖重试断言，写出批验证报告，并使用显式退出码反馈结果。"
            );
            UnityDebug.Log("[测试进度] suite=BdLauncherBatchBridge stage=start");

            var runtimeTest = new BdLauncherApiTest();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, Action Action)[]
            {
                (
                    nameof(BdLauncherApiTest.FindScriptLoderInitMethod_ShouldResolveStaticMethod),
                    () => ExecuteWithSetUp(
                        runtimeTest,
                        nameof(BdLauncherApiTest.FindScriptLoderInitMethod_ShouldResolveStaticMethod),
                        runtimeTest.FindScriptLoderInitMethod_ShouldResolveStaticMethod)
                ),
                (
                    nameof(BdLauncherApiTest.GetStreamingAssetFiles_ShouldInitializeIndexAndSkipMissingOptionalDirectory),
                    () => ExecuteWithSetUp(
                        runtimeTest,
                        nameof(BdLauncherApiTest.GetStreamingAssetFiles_ShouldInitializeIndexAndSkipMissingOptionalDirectory),
                        runtimeTest.GetStreamingAssetFiles_ShouldInitializeIndexAndSkipMissingOptionalDirectory)
                ),
                (
                    nameof(BdLauncherApiTest.LoadHotfixAssemblies_ShouldRetryWhenDependenciesBecomeAvailableLater),
                    () => ExecuteWithSetUp(
                        runtimeTest,
                        nameof(BdLauncherApiTest.LoadHotfixAssemblies_ShouldRetryWhenDependenciesBecomeAvailableLater),
                        runtimeTest.LoadHotfixAssemblies_ShouldRetryWhenDependenciesBecomeAvailableLater)
                ),
                (
                    nameof(BdLauncherApiTest.BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder),
                    () => ExecuteWithSetUp(
                        runtimeTest,
                        nameof(BdLauncherApiTest.BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder),
                        runtimeTest.BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder)
                ),
                (
                    nameof(BdLauncherApiTest.TryStartE2EFramework_ShouldUseConditionalDebugAttribute),
                    () => ExecuteWithSetUp(
                        runtimeTest,
                        nameof(BdLauncherApiTest.TryStartE2EFramework_ShouldUseConditionalDebugAttribute),
                        runtimeTest.TryStartE2EFramework_ShouldUseConditionalDebugAttribute)
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
        /// 先执行 Runtime APITest 的 SetUp，再执行实际断言。
        /// Run the Runtime APITest SetUp first and then execute the actual assertion.
        /// </summary>
        private static void ExecuteWithSetUp(BdLauncherApiTest runtimeTest, string testName, Action action)
        {
            runtimeTest.SetUp(testName);
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
            UnityDebug.Log($"[测试进度] suite=BdLauncherBatchBridge current={currentIndex}/{totalCount} name={testName}");
            try
            {
                action();
                reportBuilder.AppendLine($"PASS {testName}");
                UnityDebug.Log($"[测试进度] suite=BdLauncherBatchBridge current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                UnityDebug.LogError($"[测试进度] suite=BdLauncherBatchBridge current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}
