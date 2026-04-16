using System;
using System.Collections.Generic;

namespace BDFramework.GameServiceStore
{
    /// <summary>
    /// 游戏服务仓库。
    /// Game service store.
    /// 一般用于按模块名称隔离地动态注入服务容器，让不同模块拥有各自独立的单例与瞬态注册表。
    /// It is typically used to inject service containers by isolated module names so each module keeps its own singleton and transient registrations.
    /// </summary>
    static public class GameServiceStore
    {
        /// <summary>
        /// 游戏服务仓库映射。
        /// Game service container map.
        /// </summary>
        static private Dictionary<string, ServiceContainer> serviceStoreMap = new Dictionary<string, ServiceContainer>();

        /// <summary>
        /// 按模块类型获取服务容器。
        /// Get a service container by module type.
        /// 使用模块类型的完整名称作为稳定键，避免泛型入口递归并保证同一模块类型总是映射到同一个容器。
        /// Use the module type full name as a stable key so the generic entry does not recurse and the same module type always maps to the same container.
        /// </summary>
        /// <typeparam name="T">模块类型。Module type.</typeparam>
        static public ServiceContainer GetService<T>() where T : new()
        {
            var moduleName = typeof(T).FullName ?? typeof(T).Name;
            return GetService(moduleName);
        }

        /// <summary>
        /// 按模块名称获取服务容器。
        /// Get a service container by module name.
        /// 如果目标模块还没有注册容器，则创建一个新的空容器并缓存起来。
        /// If the target module has not registered a container yet, create a new empty container and cache it.
        /// </summary>
        /// <param name="moduleName">模块名称。Module name.</param>
        /// <returns>对应模块的服务容器。The service container for the requested module.</returns>
        static public ServiceContainer GetService(string moduleName)
        {
            serviceStoreMap.TryGetValue(moduleName, out var service);
            if (service == null)
            {
                service = new ServiceContainer();
                serviceStoreMap[moduleName] = service;
            }
            return service;
        }
    }
}
