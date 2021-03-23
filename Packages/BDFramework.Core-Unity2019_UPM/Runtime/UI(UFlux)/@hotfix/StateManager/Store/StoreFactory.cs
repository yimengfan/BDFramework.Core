using System;
using System.Collections.Generic;
using BDFramework.UFlux.Reducer;

namespace BDFramework.UFlux.Contains
{
    static public class StoreFactory
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
        static public Store<T> CreateStore<T>(AReducers<T> reducers) where T : AStateBase, new()
        {
            var store = GetStore<T>();
            //添加reducer
            store?.AddReducer(reducers);
            //返回
            return store;
        }

        /// <summary>
        /// 获取||创建 Store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public Store<T> GetStore<T>() where T : AStateBase, new()
        {
            object ret;
            var    key = typeof(T);
            if (!StoreMap.TryGetValue(key, out ret))
            {
                //这里隐藏了构造函数，所需要反射创建
                var type = typeof(Store<T>);
                ret           = Activator.CreateInstance(type); // new(state);
                StoreMap[key] = ret;
            }

            return ret as Store<T>;
        }
    }
}