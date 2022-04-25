using System;
using System.Collections.Generic;
using BDFramework.UFlux.Reducer;
using UnityEditor;

namespace BDFramework.UFlux.Contains
{
    static public class StoreFactory
    {
        /// <summary>
        /// store的map
        /// </summary>
        static List<IStore> StoreList = new List<IStore>();

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

        #region 多reducer订阅
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B>(AReducers<A> r1, AReducers<B> r2) where A : AStateBase, new() where B : AStateBase, new() 
        {
            //构造store
            var s1 = CreateStore<A>(r1);
            var s2 = CreateStore<B>(r2);

            var sw = new StoreWrapper(s1, s2);
            return sw;
        }
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B, C>(AReducers<A> r1, AReducers<B> r2, AReducers<C> r3) where A : AStateBase, new() where B : AStateBase, new() where C : AStateBase, new() 
        {
            //构造store
            var s1 = CreateStore<A>(r1);
            var s2 = CreateStore<B>(r2);
            var s3 = CreateStore<C>(r3);

            var sw = new StoreWrapper(s1, s2, s3);
            return sw;
        }
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B, C, D>(AReducers<A> r1, AReducers<B> r2, AReducers<C> r3, AReducers<D> r4) where A : AStateBase, new() where B : AStateBase, new() where C : AStateBase, new() where D : AStateBase, new()
        {
            //构造store
            var s1 = CreateStore<A>(r1);
            var s2 = CreateStore<B>(r2);
            var s3 = CreateStore<C>(r3);
            var s4 = CreateStore<D>(r4);

            var sw = new StoreWrapper(s1, s2, s3, s4);
            return sw;
        }
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B, C, D, E>(AReducers<A> r1, AReducers<B> r2, AReducers<C> r3, AReducers<D> r4, AReducers<E> r5) where A : AStateBase, new() where B : AStateBase, new() where C : AStateBase, new() where D : AStateBase, new() where E : AStateBase, new()
        {
            //构造store
            var s1 = CreateStore<A>(r1);
            var s2 = CreateStore<B>(r2);
            var s3 = CreateStore<C>(r3);
            var s4 = CreateStore<D>(r4);
            var s5 = CreateStore<E>(r5);

            var sw = new StoreWrapper(s1, s2, s3, s4, s5);
            return sw;
        }

        #endregion


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
