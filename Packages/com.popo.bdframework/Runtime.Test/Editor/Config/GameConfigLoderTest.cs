using System;
using BDFramework.Configure;
using BDFramework.RuntimeTests.Contracts;
using NUnit.Framework;
using UnityEngine;

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

            GameConfigManagerTest.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(GameConfigLoderTest) : testName,
                "验证 GameConfigLoder 只会在配置管理器实例可用时触发正式启动。",
                "直接执行纯逻辑 helper，并断言有无管理器实例时的布尔返回值。");
        }

        /// <summary>
        /// 验证配置管理器存在与否会稳定映射到装载判断结果。
        /// Verify that manager presence is mapped deterministically to the loading decision.
        /// </summary>
        [Test]
        public void ShouldLoadFrameworkConfigManager_MatchesManagerPresence()
        {
            VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence();
        }

        /// <summary>
        /// 以纯异常校验方式验证配置管理器装载前置条件，供 batchmode 路径复用。
        /// Verify the configuration-manager loading precondition through pure exception-based assertions for batchmode reuse.
        /// </summary>
        internal static void VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence()
        {
            FrameworkContractAssertions.VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence();
        }
    }
}