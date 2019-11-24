using System;
using System.Collections.Generic;
using BDFramework.UFlux.Reducer;

namespace BDFramework.UFlux.Store
{
    static public class Store
    {
        /// <summary>
        /// store的map
        /// </summary>
        static Dictionary<Type, object> StoreMap = new Dictionary<Type, object>();
        /// <summary>
        /// 创建store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public Store<T> CreateStore<T>() where T : AStateBase, new()
        {
            object ret;
            //保持1个Component 1个state实例
            var key = typeof(T);
            if (!StoreMap.TryGetValue(key, out ret))
            {
                //这里隐藏了构造函数，所需要反射创建
                var type = typeof(Store<T>);
                var store = Activator.CreateInstance( type) as Store<T>; // new(state);
                StoreMap[key] = store;
                return store;
            }
            return ret as Store<T>;
        }
    }
}