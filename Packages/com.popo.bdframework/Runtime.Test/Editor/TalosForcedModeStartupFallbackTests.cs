using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// Talos 强制模式补偿启动与运行时参数辅助器测试。
    /// Tests for the Talos forced-mode fallback and runtime launch-argument helpers.
    /// 这些测试锁定两类纯逻辑契约：
    /// 强制模式补偿调用触发规则，以及 Android `unity` extra 被并入运行时参数快照后的解析行为。
    /// These tests lock two pure-logic contracts:
    /// the fallback invocation rules for forced mode and the parsing behavior after Android `unity` extra tokens are merged into the runtime argument snapshot.
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
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 Talos 强制模式补偿启动与 Android 运行时参数合并逻辑在 Editor NUnit 下保持稳定。 实现手段=通过反射调用 Talos runtime 辅助器并断言参数快照与补偿动作结果。 ");
        }

        /// <summary>
        /// 解析指定的 Talos runtime 类型。
        /// Resolve the specified Talos runtime type.
        /// </summary>
        /// <param name="fullTypeName">类型全名。Fully qualified type name.</param>
        /// <returns>已加载的运行时类型。The loaded runtime type.</returns>
        private static Type RequireTalosRuntimeType(string fullTypeName)
        {
            var runtimeType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullTypeName))
                .FirstOrDefault(type => type != null);

            Assert.That(runtimeType, Is.Not.Null, $"应能在当前 AppDomain 中解析到 {fullTypeName} 类型。");
            return runtimeType!;
        }

        /// <summary>
        /// 解析 Talos 强制模式补偿启动辅助器类型。
        /// Resolve the Talos forced-mode fallback startup helper type.
        /// 这里通过已加载程序集反射查找运行时类型，避免 BDFramework 的 Editor 测试程序集对 Talos runtime asmdef 形成编译期硬依赖。
        /// This locates the runtime type through loaded assemblies so the BDFramework editor test assembly avoids a compile-time hard dependency on the Talos runtime asmdef.
        /// </summary>
        /// <returns>已加载的辅助器类型。The loaded helper type.</returns>
        private static Type GetFallbackHelperType()
        {
            return RequireTalosRuntimeType("Talos.E2E.ForcedModeStartupFallback");
        }

        /// <summary>
        /// 解析 Talos 运行时参数辅助器类型。
        /// Resolve the Talos runtime launch-argument helper type.
        /// </summary>
        /// <returns>已加载的辅助器类型。The loaded helper type.</returns>
        private static Type GetRuntimeLaunchArgumentsType()
        {
            return RequireTalosRuntimeType("Talos.E2E.RuntimeLaunchArguments");
        }

        /// <summary>
        /// 通过反射调用 `TryLaunchFromForcedMode`。
        /// Invoke `TryLaunchFromForcedMode` through reflection.
        /// </summary>
        /// <param name="args">测试用命令行参数。Test command-line arguments.</param>
        /// <param name="port">测试用端口。Test port.</param>
        /// <param name="launchAction">测试用启动动作。Test launch action.</param>
        /// <returns>反射调用返回的布尔结果。Boolean result returned by the reflected call.</returns>
        private static bool InvokeTryLaunchFromForcedMode(string[] args, int port, Action<int> launchAction)
        {
            var helperType = GetFallbackHelperType();
            var method = helperType.GetMethod("TryLaunchFromForcedMode");

            Assert.That(method, Is.Not.Null, "应能找到 TryLaunchFromForcedMode 公开静态方法。");
            return (bool)method!.Invoke(null, new object[] { args, port, launchAction });
        }

        /// <summary>
        /// 通过反射调用 `ContainsTalosForceE2EArgument`。
        /// Invoke `ContainsTalosForceE2EArgument` through reflection.
        /// </summary>
        /// <param name="args">测试用命令行参数。Test command-line arguments.</param>
        /// <returns>反射调用返回的布尔结果。Boolean result returned by the reflected call.</returns>
        private static bool InvokeContainsTalosForceE2EArgument(string[] args)
        {
            var helperType = GetFallbackHelperType();
            var method = helperType.GetMethod("ContainsTalosForceE2EArgument");

            Assert.That(method, Is.Not.Null, "应能找到 ContainsTalosForceE2EArgument 公开静态方法。");
            return (bool)method!.Invoke(null, new object[] { args });
        }

        /// <summary>
        /// 通过反射调用 `BuildArgumentSnapshot`。
        /// Invoke `BuildArgumentSnapshot` through reflection.
        /// </summary>
        /// <param name="environmentArgs">环境命令行参数。Environment command-line arguments.</param>
        /// <param name="androidUnityExtra">Android `unity` extra 原始字符串。Raw Android `unity` extra string.</param>
        /// <returns>反射调用返回的参数快照。Argument snapshot returned by the reflected call.</returns>
        private static string[] InvokeBuildArgumentSnapshot(string[] environmentArgs, string androidUnityExtra)
        {
            var helperType = GetRuntimeLaunchArgumentsType();
            var method = helperType.GetMethod("BuildArgumentSnapshot");

            Assert.That(method, Is.Not.Null, "应能找到 BuildArgumentSnapshot 公开静态方法。");
            return (string[])method!.Invoke(null, new object[] { environmentArgs, androidUnityExtra });
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

            var result = InvokeTryLaunchFromForcedMode(
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

            var result = InvokeTryLaunchFromForcedMode(
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
            var result = InvokeContainsTalosForceE2EArgument(
                new[] { "game.exe", "-TALOSFORCEE2E" });

            Assert.That(result, Is.True, "强制模式参数匹配应保持大小写不敏感。");
        }

        /// <summary>
        /// 验证 Android `unity` extra 会被切分并追加到运行时参数快照末尾。
        /// Verify that the Android `unity` extra is tokenized and appended to the runtime argument snapshot.
        /// </summary>
        [Test]
        public void BuildArgumentSnapshot_AppendsAndroidUnityExtraTokens()
        {
            var snapshot = InvokeBuildArgumentSnapshot(
                new[] { "game.exe" },
                "-talosPort 11002 -talosForceE2E");

            Assert.That(snapshot, Is.EqualTo(new[] { "game.exe", "-talosPort", "11002", "-talosForceE2E" }),
                "运行时参数快照应把 Android unity extra 切分后追加到环境参数末尾。");
        }

        /// <summary>
        /// 验证从 Android `unity` extra 合成的参数快照仍能触发强制模式补偿启动。
        /// Verify that a snapshot synthesized from the Android `unity` extra can still trigger the forced-mode fallback launch.
        /// </summary>
        [Test]
        public void TryLaunchFromForcedMode_UsesForceFlagFromAndroidUnityExtraSnapshot()
        {
            var invoked = false;
            var snapshot = InvokeBuildArgumentSnapshot(
                new[] { "game.exe" },
                "-talosForceE2E");

            var result = InvokeTryLaunchFromForcedMode(snapshot, 10002, _ => invoked = true);

            Assert.That(result, Is.True, "由 Android unity extra 合成的参数快照应能触发强制模式补偿调用。");
            Assert.That(invoked, Is.True, "Android unity extra 中的强制模式参数应驱动补偿启动动作。");
        }
    }
}