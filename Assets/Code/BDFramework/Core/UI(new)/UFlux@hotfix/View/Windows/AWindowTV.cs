using System;
using System.Collections.Generic;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.View.Props;
using ILRuntime.Runtime;

namespace BDFramework.UFlux
{
    /// <summary>
    /// Window基类
    /// 自动注册Reducer，并且有Store
    /// </summary>
    /// <typeparam name="T">Props</typeparam>
    /// <typeparam name="V">State</typeparam>
    public class AWindow<T, V> : AWindow<T> where T : PropsBase, new() where V : StateBase, new()
    {
        /// <summary>
        /// 当前的store
        /// </summary>
        protected Store.Store<V> store;

        /// <summary>
        /// 构造函数
        /// 显式让Component使用带参数构造函数
        /// </summary>
        /// <param name="path"></param>
        public AWindow(string path) : base(path)
        {
            //创建store
            this.store = Store.Store.CreateStore<V>();
            this.store.RegisterReducers(CreateReducers());
            //创建订阅
            this.store.Subscribe((state) =>
            {
                this.StateToProps(state);
                this.SetProps();
            });
        }

        /// <summary>
        /// 获取 Reducer
        /// </summary>
        /// <returns></returns>
        virtual public AReducers<V> CreateReducers()
        {
            return null;
        }

        /// <summary>
        /// 这里是State变动的时候，自动调用该函数
        /// </summary>
        /// <param name="stateBase"></param>
        virtual public void StateToProps(V state)
        {
        }
    }
}