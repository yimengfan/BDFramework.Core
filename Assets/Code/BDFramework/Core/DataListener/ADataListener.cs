using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        protected Dictionary<string, List<ListenerData>> callbackMap;


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
            dataMap = new Dictionary<string, object>();
            callbackMap = new Dictionary<string, List<ListenerData>>();
            valueCacheMap = new Dictionary<string, List<object>>();
        }


        /// <summary>
        /// 注册数据
        /// </summary>
        /// <param name="name"></param>
        virtual public void AddData(string name)
        {
            if (!dataMap.ContainsKey(name))
            {
                dataMap[name] = null;
            }
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        virtual public void SetData(string name, object value, bool isTriggerCallback = true)
        {
            //移除任务 执行
            while (removeTaskQueue.Count > 0)
            {
                var removeTask = removeTaskQueue.Dequeue();
                List<ListenerData> callbackWarppers = null;
                if (callbackMap.TryGetValue(removeTask.Name, out callbackWarppers))
                {
                    foreach (var callbackWarpper in callbackWarppers)
                    {
                        if (callbackWarpper.Callback == removeTask.Action)
                        {
                            callbackWarppers.Remove(callbackWarpper);
                            break;
                        }
                    }
                }
            }

            //数据验证
            if (dataMap.ContainsKey(name))
            {
#if UNITY_EDITOR
                var lastV = dataMap[name];
                if (lastV != null)
                {
                    var lastT = lastV.GetType();
                    var nowT = value.GetType();
                    if (lastT != nowT)
                    {
                        Debug.LogErrorFormat("设置失败,类型不匹配:{0}  curType:{1}  setType:{2}", name, lastT.Name, nowT.Name);
                        return;
                    }
                }
#endif
                dataMap[name] = value;
            }
            else
            {
                BDebug.LogError("设置无效,无该数据:" + name);
                return;
            }

            //调用数据改变
            if (isTriggerCallback)
            {
                //all
                List<ListenerData> listenerDatas = null;
                //
                if (callbackMap.TryGetValue(name, out listenerDatas))
                {
                    //触发回调
                    for (int i = 0; i < listenerDatas.Count; i++)
                    {
                        var listenerData = listenerDatas[i];
                        listenerData.Trigger(value);
                    }

                    //清理列表
                    for (int i = listenerDatas.Count - 1; i >= 0; i--)
                    {
                        if (listenerDatas[i].TriggerNum == 0)
                        {
                            listenerDatas.RemoveAt(i);
                        }
                    }
                }
                else //cache list
                {
                    List<object> list = null;
                    valueCacheMap.TryGetValue(name, out list);
                    if (list == null)
                    {
                        list = new List<object>();
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
        /// <param name="isUseCallback"></param>
        virtual public void TriggerEvent(string name, object value = null, bool isUseCallback = true)
        {
            SetData(name, value, isUseCallback);
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
                    t = default(T);
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


        public class ListenerData
        {
            public int Order { get; private set; } = 1;
            public int TriggerNum { get; private set; } = 1;
            public Action<object> Callback { get; private set; }

            public ListenerData(int order, int triggerNum, Action<object> callback)
            {
                this.Order = order;
                this.TriggerNum = triggerNum;
                this.Callback = callback;
            }

            /// <summary>
            /// 触发
            /// </summary>
            /// <param name="value"></param>
            public void Trigger(object value)
            {
                 if (TriggerNum > 0)
                {
                    TriggerNum--;
                    Callback.Invoke(value);
                }

              
            }
        }

        /// <summary>
        /// 属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        virtual public void AddListener(string name,
            Action<object> callback,
            int order = -1,
            int triggerNum = -1,
            bool isTriggerCacheData = false)
        {
            if (!dataMap.ContainsKey(name))
            {
                BDebug.LogError("暂时无数据,提前监听:" + name);
            }

            //创建监听数据
            var listenerData = new ListenerData(1, triggerNum, callback);
            //
            List<ListenerData> callbackList = null;

            if (!callbackMap.TryGetValue(name, out callbackList))
            {
                callbackList = new List<ListenerData>();
                callbackMap[name] = callbackList;
            }

            if (callbackList.Count == 0)
            {
                callbackList.Add(listenerData);
            }
            else
            {
                //触发排序插入
                for (int i = callbackList.Count - 1; i >= 0; i--)
                {
                    var cw = callbackList[i];
                    if (listenerData.Order >= cw.Order)
                    {
                        callbackList.Insert(i + 1, cw);
                        break;
                    }
                }
            }


            if (isTriggerCacheData)
            {
                List<object> list = null;
                this.valueCacheMap.TryGetValue(name, out list);
                if (list != null)
                {
                    foreach (var value in list)
                    {
                        callback(value);
                    }

                    //置空
                    this.valueCacheMap[name].Clear();
                }
            }
        }


        /// <summary>
        /// 属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        virtual public void AddListenerOnce(string name,
            Action<object> callback = null,
            int order = -1,
            bool isTriggerCacheData = false)
        {
            this.AddListener(name, callback, order, 1, isTriggerCacheData);
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
        /// 移除任务
        /// </summary>
        public class RemoveTask
        {
            public string Name;
            public Action<object> Action;
        }

        Queue<RemoveTask> removeTaskQueue = new Queue<RemoveTask>();

        /// <summary>
        /// 移除属性变动事件注册
        /// </summary>
        /// <param name="name"></param>
        virtual public void RemoveListener(string name, Action<object> callback)
        {
            removeTaskQueue.Enqueue(new RemoveTask() {Name = name, Action = callback});
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