using System;
using System.IO;
using System.Text;
using BDFramework.Configure;
using BDFramework.RuntimeTests.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.Config
{
    /// <summary>
    /// 验证框架配置来源回退与启动桥接纯逻辑。
    /// Verify framework configuration fallback and startup bridge pure logic.
    /// 这些断言只覆盖配置来源选择、日志格式化和批验证入口，不触发真实场景查找、文件 IO 副作用或管理器启动。
    /// These assertions only cover configuration-source selection, log formatting, and the batch verification entry without triggering real scene lookups, file-I/O side effects, or manager startup.
    /// </summary>
    public class GameConfigManagerTest
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// Explicit verification entry exposed for batchmode.
        /// 当 Unity 原生 <c>-runTests</c> 结果导出不稳定时，可以直接通过这个入口收口启动与配置切片的纯逻辑断言。
        /// When Unity's native <c>-runTests</c> result export is unreliable, this entry can be used directly to validate the startup and config slice pure-logic assertions.
        /// </summary>
        public static void RunBatchVerification()
        {
            GameConfigStartupBatchVerification.RunBatchVerification();
        }

        /// <summary>
        /// 在每个 NUnit 测试开始时输出统一的测试目的与实现手段日志。
        /// Emit a unified purpose-and-means log at the start of each NUnit test.
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
                string.IsNullOrEmpty(testName) ? nameof(GameConfigManagerTest) : testName,
                "验证启动链路中的配置来源回退顺序保持稳定。",
                "直接执行纯逻辑 helper，断言来源选择、日志文本与空来源回退契约。");
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
        /// 验证运行态 launcher 文本优先级最高。
        /// Verify that runtime launcher text has the highest priority.
        /// </summary>
        [Test]
        public void ResolveFrameworkConfigTextSource_PrefersRuntimeLauncherTextWhenPlaying()
        {
            FrameworkContractAssertions.VerifyRuntimeLauncherConfigTextPreferredWhenPlaying();
        }

        /// <summary>
        /// 验证运行态来源缺失时会回退到场景中的 launcher 文本。
        /// Verify that the logic falls back to scene launcher text when the runtime source is unavailable.
        /// </summary>
        [Test]
        public void ResolveFrameworkConfigTextSource_FallsBackToSceneLauncherText()
        {
            FrameworkContractAssertions.VerifySceneLauncherFallback();
        }

        /// <summary>
        /// 验证所有 launcher 来源缺失时，编辑器模式会回退到默认 bytes 文件。
        /// Verify that editor mode falls back to the default bytes file when all launcher sources are missing.
        /// </summary>
        [Test]
        public void ResolveFrameworkConfigTextSource_UsesEditorDefaultFileAfterLauncherFallbacks()
        {
            FrameworkContractAssertions.VerifyEditorDefaultFileFallback();
        }

        /// <summary>
        /// 验证没有任何来源时会返回空决策，而不是误报默认文件存在。
        /// Verify that the logic returns an empty decision when no source exists instead of falsely reporting a default file.
        /// </summary>
        [Test]
        public void ResolveFrameworkConfigTextSource_ReturnsNoneWhenNoSourceExists()
        {
            FrameworkContractAssertions.VerifyNoConfigSourceReturnsNone();
        }

        /// <summary>
        /// 验证配置来源日志格式在空来源时也会稳定回退到占位符。
        /// Verify that the configuration-source log format falls back to a placeholder when the source is empty.
        /// </summary>
        [Test]
        public void FormatFrameworkConfigSourceLogMessage_UsesFallbackMarkerForMissingSource()
        {
            FrameworkContractAssertions.VerifyFormatFrameworkConfigSourceLogMessageFallback();
        }

        /// <summary>
        /// 以纯异常校验方式验证运行态 launcher 来源优先级，供 batchmode 路径复用。
        /// Verify the runtime launcher source priority through pure exception-based assertions for batchmode reuse.
        /// </summary>
        internal static void VerifyResolveFrameworkConfigTextSourcePrefersRuntimeLauncherTextWhenPlaying()
        {
            FrameworkContractAssertions.VerifyRuntimeLauncherConfigTextPreferredWhenPlaying();
        }

        /// <summary>
        /// 以纯异常校验方式验证场景 launcher 回退路径，供 batchmode 路径复用。
        /// Verify the scene launcher fallback path through pure exception-based assertions for batchmode reuse.
        /// </summary>
        internal static void VerifyResolveFrameworkConfigTextSourceFallsBackToSceneLauncherText()
        {
            FrameworkContractAssertions.VerifySceneLauncherFallback();
        }

        /// <summary>
        /// 以纯异常校验方式验证编辑器默认文件回退路径，供 batchmode 路径复用。
        /// Verify the editor default-file fallback path through pure exception-based assertions for batchmode reuse.
        /// </summary>
        internal static void VerifyResolveFrameworkConfigTextSourceUsesEditorDefaultFileAfterLauncherFallbacks()
        {
            FrameworkContractAssertions.VerifyEditorDefaultFileFallback();
        }

        /// <summary>
        /// 以纯异常校验方式验证空来源分支，供 batchmode 路径复用。
        /// Verify the empty-source branch through pure exception-based assertions for batchmode reuse.
        /// </summary>
        internal static void VerifyResolveFrameworkConfigTextSourceReturnsNoneWhenNoSourceExists()
        {
            FrameworkContractAssertions.VerifyNoConfigSourceReturnsNone();
        }

        /// <summary>
        /// 以纯异常校验方式验证配置来源日志格式，供 batchmode 路径复用。
        /// Verify the configuration-source log format through pure exception-based assertions for batchmode reuse.
        /// </summary>
        internal static void VerifyFormatFrameworkConfigSourceLogMessageUsesFallbackMarkerForMissingSource()
        {
            FrameworkContractAssertions.VerifyFormatFrameworkConfigSourceLogMessageFallback();
        }

        /// <summary>
        /// 统一的纯逻辑相等断言。
        /// Shared equality assertion helper for pure logic verification.
        /// </summary>
        internal static void EnsureEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} expected={expected} actual={actual}");
            }
        }

        /// <summary>
        /// 统一的纯逻辑布尔断言。
        /// Shared boolean assertion helper for pure logic verification.
        /// </summary>
        internal static void EnsureTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }

    /// <summary>
    /// 启动与配置切片的显式批验证入口。
    /// Explicit batch verification entry for the startup and configuration slice.
    /// 该入口在 Unity 原生 <c>-runTests</c> 不稳定时，直接执行纯逻辑断言并写出稳定的 Library 报告。
    /// This entry directly executes pure-logic assertions and writes a stable Library report when Unity's native <c>-runTests</c> path is unreliable.
    /// </summary>
    internal static class GameConfigStartupBatchVerification
    {
        /// <summary>
        /// 顺序执行启动与配置切片的纯逻辑断言，并生成批验证报告。
        /// Execute the startup and configuration pure-logic assertions sequentially and generate a batch verification report.
        /// </summary>
        internal static void RunBatchVerification()
        {
            GameConfigManagerTest.LogTestPurposeAndMeans(
                nameof(GameConfigStartupBatchVerification),
                "验证启动链路中的配置来源回退和配置管理器装载前置条件保持稳定。",
                "顺序执行纯逻辑断言、写出批验证报告，并用显式退出码反馈结果。");
            Debug.Log("[测试进度] suite=GameConfigStartupBatchVerification stage=start");

            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, Action Action)[]
            {
                (nameof(GameConfigManagerTest.ResolveFrameworkConfigTextSource_PrefersRuntimeLauncherTextWhenPlaying),
                    GameConfigManagerTest.VerifyResolveFrameworkConfigTextSourcePrefersRuntimeLauncherTextWhenPlaying),
                (nameof(GameConfigManagerTest.ResolveFrameworkConfigTextSource_FallsBackToSceneLauncherText),
                    GameConfigManagerTest.VerifyResolveFrameworkConfigTextSourceFallsBackToSceneLauncherText),
                (nameof(GameConfigManagerTest.ResolveFrameworkConfigTextSource_UsesEditorDefaultFileAfterLauncherFallbacks),
                    GameConfigManagerTest.VerifyResolveFrameworkConfigTextSourceUsesEditorDefaultFileAfterLauncherFallbacks),
                (nameof(GameConfigManagerTest.ResolveFrameworkConfigTextSource_ReturnsNoneWhenNoSourceExists),
                    GameConfigManagerTest.VerifyResolveFrameworkConfigTextSourceReturnsNoneWhenNoSourceExists),
                (nameof(GameConfigManagerTest.FormatFrameworkConfigSourceLogMessage_UsesFallbackMarkerForMissingSource),
                    GameConfigManagerTest.VerifyFormatFrameworkConfigSourceLogMessageUsesFallbackMarkerForMissingSource),
                (nameof(GameConfigLoderTest.ShouldLoadFrameworkConfigManager_MatchesManagerPresence),
                    GameConfigLoderTest.VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                Execute(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "GameConfigStartupBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                Debug.LogError($"GameConfig 启动批验证失败，请查看报告: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"GameConfig 启动批验证通过，报告: {outputPath}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 执行单个纯逻辑断言并把结果写入统一报告。
        /// Execute a single pure-logic assertion and append the result to the shared report.
        /// </summary>
        private static void Execute(
            int currentIndex,
            int totalCount,
            string testName,
            Action action,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            GameConfigManagerTest.LogTestPurposeAndMeans(
                testName,
                "验证启动链路中的配置来源回退和配置管理器装载前置条件保持稳定。",
                "直接执行纯逻辑 helper 或纯逻辑断言，并校验结果是否符合既定回退规则。");
            Debug.Log($"[测试进度] suite=GameConfigStartupBatchVerification current={currentIndex}/{totalCount} name={testName}");
            try
            {
                action();
                reportBuilder.AppendLine($"PASS {testName}");
                Debug.Log($"[测试进度] suite=GameConfigStartupBatchVerification current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                Debug.LogError($"[测试进度] suite=GameConfigStartupBatchVerification current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}