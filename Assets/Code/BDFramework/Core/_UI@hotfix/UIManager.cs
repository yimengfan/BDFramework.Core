using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BDFramework.Mgr;
using BDFramework;
using BDFramework.UI;

namespace BDFramework.UI
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
    [Obsolete("please use new uiframe: uflux.")]
    public class UIManager : ManagerBase<UIManager, UIAttribute>
    {
        /// <summary>
        /// UI窗口字典
        /// </summary>
        private Dictionary<int, AWindow> windowMap = null;

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
            windowMap = new Dictionary<int, AWindow>();
            Bottom = GameObject.Find("UIRoot/Bottom").transform;
            Center = GameObject.Find("UIRoot/Center").transform;
            Top = GameObject.Find("UIRoot/Top").transform;
        }

        //
        private AWindow CreateWindow(int uiIndex)
        {
            var classData = this.GetClassData(uiIndex);
            if (classData == null)
            {
                Debug.LogError("未注册窗口，无法加载:" + uiIndex);
                return null;
            }

            //
            var attr = classData.Attribute as UIAttribute;
            var window = Activator.CreateInstance(classData.Type, new object[] {attr.ResourcePath}) as AWindow;
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
                    var uvalue = windowMap[index];
                    if (uvalue.IsLoad)
                    {
                        BDebug.Log("已经加载过并未卸载" + index, "red");
                    }
                }
                else
                {
                    //创建ui
                    var window = CreateWindow(index);
                    if (window == null)
                    {
                        BDebug.Log("不存在UI:" + index, "red");
                    }
                    else
                    {
                        windowMap[index] = window;
#if UNITY_EDITOR
                        try
                        {
#endif
                            window.Load();
#if UNITY_EDITOR
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Init窗口失败:" + window.Transform.name);
                        }
#endif
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
                    var uvalue = windowMap[index];
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
                        //开始窗口加载
                        win.AsyncLoad(() =>
                        {
                            curTaskCount++;
                            if (loadProcessAction != null)
                            {
                                loadProcessAction(allCount, curTaskCount);
                            }

                            if (win.Transform)
                            {
                                win.Transform.SetParent(this.Bottom, false);
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
            List<WindowData> cacheList = null;
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
                    var uvalue = windowMap[index];
                    uvalue.Close();
                    uvalue.Destroy();
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
            var keys = new List<int>(this.windowMap.Keys);
            foreach (var v in this.windowMap.Values)
            {
                v.Close();
                v.Destroy();
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
                var v = windowMap[uiIndex];
                if (v.IsClose && v.IsLoad && v.IsLock == false)
                {
                    switch (layer)
                    {
                        case UILayer.Bottom:
                            // UIWidgetMgr.Inst.Widget_Mask.Transform.SetParent(this.Bottom, false);
                            v.Transform.SetParent(this.Bottom, false);
                            break;
                        case UILayer.Center:
                            // UIWidgetMgr.Inst.Widget_Mask.Transform.SetParent(this.Center, false);
                            v.Transform.SetParent(this.Center, false);
                            break;
                        case UILayer.Top:
                            // UIWidgetMgr.Inst.Widget_Mask.Transform.SetParent(this.Top, false);
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


        public AWindow GetWindow(int uiIndex)
        {
            AWindow win = null;

            this.windowMap.TryGetValue(uiIndex, out win);

            return win;
        }

        public void CloseWindow(Enum index)
        {
            CloseWindow(index.GetHashCode());
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="uiIndex">窗口枚举</param>
        public void CloseWindow(int index)
        {
            var uiIndex = index.GetHashCode();
            if (windowMap.ContainsKey(uiIndex))
            {
                var v = windowMap[uiIndex];
                if (!v.IsClose && v.IsLoad)
                {
                    v.Close();
                }
            }
            else
            {
                Debug.LogErrorFormat("不存在UI：{0}", uiIndex);
            }
        }

        private Dictionary<int, List<WindowData>> uiDataCacheMap = new Dictionary<int, List<WindowData>>();

        /// <summary>
        /// 外部推送ui数据
        /// </summary>
        /// <param name="uiIndex"></param>
        /// <param name="data"></param>
        public void SendMessage(int Index, WindowData data)
        {
            var uiIndex = Index.GetHashCode();
            if (windowMap.ContainsKey(uiIndex))
            {
                var ui = windowMap[uiIndex];

                if (ui.IsLoad)
                {
                    ui.SendMessage(data);
                    return;
                }
            }

            //存入缓存
            List<WindowData> list = null;
            uiDataCacheMap.TryGetValue(uiIndex, out list);
            //
            if (list == null)
            {
                list = new List<WindowData>();
                uiDataCacheMap[uiIndex] = list;
            }

            list.Add(data);
        }


        /// <summary>
        /// 获取窗口状态
        /// </summary>
        /// <param name="uiIndex"></param>
        /// <returns></returns>
        public bool GetWindowStatus(int index)
        {
            var uiIndex = index.GetHashCode();
            bool isClose = false;

            if (windowMap.ContainsKey(uiIndex))
            {
                isClose = windowMap[uiIndex].IsClose;
            }
            else
            {
                Debug.LogError("不存在ui:" + uiIndex);
            }

            return isClose;
        }


        /// <summary>
        /// 更新
        /// </summary>
        public void Update()
        {
        }
    }
}