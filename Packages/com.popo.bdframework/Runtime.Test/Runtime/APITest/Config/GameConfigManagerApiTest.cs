using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest.Config
{
    /// <summary>
    /// 框架配置来源回退 API 的 Runtime 测试主体。
    /// Runtime test body for the framework configuration-source fallback APIs.
    /// 该类型把配置来源选择与日志格式化契约固定在 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套配置启动断言。
    /// This type fixes configuration-source selection and log-format contracts inside the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same startup-configuration assertions.
    /// </summary>
    public sealed class GameConfigManagerApiTest
    {
        /// <summary>
        /// 输出统一日志，记录框架配置 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the framework configuration API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(GameConfigManagerApiTest) : testName,
                "验证启动链路中的配置来源回退顺序保持稳定。",
                "通过直接调用 FrameworkContractAssertions 的配置断言，并校验来源优先级、空来源回退与日志格式。"
            );
        }

        /// <summary>
        /// 验证运行态 launcher 文本优先级最高。
        /// Verify that runtime launcher text has the highest priority.
        /// </summary>
        public void ResolveFrameworkConfigTextSource_PrefersRuntimeLauncherTextWhenPlaying()
        {
            FrameworkContractAssertions.VerifyRuntimeLauncherConfigTextPreferredWhenPlaying();
        }

        /// <summary>
        /// 验证运行态来源缺失时会回退到场景中的 launcher 文本。
        /// Verify that the logic falls back to scene launcher text when the runtime source is unavailable.
        /// </summary>
        public void ResolveFrameworkConfigTextSource_FallsBackToSceneLauncherText()
        {
            FrameworkContractAssertions.VerifySceneLauncherFallback();
        }

        /// <summary>
        /// 验证所有 launcher 来源缺失时会回退到默认 bytes 文件。
        /// Verify that missing launcher sources fall back to the default bytes file.
        /// </summary>
        public void ResolveFrameworkConfigTextSource_UsesEditorDefaultFileAfterLauncherFallbacks()
        {
            FrameworkContractAssertions.VerifyEditorDefaultFileFallback();
        }

        /// <summary>
        /// 验证没有任何来源时会返回空决策。
        /// Verify that the logic returns an empty decision when no source exists.
        /// </summary>
        public void ResolveFrameworkConfigTextSource_ReturnsNoneWhenNoSourceExists()
        {
            FrameworkContractAssertions.VerifyNoConfigSourceReturnsNone();
        }

        /// <summary>
        /// 验证配置来源日志格式在空来源时会回退到占位符。
        /// Verify that the configuration-source log format falls back to a placeholder when the source is empty.
        /// </summary>
        public void FormatFrameworkConfigSourceLogMessage_UsesFallbackMarkerForMissingSource()
        {
            FrameworkContractAssertions.VerifyFormatFrameworkConfigSourceLogMessageFallback();
        }
    }
}