using System;

namespace BDFramework.DataListener
{
    static public class DataListenerExtensionT
    {
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
        static public void AddListener<T>(this ADataListener dl, Action<T> action = null,
                                          int                order      = -1,
                                          int                triggerNum = -1, bool isTriggerCacheData = false)  where T:class
        {
            dl.AddListener<T>(typeof(T).FullName, action, order, triggerNum, isTriggerCacheData);
        }

        
        /// <summary>
        /// T版本触发监听
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        static public void TriggerEvent(this ADataListener dl, object value = null,
                                        bool               isTriggerCallback = true)
        {
            dl.TriggerEvent(value.GetType().FullName, value, isTriggerCallback);
        }

        
        
        
    }
}