using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFramework.Adaptor;
using UnityEngine;

namespace BDFramework.DataListener
{
    abstract public class ADataListener
    {
        /// <summary>
        /// 所有的数据
        /// </summary>
        protected Dictionary<string, object> dataMap;

        //注册数据变动事件刷新
        protected Dictionary<string, List<ListenerCallbackData>> callbackMap;


        /// <summary>
        /// 注册事件缓存
        /// </summary>
        protected Dictionary<string, List<object>> valueCacheMap;

        /// <summary>
        /// 最大缓存条数
        /// </summary>
        private int maxCacheValueCount = 20;

        public ADataListener()
        {
            dataMap       = new Dictionary<string, object>();
            callbackMap   = new Dictionary<string, List<ListenerCallbackData>>();
            valueCacheMap = new Dictionary<string, List<object>>();
        }
        

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="name">数据名</param>
        /// <param name="value">数据值</param>
        /// <param name="isTriggerCallback">是否触发回调</param>
        /// <param name="isOnlyTriggerEvent">是否只响应事件（不返回值）</param>
        virtual public void SetData(string name, object value, bool isTriggerCallback = true)
        {
            //数据验证
            object lastV = null;
            if (dataMap.TryGetValue(name,out lastV))
            {
                if (lastV != null)
                {
                    var lastT    = lastV.GetType();
                    var currentT = value.GetType();
                    if (lastT != currentT)
                    {
                        Debug.LogErrorFormat("设置失败,类型不匹配:{0}  curType:{1}  setType:{2}", name, lastT.Name, currentT.Name);
                        return;
                    }
                }
            }
            
            //设置数据
            dataMap[name] = value;


            //触发回调
            if (isTriggerCallback)
            {
                //all
                List<ListenerCallbackData> callbackList = null;
                //
                if (callbackMap.TryGetValue(name, out callbackList))
                {
                    var _callbackList = new List<ListenerCallbackData>(callbackList); //复制一份，防止callback对业务有侵扰
                    //触发回调
                    for (int i = 0; i < _callbackList.Count; i++)
                    {
                        var cb = _callbackList[i];
                        //执行回调
                        cb.Invoke(value);
                    }

                    
                    //清理列表
                    for (int i = callbackList.Count - 1; i >= 0; i--)
                    {
                        if (callbackList[i].TriggerNum == 0)
                        {
                            callbackList.RemoveAt(i);
                        }
                    }
                }
                else //cache list
                {
                    List<object> list = null;
                    valueCacheMap.TryGetValue(name, out list);
                    if (list == null)
                    {
                        list                = new List<object>();
                        valueCacheMap[name] = list;
                    }

                    if (list.Count >= maxCacheValueCount)
                    {
                        list.RemoveAt(0);
                    }

                    list.Add(value);
                }
            }
        }


        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="isTriggerCallback"></param>
        virtual public void TriggerEvent(string name, object value = null, bool isTriggerCallback = true)
        {
            SetData(name, value, isTriggerCallback);
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        virtual public T GetData<T>(string name)
        {
            T t = default(T);
            if (dataMap.ContainsKey(name))
            {
                var value = dataMap[name];
                if (value == null)
                {
                    t             = default(T);
                    dataMap[name] = t;
                }
                else
                {
                    t = (T) value;
                }
            }
            else
            {
                dataMap[name] = t;
            }

            return t;
        }


        /// <summary>
        /// 监听的回调数据
        /// </summary>
        public class ListenerCallbackData
        {
            public int            Order         { get; private set; } = 1;
            public int            TriggerNum    { get; private set; } = 1;
            public AActionAdaptor ActionAdaptor { get; private set; }

            public ListenerCallbackData(int order, int triggerNum, AActionAdaptor callback)
            {
                this.Order         = order;
                this.TriggerNum    = triggerNum;
                this.ActionAdaptor = callback;
            }

            /// <summary>
            /// 触发
            /// </summary>
            /// <param name="value"></param>
            public void Invoke(object value)
            {
                if (TriggerNum == 0) return;
                if (TriggerNum > 0)
                {
                    TriggerNum--;
                }

                ActionAdaptor.Invoke(value);
            }
        }

        /// <summary>
        /// 属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        virtual public void AddListener<T>(string name, Action<T> callback, int order = -1, int triggerNum = -1, bool isTriggerCacheData = false) where T : class
        {
            var actionAdaptor = new ActionAdaptor<T>(callback);
            //创建监听数据
            var callbackData = new ListenerCallbackData(order, triggerNum, actionAdaptor);
            //
            List<ListenerCallbackData> callbackList = null;
            if (!callbackMap.TryGetValue(name, out callbackList))
            {
                callbackList      = new List<ListenerCallbackData>();
                callbackMap[name] = callbackList;
            }


            //触发排序插入
            bool isadd = false;
            for (int i = 0; i < callbackList.Count; i++)
            {
                var cw = callbackList[i];
                if (callbackData.Order < cw.Order)
                {
                    callbackList.Insert(i, callbackData);
                    isadd = true;
                    break;
                }
            }

            if (!isadd)
            {
                callbackList.Add(callbackData);
            }


            if (isTriggerCacheData)
            {
                List<object> list = null;
                this.valueCacheMap.TryGetValue(name, out list);
                if (list != null)
                {
                    foreach (var value in list)
                    {
                        actionAdaptor.Invoke(value);
                    }

                    //置空
                    this.valueCacheMap[name].Clear();
                }
            }
        }


        /// <summary>
        /// 移除属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        virtual public void ClearListener(string name)
        {
            if (callbackMap.ContainsKey(name))
            {
                callbackMap.Remove(name);
            }
        }

        

        /// <summary>
        /// 移除属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        virtual public void RemoveListener<T>(string name, Action<T> callback)
        {
            List<ListenerCallbackData> callbackList = null;
            if (callbackMap.TryGetValue(name, out callbackList))
            {
               var index =  callbackList.FindIndex((cb)=>cb.ActionAdaptor.Equals(callback));
               if (index >= 0)
               {
                   callbackList.RemoveAt(index);
               }
            }
        }

        /// <summary>
        /// 移除属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        virtual public void RemoveListener(string name)
        {
            callbackMap.Remove(name);
        }


        /// <summary>
        /// 获取所有的name
        /// </summary>
        /// <returns></returns>
        public List<string> GetDataNames()
        {
            return this.dataMap.Keys.ToList();
        }


        /// <summary>
        /// 是否含有某个值
        /// </summary>
        /// <returns></returns>
        public bool ContainsKey(string name)
        {
            return this.dataMap.ContainsKey(name);
        }
    }
}