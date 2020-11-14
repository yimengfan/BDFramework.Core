using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BDFramework.UFlux.Store;

namespace BDFramework.UFlux.Reducer
{
    abstract public class AReducers<T> where T : AStateBase, new()
    {
        /// <summary>
        /// 当前的Reducermap,同步Reducer，
        /// </summary>
        protected Dictionary<int, Store<T>.Reducer> ReducersMap = new Dictionary<int, Store<T>.Reducer>();
        protected Dictionary<int, Store<T>.ReducerAsync> AsyncReducersMap = new Dictionary<int, Store<T>.ReducerAsync>();
        /// <summary>
        /// 当前的Reducermap，callback模式
        /// </summary>
        protected Dictionary<int, Store<T>.ReducerCallback> CallbackReducersMap = new Dictionary<int, Store<T>.ReducerCallback>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public AReducers()
        {
            //注册reducer
            RegisterReducers();
        }
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
        /// 添加异步 reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddAsyncRecucer(Enum @enum,Store<T>.ReducerAsync reducer)
        {
            var key = @enum.GetHashCode();
            if (ReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" +@enum);
                return;
            }
            AsyncReducersMap[key] = reducer;
        }

        /// <summary>
        /// 添加异步reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddCallbackReducer(Enum @enum,Store<T>.ReducerCallback reducerCallback)
        {
            var key = @enum.GetHashCode();
            if (CallbackReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" +@enum);
                return;
            }
            CallbackReducersMap[key] = reducerCallback;
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

            if (this.ReducersMap.ContainsKey(key)
                ||this.AsyncReducersMap.ContainsKey(key))
            {
                return false;
            }

            if (this.CallbackReducersMap.ContainsKey(key))
            {
                return true;
            }

            throw new Exception("not exsit the key:" + @enum);
        }
        
        /// <summary>
        /// 执行Reducer
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="oldState"></param>
        /// <param name="callback"></param>
        /// <returns>返回是否为异步模式</returns>
        public T Excute(Enum @enum, object @params, T oldState)
        {
            //同步列表下寻找
            Store<T>.Reducer func = null;
            var key = @enum.GetHashCode();
            if (this.ReducersMap.TryGetValue(key, out func))
            {
                //同步接口
               return func(oldState, @params);
            }

            return null;
        }
        
        /// <summary>
        /// 执行Reducer
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="oldState"></param>
        /// <param name="callback"></param>
        /// <returns>返回是否为异步模式</returns>
        async public Task<T> ExcuteAsync(Enum @enum, object @params,  T oldState)
        {
            //同步列表下寻找
            Store<T>.ReducerAsync func = null;
            var key = @enum.GetHashCode();
            if (this.AsyncReducersMap.TryGetValue(key, out func))
            {
                //同步接口
                return  await func(oldState, @params);
            }

            return null;
        }
        
        /// <summary>
        /// 异步执行接口
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="params"></param>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        public void ExcuteByCallback(Enum @enum, object @params, Store<T>.GetState getStateFunc,Action<T> callback)
        {
            Store<T>.ReducerCallback func = null;
            var key = @enum.GetHashCode();
            if (this.CallbackReducersMap.TryGetValue(key, out func))
            {
                //异步接口
                func(getStateFunc, @params, callback);
            }
        }
    }
    
    
}