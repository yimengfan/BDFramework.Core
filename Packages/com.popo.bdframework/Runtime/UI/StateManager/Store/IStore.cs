using System;
using BDFramework.UFlux.Reducer;

namespace BDFramework.UFlux.Contains
{
    public interface IStore
    {
        /// <summary>
        /// 派发消息
        /// </summary>
        /// <param name="actionEnum"></param>
        /// <param name="params"></param>
        bool Dispatch(Enum actionEnum, object @params = null);

        /// <summary>
        /// 订阅接口包装
        /// </summary>
        /// <param name="callback"></param>
        void SubscribeWrapper(Action<object> callback);
        /// <summary>
        /// 订阅接口包装
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="callback"></param>
        //void Subscribe(Enum tag, Action<object> callback);
    }
}
