using System;
using System.Collections.Generic;

namespace BDFramework.GameServiceStore
{
    /// <summary>
    /// 游戏服务仓库
    /// 一般用于使用者动态注入服务
    /// </summary>
    static public class GameServiceStore
    {
        /// <summary>
        /// 游戏服务仓库
        /// </summary>
        static private Dictionary<string, ServiceContainer> serviceStoreMap = new Dictionary<string, ServiceContainer>();

        /// <summary>
        /// 获取一个service
        /// </summary>
        /// <typeparam name="T">模块</typeparam>
        static public ServiceContainer GetService<T>() where T : new()
        {
            return GetService<T>();
        }
        /// <summary>
        /// 获取服务
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
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
