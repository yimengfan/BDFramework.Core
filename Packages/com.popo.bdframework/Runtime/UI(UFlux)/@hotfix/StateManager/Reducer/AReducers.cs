using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BDFramework.UFlux.Contains;
using Cysharp.Text;

namespace BDFramework.UFlux.Reducer
{
    abstract public class AReducers<T> where T : AStateBase, new()
    {
        public enum ExecuteTypeEnum
        {
            None,
            /// <summary>
            /// 同步
            /// </summary>
            Synchronization,

            /// <summary>
            /// 异步
            /// </summary>
            Async,
            
            /// <summary>
            /// 回调
            /// </summary>
            Callback
        }

        /// <summary>
        /// 当前的Reducermap,同步Reducer，
        /// </summary>
        protected Dictionary<string, Store<T>.Reducer> ReducersMap = new Dictionary<string, Store<T>.Reducer>();

        /// <summary>
        /// 当前的Reducermap,异步Reducer，
        /// </summary>
        protected Dictionary<string, Store<T>.ReducerAsync> AsyncReducersMap = new Dictionary<string, Store<T>.ReducerAsync>();

        /// <summary>
        /// 当前的Reducermap，callback模式
        /// </summary>
        protected Dictionary<string, Store<T>.ReducerCallback> CallbackReducersMap = new Dictionary<string, Store<T>.ReducerCallback>();


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
        }


        /// <summary>
        /// 获取enum的key
        /// </summary>
        /// <param name="enum"></param>
        /// <returns></returns>
        private string GetEnumKey(Enum @enum)
        {
            var key = ZString.Concat( @enum.GetType().FullName ,".", @enum.ToString());
            return key;
        }

        /// <summary>
        /// 添加同步 reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddRecucer(Enum @enum, Store<T>.Reducer reducer)
        {
            var key =GetEnumKey(@enum);
            if (ReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" + @enum);
                return;
            }

            ReducersMap[key] = reducer;
        }


        /// <summary>
        /// 添加同步 reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddAsyncRecucer(Enum @enum, Store<T>.ReducerAsync reducer)
        {
            var key =GetEnumKey(@enum);;
            if (ReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" + @enum);
                return;
            }

            AsyncReducersMap[key] = reducer;
        }

        /// <summary>
        /// 添加异步reducer
        /// 这里需要注意乱序问题，如果强制保证顺序 则有可能导致后续逻辑不执行
        /// </summary>
        /// <param name="enum"></param>
        protected void AddCallbackReducer(Enum @enum, Store<T>.ReducerCallback reducerCallback)
        {
            var key =GetEnumKey(@enum);
            if (CallbackReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" + @enum);
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
        public ExecuteTypeEnum GetExecuteType(Enum @enum)
        {
            var key =GetEnumKey(@enum);

            if (this.ReducersMap.ContainsKey(key))
            {
                return ExecuteTypeEnum.Synchronization;
            }
            else if (this.AsyncReducersMap.ContainsKey(key))
            {
                return ExecuteTypeEnum.Async;
            }
            else if (this.CallbackReducersMap.ContainsKey(key))
            {
                return ExecuteTypeEnum.Callback;
            }

            return ExecuteTypeEnum.None;
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
            var key =GetEnumKey(@enum);
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
        async public Task<T> ExcuteAsync(Enum @enum, object @params, T oldState)
        {
            //同步列表下寻找
            Store<T>.ReducerAsync func = null;
            var key =GetEnumKey(@enum);
            if (this.AsyncReducersMap.TryGetValue(key, out func))
            {
                //同步接口
                return await func(oldState, @params);
            }

            return null;
        }

        /// <summary>
        /// 异步回调接口
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="params"></param>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        public void ExcuteByCallback(Enum @enum, object @params, Store<T>.GetState getStateFunc, Action<T> callback)
        {
            Store<T>.ReducerCallback func = null;
            var key =GetEnumKey(@enum);
            if (this.CallbackReducersMap.TryGetValue(key, out func))
            {
                //异步接口
                func(getStateFunc, @params, callback);
            }
        }
    }
}
