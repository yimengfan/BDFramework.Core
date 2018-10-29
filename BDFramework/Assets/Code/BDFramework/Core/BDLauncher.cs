using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BDFramework;
using UnityEngine;
using UnityEngine.Serialization;

public class BDLauncher : MonoBehaviour
{
   public bool IsCodeHotfix = false;
   public bool IsLoadPdb = false;
   public bool IsAssetBundleModel = false;
   static public Action OnStart      { get; set; }
   static public Action OnUpdate     { get; set; }
   static public Action OnLateUpdate { get; set; }

    // Use this for initialization
    private void Awake()
    {
        this.gameObject.AddComponent<IEnumeratorTool>();
        
        BResources.Init(IsAssetBundleModel);
        //热更资源模式
        if (IsAssetBundleModel)
        {           
            //开始启动逻辑  
            var dd = DataListenerServer.Create("BDFrameLife");
            dd.AddData("OnAssetBundleOever");
            dd.AddListener("OnAssetBundleOever", (o) =>
            {
                //等待ab完成
                OnLaunch();               
            });
        }
        else
        {
            OnLaunch();
        }
       
        
       
    }

    private void Start()
    {
        if (OnStart != null)
        {
            OnStart();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (OnUpdate != null)
        {
            OnUpdate();
        }
    }

    private void LateUpdate()
    {
        if (OnLateUpdate != null)
        {
            OnLateUpdate();
        }
    }


    /// <summary>
    /// 开始启动游戏
    /// </summary>
    private void OnLaunch()
    {
        if (IsCodeHotfix) //热更代码模式
        {
            ILRuntimeHelper.LoadHotfix(IsLoadPdb);
           
            ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start",null ,new object[]{IsCodeHotfix ,IsAssetBundleModel});
        }
        else
        {
            //这里用反射是为了 不访问逻辑模块的具体类，防止编译失败
            var assembly = Assembly.GetExecutingAssembly();
            var type = assembly.GetType("BDLauncherBridge");
            var method = type.GetMethod("Start", BindingFlags.Public| BindingFlags.Static);
            method.Invoke(null , new object[]{IsCodeHotfix ,IsAssetBundleModel});
        }
    }
    
}