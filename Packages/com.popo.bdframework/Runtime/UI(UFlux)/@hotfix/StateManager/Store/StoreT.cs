using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BDFramework.ResourceMgr;
using BDFramework.UFlux.Reducer;

namespace BDFramework.UFlux.Contains
{
    /// <summary>
    /// 一个Store 对应一个 数据状态
    /// </summary>
    public class Store<S> where S : AStateBase, new()
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

        //当前State
        private S state;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="state"></param>
        public Store()
        {
            this.state = new S();
        }

        #region Reducers

        /// <summary>
        /// reducer的实例
        /// </summary>
        AReducers<S> reducer = null;

        /// <summary>
        /// 注册Reducers
        /// </summary>
        /// <param name="reducer"></param>
        public void AddReducer(AReducers<S> reducer)
        {
            if (reducer == null)
            {
                this.reducer = reducer;
            }
        }

        #endregion

        #region 事件分发

        /// <summary>
        /// callback 包装
        /// </summary>
        public class CallbackWarpper
        {
            public Enum      Tag      = null;
            public Action<S> Callback = null;
        }

        /// <summary>
        /// 回调包装
        /// </summary>
        List<CallbackWarpper> callbackList = new List<CallbackWarpper>();

        /// <summary>
        /// 订阅
        /// 每次State修改后都会触发
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(Action<S> callback)
        {
            var callbackWarpper = new CallbackWarpper();
            callbackWarpper.Callback = callback;
            callbackList.Add(callbackWarpper);
        }


        /// <summary>
        /// 订阅
        /// 指定事件触发后才会修改
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(Enum tag, Action<S> callback)
        {
            var callbackWarpper = new CallbackWarpper();
            callbackWarpper.Tag      = tag;
            callbackWarpper.Callback = callback;
            callbackList.Add(callbackWarpper);
        }


        /// <summary>
        /// 锁住发布
        /// </summary>
        private bool lockDispatch = false;

        /// <summary>
        /// 分发
        /// </summary>
        /// <param name="actionEnum"></param>
        public void Dispatch(Enum actionEnum, object @params = null)
        {
            var action = new UFluxAction() { ActionTag = actionEnum };
            action.SetParams(@params);
            //分发
            Dispatch(action);
        }

        /// <summary>
        /// 调度
        /// 1.这里要处理好异步任务，并且组装好事件队列
        /// 2.异步会堵塞同步方法，同一时间只有1个dispatch能执行
        /// </summary>
        async public void Dispatch(UFluxAction action)
        {
            var oldState = GetCurrentState();

            // foreach (var reducer in reducer)
            {
                var ret = reducer.IsAsyncLoad(action.ActionTag);
                if (!ret) //同步模式
                {
                    //先执行await
                    var newstate = await reducer.ExcuteAsync(action.ActionTag, action.Params, oldState);
                    //再执行普通
                    if (newstate == null)
                    {
                        newstate = reducer.Excute(action.ActionTag, action.Params, oldState);
                    }

                    //设置new state
                    SetNewState(action.ActionTag, newstate);
                }
                else //回调模式
                {
                    reducer.ExcuteByCallback(action.ActionTag, action.Params, GetCurrentState, (newState) =>
                    {
                        //设置new state
                        SetNewState(action.ActionTag, newState);
                    });
                }
            }
        }

        /// <summary>
        /// 获取实时的State;
        /// </summary>
        /// <returns></returns>
        private S GetCurrentState()
        {
            var newState = state.Clone() as S;
            return newState;
        }

        /// <summary>
        /// 触发监听
        /// </summary>
        private void TriggerCallback(Enum tag = null)
        {
            //触发
            for (int i = 0; i < callbackList.Count; i++)
            {
                var cbw = callbackList[i];
                if (cbw.Tag == null)
                {
                    cbw.Callback.Invoke(this.state);
                }
                else if (cbw.Tag == tag)
                {
                    cbw.Callback.Invoke(this.state);
                }
            }

            //触发具体事件监听
            for (int i = 0; i < callbackList.Count; i++)
            {
                var cbw = callbackList[i];
                if (tag != null && cbw.Tag == tag)
                {
                    cbw.Callback.Invoke(this.state);
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

            stateCacheQueue.Enqueue(this.state);
            //设置
            this.state = newState;
            //触发回调
            TriggerCallback(reducerEnum);
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