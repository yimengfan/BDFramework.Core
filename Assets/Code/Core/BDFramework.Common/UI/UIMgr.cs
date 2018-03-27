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
        private Dictionary<WinEnum, AWindow> windowMap = null;
        
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
            windowMap  = new Dictionary<WinEnum, AWindow>();
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
                    var name = ((WinEnum)_attr.Type).ToString();
                    SaveAttribute(name, new ClassData() { Attribute = _attr, Type = type });
                }
            }

        }
        //
        private AWindow CreateWindow(WinEnum uiEnum)
        {
            var classData = this.GetCalssData(uiEnum.ToString());
            if (classData == null)
            {
                Debug.LogError("未注册窗口，无法加载:" + uiEnum);
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
        /// <param name="winEnums">窗口枚举</param>
        public void LoadWindows(params WinEnum[] winEnums)
        {
            foreach (var we in winEnums)
            {
                if (windowMap.ContainsKey(we))
                {
                    var uvalue = windowMap[we];
                    if (uvalue.IsLoad)
                    {
                        Debug.LogWarning("已经加载过并未卸载" + we);
                    }
                }
                else
                {
                    //创建ui
                    var window = CreateWindow(we);
                    if (window == null)
                    {
                        Debug.LogErrorFormat("不存在UI:{0}", we);
                    }
                    else
                    {
                        windowMap[we] = window;
                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        window.Load();
                        window.Transform.SetParent(this.Bottom, false);
                        watch.Stop();
                        BDeBug.I.LogFormat("加载{0},耗时: {1}ms", we, watch.ElapsedMilliseconds);
                        PushCaheData(we);
                    }


                }
            }
        }
        /// <summary>
        /// 异步加载窗口
        /// </summary>
        /// <param name="winEnums"></param>
        /// <param name="loadProcessAction"></param>
        public void AsyncLoadWindows(List<WinEnum> winEnums, Action<int, int> loadProcessAction)
        {
            int allCount = winEnums.Count;
            int curTaskCount = 0;
            foreach (var we in winEnums)
            {
                if (windowMap.ContainsKey(we))
                {
                    var uvalue = windowMap[we];
                    if (uvalue.IsLoad)
                    {
                        Debug.LogError("已经加载过并未卸载" + we);
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
                    var win = CreateWindow(we);
                    if (win == null)
                    {
                        Debug.LogErrorFormat("不存在UI:{0}", we);
                    }
                    else
                    {
                        windowMap[we] = win;
                        //开始窗口加载

                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        win.AsyncLoad(() =>
                        {
                            watch.Stop();
                            BDeBug.I.LogFormat("加载{0},耗时: {1}ms", we, watch.ElapsedMilliseconds);
                            curTaskCount++;
                            loadProcessAction(allCount, curTaskCount);

                            win.Transform.SetParent(this.Bottom, false);
                        //推送缓存的数据
                        PushCaheData(we);
                        });
                    }
                }
            }
        }

        private void PushCaheData(WinEnum we)
        {
            // return;
            //检查ui数据缓存
            List<WinData> cacheList = null;
            uiDataCacheMap.TryGetValue(we, out cacheList);
            if (cacheList != null)
            {
                for (int i = 0; i < cacheList.Count; i++)
                {
                    var data = cacheList[i];

                    windowMap[we].SendMessage(data);
                    BDeBug.I.Log("push cache data " + we);
                }
                cacheList.Clear();
                BDeBug.I.LogFormat("推送数据：{0} ,{1}条", we, cacheList.Count);
            }
        }
        /// <summary>
        /// 卸载窗口
        /// </summary>
        /// <param name="uiEnums">窗口枚举</param>
        public void UnLoadWindows(params WinEnum[] uiEnums)
        {
            foreach (var ue in uiEnums)
            {
                if (windowMap.ContainsKey(ue))
                {
                    var uvalue = windowMap[ue];
                    uvalue.Close();
                    uvalue.Destroy();
                    windowMap.Remove(ue);
                }
                else
                {
                    Debug.LogErrorFormat("不存在UI：{0}", uiEnums);
                }
            }
        }


        /// <summary>
        /// 卸载窗口
        /// </summary>
        public void UnLoadALLWindows()
        {
            var keys = new List<WinEnum>(this.windowMap.Keys);
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
        /// <param name="winEnum">窗口枚举</param>
        public void ShowWindow(WinEnum winEnum, bool ReSetMask = true, UILayer layer = UILayer.Bottom)
        {
            if (windowMap.ContainsKey(winEnum))
            {
                var v = windowMap[winEnum];
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
                    Debug.LogErrorFormat("UI处于[unload,lock,open]状态之一：{0}", winEnum);
                }
            }
            else
            {
                Debug.LogErrorFormat("不存在UI：{0}", winEnum);
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="uiEnum">窗口枚举</param>
        public void CloseWindow(WinEnum uiEnum, bool isMask = true)
        {
            if (windowMap.ContainsKey(uiEnum))
            {
                var v = windowMap[uiEnum];
                if (!v.IsClose && v.IsLoad)
                {
                    v.Close();
                }
                else
                {
                    Debug.LogErrorFormat("UI未加载或已经处于close状态：{0}", uiEnum);
                }
            }
            else
            {
                Debug.LogErrorFormat("不存在UI：{0}", uiEnum);

            }
        }

        private Dictionary<WinEnum, List<WinData>> uiDataCacheMap = new Dictionary<WinEnum, List<WinData>>();
        /// <summary>
        /// 外部推送ui数据
        /// </summary>
        /// <param name="uiEnum"></param>
        /// <param name="data"></param>
        public void SendMessage(WinEnum uiEnum, WinData data)
        {
            if (windowMap.ContainsKey(uiEnum))
            {
                var ui = windowMap[uiEnum];

                if (ui.IsLoad)
                {
                    ui.SendMessage(data);
                    return;
                }
            }

            //存入缓存
            List<WinData> list = null;
            uiDataCacheMap.TryGetValue(uiEnum, out list);
            //
            if (list == null)
            {
                list = new List<WinData>();
                uiDataCacheMap[uiEnum] = list;
            }
            list.Add(data);

        }

        /// <summary>
        /// 获取窗口状态
        /// </summary>
        /// <param name="win"></param>
        /// <returns></returns>
        public bool GetWindowStatus(WinEnum win)
        {
            bool isClose = false;

            if (windowMap.ContainsKey(win))
            {
                isClose = windowMap[win].IsClose;
            }
            else
            {
                Debug.LogError("不存在ui:" + win);
            }
            return isClose;
        }

        public void Lock(WinEnum we)
        {
            AWindow win = null;
            this.windowMap.TryGetValue(we, out win);
            if (win != null)
            {
                win.Lock();
            }
        }

        public void UnLock(WinEnum we)
        {
            AWindow win = null;
            this.windowMap.TryGetValue(we, out win);
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