using UnityEngine;

namespace BDFramework.RuntimeTests.ApiTest
{
    /// <summary>
    /// Runtime API 测试使用的统一日志辅助器。
    /// Unified logging helper used by runtime API tests.
    /// 该辅助器强制输出包含测试名称、测试目的与实现手段的中文日志，便于真机、BatchMode 与本地编辑器共享同一套观察口径。
    /// This helper enforces Chinese logs that include the test name, purpose, and means so packaged players, BatchMode, and the local editor share the same observation format.
    /// </summary>
    public static class ApiTestLog
    {
        /// <summary>
        /// 输出统一的测试开始日志。
        /// Emit a unified test-start log.
        /// </summary>
        public static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }
    }
}