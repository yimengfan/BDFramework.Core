using System;

namespace BDFramework.DataListener
{
    /// <summary>
    /// 状态事件监听扩展版本
    /// </summary>
    static public class StatusListenerExtensionT
    {
        /// <summary>
        /// T版本监听一次
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <param name="isTriggerCacheData"></param>
        static public void AddListenerOnce<T>(this AStatusListener dl, Action<T> callback = null, int order = -1, bool isTriggerCacheData = false) where T : class, new()
        {
            dl.AddListener<T>(typeof(T).FullName, callback, order, 1, isTriggerCacheData);
        }

        /// <summary>
        /// T版本添加监听
        /// </summary>
        /// <param name="dl"></param>
        /// <param name="name">监听数据名</param>
        /// <param name="action">回调</param>
        /// <param name="order">顺序</param>
        /// <param name="triggerNum">触发次数，-1代表一直触发</param>
        /// <param name="isTriggerCacheData">是否触发回调</param>
        /// <typeparam name="T"></typeparam>
        static public void AddListener<T>(this AStatusListener dl, Action<T> action = null, int order = -1, int triggerNum = -1, bool isTriggerCacheData = false) where T : class, new()
        {
            dl.AddListener<T>(typeof(T).FullName, action, order, triggerNum, isTriggerCacheData);
        }


        /// <summary>
        /// 移除所有的监听
        /// </summary>
        static public void ClearAllListener<T>(this AStatusListener dl) where T : class, new()
        {
            dl.RemoveListener(typeof(T).FullName);
        }



        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        static public void RemoveListener<T>(this AStatusListener dl, Action<T> callback) where T : class, new()
        {
            dl.RemoveListener(typeof(T).FullName, callback);
        }

        /// <summary>
        /// T版本触发监听
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        static public void TriggerEvent<T>(this AStatusListener dl, T value = null) where T : class
        {
            dl.TriggerEvent(typeof(T).FullName, value, true);
        }
    }
}