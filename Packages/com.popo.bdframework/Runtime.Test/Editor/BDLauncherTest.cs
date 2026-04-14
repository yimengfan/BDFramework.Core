using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// 验证启动器对 <c>ScriptLoder.Init</c> 的反射契约。
    /// 这个测试只检查静态入口是否可被发现，避免后续把热更初始化改回实例查找后再次出现启动失败。
    /// </summary>
    public class BdLauncherTest
    {
        /// <summary>
        /// 在每个测试开始时输出统一的中文启动日志，便于 BatchMode 和 Unity Test Runner 直接定位验证目标。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            UnityDebug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证启动器能够定位静态的 ScriptLoder.Init。 实现手段=按运行时相同的程序集扫描规则查找方法信息，但不实际执行热更初始化。 ");
        }

        /// <summary>
        /// 验证热更脚本加载入口仍然是可发现的静态方法。
        /// </summary>
        [Test]
        public void FindScriptLoderInitMethod_ShouldResolveStaticMethod()
        {
            MethodInfo method = null;

            // Phase 1: 按启动器相同的程序集扫描方式查找热更入口类型。
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("BDFramework.ScriptLoder");
                if (type == null)
                {
                    continue;
                }

                // Phase 2: 只断言静态入口可被发现，不执行方法体，避免把测试耦合到加载流程副作用。
                method = type.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    break;
                }
            }

            Assert.That(method, Is.Not.Null, "应该能够找到 BDFramework.ScriptLoder 的静态 Init 方法。");
            Assert.That(method.IsStatic, Is.True, "Init 入口必须保持静态，才能被启动器按静态方式调用。");
            Assert.That(method.GetParameters(), Is.Empty, "Init 入口应保持无参，避免启动阶段额外参数耦合。");
        }

        /// <summary>
        /// 验证 AOT 阶段的 E2E 自动检测入口不再依赖编译期 DEBUG 条件裁剪。
        /// 这样远端调试母包即使依赖运行时参数，也能在 WindowPreconfig 出现前完成自动检测。
        /// </summary>
        [Test]
        public void TryStartE2EFramework_ShouldNotDependOnConditionalDebugAttribute()
        {
            UnityDebug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 ScriptLoder 的 E2E 自动检测不会再被编译期 DEBUG 条件直接裁掉。 实现手段=反射读取私有静态方法上的 ConditionalAttribute。 ");

            var type = typeof(BDFramework.ScriptLoder);
            var method = type.GetMethod("TryStartE2EFramework", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "应该能够找到 ScriptLoder.TryStartE2EFramework 私有静态方法。");
            Assert.That(
                method.GetCustomAttributes(typeof(ConditionalAttribute), false),
                Is.Empty,
                "TryStartE2EFramework 不应再使用 Conditional(DEBUG)，否则 AOT 阶段的自动检测调用会被编译期裁掉。"
            );
        }
    }
}



