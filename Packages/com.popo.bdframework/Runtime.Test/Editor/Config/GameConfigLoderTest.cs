using BDFramework.RuntimeTests.ApiTest.Config;
using NUnit.Framework;

namespace BDFramework.EditorTest.Config
{
    /// <summary>
    /// 验证 GameConfigLoder 的启动前置条件纯逻辑。
    /// Verify the pure startup-precondition logic used by GameConfigLoder.
    /// 这些断言只覆盖“是否应启动配置管理器”这一判断，不触发真实的单例装载与配置解析。
    /// These assertions only cover the decision of whether the configuration manager should start and do not trigger real singleton startup or configuration parsing.
    /// </summary>
    public class GameConfigLoderTest
    {
        private readonly GameConfigLoderApiTest runtimeTest = new GameConfigLoderApiTest();

        /// <summary>
        /// 在每个 NUnit 测试开始时输出统一的测试目的与实现手段日志。
        /// Emit a unified purpose-and-means log at the start of each NUnit test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            runtimeTest.SetUp(GameConfigManagerTest.ResolveCurrentTestName(nameof(GameConfigLoderTest)));
        }

        /// <summary>
        /// 验证配置管理器存在与否会稳定映射到装载判断结果。
        /// Verify that manager presence is mapped deterministically to the loading decision.
        /// </summary>
        [Test]
        public void ShouldLoadFrameworkConfigManager_MatchesManagerPresence()
        {
            runtimeTest.ShouldLoadFrameworkConfigManager_MatchesManagerPresence();
        }
    }
}