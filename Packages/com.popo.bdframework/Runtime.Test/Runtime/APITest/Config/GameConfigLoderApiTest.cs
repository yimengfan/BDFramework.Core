using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest.Config
{
    /// <summary>
    /// GameConfigLoder 启动前置条件 API 的 Runtime 测试主体。
    /// Runtime test body for the GameConfigLoder startup-precondition API.
    /// 该类型把“是否应启动配置管理器”的纯逻辑契约固定在 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套装载前置条件断言。
    /// This type fixes the pure-logic contract of whether the configuration manager should start inside the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same loading-precondition assertions.
    /// </summary>
    public sealed class GameConfigLoderApiTest
    {
        /// <summary>
        /// 输出统一日志，记录配置管理器装载前置条件 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the configuration-manager loading-precondition API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(GameConfigLoderApiTest) : testName,
                "验证 GameConfigLoder 只会在配置管理器实例可用时触发正式启动。",
                "通过直接调用 FrameworkContractAssertions 的装载前置条件断言，并校验有无管理器实例时的布尔结果。"
            );
        }

        /// <summary>
        /// 验证配置管理器存在与否会稳定映射到装载判断结果。
        /// Verify that manager presence is mapped deterministically to the loading decision.
        /// </summary>
        public void ShouldLoadFrameworkConfigManager_MatchesManagerPresence()
        {
            FrameworkContractAssertions.VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence();
        }
    }
}