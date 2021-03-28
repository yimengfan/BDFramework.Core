using BDFramework;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using BDFramework.Mgr;
using System.Linq;
using BDFramework.GameStart;
using BDFramework.UFlux;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// using BDFramework.UFlux;
// using BDFramework.UnitTest;


public class BDLauncherBridge
{
    static private IGameStart hotfixStart = null;


    /// <summary>
    /// 整个游戏的启动器
    /// </summary>
    /// <param name="gameLogicTypes">游戏逻辑域传过来的所有type</param>
    static public void Start(Type[] gameLogicTypes = null)
    {
        //组件加载
        List<Type> allTypes = new List<Type>();
        //获取DLL ALLtype
        if (gameLogicTypes == null)
        {
            BDebug.Log("缺少游戏逻辑域的type！");
        }

        allTypes.AddRange(gameLogicTypes);
        //allTypes.Add();

        //管理器列表
        var mgrList = new List<IMgr>();
        //寻找所有的管理器
        allTypes = allTypes.Distinct().ToList();
        foreach (var t in allTypes)
        {
            if (t != null
                && t.BaseType != null
                && t.BaseType.FullName != null
                && t.BaseType.FullName.Contains(".ManagerBase`2")) //这里ILR里面只能这么做，丑但有效
            {
                BDebug.Log("加载管理器-" + t, "green");
                var i = t.BaseType.GetProperty("Inst").GetValue(null, null) as IMgr;
                mgrList.Add(i);
            }
        }

        BDebug.Log("ALLtype:" +  allTypes.Count);
        //遍历type执行逻辑
        foreach (var type in allTypes)
        {
            var baseAttributes = type.GetCustomAttributes();
            if (baseAttributes==null || baseAttributes.Count() == 0)
            {
                continue;
            }
            BDebug.Log("-------");
            //1.类型注册到管理器
            var attributes = baseAttributes.Where((attr) => attr is ManagerAtrribute);
            if (attributes.Count() > 0)
            {
                foreach (var iMgr in mgrList)
                {
                    iMgr.CheckType(type, attributes);
                }
            }

            //2.游戏启动器
            if (hotfixStart == null)
            {
                var attr = baseAttributes.FirstOrDefault((a) => a is GameStartAtrribute);
                if (attr != null && (attr as GameStartAtrribute).Index == 1)
                {
                    hotfixStart = Activator.CreateInstance(type) as IGameStart;
                }
            }
        }

        //UI相关逻辑整理
        List<Type> types = new List<Type>();
        types.AddRange(typeof(Button).Assembly.GetTypes()); //Unity
        types.AddRange(typeof(IButton).Assembly.GetTypes()); //BDFramework.component
        types.AddRange(gameLogicTypes); //游戏业务逻辑
        var uitype = typeof(UIBehaviour);
        foreach (var t in types)
        {
            //注册所有uiComponent
            if (t.IsSubclassOf(uitype))
            {
                ILRuntimeHelper.UIComponentTypes[t.FullName] = t;
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
            BDLauncher.OnUpdate = hotfixStart.Update;
            BDLauncher.OnLateUpdate = hotfixStart.LateUpdate;
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