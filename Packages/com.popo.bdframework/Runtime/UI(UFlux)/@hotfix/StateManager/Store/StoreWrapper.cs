using System;
using System.Collections.Generic;
using System.Reflection;

namespace BDFramework.UFlux.Contains
{
    /// <summary>
    /// 多个Store的包装类
    /// </summary>
    public class StoreWrapper
    {
        /// <summary>
        /// sotre列表
        /// </summary>
        /// <returns></returns>
        private List<IStore> storeList = new List<IStore>();

        public StoreWrapper(params IStore[] stores)
        {
            this.storeList = new List<IStore>(stores);
        }

        /// <summary>
        /// 订阅具体类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Subscribe<T>(Action<T> action) where T : AStateBase, new()
        {
            foreach (var store in storeList)
            {
                if (store is Store<T> storeT)
                {
                    storeT.Subscribe(action);
                    break;
                }
            }
        }

        /// <summary>
        /// 发布Action
        /// </summary>
        public void Dispatch(Enum actionEnum, object @params = null)
        {
            foreach (var store in storeList)
            {
                var ret =store.Dispatch(actionEnum, @params);
                if (ret)
                {
                    break;
                }
            }
        }
    }
}
