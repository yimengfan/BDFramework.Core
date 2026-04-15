using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BDFramework.UFlux.Contains;
using Cysharp.Text;
using UnityEngine;

namespace BDFramework.UFlux.Reducer
{
    /// <summary>
    /// Reduder基类
    /// 类似函数式编程，一个每个函数均为无状态，
    /// 永远只处理:接收State,返回State
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract public class AReducers<T> where T : AStateBase, new()
    {
        /// <summary>
        /// 是否激活状态
        /// </summary>
        protected bool isActive { get; set; }

        public delegate bool DispatchDelegate(Enum actionEnum, object @params = null);

        /// <summary>
        /// 桥接Store的Dispatch，用以Reducer内部使用
        /// </summary>
        /// <param name="actionEnum"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        public DispatchDelegate Dispatch { get; set; }

        /// <summary>
        /// 当前状态数据
        /// </summary>
        public T State { get; set; }

        //
        public enum ExecuteTypeEnum
        {
            None,

            /// <summary>
            /// 同步
            /// </summary>
            Sync,

            /// <summary>
            /// 异步
            /// </summary>
            Async,

            /// <summary>
            /// 回调
            /// TODO 即将在未来某个版本淘汰
            /// </summary>
            Callback
        }

        /// <summary>
        /// 当前的Reducermap,同步Reducer，
        /// </summary>
        protected Dictionary<string, Store<T>.Reducer> ReducersMap = new Dictionary<string, Store<T>.Reducer>();

        //
        protected Dictionary<int, List<MethodInfo>> ReducersMethodMap = new Dictionary<int, List<MethodInfo>>();

        /// <summary>
        /// 当前的Reducermap,异步Reducer，
        /// </summary>
        protected Dictionary<string, Store<T>.ReducerAsync> AsyncReducersMap = new Dictionary<string, Store<T>.ReducerAsync>();

        protected Dictionary<int, List<MethodInfo>> AsyncReducersMethodMap = new Dictionary<int, List<MethodInfo>>();

        /// <summary>
        /// 当前的Reducermap，callback模式
        /// TODO 即将在未来某个版本淘汰
        /// </summary>
        protected Dictionary<string, Store<T>.ReducerCallback> CallbackReducersMap = new Dictionary<string, Store<T>.ReducerCallback>();

        protected Dictionary<int, List<MethodInfo>> CallbackReducersMethodMap = new Dictionary<int, List<MethodInfo>>();

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
            var ms = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var m in ms)
            {
                var reducerAttr = m.GetCustomAttribute<ReducerAttribute>();
                if (reducerAttr != null)
                {
                    var @params = m.GetParameters();
                    if (@params[0].ParameterType == typeof(T))
                    {
                        if (@params.Length == 1 || @params.Length == 2)
                        {
                            //异步
                            if (m.ReturnType == typeof(Task<T>))
                            {
                                var key = reducerAttr.ReducerEnum.GetHashCode();
                                var ret = this.AsyncReducersMethodMap.TryGetValue(key, out var miList);
                                if (!ret)
                                {
                                    miList = new List<MethodInfo>();
                                    this.AsyncReducersMethodMap[key] = miList;
                                }

                                miList.Add(m);
                                BDebug.Log($"[Reducer]注册异步Reducer:{reducerAttr.ReducerEnum.ToString()}- {this.GetType().Name}.{m.Name}", Color.green);
                            }
                            //同步
                            else if (m.ReturnType == typeof(T))
                            {
                                var key = reducerAttr.ReducerEnum.GetHashCode();
                                var ret = this.ReducersMethodMap.TryGetValue(key, out var miList);
                                if (!ret)
                                {
                                    miList = new List<MethodInfo>();
                                    this.ReducersMethodMap[key] = miList;
                                }

                                miList.Add(m);
                                BDebug.Log($"[Reducer]注册同步Reducer:{reducerAttr.ReducerEnum.ToString()}- {this.GetType().Name}.{m.Name}", Color.green);
                            }
                        }
                        //callback,callback必须接收参数 TODO 即将在未来某个版本淘汰
                        else if (@params.Length == 3)
                        {
                            var key = reducerAttr.ReducerEnum.GetHashCode();
                            var ret = this.CallbackReducersMethodMap.TryGetValue(key, out var miList);
                            if (!ret)
                            {
                                miList = new List<MethodInfo>();
                                this.CallbackReducersMethodMap[key] = miList;
                            }

                            miList.Add(m);
                            BDebug.Log($"[Reducer]注册回调Reducer:{reducerAttr.ReducerEnum.ToString()}- {this.GetType().Name}.{m.Name}", Color.green);
                        }
                    }
                    else
                    {
                        BDebug.Log($"[Reducer]函数形参匹配失败:{m.Name}-{m.Name}");
                    }
                }
            }
        }


        /// <summary>
        /// 获取enum的key
        /// </summary>
        /// <param name="enum"></param>
        /// <returns></returns>
        private string GetEnumKey(Enum @enum)
        {
            var key = ZString.Concat(@enum.GetType().FullName, ".", @enum.ToString());
            return key;
        }

        /// <summary>
        /// 添加同步 reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddRecucer(Enum @enum, Store<T>.Reducer reducer)
        {
            var key = GetEnumKey(@enum);
            if (ReducersMap.ContainsKey(key))
            {
                BDebug.LogError("重复添加key,请检查" + @enum);
                return;
            }

            ReducersMap[key] = reducer;
        }


        /// <summary>
        /// 添加异步 reducer
        /// </summary>
        /// <param name="enum"></param>
        protected void AddAsyncRecucer(Enum @enum, Store<T>.ReducerAsync reducer)
        {
            var key = GetEnumKey(@enum);
            if (AsyncReducersMap.ContainsKey(key))
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
            var key = GetEnumKey(@enum);
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
            var key = GetEnumKey(@enum);

            if (this.ReducersMap.ContainsKey(key) || this.ReducersMethodMap.ContainsKey(@enum.GetHashCode()))
            {
                return ExecuteTypeEnum.Sync;
            }
            else if (this.AsyncReducersMap.ContainsKey(key) || this.AsyncReducersMethodMap.ContainsKey(@enum.GetHashCode()))
            {
                return ExecuteTypeEnum.Async;
            }
            else if (this.CallbackReducersMap.ContainsKey(key) || this.CallbackReducersMethodMap.ContainsKey(@enum.GetHashCode()))
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
            var key = GetEnumKey(@enum);
            if (this.ReducersMap.TryGetValue(key, out func))
            {
                //同步接口
                var newState = func(oldState, @params);
                if (newState != null)
                {
                    this.State = newState;
                }

                return newState;
            }
            //获取ReducerIntMap
            else if (this.ReducersMethodMap.TryGetValue(@enum.GetHashCode(), out var miList))
            {
                //其中一个响应则return
                bool isTrigger = false;
                T newState = null;
                foreach (var mi in miList)
                {
                    //匹配参数类型
                    var methodParams = mi.GetParameters();
                    if (methodParams.Length == 1)
                    {
                        if (@params == null)
                        {
                            BDebug.Log($"触发同步Reducer-无参数:{this.GetType().Name}.{mi.Name}", Color.green);
                            newState = (T) mi.Invoke(this, new object[] {oldState});
                            isTrigger = true;
                        }

                    }
                    else if (methodParams.Length == 2 && @params != null)
                    {
                        if (methodParams[1].ParameterType == @params.GetType())
                        {
                            BDebug.Log($"触发同步Reducer:{this.GetType().Name}.{mi.Name}", Color.green);
                            newState = (T) mi.Invoke(this, new object[] {oldState, @params});
                            isTrigger = true;
                        }
                    }
                }

                if (isTrigger)
                {
                    if (newState != null)
                    {
                        this.State = newState;
                    }

                    return newState;
                }

#if UNITY_EDITOR
                foreach (var mi in miList)
                {
                    var methodParams = mi.GetParameters();
                    var str1 = "";
                    if (methodParams.Length == 1)
                    {
                        str1 = "无形参";
                    }
                    else
                    {
                        str1 = methodParams[1].ParameterType.Name;
                    }

                    string str2 = "";
                    if(@params!=null)
                    {
                        str2 = @params.GetType().Name;
                    }
                    else
                    {
                        str2 = "无传参";
                    }
                    
                    BDebug.LogError($"触发失败，参数不匹配!同步Reducer:{this.GetType().Name}.{mi.Name} , 形参:{str1} 传入:{str2}");
                }
#endif 
            }
            else
            {
                BDebug.LogError($"未触发同步Reducer:{@enum.ToString()}");
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
            var key = GetEnumKey(@enum);
            if (this.AsyncReducersMap.TryGetValue(key, out func))
            {
                //异步接口
                var newState = await func(oldState, @params);
                if (newState != null)
                {
                    this.State = newState;
                }

                return newState;
            }

            //获取ReducerIntMap
            else if (this.AsyncReducersMethodMap.TryGetValue(@enum.GetHashCode(), out var miList))
            {
                //其中一个响应则return
                bool isTrigger = false;
                T newState = null;
                foreach (var mi in miList)
                {
                    var methodParams = mi.GetParameters();
                    if (methodParams.Length == 1)
                    {
                        if (@params == null)
                        {
                            BDebug.Log($"触发异步Reducer-无参数:{this.GetType().Name}.{mi.Name}", Color.green);
                            newState = await (Task<T>) mi.Invoke(this, new object[] {oldState});
                            isTrigger = true;
                        }
                    }
                    else if (methodParams.Length == 2 && @params != null)
                    {
                        if (methodParams[1].ParameterType == @params.GetType())
                        {
                            BDebug.Log($"触发异步Reducer:{this.GetType().Name}.{mi.Name}", Color.green);
                            newState = await (Task<T>) mi.Invoke(this, new object[] {oldState, @params});

                            isTrigger = true;
                        }
                    }
                }


                if (isTrigger)
                {
                    if (newState != null)
                    {
                        this.State = newState;
                    }

                    return newState;
                }

#if UNITY_EDITOR
                foreach (var mi in miList)
                {
                    var methodParams = mi.GetParameters();
                    var str1 = "";
                    if (methodParams.Length == 1)
                    {
                        str1 = "无形参";
                    }
                    else
                    {
                        str1 = methodParams[1].ParameterType.Name;
                    }

                    string str2 = "";
                    if(@params!=null)
                    {
                        str2 = @params.GetType().Name;
                    }
                    else
                    {
                        str2 = "无传参";
                    }
                    
                    BDebug.LogError($"触发失败，参数不匹配!同步Reducer:{this.GetType().Name}.{mi.Name} , 形参:{str1} 传入:{str2}");
                }
#endif
            }
            else
            {
                BDebug.LogError($"未触发异步Reducer:{@enum.ToString()}");
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
            var key = GetEnumKey(@enum);
            if (this.CallbackReducersMap.TryGetValue(key, out func))
            {
                //异步接口
                func(getStateFunc, @params, (s) =>
                {
                    this.State = s;
                    callback?.Invoke(s);
                });
            }

            //获取ReducerIntMap
            else if (this.CallbackReducersMethodMap.TryGetValue(@enum.GetHashCode(), out var miList))
            {
                //匹配参数类型
                //其中一个响应则return
                bool isTrigger = false;
                T newState = null;
                foreach (var mi in miList)
                {
                    if (mi.GetParameters()[1].ParameterType == @params.GetType())
                    {
                        mi.Invoke(this, new object[] {getStateFunc, @params, callback});
                        BDebug.Log($"触发回调Reducer:{this.GetType().Name}.{mi.Name}");
                        isTrigger = true;
                    }
                }
                
#if UNITY_EDITOR
                if(!isTrigger)
                foreach (var mi in miList)
                {
                    var methodParams = mi.GetParameters();
                    BDebug.LogError($"触发失败，参数不匹配!同步Reducer:{this.GetType().Name}.{mi.Name} , 形参:{methodParams[1].ParameterType.Name} 传入:{@params.GetType().Name}");
                }
#endif
            }
        }
    }
}
