using System;
using System.Collections.Generic;
using BDFramework.ResourceMgr;
using BDFramework.UFlux.Reducer;

namespace BDFramework.UFlux.Store
{
    /// <summary>
    /// 这里集中管理
    /// </summary>
    public class Store<T> where T : AStateBase, new()
    {
        /// <summary>
        /// reducer的delegate，同步
        /// </summary>
        public delegate T Reducer(T oldState, object @params = null);

        //异步
        public delegate void AsyncReducer(GetState getStateFunc, object @params = null, Action<T> callback = null);

        /// <summary>
        /// 获取当前State的Delegate,异步接口用来实时获取 
        /// </summary>
        public delegate T GetState();

        //当前State
        private T state;

        //最大的数据缓存数量
        private int maxStateCacheNum = 20;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="state"></param>
        private Store()
        {
            this.state = new T();
        }

        #region Reducers

        private AReducers<T> reducers = null;

        /// <summary>
        /// 注册Reducers
        /// </summary>
        /// <param name="reducers"></param>
        public void RegisterReducers(AReducers<T> reducers)
        {
            this.reducers = reducers;
        }

        #endregion

        #region 事件分发

        List<Action<T>> callbackList = new List<Action<T>>();

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(Action<T> callback)
        {
            callbackList.Add(callback);
        }

        private bool lockDispatch = false;
        private List<UFluxAction> taskList = new List<UFluxAction>();

        /// <summary>
        /// 简化版接口
        /// </summary>
        /// <param name="actionEnum"></param>
        public void Dispatch(Enum actionEnum, object @params = null)
        {
            Dispatch(new UFluxAction() {ActionEnum = actionEnum, Params = @params});
        }

        /// <summary>
        /// 调度
        /// 1.这里要处理好异步任务，并且组装好事件队列
        /// 2.异步会堵塞同步方法，同一时间只有1个dispatch能执行
        /// </summary>
        public void Dispatch(UFluxAction action)
        {
            //新对象
            var newState = GetCurrentState();
            var ret = this.reducers.IsAsyncLoad(action.ActionEnum);
            if (!ret) //同步模式
            {
                this.reducers.Excute(action.ActionEnum, action.Params, newState);
                //素质三连
                CacheCurrentState();
                this.state = newState;
                TriggerAllCallback();
            }
            else //异步模式
            {
                this.reducers.AsyncExcute(action.ActionEnum, action.Params, GetCurrentState, (_newState) =>
                {
                    //素质三连
                    CacheCurrentState();
                    this.state = _newState;
                    TriggerAllCallback();
                });
            }
        }

        /// <summary>
        /// 获取实时的State;
        /// </summary>
        /// <returns></returns>
        private T GetCurrentState()
        {
            var newState = state.Clone() as T;
            return newState;
        }

        /// <summary>
        /// 触发所有监听
        /// </summary>
        private void TriggerAllCallback()
        {
            foreach (var cb in callbackList)
            {
                cb(this.state);
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
        private List<T> stateList=new List<T>();
        /// <summary>
        /// 缓存当前State
        /// </summary>
        public void CacheCurrentState()
        {
            if (stateList.Count == 20)
            {
                stateList.RemoveAt(0);
            }
            stateList.Add(this.state);
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