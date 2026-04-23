using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// Talos E2E 宿主批入口测试。
    /// Talos E2E host batch-entry tests.
    /// 该测试只验证 BDFramework 自己暴露了稳定的 executeMethod 入口，
    /// 让本地脚本和 CI 可以从宿主侧进入 Talos E2E，而不是再反向依赖 Talos 包内的宿主回调注册。
    /// This test verifies only that BDFramework exposes stable executeMethod entrypoints of its own,
    /// so local scripts and CI can enter Talos E2E from the host side instead of depending again on host callback registration inside the Talos package.
    /// </summary>
    public class TalosE2EBatchBridgeTest
    {
        private BDFramework.BDLauncher originalLauncherInstance;
        private BDFramework.BDLauncher[] originalLaunchers = Array.Empty<BDFramework.BDLauncher>();
        private string[] originalLauncherVersions = Array.Empty<string>();

        /// <summary>
        /// 每个测试开始时输出统一中文日志，便于在 Unity Test Runner 和 BatchMode 输出中快速定位验证目标。
        /// Emit a unified Chinese start log before each test so Unity Test Runner and BatchMode output can quickly reveal the verification target.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 BDFramework 自己提供 Talos E2E 的 executeMethod 宿主入口。 实现手段=通过反射检查 TalosE2EBatchBridge 上的公开静态无参方法契约。 ");
            originalLauncherInstance = BDFramework.BDLauncher.Inst;
            originalLaunchers = Resources.FindObjectsOfTypeAll<BDFramework.BDLauncher>();
            originalLauncherVersions = new string[originalLaunchers.Length];
            for (var index = 0; index < originalLaunchers.Length; index++)
            {
                var launcher = originalLaunchers[index];
                originalLauncherVersions[index] = launcher ? launcher.ClientVersion : null;
            }
        }

        /// <summary>
        /// 在每个测试后恢复原始启动器状态。
        /// Restore the original launcher state after each test.
        /// 该清理步骤会回写测试前的单例引用、还原原有启动器版本号，并删除测试期间额外创建的隐藏占位启动器，
        /// 避免 editor 测试把静态状态泄漏到后续套件。
        /// This cleanup writes back the pre-test singleton reference, restores original launcher versions, and removes any hidden placeholder launchers created during the test,
        /// preventing editor tests from leaking static state into later suites.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            for (var index = 0; index < originalLaunchers.Length; index++)
            {
                var launcher = originalLaunchers[index];
                if (launcher)
                {
                    launcher.ClientVersion = originalLauncherVersions[index];
                }
            }

            var currentLaunchers = Resources.FindObjectsOfTypeAll<BDFramework.BDLauncher>();
            foreach (var launcher in currentLaunchers)
            {
                if (Array.IndexOf(originalLaunchers, launcher) >= 0)
                {
                    continue;
                }

                if (launcher)
                {
                    UnityEngine.Object.DestroyImmediate(launcher.gameObject);
                }
            }

            SetLauncherInstance(originalLauncherInstance);
        }

        /// <summary>
        /// 验证 BDFramework 自己暴露了 editor-only、sync export 与 PlayMode 三个批入口。
        /// Verify that BDFramework itself exposes the editor-only, sync-export, and PlayMode batch entrypoints.
        /// </summary>
        [Test]
        public void TalosE2EBatchBridge_ShouldExposePublicStaticParameterlessEntries()
        {
            var bridgeType = typeof(BDFramework.Editor.Environment.TalosE2EBatchBridge);

            AssertHasPublicStaticParameterlessMethod(bridgeType, "LaunchTalosE2EBatchMode");
            AssertHasPublicStaticParameterlessMethod(bridgeType, "LaunchTalosE2EEditorOnly");
            AssertHasPublicStaticParameterlessMethod(bridgeType, "RunTalosE2EAndExport");
        }

        /// <summary>
        /// 验证 editor-only 宿主入口会补齐启动器单例与母包版本信号。
        /// Verify that the editor-only host entry restores the launcher singleton and base-package version signal.
        /// 本地 sync fallback 不会自动进入真实启动场景，因此这个契约必须由宿主 bridge 自己保证，
        /// 否则 launch suite 会在进入远程 TeamCity 之前就失去与真机一致的最小启动信号。
        /// The local sync fallback does not enter the real startup scene automatically, so the host bridge must guarantee this contract on its own;
        /// otherwise the launch suite loses the minimal startup signal that should match the device flow before TeamCity is even queued.
        /// </summary>
        [Test]
        public void EnsureEditorOnlyLauncherSignal_ShouldRegisterLauncherInstanceWithRequestedClientVersion()
        {
            SetLauncherInstance(null);

            var bridgeType = typeof(BDFramework.Editor.Environment.TalosE2EBatchBridge);
            var method = bridgeType.GetMethod("EnsureEditorOnlyLauncherSignal", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "应该能够找到 TalosE2EBatchBridge.EnsureEditorOnlyLauncherSignal 私有静态方法。");

            method!.Invoke(null, new object[] { "9.8.7" });

            Assert.That(BDFramework.BDLauncher.Inst, Is.Not.Null, "editor-only 宿主入口应该补齐 BDLauncher.Inst。");
            Assert.That(BDFramework.BDLauncher.Inst.ClientVersion, Is.EqualTo("9.8.7"), "editor-only 宿主入口应该把请求的母包版本写回启动器信号。");
        }

        /// <summary>
        /// 断言指定类型存在公开静态无参方法。
        /// Assert that the given type exposes a public static parameterless method.
        /// </summary>
        private static void AssertHasPublicStaticParameterlessMethod(System.Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, $"应该能够找到 {type.FullName}.{methodName} 公开静态方法。");
            Assert.That(method!.GetParameters(), Has.Length.EqualTo(0), $"{type.FullName}.{methodName} 应保持无参，便于 Unity -executeMethod 稳定调用。");
        }

        /// <summary>
        /// 通过反射回写测试期间的启动器单例。
        /// Assign the launcher singleton for test setup and cleanup through reflection.
        /// 测试需要显式清空和恢复 <c>BDLauncher.Inst</c>，以便直接验证 batch bridge 的补环境行为，
        /// 因此这里复用与生产 bridge 相同的受控反射策略，而不是让测试依赖 Unity 生命周期侧效应。
        /// The tests need to explicitly clear and restore <c>BDLauncher.Inst</c> so they can validate the batch bridge environment-restoration behavior directly,
        /// so this helper reuses the same controlled reflection strategy as production code instead of depending on Unity lifecycle side effects.
        /// </summary>
        /// <param name="launcher">要写回的启动器实例；传入 null 时清空单例。</param>
        /// <param name="launcher">Launcher instance to write back; pass null to clear the singleton.</param>
        private static void SetLauncherInstance(BDFramework.BDLauncher launcher)
        {
            var instProperty = typeof(BDFramework.BDLauncher).GetProperty(nameof(BDFramework.BDLauncher.Inst), BindingFlags.Public | BindingFlags.Static);
            var instSetter = instProperty?.GetSetMethod(true);

            Assert.That(instSetter, Is.Not.Null, "应该能够找到 BDLauncher.Inst 的私有 setter。");
            instSetter!.Invoke(null, new object[] { launcher });
        }
    }
}