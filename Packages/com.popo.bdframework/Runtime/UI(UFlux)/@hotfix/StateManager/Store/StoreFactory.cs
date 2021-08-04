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
        static List< object> StoreList = new List< object >();

        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public Store<T> CreateStore<T>(AReducers<T> reducer) where T : AStateBase, new()
        {
            //构造store
            var store = Activator.CreateInstance<Store<T>>();
            store.AddReducer(reducer);
            StoreList.Add(store);
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
            //这里隐藏了构造函数，所需要反射创建
            //var type = typeof(Store<T>);
            var ret = Activator.CreateInstance<Store<T>>(); //as Store<T>;
            return ret;
        }
    }
}