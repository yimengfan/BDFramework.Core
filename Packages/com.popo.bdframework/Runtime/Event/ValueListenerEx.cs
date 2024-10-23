using System;

namespace BDFramework.DataListener
{
    /// <summary>
    /// 值监听扩展版本
    /// </summary>
    static public class ValueListenerEx
    {
        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        static public void SetData(this AStatusListener dl, Enum @enum, object value, bool isTriggerCallback = true)
        {
            dl.SetData(@enum.ToString(), value, isTriggerCallback);
        }

        static public void SetData(this AStatusListener dl, string @enum, object value, bool isTriggerCallback = true)
        {
            dl.SetData(@enum, value, isTriggerCallback);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public T GetData<T>(this AStatusListener dl, Enum name)
        {
            return dl.GetData<T>(name.ToString());
        }

        static public T GetData<T>(this AStatusListener dl, string name)
        {
            return dl.GetData<T>(name);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        static public void TriggerEvent(this AStatusListener dl, Enum name, object value = null,
            bool isTriggerCallback = true)
        {
            dl.TriggerEvent(name.ToString(), value, isTriggerCallback);
        }

        static public void TriggerEvent(this AStatusListener dl, string name, object value = null,
            bool isTriggerCallback = true)
        {
            dl.TriggerEvent(name, value, isTriggerCallback);
        }


        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="dl"></param>
        /// <param name="name">监听名</param>
        /// <param name="action">回调</param>
        /// <param name="order">触发顺序</param>
        /// <param name="triggerNum">触发次数</param>
        /// <param name="isTriggerCacheData">是否触发回调</param>
        static public void AddListener(this AStatusListener dl, Enum name, Action<object> action = null,
            int order = -1,
            int triggerNum = -1, bool isTriggerCacheData = false)
        {
            dl.AddListener(name.ToString(), action, order, triggerNum, isTriggerCacheData);
        }

        static public void AddListener(this AStatusListener dl, string name, Action<object> action = null,
            int order = -1,
            int triggerNum = -1, bool isTriggerCacheData = false)
        {
            dl.AddListener(name, action, order, triggerNum, isTriggerCacheData);
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
        static public void AddListener<T>(this AStatusListener dl, Enum name, Action<T> action = null,
            int order = -1,
            int triggerNum = -1, bool isTriggerCacheData = false) where T : class
        {
            dl.AddListener<T>(name.ToString(), action, order, triggerNum, isTriggerCacheData);
        }

        static public void AddListener<T>(this AStatusListener dl, string name, Action<T> action = null,
            int order = -1,
            int triggerNum = -1, bool isTriggerCacheData = false) where T : class
        {
            dl.AddListener<T>(name, action, order, triggerNum, isTriggerCacheData);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <param name="isTriggerCacheData"></param>
        static public void AddListenerOnce(this AStatusListener dl, Enum name,
            Action<object> callback = null,
            int oder = -1, bool isTriggerCacheData = false)
        {
            AddListener(dl, name, callback, oder, 1, isTriggerCacheData);
        }

        static public void AddListenerOnce(this AStatusListener dl, string name,
            Action<object> callback = null,
            int oder = -1, bool isTriggerCacheData = false)
        {
            AddListener(dl, name, callback, oder, 1, isTriggerCacheData);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        static public void ClearListener(this AStatusListener dl, Enum name)
        {
            dl.ClearListener(name.ToString());
        }

        static public void ClearListener(this AStatusListener dl, string name)
        {
            dl.ClearListener(name.ToString());
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        static public void RemoveListener<T>(this AStatusListener dl, Enum name, Action<T> callback)
        {
            dl.RemoveListener(name.ToString(), callback);
        }

        static public void RemoveListener<T>(this AStatusListener dl, string name, Action<T> callback)
        {
            dl.RemoveListener(name.ToString(), callback);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        static public void RemoveListener(this AStatusListener dl, Enum name, Action<object> callback)
        {
            dl.RemoveListener(name.ToString(), callback);
        }

        static public void RemoveListener(this AStatusListener dl, string name, Action<object> callback)
        {
            dl.RemoveListener(name, callback);
        }

        /// <summary>
        /// 枚举版本 
        /// </summary>
        /// <param name="name"></param>
        static public void RemoveListener(this AStatusListener dl, Enum name)
        {
            dl.RemoveListener(name.ToString());
        }

        static public void RemoveListener(this AStatusListener dl, string name)
        {
            dl.RemoveListener(name);
        }
    }
}
