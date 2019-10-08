using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 窗口基类
    /// </summary>
    public abstract class M_AWindow
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        private string resourcePath = null;

        public M_AWindow(string path)
        {
            resourcePath = path;
            this.TempData = M_WindowData.Create();
            subWindowsDictionary = new Dictionary<string, M_SubWindow>();
        }

        public M_AWindow(Transform transform)
        {
            this.Transform = transform;
            this.TempData = M_WindowData.Create();
            subWindowsDictionary = new Dictionary<string, M_SubWindow>();

            M_UITools.AutoSetTransformPath(this);
        }

        /// <summary>
        /// 窗口临时数据
        /// </summary>
        private M_WindowData TempData;

        /// <summary>
        /// 窗口是否关闭
        /// </summary>
        public bool IsClose { get; protected set; }

        /// <summary>
        /// 窗口是否已经加载
        /// </summary>
        public bool IsLoad { get; private set; }

        //锁住窗口
        public bool IsLock { get; private set; }

        /// <summary>
        /// transform
        /// </summary>
        public Transform Transform { get; protected set; }

        /// <summary>
        /// 子窗口列表
        /// </summary>
        protected Dictionary<string, M_SubWindow> subWindowsDictionary;

        //回调表
        protected Dictionary<string, Action<object>> callbackMap;

        /// <summary>
        /// 注册回调 当数据传回时候,执行action
        /// </summary>
        protected void RegisterAction(string name, Action<object> callback)
        {
            callbackMap[name] = callback;
        }

        #region 子窗口操作

        /// <summary>
        /// 添加子窗口
        /// </summary>
        /// <param name="name"></param>
        /// <param name="win"></param>
        protected void AddSubWindow(string name, M_SubWindow win)
        {
            this.subWindowsDictionary[name] = win;
        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <param name="name"></param>
        protected void OpenSubWindow(string name, M_WindowData mWindowData = null)
        {
            M_SubWindow subwin = null;
            if (this.subWindowsDictionary.TryGetValue(name, out subwin))
            {
                subwin.Open(mWindowData);
            }

            else
            {
                BDebug.LogError("不存在子窗口:" + name);
            }
        }


        //关闭子窗口
        protected void CloseSubWindow(string name)
        {
            M_SubWindow subwin = null;
            if (this.subWindowsDictionary.TryGetValue(name, out subwin))
            {
                subwin.Close();
            }
            else
            {
                BDebug.LogError("不存在子窗口:" + name);
            }
        }

        #endregion


        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="callback"></param>
        public void AsyncLoad(Action callback)
        {
            //  JDeBug.Inst.Log("开始任务:" + resourcePath);
            var o = Resources.Load<GameObject>(resourcePath);
            var go = GameObject.Instantiate(o);
            Transform = go.transform;
            Transform.gameObject.SetActive(false);
            IsLoad = true;
            //自动查找节点
            M_UITools.AutoSetTransformPath(this);
            Init();
            if (callback != null)
            {
                callback();
            }
        }

//
        /// <summary>
        /// 同步加载
        /// </summary>
        public void Load()
        {
            var o = Resources.Load<GameObject>(resourcePath);
            if (o == null)
            {
                Debug.LogError("窗口资源不存在:" + resourcePath);
                return;
            }
            var go = GameObject.Instantiate(o);
            Transform = go.transform;
            Transform.gameObject.SetActive(false);
            IsLoad = true;
            //自动查找节点
            M_UITools.AutoSetTransformPath(this);
            Init();
        }

        /// <summary>
        /// 初始化窗口
        /// </summary>
        virtual public void Init()
        {
            callbackMap = new Dictionary<string, Action<object>>();
            IsClose = true;

            //自动赋值
            var fields = this.GetType();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        virtual public void Close()
        {
            IsClose = true;
            this.Transform.gameObject.SetActive(false);
        }

        /// <summary>
        /// 打开窗口
        /// </summary>
        /// <param name="data"></param>
        virtual public void Open(M_WindowData data = null)
        {
            this.TempData.MergeData(data);
            IsClose = false;
            this.Transform.gameObject.SetActive(true);
        }

        /// <summary>
        /// 销毁窗口
        /// </summary>
        virtual public void Destroy()
        {
            //卸载
            if (Transform)
            {
                GameObject.Destroy(this.Transform.gameObject);
            }

            //
            foreach (var subwin in this.subWindowsDictionary.Values)
            {
                subwin.Destroy();
            }

            IsLoad = false;
            //卸载窗口
            Resources.UnloadAsset(this.Transform);
        }

        /// <summary>
        /// 更新UI使用的数据
        /// </summary>
        /// <param name="data">数据</param>
        public void SendMessage(M_WindowData data)
        {
            foreach (var key in data.DataMap.Keys)
            {
                Action<object> action = null;
                callbackMap.TryGetValue(key, out action);
                if (action != null)
                {
                    action(data.DataMap[key]);
                }
            }
        }

        /// <summary>
        /// 更新窗口
        /// </summary>
        virtual public void Update()
        {
        }

        /// <summary>
        /// 重置窗口
        /// </summary>
        virtual public void Reset()
        {
        }


        #region Tools

        private void AutoSetTransformPath()
        {
        }

        #endregion
    }
}