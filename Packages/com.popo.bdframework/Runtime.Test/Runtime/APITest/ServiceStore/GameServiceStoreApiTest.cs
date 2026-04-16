using System;
using RuntimeGameServiceStore = BDFramework.GameServiceStore.GameServiceStore;

namespace BDFramework.RuntimeTests.ApiTest.ServiceStore
{
    /// <summary>
    /// 游戏服务仓库公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the game service store.
    /// 该类型把模块级容器复用、泛型入口键稳定性和模块隔离规则固定在 Runtime.Test 的 APITest 层内，
    /// 让 Editor 包装与真机 Talos 套件共享同一套服务仓库契约断言。
    /// This type fixes module-container reuse, generic-entry key stability, and module-isolation rules inside the Runtime.Test APITest layer,
    /// allowing editor wrappers and packaged Talos suites to share the same service-store contract assertions.
    /// </summary>
    public sealed class GameServiceStoreApiTest
    {
        /// <summary>
        /// 输出统一日志，记录服务仓库 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the service-store API test.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(GameServiceStoreApiTest) : testName,
                "验证服务仓库的模块隔离与泛型入口映射保持稳定。",
                "通过直接调用公开 API，断言容器缓存行为、模块键选择和隔离边界。"
            );
        }

        /// <summary>
        /// 验证相同模块名称会复用同一个服务容器实例。
        /// Verify that the same module name reuses the same service container instance.
        /// </summary>
        public void GetService_WithSameModuleName_ReusesContainerInstance()
        {
            var moduleName = $"service-store-module-{Guid.NewGuid():N}";

            var first = RuntimeGameServiceStore.GetService(moduleName);
            var second = RuntimeGameServiceStore.GetService(moduleName);

            ApiTestAssert.AreSame(first, second, "相同模块名称应复用同一个服务容器实例。");
        }

        /// <summary>
        /// 验证泛型模块入口会稳定映射到模块类型完整名称对应的容器。
        /// Verify that the generic module entry maps stably to the container keyed by the module type full name.
        /// </summary>
        public void GetService_WithGenericModuleType_UsesStableTypeKey()
        {
            var fromGeneric = RuntimeGameServiceStore.GetService<GenericModuleProbe>();
            var fromName = RuntimeGameServiceStore.GetService(typeof(GenericModuleProbe).FullName);

            ApiTestAssert.AreSame(fromGeneric, fromName, "泛型模块入口应映射到模块类型完整名称对应的容器。");
        }

        /// <summary>
        /// 验证不同模块名称会得到彼此隔离的服务容器。
        /// Verify that different module names receive isolated service containers.
        /// </summary>
        public void GetService_WithDifferentModuleNames_ReturnsDifferentContainers()
        {
            var first = RuntimeGameServiceStore.GetService($"service-store-module-a-{Guid.NewGuid():N}");
            var second = RuntimeGameServiceStore.GetService($"service-store-module-b-{Guid.NewGuid():N}");

            ApiTestAssert.AreNotSame(first, second, "不同模块名称应返回彼此隔离的服务容器。");
        }

        /// <summary>
        /// 用于验证泛型模块键的测试探针。
        /// Test probe used to validate the generic module key.
        /// </summary>
        public sealed class GenericModuleProbe
        {
            /// <summary>
            /// 初始化泛型模块探针。
            /// Initialize the generic module probe.
            /// </summary>
            public GenericModuleProbe()
            {
            }
        }
    }
}