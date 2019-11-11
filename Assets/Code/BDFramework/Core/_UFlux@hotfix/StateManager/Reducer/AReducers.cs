using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.UFlux.Store;

namespace BDFramework.UFlux.Reducer
{
    abstract public class AReducers<T> where T : AStateBase, new()
    {
        /// <summary>
        /// 当前的Reducermap,同步Reducer，
        /// await 也适用
        /// </summary>
        protected Dictionary<int, Store<T>.Reducer> ReducersMap = new Dictionary<int, Store<T>.Reducer>();

        /// <summary>
        /// 当前的Reducermap，callback模式
        /// </summary>
        protected Dictionary<int, Store<T>.AsyncReducer> AsyncReducersMap = new Dictionary<int, Store<T>.AsyncReducer>();
        
        /// <summary>
        /// 注册所有的Reducer
        /// </summary>
        virtual public void RegisterReducers()
        {
            //这里改为显示注册，以减少使用者的 输入成本

            #region 注释掉过渡设计,直接语法层约束，减少错误成本
//            var t = this.GetType();
//            var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
//            foreach (var methodInfo in t.GetMethods(flag))
//            {
//                var attr = methodInfo.GetCustomAttributes(typeof(ReducerAttribute), false);
//                if (attr.Length > 0)
//                {
//                    var _attr = attr[0] as ReducerAttribute;
//
//                    var action = Delegate.CreateDelegate(typeof(Store<T>.Reducer), this, methodInfo) as Store<T>.Reducer;
//                    if (action != null)
//                    {
//                        ReducersMap[_attr.Reducer] = action;
//                    }
//                    else
//                    {
//                        BDebug.LogError("reducer 错误:" + methodInfo.Name);
//                    }
//                }
//                else
//                {
//                    var asyncAttr = methodInfo.GetCustomAttributes(typeof(AsyncReducerAttribute), false);
//                    if (asyncAttr.Length > 0)
//                    {
//                        var _attr = attr[0] as AsyncReducerAttribute;
//
//                        var action = Delegate.CreateDelegate(typeof(Store<T>.AsyncReducer), this, methodInfo) as Store<T>.AsyncReducer;
//                        if (action != null)
//                        {
//                            AsyncReducersMap[_attr.Reducer] = action;
//                        }
//                        else
//                        {
//                            BDebug.LogError("async reducer 错误:" + methodInfo.Name);
//                        }
//                    }
//                }
//            }
            

            #endregion

        }


        /// <summary>
        /// 添加同步 reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddRecucer(Enum @enum,Store<T>.Reducer reducer)
        {
            var key = @enum.GetHashCode();
            if (ReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" +@enum);
                return;
            }
            ReducersMap[key] = reducer;
        }


        /// <summary>
        /// 添加异步reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddAsyncReducer(Enum @enum,Store<T>.AsyncReducer asyncReducer)
        {
            var key = @enum.GetHashCode();
            if (AsyncReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" +@enum);
                return;
            }
            AsyncReducersMap[key] = asyncReducer;
        }
        
        /// <summary>
        /// 是否为异步方法
        /// </summary>
        /// <param name="enum"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool IsAsyncLoad(Enum @enum)
        {
            var key = @enum.GetHashCode();

            if (this.ReducersMap.ContainsKey(key))
            {
                return false;
            }

            if (this.AsyncReducersMap.ContainsKey(key))
            {
                return true;
            }

            throw new Exception("not exsit the key:" + @enum);
        }
        
        /// <summary>
        /// 执行Reducer
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        /// <returns>返回是否为异步模式</returns>
        public void Excute(Enum @enum, object @params, T state)
        {
            //同步列表下寻找
            Store<T>.Reducer func = null;
            var key = @enum.GetHashCode();
            if (this.ReducersMap.TryGetValue(key, out func))
            {
                //同步接口
                func(state, @params);
            }
        }
        
        /// <summary>
        /// 异步执行接口
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="params"></param>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        public void AsyncExcute(Enum @enum, object @params, Store<T>.GetState getStateFunc,Action<T> callback)
        {
            Store<T>.AsyncReducer asyncFunc = null;
            var key = @enum.GetHashCode();
            if (this.AsyncReducersMap.TryGetValue(key, out asyncFunc))
            {
                //异步接口
                asyncFunc(getStateFunc, @params, callback);
            }
        }
    }
}