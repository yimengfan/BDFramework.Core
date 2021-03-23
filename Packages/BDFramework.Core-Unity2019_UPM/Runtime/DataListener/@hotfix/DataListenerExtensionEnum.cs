using System;

namespace BDFramework.DataListener
{
    static public class DataListenerExtensionEnum
    {
        #region 枚举版本
        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        static public void SetData(this ADataListener dl, Enum @enum, object value, bool isTriggerCallback = true)
        {
            dl.SetData(@enum.ToString(), value, isTriggerCallback);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public T GetData<T>(this ADataListener dl, Enum name)
        {
            return dl.GetData<T>(name.ToString());
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        static public void TriggerEvent(this ADataListener dl, Enum name, object value = null,
                                        bool               isTriggerCallback = true)
        {
            dl.TriggerEvent(name.ToString(), value, isTriggerCallback);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <param name="isTriggerCacheData"></param>
        static public void AddListener(this ADataListener dl, Enum name, Action<object> action = null,
                                       int                order      = -1,
                                       int                triggerNum = -1, bool isTriggerCacheData = false)
        {
            dl.AddListener<object>(name.ToString(), action, order, triggerNum, isTriggerCacheData);
        }

        
        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="dl"></param>
        /// <param name="name">监听数据名</param>
        /// <param name="action">回调</param>
        /// <param name="order">顺序</param>
        /// <param name="triggerNum">触发次数，-1代表一直触发</param>
        /// <param name="isTriggerCacheData">是否触发回调</param>
        /// <typeparam name="T"></typeparam>
        static public void AddListener<T>(this ADataListener dl, Enum name, Action<T> action = null,
                                          int                order      = -1,
                                          int                triggerNum = -1, bool isTriggerCacheData = false)  where T:class
        {
            dl.AddListener<T>(name.ToString(), action, order, triggerNum, isTriggerCacheData);
        }
        
        
        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <param name="isTriggerCacheData"></param>
        static public void AddListenerOnce(this ADataListener dl, Enum name,
                                           Action<object>     callback   = null,
                                           int                oder = -1, bool isTriggerCacheData = false)
        {
            AddListener(dl, name, callback, oder, 1, isTriggerCacheData);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        static public void ClearListener(this ADataListener dl, Enum name)
        {
            dl.ClearListener(name.ToString());
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        static public void RemoveListener<T>(this ADataListener dl, Enum name, Action<T> callback)
        {
            dl.RemoveListener(name.ToString(), callback);
        }
        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        static public void RemoveListener(this ADataListener dl, Enum name, Action<object> callback)
        {
            dl.RemoveListener(name.ToString(), callback);
        }
        /// <summary>
        /// 枚举版本 
        /// </summary>
        /// <param name="name"></param>
        static public void RemoveListener(this ADataListener dl, Enum name)
        {
            dl.RemoveListener(name.ToString());
        }

        #endregion
    }
}