using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using BDFramework.Mgr;


namespace BDFramework.UFlux
{
    public enum UILayer
    {
        Bottom = 0,
        Center,
        Top
    }

    /// <summary>
    /// UI管理类
    /// </summary>
    public class UIManager : ManagerBase<UIManager, UIAttribute>
    {
        /// <summary>
        /// UI窗口字典
        /// </summary>
        private Dictionary<int, IWindow> windowMap = null;

        /// <summary>
        /// ui的三个层级
        /// </summary>
        private Transform Bottom, Center, Top;

        //
        public UIManager()
        {
        }

        override public void Init()
        {
            //初始化
            windowMap = new Dictionary<int, IWindow>();
            Bottom = GameObject.Find("UIRoot/Bottom").transform;
            Center = GameObject.Find("UIRoot/Center").transform;
            Top = GameObject.Find("UIRoot/Top").transform;
        }

        //
        private IWindow CreateWindow(int uiIndex)
        {
            var classData = this.GetClassData(uiIndex);
            if (classData == null)
            {
                Debug.LogError("未注册窗口，无法加载:" + uiIndex);
                return null;
            }

            //
            var attr = classData.Attribute as UIAttribute;
            var window = Activator.CreateInstance(classData.Type, new object[] {attr.ResourcePath}) as IWindow;
            //
            return window;
        }

        /// <summary>
        /// 加载窗口
        /// </summary>
        /// <param name="uiIndexs">窗口枚举</param>
        public void LoadWindows(params int[] uiIndexs)
        {
            foreach (var i in uiIndexs)
            {
                var index = i.GetHashCode();

                if (windowMap.ContainsKey(index))
                {
                    var uvalue = windowMap[index] as IUFluxComponent;
                    if (uvalue.IsLoad)
                    {
                        BDebug.Log("已经加载过并未卸载" + index, "red");
                    }
                }
                else
                {
                    //创建ui
                    var window = CreateWindow(index) as IUFluxComponent;
                    if (window == null)
                    {
                        BDebug.Log("不存在UI:" + index, "red");
                    }
                    else
                    {
                        windowMap[index] = window as IWindow;
                        window.Load();
                        window.Transform.SetParent(this.Bottom, false);
                        PushCaheData(index);
                    }
                }
            }
        }

        /// <summary>
        /// 异步加载窗口
        /// </summary>
        /// <param name="indexes"></param>
        /// <param name="loadProcessAction"></param>
        public void AsyncLoadWindows(List<int> indexes, Action<int, int> loadProcessAction)
        {
            //去重操作
            indexes = indexes.Distinct().ToList();
            //
            int allCount = indexes.Count;
            int curTaskCount = 0;
            foreach (var i in indexes)
            {
                var index = i.GetHashCode();
                if (windowMap.ContainsKey(index))
                {
                    var uvalue = windowMap[index] as IUFluxComponent;
                    if (uvalue.IsLoad)
                    {
                        Debug.LogError("已经加载过并未卸载" + index);
                        //任务直接完成
                        curTaskCount++;
                        loadProcessAction(allCount, curTaskCount);
                    }
                }
                else
                {
                    //创建窗口
                    var win = CreateWindow(index);
                    if (win == null)
                    {
                        Debug.LogErrorFormat("不存在UI:{0}", index);
                        curTaskCount++;
                        loadProcessAction(allCount, curTaskCount);
                    }
                    else
                    {
                        windowMap[index] = win;
                        var com = win as IUFluxComponent;
                        //开始窗口加载
                        com.AsyncLoad(() =>
                        {
                            curTaskCount++;
                            if (loadProcessAction != null)
                            {
                                loadProcessAction(allCount, curTaskCount);
                            }

                            if (com.Transform)
                            {
                                com.Transform.SetParent(this.Bottom, false);
                            }

                            //推送缓存的数据
                            PushCaheData(index);
                        });
                    }
                }
            }
        }

        private void PushCaheData(int uiIndex)
        {
            // return;
            //检查ui数据缓存
            List<UIMessage> cacheList = null;
            uiDataCacheMap.TryGetValue(uiIndex, out cacheList);
            if (cacheList != null)
            {
                for (int i = 0; i < cacheList.Count; i++)
                {
                    var data = cacheList[i];

                    windowMap[uiIndex].SendMessage(data);
                    BDebug.Log("push cache data " + uiIndex);
                }

                cacheList.Clear();
                BDebug.LogFormat("推送数据：{0} ,{1}条", uiIndex, cacheList.Count);
            }
        }

        /// <summary>
        /// 卸载窗口
        /// </summary>
        /// <param name="indexs">窗口枚举</param>
        public void UnLoadWindows(params int[] indexs)
        {
            foreach (var i in indexs)
            {
                var index = i.GetHashCode();
                if (windowMap.ContainsKey(index))
                {
                    var winCom = windowMap[index] as IUFluxComponent;
                    winCom.Close();
                    winCom.Destroy();
                    windowMap.Remove(index);
                }
                else
                {
                    Debug.LogErrorFormat("不存在UI：{0}", indexs);
                }
            }
        }


        /// <summary>
        /// 卸载窗口
        /// </summary>
        public void UnLoadALLWindows()
        {
            foreach (var v in this.windowMap.Values)
            {
                var vcom = v as IUFluxComponent;
                vcom.Close();
                vcom.Destroy();
            }

            this.windowMap.Clear();
            this.uiDataCacheMap.Clear();
        }


        /// <summary>
        /// 显示窗口
        /// </summary>
        /// <param name="uiIndex">窗口枚举</param>
        public void ShowWindow(int index, bool resetMask = true, UILayer layer = UILayer.Bottom)
        {
            int uiIndex = index.GetHashCode();
            if (windowMap.ContainsKey(uiIndex))
            {
                var v = windowMap[uiIndex] as IUFluxComponent;
                if (!v.IsOpen && v.IsLoad)
                {
                    switch (layer)
                    {
                        case UILayer.Bottom:
                            v.Transform.SetParent(this.Bottom, false);
                            break;
                        case UILayer.Center:
                            v.Transform.SetParent(this.Center, false);
                            break;
                        case UILayer.Top:
                            v.Transform.SetParent(this.Top, false);
                            break;
                        default:
                            break;
                    }

                    v.Transform.SetAsLastSibling();
                    v.Open();
                    //effect
                }
                else
                {
                    Debug.LogErrorFormat("UI处于[unload,lock,open]状态之一：{0}", uiIndex);
                }
            }
            else
            {
                Debug.LogErrorFormat("未加载UI：{0}", uiIndex);
            }
        }


        public IWindow GetWindow(int index)
        {
            IWindow win = null;
            this.windowMap.TryGetValue(index, out win);

            return win;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="uiIndex">窗口枚举</param>
        public void CloseWindow(int index, bool isMask = true)
        {
            var uiIndex = index.GetHashCode();
            if (windowMap.ContainsKey(uiIndex))
            {
                var v = windowMap[uiIndex] as IUFluxComponent;
                if (v.IsOpen && v.IsLoad)
                {
                    v.Close();
                }
                else
                {
                    Debug.LogErrorFormat("UI未加载或已经处于close状态：{0}", uiIndex);
                }
            }
            else
            {
                Debug.LogErrorFormat("不存在UI：{0}", uiIndex);
            }
        }

        private Dictionary<int, List<UIMessage>> uiDataCacheMap = new Dictionary<int, List<UIMessage>>();

        /// <summary>
        /// 外部推送ui数据
        /// </summary>
        /// <param name="uiIndex"></param>
        /// <param name="message"></param>
        public void SendMessage(int index, UIMessage message)
        {
            var uiIndex = index;
            if (windowMap.ContainsKey(uiIndex))
            {
                var ui = windowMap[uiIndex];
                ui.SendMessage(message);
                return;
            }

            //存入缓存
            List<UIMessage> list = null;
            uiDataCacheMap.TryGetValue(uiIndex, out list);
            //
            if (list == null)
            {
                list = new List<UIMessage>();
                uiDataCacheMap[uiIndex] = list;
            }

            list.Add(message);
        }
    }
}