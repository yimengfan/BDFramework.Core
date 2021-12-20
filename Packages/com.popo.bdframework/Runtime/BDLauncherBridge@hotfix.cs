using BDFramework;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using BDFramework.Mgr;
using BDFramework.GameStart;
using BDFramework.HotFix.Mgr;
using BDFramework.Hotfix.Reflection;
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
        //UI组件类型注册
        List<Type> types = new List<Type>();
        types.AddRange(typeof(Button).Assembly.GetTypes()); //Unity
        types.AddRange(typeof(IButton).Assembly.GetTypes()); //BDFramework.Core
        types.AddRange(mainProjectTypes); //游戏业务逻辑
        if (Application.isEditor)
        {
            types = types.Distinct().ToList();
        }

        //ui类型
        var uitype = typeof(UIBehaviour);
        for (int i = 0; i < types.Count; i++)
        {
            var type = types[i];
            //注册所有uiComponent
            bool ret = type.IsSubclassOf(uitype);
            if (ret)
            {
                if (!ILRuntimeHelper.UIComponentTypes.ContainsKey(type.Name))
                {
                    //因为Attribute typeof（Type）后无法获取fullname
                    ILRuntimeHelper.UIComponentTypes[type.FullName] = type;
                }
                else
                {
                    BDebug.LogError("有重名UI组件，请注意" + type.FullName);
                }
            }
        }


        //执行主工程逻辑
        BDebug.Log("主工程Instance初始化...","red");
        ManagerInstHelper.Load(mainProjectTypes);
        //执行热更逻辑
        if (hotfixTypes != null)
        {
            TriggerHotFixGameStart(hotfixTypes);
            //获取管理器列表，开始工作
            BDebug.Log("热更Instance初始化...","red");
            var hotfixMgrList = ILRuntimeManagerInstHelper.LoadManagerInstance(hotfixTypes);
            //启动热更管理器
            foreach (var hotfixMgr in hotfixMgrList)
            {
                hotfixMgr.Start();
            }
        }
        else
        {
            //热更逻辑为空,触发HotfixGamestart
            TriggerHotFixGameStart(mainProjectTypes);
            //启动著工程的管理器
            ManagerInstHelper.Start();
        }
    }

    /// <summary>
    /// 热更启动
    /// </summary>
    /// <param name="types"></param>
    static private void TriggerHotFixGameStart(Type[] types)
    {
        //寻找IGameStart
        for (int i = 0; i < types.Length; i++)
        {
            // 游戏启动器
            var type = types[i];
            if (!type.IsClass) continue;

            var interfaceTypes = type.GetInterfaces();
            for (int j = 0; j < interfaceTypes.Length; j++)
            {
                var interfaceType = interfaceTypes[j];
                if (interfaceType.Name.Contains(nameof(IHotfixGameStart)))
                {
                    hotfixStart = Activator.CreateInstance(type) as IHotfixGameStart;
                    break;
                }
            }
        }

        //gamestart生命注册
        if (hotfixStart != null)
        {
            hotfixStart.Start();
            BDLauncher.OnUpdate += hotfixStart.Update;
            BDLauncher.OnLateUpdate += hotfixStart.LateUpdate;
        }
    }
}