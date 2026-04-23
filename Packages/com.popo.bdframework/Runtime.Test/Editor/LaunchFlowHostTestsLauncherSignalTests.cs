using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// LaunchFlow 宿主启动器信号恢复测试。
    /// Tests for LaunchFlow host launcher-signal recovery.
    /// 这些测试只覆盖 editor-only sync fallback 的最小恢复契约：当宿主上下文里仍存在启动器对象但单例丢失时，
    /// LaunchFlowHostTests 需要能把该对象重新注册为可读的启动器信号，避免本地 gate 与真机链路在启动器可见性上出现假阴性。
    /// These tests cover only the minimal recovery contract for the editor-only sync fallback: when a launcher object still exists in the host context but the singleton is lost,
    /// LaunchFlowHostTests must re-register that object as a readable launcher signal so the local gate does not report a false negative compared with the device flow.
    /// </summary>
    public class LaunchFlowHostTestsLauncherSignalTests
    {
        private BDFramework.BDLauncher originalLauncherInstance;

        /// <summary>
        /// 每个测试开始前输出统一中文日志并保存原始启动器单例。
        /// Emit a unified Chinese log before each test and capture the original launcher singleton.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 LaunchFlow 宿主测试在 editor-only fallback 下能够从现有 launcher 对象恢复单例信号。 实现手段=通过反射调用 LaunchFlowHostTests 私有恢复辅助器并断言返回实例与 BDLauncher.Inst 已同步恢复。 ");
            originalLauncherInstance = BDFramework.BDLauncher.Inst;
        }

        /// <summary>
        /// 每个测试结束后恢复启动器单例并清理测试对象。
        /// Restore the launcher singleton and clean up test objects after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            var launchers = Resources.FindObjectsOfTypeAll<BDFramework.BDLauncher>();
            foreach (var launcher in launchers)
            {
                if (launcher == null)
                {
                    continue;
                }

                if (launcher == originalLauncherInstance)
                {
                    continue;
                }

                if (launcher.gameObject != null && string.Equals(launcher.gameObject.name, "TalosE2E.EditorOnlyBDLauncher", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(launcher.gameObject);
                }
            }

            SetLauncherInstance(originalLauncherInstance);
        }

        /// <summary>
        /// 验证 editor-only 恢复逻辑能够从隐藏 launcher 对象补回单例信号。
        /// Verify that the editor-only recovery logic restores the singleton signal from a hidden launcher object.
        /// </summary>
        [Test]
        public void ResolveLauncherInstanceForCurrentContext_ShouldRecoverSingletonFromHiddenLauncher()
        {
            var launcherObject = new GameObject("TalosE2E.EditorOnlyBDLauncher")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var launcher = launcherObject.AddComponent<BDFramework.BDLauncher>();
            launcher.hideFlags = HideFlags.HideAndDontSave;
            launcher.ClientVersion = "7.6.5";
            SetLauncherInstance(null);

            var hostType = GetLaunchFlowHostTestsType();
            var resolveMethod = hostType.GetMethod("ResolveLauncherInstanceForCurrentContext", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(resolveMethod, Is.Not.Null, "应该能够找到 LaunchFlowHostTests.ResolveLauncherInstanceForCurrentContext 私有静态方法。");

            var resolvedLauncher = resolveMethod!.Invoke(null, null) as BDFramework.BDLauncher;

            Assert.That(resolvedLauncher, Is.SameAs(launcher), "恢复逻辑应返回当前隐藏 launcher 对象。");
            Assert.That(BDFramework.BDLauncher.Inst, Is.SameAs(launcher), "恢复逻辑应同步回写 BDLauncher.Inst。");
            Assert.That(BDFramework.BDLauncher.Inst.ClientVersion, Is.EqualTo("7.6.5"), "恢复后的启动器信号应保留原有母包版本。");
        }

        /// <summary>
        /// 解析 LaunchFlow 宿主测试类型。
        /// Resolve the LaunchFlow host-test type.
        /// </summary>
        /// <returns>当前 AppDomain 中的宿主测试类型。</returns>
        /// <returns>The host test type from the current AppDomain.</returns>
        private static Type GetLaunchFlowHostTestsType()
        {
            var loadedType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("BDFramework.HostE2E.LaunchFlowHostTests"))
                .FirstOrDefault(type => type != null);

            Assert.That(loadedType, Is.Not.Null, "应能在当前 AppDomain 中解析到 BDFramework.HostE2E.LaunchFlowHostTests 类型。");
            return loadedType!;
        }

        /// <summary>
        /// 通过反射回写测试用启动器单例。
        /// Assign the launcher singleton for the test through reflection.
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