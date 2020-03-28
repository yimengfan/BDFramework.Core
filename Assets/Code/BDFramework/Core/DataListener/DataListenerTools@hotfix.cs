using System;

namespace BDFramework.DataListener
{
    static public class DataListenerTools
    {
        #region 枚举版本

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        static public void AddData(this ADataListener dl, Enum name)
        {
            dl.AddData(name.ToString());
        }

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
        /// <param name="isUseCallback"></param>
        static public void TriggerEvent(this ADataListener dl,
            Enum name,
            object value = null,
            bool isUseCallback = true)
        {
            dl.TriggerEvent(name.ToString(), value, isUseCallback);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <param name="isTriggerCacheData"></param>
        static public void AddListener(this ADataListener dl,
            Enum name,
            Action<object> callback = null,
            int order = -1,
            int triggernum = -1,
            bool isTriggerCacheData = false)
        {
            dl.AddListener(name.ToString(), callback, order, triggernum, isTriggerCacheData);
        }

        /// <summary>
        /// 枚举版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <param name="isTriggerCacheData"></param>
        static public void AddListenerOnce(this ADataListener dl,
            Enum name,
            Action<object> callback = null,
            int triggernum = -1,
            bool isTriggerCacheData = false)
        {
            AddListener(dl, name, callback, 1, triggernum, isTriggerCacheData);
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