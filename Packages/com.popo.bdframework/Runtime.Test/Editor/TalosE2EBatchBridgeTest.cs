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
        /// <summary>
        /// 每个测试开始时输出统一中文日志，便于在 Unity Test Runner 和 BatchMode 输出中快速定位验证目标。
        /// Emit a unified Chinese start log before each test so Unity Test Runner and BatchMode output can quickly reveal the verification target.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 BDFramework 自己提供 Talos E2E 的 executeMethod 宿主入口。 实现手段=通过反射检查 TalosE2EBatchBridge 上的公开静态无参方法契约。 ");
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
        /// 断言指定类型存在公开静态无参方法。
        /// Assert that the given type exposes a public static parameterless method.
        /// </summary>
        private static void AssertHasPublicStaticParameterlessMethod(System.Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, $"应该能够找到 {type.FullName}.{methodName} 公开静态方法。");
            Assert.That(method!.GetParameters(), Has.Length.EqualTo(0), $"{type.FullName}.{methodName} 应保持无参，便于 Unity -executeMethod 稳定调用。");
        }
    }
}