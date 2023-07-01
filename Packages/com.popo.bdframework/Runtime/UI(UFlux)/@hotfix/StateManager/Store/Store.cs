using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BDFramework.ResourceMgr;
using BDFramework.UFlux.Reducer;
using UnityEngine;

namespace BDFramework.UFlux.Contains
{
    /// <summary>
    /// 订阅消息
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscribeAttribute : Attribute
    {
        /// <summary>
        /// 订阅枚举Tag
        /// </summary>
        public Enum SubscribeTag { get;  set; }
        /// <summary>
        /// 订阅inttag
        /// </summary>
        public int SubscribeIntTag { get; set; } = -1;
        // public SubscribeAttribute(Enum @enum)
        // {
        //     this.SubscribeTag = @enum;
        // }
        public SubscribeAttribute(int @enum)
        {
            this.SubscribeIntTag = @enum;
        }
        
    }
    /// <summary>
    /// 一个Store 对应一个 数据状态
    /// </summary>
    public class Store<S> :IStore  where S : AStateBase, new()
    {
        //最大的数据缓存数量
        static int MAX_STATE_NUM = 20;

        /// <summary>
        /// 异步堵塞
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="params"></param>
        public delegate Task<S> ReducerAsync(S oldState, object @params = null);

        /// <summary>
        /// 同步
        /// </summary>
        public delegate S Reducer(S oldState, object @params = null);

        /// <summary>
        /// 异步回调方式
        /// </summary>
        /// <param name="getStateFunc"></param>
        /// <param name="params"></param>
        /// <param name="callback"></param>
        public delegate void ReducerCallback(GetState getStateFunc, object @params = null, Action<S> callback = null);

        /// <summary>
        /// 获取当前State的Delegate,异步接口用来实时获取 
        /// </summary>
        public delegate S GetState();

        /// <summary>
        ///当前State
        /// </summary>
        public S State { get; private set; }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="state"></param>
        public Store()
        {
            this.State = new S();
        }

        #region Reducers

        private AReducers<S> reducer;

        /// <summary>
        /// 注册Reducers
        /// </summary>
        /// <param name="reducer"></param>
        public void AddReducer(AReducers<S> reducer)
        {
            if (this.reducer == null)
            {
                this.reducer = reducer;
                reducer.Dispatch += this.Dispatch;
            }
        }

        #endregion

        #region 事件分发

        /// <summary>
        /// callback 包装
        /// </summary>
        public class SubscribeCallback
        {
            /// <summary>
            /// 订阅的tag,为null则订阅所有
            /// </summary>
            public Enum Tag { get; set; } = null;

            /// <summary>
            /// IntTag
            /// </summary>
            public int IntTag { get; set; } = -9999;

            /// <summary>
            /// 是否订阅所有
            /// </summary>
            /// <returns></returns>
            public bool IsSubscribeAll()
            {
                return (Tag == null && IntTag == -9999);
            }
            
            /// <summary>
            /// 回调
            /// </summary>
            public Action<S> Callback { get; set; } = null;
        }

        /// <summary>
        /// 回调包装
        /// </summary>
        private List<SubscribeCallback> SubscribeCallbackList { get; set; } = new List<SubscribeCallback>();

        /// <summary>
        /// 订阅所有State修改
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(Action<S> callback)
        {
            var callbackWarpper = new SubscribeCallback();
            callbackWarpper.Callback = callback;
            SubscribeCallbackList.Add(callbackWarpper);
        }


        /// <summary>
        /// 订阅
        /// 指定事件触发后才会修改
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(Enum tag, Action<S> callback)
        {
            var sc = new SubscribeCallback();
            sc.Tag = tag;
            sc.IntTag = tag.GetHashCode();
            sc.Callback = callback;
            SubscribeCallbackList.Add(sc);
        }
        /// <summary>
        /// 订阅
        /// 指定事件触发后才会修改
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(int tag, Action<S> callback)
        {
            var sc = new SubscribeCallback();
            sc.IntTag = tag;
            sc.Callback = callback;
            SubscribeCallbackList.Add(sc);
        }
        
        /// <summary>
        /// 订阅包装，不建议业务层直接调用
        /// </summary>
        /// <param name="callback"></param>
        public void SubscribeWrapper(Action<object> callback)
        {
            this.Subscribe((s) =>
            {
                //包装用以外部方便做类型转换
                callback?.Invoke(s);
            });
        }
        /// <summary>
        /// 订阅包装，不建议业务层直接调用
        /// </summary>
        /// <param name="callback"></param>
        // public void Subscribe(Enum tag, Action<object> callback)
        // {
        //     this.Subscribe(tag,(s) =>
        //     {
        //         //包装用以外部方便做类型转换
        //         callback?.Invoke(s);
        //     });
        // }

        /// <summary>
        /// 扫描标签订阅对应的事件
        /// </summary>
        /// <param name="obj"></param>
        public void ScanThisSubscribe(object obj)
        {
            var ms =obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
            foreach (var m in ms)
            {
                var attrList = m.GetCustomAttributes<SubscribeAttribute>();
               
                //注册
                if (attrList != null && attrList.Count()>0)
                {
                    var @params = m.GetParameters();
                    if (@params.Length == 1 && @params[0].ParameterType == typeof(S))
                    {
                        //批量注册订阅
                        foreach (var attr in attrList)
                        {
                            this.Subscribe(attr.SubscribeIntTag, (s) =>
                            {
                              
                                //有参数
                                m.Invoke(obj, new object[] {s});
                            
                             
                            });
                        
                            BDebug.Log($"[Reducer]自动subscribe:{this.GetType().Namespace}.{m.Name}",Color.yellow);
                        }
                    }
                    else
                    {
                        BDebug.LogError("注册函数参数不对:" + m.Name + " 请检查");
                    }
                }
                 
            }
             
        }
        
        /// <summary>
        /// 锁住发布
        /// </summary>
        private bool lockDispatch = false;

        /// <summary>
        /// 分发
        /// </summary>
        /// <param name="actionEnum"></param>
        /// <param name="params"></param>
        public bool Dispatch(Enum actionEnum, object @params = null)
        {
            var type = this.reducer.GetExecuteType(@actionEnum);
            if (type != AReducers<S>.ExecuteTypeEnum.None)
            {
                var action = new UFluxAction() {ActionTag = actionEnum};
                action.SetParams(@params);
                //分发
                Dispatch(action);

                return true;
            }
            else
            {
               // BDebug.LogError($"不存在reducer:{actionEnum.ToString()}");
                SetNewState(actionEnum, this.State);
            }

            return false;

        }


        /// <summary>
        /// 调度
        /// 1.这里要处理好异步任务，并且组装好事件队列
        /// 2.异步会堵塞同步方法，同一时间只有1个dispatch能执行
        /// </summary>
        async private void Dispatch(UFluxAction action)
        {
          
            var executeType = reducer.GetExecuteType(action.ActionTag);
            switch (executeType)
            {
                case AReducers<S>.ExecuteTypeEnum.Sync:
                { 
                    var oldState = CopyCurrentState();
                    var newstate = reducer.Excute(action.ActionTag, action.Params, oldState);
                    if (newstate != null)
                    {
                        //设置new state
                        SetNewState(action.ActionTag, newstate);
                    }
                    else
                    {
                        SetNewState(action.ActionTag, oldState);
                    }

                   
                }
                    break;
                case AReducers<S>.ExecuteTypeEnum.Async:
                {
                    var oldState = CopyCurrentState();
                    var newstate = await reducer.ExcuteAsync(action.ActionTag, action.Params, oldState);
                    if (newstate != null)
                    {
                        //设置new state
                        SetNewState(action.ActionTag, newstate);
                    }
                    else
                    {
                        SetNewState(action.ActionTag, oldState);
                    }
                }
                    break;
                case AReducers<S>.ExecuteTypeEnum.Callback:
                {
                    reducer.ExcuteByCallback(action.ActionTag, action.Params, CopyCurrentState, (newstate) =>
                    {
                       
                        if (newstate != null)
                        {
                            //设置new state
                            SetNewState(action.ActionTag, newstate);
                        }
                        else
                        {
                            SetNewState(action.ActionTag, this.State);
                        }
                    });
                }
                    break;
            }

        }

        /// <summary>
        /// 获取实时的State;
        /// </summary>
        /// <returns></returns>
        private S CopyCurrentState()
        {
            if (State != null)
            {
                var newState = State.Clone() as S;
                return newState;
            }
            else
            {
                return default(S);
            }
        }

        /// <summary>
        /// 触发监听
        /// </summary>
        private void DispachCallback(Enum tag = null)
        {
            //触发
            for (int i = 0; i < SubscribeCallbackList.Count; i++)
            {
                var cbw = SubscribeCallbackList[i];
                if (cbw.IsSubscribeAll())
                {
                    cbw.Callback.Invoke(this.State);
                }
                else if (tag.GetHashCode() == cbw.IntTag  || (cbw.Tag!=null && tag.GetHashCode() == cbw.Tag.GetHashCode()))
                {
                    cbw.Callback.Invoke(this.State);
                }
            }
        }

        #endregion

        #region 回溯功能

        /// <summary>
        /// 默认缓存20条
        /// </summary>
        private int MaxCacheNumber = 20;

        /// <summary>
        /// State集合
        /// </summary>
        private Queue<S> stateCacheQueue = new Queue<S>();

        /// <summary>
        /// 缓存当前State
        /// </summary>
        public void SetNewState(Enum reducerEnum, S newState)
        {
            //缓存
            if (stateCacheQueue.Count == MaxCacheNumber)
            {
                stateCacheQueue.Dequeue();
            }

            stateCacheQueue.Enqueue(this.State);
            //设置
            this.State = newState;
            //触发回调
            DispachCallback(reducerEnum);
        }

        /// <summary>
        /// 回溯状态
        /// </summary>
        public void UnDo()
        {
        }

        /// <summary>
        /// 取消undo
        /// </summary>
        public void CancelUnDo()
        {
        }

        #endregion
    }
}
