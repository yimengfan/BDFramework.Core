using NUnit.Framework;
using Talos.E2E;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// Talos 强制模式补偿启动辅助器测试。
    /// Tests for the Talos forced-mode fallback startup helper.
    /// 这些测试只验证强制模式判定与补偿调用触发契约，
    /// 让 Android 启动链的宿主补偿逻辑可以在 Editor NUnit 下先锁住行为，再交给 TeamCity 真机链路做端到端验证。
    /// These tests verify only the forced-mode detection and fallback invocation contract,
    /// so the host compensation logic for the Android startup path is locked down in editor NUnit before TeamCity validates it end to end on device.
    /// </summary>
    public class TalosForcedModeStartupFallbackTests
    {
        /// <summary>
        /// 每个测试开始时输出统一中文日志，便于在 Unity Test Runner 与 BatchMode 输出里快速识别当前验证目标。
        /// Emit a unified Chinese start log before each test so Unity Test Runner and BatchMode output can identify the current verification target quickly.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 Talos 强制模式补偿启动辅助器只在 -talosForceE2E 路径下触发一次显式补偿调用。 实现手段=构造命令行参数数组并断言补偿动作是否被调用。 ");
        }

        /// <summary>
        /// 验证存在 `-talosForceE2E` 参数时会触发补偿启动，并把端口原样透传给调用动作。
        /// Verify that the fallback launch is invoked when `-talosForceE2E` is present and that the port is passed through unchanged.
        /// </summary>
        [Test]
        public void TryLaunchFromForcedMode_InvokesLaunchActionWhenForceFlagExists()
        {
            var invoked = false;
            var capturedPort = -1;

            var result = ForcedModeStartupFallback.TryLaunchFromForcedMode(
                new[] { "game.exe", "-talosPort", "10002", "-talosForceE2E" },
                10002,
                port =>
                {
                    invoked = true;
                    capturedPort = port;
                });

            Assert.That(result, Is.True, "强制模式下应返回 true，表示已经执行补偿调用。");
            Assert.That(invoked, Is.True, "强制模式下应触发补偿启动动作。");
            Assert.That(capturedPort, Is.EqualTo(10002), "补偿启动动作应收到调用侧提供的端口值。");
        }

        /// <summary>
        /// 验证缺少强制模式参数时不会触发补偿启动动作。
        /// Verify that the fallback launch is not invoked when the forced-mode flag is missing.
        /// </summary>
        [Test]
        public void TryLaunchFromForcedMode_DoesNotInvokeLaunchActionWhenForceFlagMissing()
        {
            var invoked = false;

            var result = ForcedModeStartupFallback.TryLaunchFromForcedMode(
                new[] { "game.exe", "-batchmode" },
                10002,
                _ => invoked = true);

            Assert.That(result, Is.False, "非强制模式下应返回 false，表示未执行补偿调用。");
            Assert.That(invoked, Is.False, "非强制模式下不应触发补偿启动动作。");
        }

        /// <summary>
        /// 验证强制模式参数匹配保持大小写不敏感，避免不同平台传参大小写差异导致补偿逻辑失效。
        /// Verify that forced-mode matching stays case-insensitive so platform-specific casing differences do not disable the fallback logic.
        /// </summary>
        [Test]
        public void ContainsTalosForceE2EArgument_IsCaseInsensitive()
        {
            var result = ForcedModeStartupFallback.ContainsTalosForceE2EArgument(
                new[] { "game.exe", "-TALOSFORCEE2E" });

            Assert.That(result, Is.True, "强制模式参数匹配应保持大小写不敏感。");
        }
    }
}