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
        static List<IStore> StoreList = new List<IStore> ();

        /// <summary>
        /// reducer的map
        /// </summary>
        static Dictionary<Type , IReducer> Type2Reducer = new Dictionary<Type , IReducer> ();

        /// <summary>
        /// 从缓存里获取一个Reducer，没有就创建
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        static IReducer GetReducer<R> () where R : IReducer
        {
            var reducerRealType = typeof (R);
            if ( Type2Reducer.ContainsKey (reducerRealType) )
            {
                return Type2Reducer [reducerRealType];
            }
            R reducer = Activator.CreateInstance<R> ();
            Type2Reducer [reducerRealType] = reducer;
            return reducer;
        }

        /// <summary>
        /// 从缓存里获取一个Reducer，没有就创建
        /// </summary>
        /// <param name="reducerRealType"></param>
        /// <returns></returns>
        static IReducer GetReducer (Type reducerRealType)
        {
            if ( Type2Reducer.ContainsKey (reducerRealType) )
            {
                return Type2Reducer [reducerRealType];
            }

            IReducer reducer = Activator.CreateInstance (reducerRealType) as IReducer;
            if ( reducer == null )
            {
                BDebug.LogError ($"[{reducerRealType}]不兼容IReducer规范");
                return default;
            }

            Type2Reducer [reducerRealType] = reducer;
            return reducer;
        }

        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public Store<T> CreateStore<T, R> () where T : AStateBase, new() where R : IReducer
        {
            //构造store
            var store = Activator.CreateInstance<Store<T>> ();
            store.AddReducer (GetReducer<R> () as AReducers<T>);
            StoreList.Add (store);
            //返回
            return store;
        }

        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reducerRealType"></param>
        /// <returns></returns>
        static public Store<T> CreateStore<T> (Type reducerRealType) where T : AStateBase, new()
        {
            //构造store
            var store = Activator.CreateInstance<Store<T>> ();
            store.AddReducer (GetReducer (reducerRealType) as AReducers<T>);
            StoreList.Add (store);
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
        static public StoreWrapper CreateStore<A, B> (Type r1 , Type r2) where A : AStateBase, new() where B : AStateBase, new()
        {
            //构造store
            var s1 = CreateStore<A> (r1);
            var s2 = CreateStore<B> (r2);

            var sw = new StoreWrapper (s1 , s2);
            return sw;
        }
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B, C> (Type r1 , Type r2 , Type r3) where A : AStateBase, new() where B : AStateBase, new() where C : AStateBase, new()
        {
            //构造store
            var s1 = CreateStore<A> (r1);
            var s2 = CreateStore<B> (r2);
            var s3 = CreateStore<C> (r3);

            var sw = new StoreWrapper (s1 , s2 , s3);
            return sw;
        }
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B, C, D> (Type r1 , Type r2 , Type r3 , Type r4) where A : AStateBase, new() where B : AStateBase, new() where C : AStateBase, new() where D : AStateBase, new()
        {
            //构造store
            var s1 = CreateStore<A> (r1);
            var s2 = CreateStore<B> (r2);
            var s3 = CreateStore<C> (r3);
            var s4 = CreateStore<D> (r4);

            var sw = new StoreWrapper (s1 , s2 , s3 , s4);
            return sw;
        }
        /// <summary>
        /// 创建store
        /// 每次请求都是独立的Store
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public StoreWrapper CreateStore<A, B, C, D, E> (Type r1 , Type r2 , Type r3 , Type r4 , Type r5) where A : AStateBase, new() where B : AStateBase, new() where C : AStateBase, new() where D : AStateBase, new() where E : AStateBase, new()
        {
            //构造store
            var s1 = CreateStore<A> (r1);
            var s2 = CreateStore<B> (r2);
            var s3 = CreateStore<C> (r3);
            var s4 = CreateStore<D> (r4);
            var s5 = CreateStore<E> (r5);

            var sw = new StoreWrapper (s1 , s2 , s3 , s4 , s5);
            return sw;
        }

        #endregion


        /// <summary>
        /// 获取||创建 Store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public Store<T> GetStore<T> () where T : AStateBase, new()
        {
            //这里隐藏了构造函数，所需要反射创建
            //var type = typeof(Store<T>);
            var ret = Activator.CreateInstance<Store<T>> (); //as Store<T>;
            return ret;
        }
    }
}