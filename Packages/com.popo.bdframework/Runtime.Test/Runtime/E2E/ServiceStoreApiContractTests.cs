using BDFramework.RuntimeTests.ApiTest.ServiceStore;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 服务仓库 Runtime API 契约测试套件。
    /// Runtime API contract suite for the service-store layer.
    /// 该套件把服务仓库与服务容器的纯逻辑 API 断言迁移为可打包执行的 Talos E2E 用例，
    /// 让真机环境也能持续覆盖模块容器隔离与容器注册规则。
    /// This suite moves the pure-logic API assertions for the service store and service container into packaged Talos E2E cases,
    /// allowing device runs to keep covering module-container isolation and container registration rules.
    /// </summary>
    public static class ServiceStoreApiContractTests
    {
        /// <summary>
        /// 验证相同模块名称会复用同一个服务容器实例。
        /// Verify that the same module name reuses the same service container instance.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 1, des: "service-store-same-module-reuse")]
        public static void ServiceStoreSameModuleReuse()
        {
            var test = new GameServiceStoreApiTest();
            test.SetUp(nameof(ServiceStoreSameModuleReuse));
            test.GetService_WithSameModuleName_ReusesContainerInstance();
        }

        /// <summary>
        /// 验证泛型模块入口会稳定映射到模块类型完整名称对应的容器。
        /// Verify that the generic module entry maps stably to the container keyed by the module type full name.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 2, des: "service-store-generic-key-stability")]
        public static void ServiceStoreGenericKeyStability()
        {
            var test = new GameServiceStoreApiTest();
            test.SetUp(nameof(ServiceStoreGenericKeyStability));
            test.GetService_WithGenericModuleType_UsesStableTypeKey();
        }

        /// <summary>
        /// 验证不同模块名称会得到彼此隔离的服务容器。
        /// Verify that different module names receive isolated service containers.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 3, des: "service-store-module-isolation")]
        public static void ServiceStoreModuleIsolation()
        {
            var test = new GameServiceStoreApiTest();
            test.SetUp(nameof(ServiceStoreModuleIsolation));
            test.GetService_WithDifferentModuleNames_ReturnsDifferentContainers();
        }

        /// <summary>
        /// 验证按类型注册单例后，多次解析会返回同一实例。
        /// Verify that resolving a singleton by type returns the same instance repeatedly after registration.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 10, des: "service-container-singleton-reuse")]
        public static void ServiceContainerSingletonReuse()
        {
            var test = new ServiceContainerApiTest();
            test.SetUp(nameof(ServiceContainerSingletonReuse));
            test.AddSingleton_WithTypeRegistration_ReusesSameInstance();
        }

        /// <summary>
        /// 验证重复注册同类型单例时，原始注册会被保留而不会被后续实例覆盖。
        /// Verify that when the same singleton type is registered twice, the original registration is kept instead of being replaced by the later instance.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 11, des: "service-container-duplicate-singleton")]
        public static void ServiceContainerDuplicateSingleton()
        {
            var test = new ServiceContainerApiTest();
            test.SetUp(nameof(ServiceContainerDuplicateSingleton));
            test.AddSingleton_WithDuplicateType_KeepsOriginalRegistration();
        }

        /// <summary>
        /// 验证瞬态类型注册后，每次解析都会创建新的实例。
        /// Verify that resolving a transient type creates a new instance on every request after registration.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 12, des: "service-container-transient-new-instance")]
        public static void ServiceContainerTransientNewInstance()
        {
            var test = new ServiceContainerApiTest();
            test.SetUp(nameof(ServiceContainerTransientNewInstance));
            test.AddTransient_WithRegisteredType_CreatesNewInstancePerResolve();
        }

        /// <summary>
        /// 验证未注册类型解析时会返回空值。
        /// Verify that resolving an unknown type returns null.
        /// </summary>
        [E2ETest(suite: "service-store-api", order: 13, des: "service-container-unknown-null")]
        public static void ServiceContainerUnknownNull()
        {
            var test = new ServiceContainerApiTest();
            test.SetUp(nameof(ServiceContainerUnknownNull));
            test.GetService_WithUnknownType_ReturnsNull();
        }
    }
}