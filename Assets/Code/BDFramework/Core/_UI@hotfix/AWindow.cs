using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Runtime.CompilerServices;
using BDFramework.UI;
using BDFramework.ResourceMgr;
/// <summary>
/// 窗口基类
/// </summary>
public abstract class AWindow
{
    /// <summary>
    /// 资源路径
    /// </summary>
    private string resourcePath = null;

    public AWindow(string path)
    {
        resourcePath = path;
       // this.TempData = WindowData.Create();
        subWindowsDictionary = new Dictionary<string, SubWindow>();
    }

    public AWindow(Transform transform)
    {
        this.Transform = transform;
       // this.TempData = WindowData.Create();
        subWindowsDictionary = new Dictionary<string, SubWindow>();
        
        UITools.AutoSetTransformPath(this);
    }

    /// <summary>
    /// 窗口临时数据
    /// </summary>
  //  private WindowData TempData;

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
    protected Dictionary<string, SubWindow> subWindowsDictionary;

    //回调表
    protected Dictionary<string, Action<WindowData>> callbackMap;

    /// <summary>
    /// 注册回调 当数据传回时候,执行action
    /// </summary>
    protected void RegisterAction(string name, Action<WindowData> callback)
    {
        callbackMap[name] = callback;
    }

    #region 子窗口操作

    /// <summary>
    /// 添加子窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="win"></param>
    protected void AddSubWindow(string name, SubWindow win)
    {
        this.subWindowsDictionary[name] = win;
    }

    /// <summary>
    /// 打开
    /// </summary>
    /// <param name="name"></param>
    protected void OpenSubWindow(string name, WindowData windowData =null)
    {
        SubWindow subwin = null;
        if (this.subWindowsDictionary.TryGetValue(name, out subwin))
        {
            subwin.Open(windowData);
        }

        else
        {
            BDebug.LogError("不存在子窗口:" + name);
        }

    }


    //关闭子窗口
    protected void CloseSubWindow(string name)
    {
        SubWindow subwin = null;
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
        BResources.AsyncLoad<GameObject>(resourcePath, (result,  o) =>
        {
            if (o != null)
            {
                var go = GameObject.Instantiate(o);
                Transform = go.transform;
                Transform.gameObject.SetActive(false);
                IsLoad = true;
                //自动查找节点
                UITools.AutoSetTransformPath(this);
                Init();
            }
            else
            {
                BDebug.LogError("窗口资源不存在:" + this.GetType().FullName);
            }

            if (callback != null)
            {
                callback();
            }

        });
    }
//
    /// <summary>
    /// 同步加载
    /// </summary>
    public void Load()
    {
        var o = BResources.Load<GameObject>(resourcePath);
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
        UITools.AutoSetTransformPath(this);
        Init();
    }

    /// <summary>
    /// 初始化窗口
    /// </summary>
    virtual public void Init()
    {
        callbackMap = new Dictionary<string, Action<WindowData>>();
        IsClose = true;

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
    virtual public void Open(WindowData data = null)
    {
       
        //this.TempData.MergeData(data);
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
            BResources.Destroy(this.Transform);
        }

        //
        foreach (var subwin in this.subWindowsDictionary.Values)
        {
            subwin.Destroy();
        }

        IsLoad = false;
        //卸载窗口
        BResources.UnloadAsset(resourcePath);
    }

    /// <summary>
    /// 更新UI使用的数据
    /// </summary>
    /// <param name="data">数据</param>
    public void SendMessage(WindowData data)
    {

        Action<WindowData> action = null;
        callbackMap.TryGetValue(data.Name, out action);
        if (action != null)
        {
            action(data);
        }
        
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

