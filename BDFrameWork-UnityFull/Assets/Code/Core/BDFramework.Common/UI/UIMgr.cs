using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using  BDFramework.Mgr;
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
    public  class UIMgr : MgrBase<UIMgr>
    {
        /// <summary>
        /// UI窗口字典
        /// </summary>
        private Dictionary<int, AWindow> windowMap = null;
        
        /// <summary>
        /// ui的三个层级
        /// </summary>
        private Transform Bottom, Center, Top;



        public UIMgr()
        {
            
        }

        public override void Awake()
        {
            base.Awake();
            //初始化
            windowMap  = new Dictionary<int, AWindow>();
            Bottom = GameObject.Find("UIRoot/Bottom").transform;
            Center = GameObject.Find("UIRoot/Center").transform;
            Top    = GameObject.Find("UIRoot/Top").transform;
        }

        /// <summary>
        /// 重写mgr checktype
        /// </summary>
        /// <param name="type"></param>
        override public void CheckType(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(UIAttribute), false);
            if (attrs.Length > 0)
            {
                foreach (var attr in attrs)
                {
                    var _attr = (UIAttribute)attr;
                    var name = _attr.Index.ToString();
                    SaveAttribute(name, new ClassData() { Attribute = _attr, Type = type });
                }
            }

        }
        //
        private AWindow CreateWindow(int uiIndex)
        {
            var classData = this.GetCalssData(uiIndex.ToString());
            if (classData == null)
            {
                Debug.LogError("未注册窗口，无法加载:" + uiIndex);
                return null;
            }
            //
            var attr = classData.Attribute as UIAttribute;
            var window = Activator.CreateInstance(classData.Type, new object[] { attr.ResourcePath }) as AWindow;
            //
            return window;
        }

        /// <summary>
        /// 加载窗口
        /// </summary>
        /// <param name="uiIndexs">窗口枚举</param>
        public void LoadWindows(params int[] uiIndexs)
        {
            foreach (var index in uiIndexs)
            {
         
                if (windowMap.ContainsKey(index))
                {
                    var uvalue = windowMap[index];
                    if (uvalue.IsLoad)
                    {
                        BDebug.Log("已经加载过并未卸载" +  index, "red");
                    }
                }
                else
                {
                    //创建ui
                    var window = CreateWindow(index);
                    if (window == null)
                    {
                        BDebug.Log("不存在UI:" + index , "red" );
                    }
                    else
                    {
                        windowMap[index] = window;
                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        window.Load();
                        window.Transform.SetParent(this.Bottom, false);
                        watch.Stop();
                        BDebug.LogFormat("加载{0},耗时: {1}ms", index, watch.ElapsedMilliseconds);
                        PushCaheData(index);
                    }


                }
            }
        }
        /// <summary>
        /// 异步加载窗口
        /// </summary>
        /// <param name="uiIndexs"></param>
        /// <param name="loadProcessAction"></param>
        public void AsyncLoadWindows(List<int> uiIndexs, Action<int, int> loadProcessAction)
        {
            int allCount = uiIndexs.Count;
            int curTaskCount = 0;
            foreach (var index in uiIndexs)
            {
                if (windowMap.ContainsKey(index))
                {
                    var uvalue = windowMap[index];
                    if (uvalue.IsLoad)
                    {
                        Debug.LogError("已经加载过并未卸载" + index);
                        //任务直接完成
                        {
                            curTaskCount++;
                            loadProcessAction(allCount, curTaskCount);
                        }
                        continue;
                    }
                }
                else
                {
                    //创建窗口
                    var win = CreateWindow(index);
                    if (win == null)
                    {
                        Debug.LogErrorFormat("不存在UI:{0}", index);
                    }
                    else
                    {
                        windowMap[index] = win;
                        //开始窗口加载

                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        win.AsyncLoad(() =>
                        {
                            watch.Stop();
                            BDebug.LogFormat("加载{0},耗时: {1}ms", index, watch.ElapsedMilliseconds);
                            curTaskCount++;
                            loadProcessAction(allCount, curTaskCount);

                            win.Transform.SetParent(this.Bottom, false);
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
            List<WinData> cacheList = null;
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
        /// <param name="uiIndexs">窗口枚举</param>
        public void UnLoadWindows(params int[] uiIndexs)
        {
            foreach (var index in uiIndexs)
            {
                if (windowMap.ContainsKey(index))
                {
                    var uvalue = windowMap[index];
                    uvalue.Close();
                    uvalue.Destroy();
                    windowMap.Remove(index);
                }
                else
                {
                    Debug.LogErrorFormat("不存在UI：{0}", uiIndexs);
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
        public void ShowWindow(int uiIndex, bool ReSetMask = true, UILayer layer = UILayer.Bottom)
        {
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
                Debug.LogErrorFormat("不存在UI：{0}", uiIndex);
            }
        }


        public AWindow GetWindow(int uiIndex)
        {
            AWindow win = null;

            this.windowMap.TryGetValue(uiIndex, out win);

            return win;
        }
        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="uiIndex">窗口枚举</param>
        public void CloseWindow(int uiIndex, bool isMask = true)
        {
            if (windowMap.ContainsKey(uiIndex))
            {
                var v = windowMap[uiIndex];
                if (!v.IsClose && v.IsLoad)
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

        private Dictionary<int, List<WinData>> uiDataCacheMap = new Dictionary<int, List<WinData>>();
        /// <summary>
        /// 外部推送ui数据
        /// </summary>
        /// <param name="uiIndex"></param>
        /// <param name="data"></param>
        public void SendMessage(int uiIndex, WinData data)
        {
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
            List<WinData> list = null;
            uiDataCacheMap.TryGetValue(uiIndex, out list);
            //
            if (list == null)
            {
                list = new List<WinData>();
                uiDataCacheMap[uiIndex] = list;
            }
            list.Add(data);

        }

        /// <summary>
        /// 获取窗口状态
        /// </summary>
        /// <param name="uiIndex"></param>
        /// <returns></returns>
        public bool GetWindowStatus(int uiIndex)
        {
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

        public void Lock(int uiIndex)
        {
            AWindow win = null;
            this.windowMap.TryGetValue(uiIndex, out win);
            if (win != null)
            {
                win.Lock();
            }
        }

        public void UnLock(int uiIndex)
        {
            AWindow win = null;
            this.windowMap.TryGetValue(uiIndex, out win);
            if (win != null)
            {
                win.UnLock();
            }
        }
        /// <summary>
        /// 更新
        /// </summary>
        override  public void Update()
        {
            if (windowMap.Count > 0)
            {
                foreach (var v in windowMap)
                {
                    if (!v.Value.IsClose && v.Value.IsLoad)
                    {
                        //执行帧逻辑
                        v.Value.Update();
                    }
                }
            }
        }

    }
}