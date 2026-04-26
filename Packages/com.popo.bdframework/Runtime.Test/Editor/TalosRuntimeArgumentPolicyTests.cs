using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// Talos 运行时参数快照与端口策略测试。
    /// Tests for the Talos runtime argument snapshot and port policy helpers.
    /// 这些测试锁定两类纯逻辑契约：
    /// Android `unity` extra 被并入运行时参数快照后的行为，以及平台隔离端口池的稳定定义。
    /// These tests lock two pure-logic contracts:
    /// the behavior after Android `unity` extra tokens are merged into the runtime argument snapshot,
    /// and the stable definitions of the platform-isolated port pools.
    /// </summary>
    public class TalosRuntimeArgumentPolicyTests
    {
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 Talos 运行时参数快照与平台端口池逻辑在 Editor NUnit 下保持稳定。 实现手段=通过反射调用 Talos runtime 辅助器并断言参数快照与端口池结果。 ");
        }

        private static Type RequireTalosRuntimeType(string fullTypeName)
        {
            var runtimeType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullTypeName))
                .FirstOrDefault(type => type != null);

            Assert.That(runtimeType, Is.Not.Null, $"应能在当前 AppDomain 中解析到 {fullTypeName} 类型。");
            return runtimeType!;
        }

        private static Type GetRuntimeLaunchArgumentsType()
        {
            return RequireTalosRuntimeType("Talos.E2E.RuntimeLaunchArguments");
        }

        private static string[] InvokeBuildArgumentSnapshot(string[] environmentArgs, string androidUnityExtra)
        {
            var helperType = GetRuntimeLaunchArgumentsType();
            var method = helperType.GetMethod("BuildArgumentSnapshot");

            Assert.That(method, Is.Not.Null, "应能找到 BuildArgumentSnapshot 公开静态方法。");
            return (string[])method!.Invoke(null, new object[] { environmentArgs, androidUnityExtra });
        }

        private static Type GetTalosPortPolicyType()
        {
            return RequireTalosRuntimeType("Talos.E2E.Transport.TalosPortPolicy");
        }

        private static int[] ReadPortPool(string fieldName)
        {
            var policyType = GetTalosPortPolicyType();
            var field = policyType.GetField(fieldName);

            Assert.That(field, Is.Not.Null, $"应能找到 TalosPortPolicy.{fieldName} 静态字段。");
            return ((int[])field!.GetValue(null)!).ToArray();
        }

        [Test]
        public void BuildArgumentSnapshot_AppendsAndroidUnityExtraTokens()
        {
            var snapshot = InvokeBuildArgumentSnapshot(
                new[] { "game.exe" },
                "--android-session player-ready");

            Assert.That(snapshot, Is.EqualTo(new[] { "game.exe", "--android-session", "player-ready" }),
                "运行时参数快照应把 Android unity extra 切分后追加到环境参数末尾。");
        }

        [Test]
        public void BuildArgumentSnapshot_FiltersBlankEnvironmentTokens()
        {
            var snapshot = InvokeBuildArgumentSnapshot(
                new[] { "game.exe", string.Empty, "  " },
                "   ");

            Assert.That(snapshot, Is.EqualTo(new[] { "game.exe" }),
                "运行时参数快照应过滤空白环境参数与空白 Android extra。");
        }

        [Test]
        public void TalosPortPolicy_DeclaresPlatformIsolatedCandidatePools()
        {
            Assert.That(ReadPortPool("WindowsPlayerPorts"), Is.EqualTo(new[] { 10002, 10012, 10022 }));
            Assert.That(ReadPortPool("AndroidPlayerPorts"), Is.EqualTo(new[] { 11002, 11012, 11022 }));
            Assert.That(ReadPortPool("MacOSPlayerPorts"), Is.EqualTo(new[] { 12002, 12012, 12022 }));
            Assert.That(ReadPortPool("EditorPorts"), Is.EqualTo(new[] { 13002, 13012, 13022 }));
        }
    }
}
