using BDFramework;
using System.Collections.Generic;
using UnityEngine;
using System;
using BDFramework.Mgr;
using BDFramework.GameStart;
using BDFramework.Reflection;
using BDFramework.UFlux;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// using BDFramework.UFlux;
// using BDFramework.UnitTest;


public class BDLauncherBridge
{
    static private IHotfixGameStart hotfixStart = null;


    /// <summary>
    /// 整个游戏的启动器
    /// </summary>
    /// <param name="mainProjectTypes">游戏逻辑域传过来的所有type</param>
    static public void Start(Type[] mainProjectTypes = null, Type[] hotfixTypes = null)
    {
        //获取DLL ALLtype
        if (hotfixTypes == null)
        {
            if (Application.isEditor)
            {
                hotfixTypes = mainProjectTypes;
            }

            BDebug.Log("缺少游戏逻辑域的type！");
        }


        //UI组件类型注册
        List<Type> types = new List<Type>();
        types.AddRange(typeof(Button).Assembly.GetTypes()); //Unity
        types.AddRange(typeof(IButton).Assembly.GetTypes()); //BDFramework.component
        types.AddRange(mainProjectTypes); //游戏业务逻辑
        var uitype = typeof(UIBehaviour);
        foreach (var type in types)
        {
            //注册所有uiComponent
            if (type.IsSubclassOf(uitype))
            {
                Debug.Log("hotfix:" + type.FullName);
                //

                ILRuntimeHelper.UIComponentTypes[type.FullName] = type;
            }
        }

        //管理器列表
        var mgrList = new List<IMgr>();
        foreach (var type in hotfixTypes)
        {
            if (type != null
                && type.BaseType != null
                && type.BaseType.FullName != null)
            {
                if (type.BaseType.FullName.Contains(".ManagerBase`2")) //这里ILR里面只能这么做，丑但有效
                {
                    BDebug.Log("加载管理器-" + type, "green");
                    var i = type.BaseType.GetProperty("Inst").GetValue(null, null) as IMgr;
                    mgrList.Add(i);
                }
                else
                {
                    // 2.游戏启动器
                    if (hotfixStart == null)
                    {
                        if (type.IsClass && type.GetInterface(nameof(IHotfixGameStart)) != null)
                        {
                            hotfixStart = Activator.CreateInstance(type) as IHotfixGameStart;
                        }
                    }
                }
            }
        }

        //遍历type执行逻辑
        foreach (var type in hotfixTypes)
        {
            var mgrAttribute = type.GetAttributeInILRuntime<ManagerAtrribute>();
            if (mgrAttribute == null)
            {
                continue;
            }

            //1.类型注册到管理器
            foreach (var iMgr in mgrList)
            {
                iMgr.CheckType(type, mgrAttribute);
            }
        }


        //管理器初始化
        foreach (var m in mgrList)
        {
            m.Init();
        }

        //gamestart生命注册
        if (hotfixStart != null)
        {
            hotfixStart.Start();
            BDLauncher.OnUpdate += hotfixStart.Update;
            BDLauncher.OnLateUpdate += hotfixStart.LateUpdate;
        }

        //执行框架初始化完成的测试
        BDLauncher.OnBDFrameInitialized?.Invoke();
        BDLauncher.OnBDFrameInitializedForTest?.Invoke();
        //所有管理器开始工作
        foreach (var m in mgrList)
        {
            m.Start();
        }


        // IEnumeratorTool.WaitingForExec(5, () =>
        // {
        //     //执行单元测试
        //     if (BDLauncher.Inst.GameConfig.IsExcuteHotfixUnitTest && ILRuntimeHelper.IsRunning)
        //     {
        //         HotfixTestRunner.RunHotfixUnitTest();
        //     }
        // });
    }
}