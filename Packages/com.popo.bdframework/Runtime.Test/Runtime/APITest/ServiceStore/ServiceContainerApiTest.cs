using BDFramework.GameServiceStore;

namespace BDFramework.RuntimeTests.ApiTest.ServiceStore
{
    /// <summary>
    /// 服务容器公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the service container.
    /// 该类型把单例注册、瞬态注册和未知类型解析规则固定在 Runtime.Test 的 APITest 层内，
    /// 让 Editor 包装与真机 Talos 套件共享同一套基础容器契约断言。
    /// This type fixes singleton registration, transient registration, and unknown-type resolution rules inside the Runtime.Test APITest layer,
    /// allowing editor wrappers and packaged Talos suites to share the same baseline container contract assertions.
    /// </summary>
    public sealed class ServiceContainerApiTest
    {
        /// <summary>
        /// 输出统一日志，记录服务容器 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the service-container API test.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(ServiceContainerApiTest) : testName,
                "验证服务容器的单例、瞬态与未注册解析行为保持稳定。",
                "通过直接注册测试探针类型并调用 GetService，断言返回实例与容器约束是否符合预期。"
            );
        }

        /// <summary>
        /// 验证按类型注册单例后，多次解析会返回同一实例。
        /// Verify that resolving a singleton by type returns the same instance repeatedly after registration.
        /// </summary>
        public void AddSingleton_WithTypeRegistration_ReusesSameInstance()
        {
            var container = new ServiceContainer();

            container.AddSingleton<SingletonProbe>();
            var first = container.GetService<SingletonProbe>();
            var second = container.GetService<SingletonProbe>();

            ApiTestAssert.IsNotNull(first, "注册单例后第一次解析不应为空。");
            ApiTestAssert.AreSame(first, second, "单例注册后多次解析应返回同一实例。");
        }

        /// <summary>
        /// 验证重复注册同类型单例时，原始注册会被保留而不会被后续实例覆盖。
        /// Verify that when the same singleton type is registered twice, the original registration is kept instead of being replaced by the later instance.
        /// </summary>
        public void AddSingleton_WithDuplicateType_KeepsOriginalRegistration()
        {
            var container = new ServiceContainer();
            var original = new SingletonProbe { Value = 1 };
            var duplicate = new SingletonProbe { Value = 2 };

            container.AddSingleton(original);
            container.AddSingleton(duplicate);

            var resolved = container.GetService<SingletonProbe>();

            ApiTestAssert.AreSame(original, resolved, "重复注册同类型单例时应保留首次注册的实例。");
            ApiTestAssert.AreEqual(1, resolved.Value, "重复注册单例后解析结果应保留首次实例的数据。");
        }

        /// <summary>
        /// 验证瞬态类型注册后，每次解析都会创建新的实例。
        /// Verify that resolving a transient type creates a new instance on every request after registration.
        /// </summary>
        public void AddTransient_WithRegisteredType_CreatesNewInstancePerResolve()
        {
            var container = new ServiceContainer();

            container.AddTransient(new TransientProbe());
            var first = container.GetService<TransientProbe>();
            var second = container.GetService<TransientProbe>();

            ApiTestAssert.IsNotNull(first, "瞬态注册后的第一次解析不应为空。");
            ApiTestAssert.IsNotNull(second, "瞬态注册后的第二次解析不应为空。");
            ApiTestAssert.AreNotSame(first, second, "瞬态注册后每次解析都应创建新的实例。");
        }

        /// <summary>
        /// 验证未注册类型解析时会返回空值，而不是抛出异常或构造意外实例。
        /// Verify that resolving an unknown type returns null instead of throwing or creating an unexpected instance.
        /// </summary>
        public void GetService_WithUnknownType_ReturnsNull()
        {
            var container = new ServiceContainer();

            var resolved = container.GetService<UnknownProbe>();

            ApiTestAssert.IsNull(resolved, "未注册类型解析时应返回空值。");
        }

        /// <summary>
        /// 用于验证单例注册行为的测试探针。
        /// Test probe used to verify singleton registration behavior.
        /// </summary>
        public sealed class SingletonProbe
        {
            /// <summary>
            /// 记录测试值。
            /// Store the test value.
            /// </summary>
            public int Value { get; set; }

            /// <summary>
            /// 初始化单例探针。
            /// Initialize the singleton probe.
            /// </summary>
            public SingletonProbe()
            {
            }
        }

        /// <summary>
        /// 用于验证瞬态注册行为的测试探针。
        /// Test probe used to verify transient registration behavior.
        /// </summary>
        public sealed class TransientProbe
        {
            /// <summary>
            /// 初始化瞬态探针。
            /// Initialize the transient probe.
            /// </summary>
            public TransientProbe()
            {
            }
        }

        /// <summary>
        /// 用于验证未注册解析行为的测试探针。
        /// Test probe used to verify unknown-type resolution behavior.
        /// </summary>
        public sealed class UnknownProbe
        {
            /// <summary>
            /// 初始化未注册探针。
            /// Initialize the unknown probe.
            /// </summary>
            public UnknownProbe()
            {
            }
        }
    }
}