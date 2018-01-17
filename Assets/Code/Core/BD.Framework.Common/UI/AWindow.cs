using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 窗口基类
/// </summary>
public abstract class AWindow 
{
    //
    private string resourcePath = null;
    public AWindow(string path)
    {
       resourcePath = path;
       subWindowsDictionary = new Dictionary<string, AWindow>();
    }

    public AWindow(Transform transform)
    {
        this.Transform = transform;
        subWindowsDictionary = new Dictionary<string, AWindow>();
    }
    /// <summary>
    /// 窗口是否关闭
    /// </summary>
    public bool IsClose { get; protected set; }
    /// <summary>
    /// 窗口是否已经加载
    /// </summary>
    public bool IsLoad { get; private set; }

    /// <summary>
    /// 窗口是否需要刷新数据
    /// </summary>
    public bool IsNeedRefreshData { get; set; }
    //锁住窗口
    public bool IsLock { get; private set; }
    /// <summary>
    /// transform
    /// </summary>
    public Transform Transform { get; protected set; }

    /// <summary>
    /// 子窗口列表
    /// </summary>
    protected Dictionary<string, AWindow> subWindowsDictionary;
    //回调表
    protected Dictionary<string, Action<object>> callbackMap;
    
    /// <summary>
    /// 注册回调 当数据传回时候,执行action
    /// </summary>
    protected  void RegisterAction(string name,Action<object> callback)
    {
        callbackMap[name] = callback;
    }

    /// <summary>
    /// 添加子窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="win"></param>
    protected void AddSubWindow(string name, AWindow win)
    {
        this.subWindowsDictionary[name] = win;
    }
    public void AsyncLoad(Action callback)
    {
       //  JDeBug.I.Log("开始任务:" + resourcePath);
        BResources.LoadAsync<GameObject>(resourcePath,(bool result,GameObject o)=>
        {
            var go = GameObject.Instantiate(o);
            Transform = go.transform;
            Transform.gameObject.SetActive(false);
//            ClientMain.AddToUIRoot(Transform);
            IsLoad = true;
            Init();
            if (callback != null)
            {
                callback();
            }
            
        });       
    }
    public void Load()
    {
        var o = BResources.Load<GameObject>(resourcePath);
        if(o == null)
        {
            Debug.LogError("窗口资源不存在:" + resourcePath);
            return;
        }
        var go = GameObject.Instantiate(o);
        Transform = go.transform;
        Transform.gameObject.SetActive(false);
//        ClientMain.AddToUIRoot(Transform);
        IsLoad = true;
        Init();
    }

    /// <summary>
    /// 初始化窗口
    /// </summary>
    virtual public void Init()
    {
        callbackMap = new Dictionary<string, Action<object>>();
        IsClose = true;
    } 

    /// <summary>
    /// 关闭窗口
    /// </summary>
    virtual public void Close()
    {
        IsClose = true;

    }

    /// <summary>
    /// 打开窗口
    /// </summary>
    virtual public void Open()
    {
        //判断是否有锁
        if (IsLock) return;
        IsClose = false;
     //   UIEffectMgr.ShowWindow_Scale(Transform);
    }

    /// <summary>
    /// 销毁窗口
    /// </summary>
    virtual public void Destroy()
    {
        foreach (var subwin in  this.subWindowsDictionary.Values)
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
    virtual public void PushData(IDictionary<string ,object> data)
    {
       foreach(var key in data.Keys)
       {
            Action<object> action = null;
            callbackMap.TryGetValue(key, out action);
            if(action!= null)
            {
                // JDeBug.I.Log("sdfdsf:" + data[key].ToDict<string,object>().Keys.ToList()[0]);

                action( data[key] );
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
    virtual  public void Reset()
    {

    }


     public void Lock()
     {
        IsLock = true;
     }

    public void UnLock()
    {
        IsLock = false;
    }

}

