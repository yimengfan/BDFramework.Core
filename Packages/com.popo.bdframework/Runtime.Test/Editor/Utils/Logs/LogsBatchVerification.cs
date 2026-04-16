using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.Logs
{
    /// <summary>
    /// 日志 API 测试的统一日志辅助器。
    /// Shared logging helper for the log API tests.
    /// 该辅助器为普通 NUnit 执行与手工 BatchMode 调用提供一致的测试命名与日志格式，
    /// 避免批验证路径缺少上下文信息。
    /// This helper provides consistent test naming and log formatting for standard NUnit execution and manual BatchMode invocation,
    /// preventing the batch-verification path from losing context.
    /// </summary>
    internal static class LogsApiTestLog
    {
        /// <summary>
        /// 获取当前测试名；当批验证路径没有有效 NUnit 上下文时回退到指定名称。
        /// Resolve the current test name and fall back to the provided name when the batch path has no valid NUnit context.
        /// </summary>
        internal static string ResolveCurrentTestName(string fallbackName)
        {
            try
            {
                var testName = NUnit.Framework.TestContext.CurrentContext?.Test?.Name;
                return string.IsNullOrEmpty(testName) ? fallbackName : testName;
            }
            catch
            {
                return fallbackName;
            }
        }

        /// <summary>
        /// 输出统一的测试开始日志。
        /// Emit a unified test-start log.
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }
    }

    /// <summary>
    /// 日志模块测试的显式批验证入口。
    /// Explicit batch verification entry for the log-module tests.
    /// 该入口复用现有日志单元测试夹具，在 Unity 原生 <c>-runTests</c> 结果导出不稳定时，
    /// 仍然可以通过 <c>-executeMethod</c> 稳定验证日志加密、读取、导出与清理契约。
    /// This entry reuses the existing log-unit-test fixtures so log encryption, reading, export, and retention contracts
    /// can still be validated through <c>-executeMethod</c> when Unity native <c>-runTests</c> result export is unreliable.
    /// </summary>
    public static class LogsBatchVerification
    {
        /// <summary>
        /// 顺序执行日志模块的关键单元测试，并写出稳定的批验证报告。
        /// Execute the key log-module unit tests sequentially and write a stable batch verification report.
        /// </summary>
        public static void RunBatchVerification()
        {
            LogsApiTestLog.LogTestPurposeAndMeans(
                nameof(LogsBatchVerification),
                "验证日志模块的加密、读取、导出和清理契约在 BatchMode 下保持稳定。",
                "顺序执行现有日志测试夹具，输出批验证报告，并使用显式退出码反馈结果。"
            );
            Debug.Log("[测试进度] suite=LogsBatchVerification stage=start");

            var cryptoFixture = new LogCryptoAndReaderTests();
            var persistenceFixture = new PersistenceAndBDebugTests();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, string Purpose, Action Action)[]
            {
                (
                    nameof(LogCryptoAndReaderTests.EncryptAndDecrypt_RoundTrip_Succeeds),
                    "验证日志加密与解密可以稳定完成往返。",
                    () => ExecuteWithSetUp(
                        cryptoFixture.SetUp,
                        cryptoFixture.EncryptAndDecrypt_RoundTrip_Succeeds)
                ),
                (
                    nameof(LogCryptoAndReaderTests.Decrypt_WithWrongPassword_Throws),
                    "验证错误密码会触发明确的解密失败契约。",
                    () => ExecuteWithSetUp(
                        cryptoFixture.SetUp,
                        cryptoFixture.Decrypt_WithWrongPassword_Throws)
                ),
                (
                    nameof(LogCryptoAndReaderTests.LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords),
                    "验证明文与密文日志都可以被读取并导出文本。",
                    () => ExecuteWithSetUp(
                        cryptoFixture.SetUp,
                        cryptoFixture.LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords)
                ),
                (
                    nameof(PersistenceAndBDebugTests.PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted),
                    "验证 Player 默认日志持久化设置保持开启且启用加密。",
                    () => ExecuteWithSetUp(
                        persistenceFixture.SetUp,
                        persistenceFixture.PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted)
                ),
                (
                    nameof(PersistenceAndBDebugTests.PersistenceSettings_Normalize_UsesDefaultsAndMinimums),
                    "验证日志配置规范化会补齐默认目录、最小刷新间隔与默认密码。",
                    () => ExecuteWithSetUp(
                        persistenceFixture.SetUp,
                        persistenceFixture.PersistenceSettings_Normalize_UsesDefaultsAndMinimums)
                ),
                (
                    nameof(PersistenceAndBDebugTests.CloneNormalized_ReturnsIndependentNormalizedCopy),
                    "验证日志配置克隆规范化会返回独立副本并保留规范化结果。",
                    () => ExecuteWithSetUp(
                        persistenceFixture.SetUp,
                        persistenceFixture.CloneNormalized_ReturnsIndependentNormalizedCopy)
                ),
                (
                    nameof(PersistenceAndBDebugTests.SerializedLogEntry_LocalTime_ConvertsFromUtcTicks),
                    "验证序列化日志条目的本地时间换算保持正确。",
                    () => ExecuteWithSetUp(
                        persistenceFixture.SetUp,
                        persistenceFixture.SerializedLogEntry_LocalTime_ConvertsFromUtcTicks)
                ),
                (
                    nameof(PersistenceAndBDebugTests.CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile),
                    "验证日志清理策略会删除最旧文件并保留当前活跃文件。",
                    () => ExecuteWithSetUp(
                        persistenceFixture.SetUp,
                        persistenceFixture.CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile)
                ),
                (
                    nameof(PersistenceAndBDebugTests.BDebug_PlayerLogPaths_AreEmptyInEditor),
                    "验证编辑器环境不会错误暴露 Player 侧日志落盘路径。",
                    () => ExecuteWithSetUp(
                        persistenceFixture.SetUp,
                        persistenceFixture.BDebug_PlayerLogPaths_AreEmptyInEditor)
                ),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                Execute(index + 1, checks.Length, check.Name, check.Purpose, check.Action, reportBuilder, ref failedCount);
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "LogsBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                Debug.LogError($"日志模块批验证失败，请查看报告: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"日志模块批验证通过，报告: {outputPath}");
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
        /// 执行单个日志模块断言并把结果写入统一批验证报告。
        /// Execute a single log-module assertion and append the result to the shared batch verification report.
        /// </summary>
        private static void Execute(
            int currentIndex,
            int totalCount,
            string testName,
            string purpose,
            Action action,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            LogsApiTestLog.LogTestPurposeAndMeans(
                testName,
                purpose,
                "调用日志测试夹具的 SetUp 与测试方法，并断言输出、规范化与清理结果。"
            );
            Debug.Log($"[测试进度] suite=LogsBatchVerification current={currentIndex}/{totalCount} name={testName}");
            try
            {
                action();
                reportBuilder.AppendLine($"PASS {testName}");
                Debug.Log($"[测试进度] suite=LogsBatchVerification current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                Debug.LogError($"[测试进度] suite=LogsBatchVerification current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}