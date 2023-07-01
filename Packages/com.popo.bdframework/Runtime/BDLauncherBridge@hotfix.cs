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

public class BDLauncherBridge
{
    static private IHotfixGameStart hotfixStart = null;

    /// <summary>
    /// 整个游戏的启动器
    /// </summary>
    /// <param name="mainProjectTypes">TODO 废弃的传参，回头删掉</param>
    static public void Start(Type[] mainProjectTypes = null, Type[] hotfixTypes = null)
    {
        //list
        mainProjectTypes = ManagerInstHelper.GetMainProjectTypes();
       

     

        
        //UI组件类型注册
        //ui类型
#if ENABLE_ILRUNTIME
        var uitype = typeof(UIBehaviour);
        for (int i = 0; i < mainProjectTypes.Length; i++)
        {
            var type = mainProjectTypes[i];
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
#endif

        //执行主工程逻辑
        BDebug.Log("【Launch】主工程管理器初始化..", Color.red);
    
        //执行热更逻辑
        if (hotfixTypes != null)
        {
            //初始化manager
            var hotfixMgrList= HotfixManagerInstHelper.LoadManager(hotfixTypes);
            var hotfixMgrNames = hotfixMgrList.Select((m) => m.GetType().Name);
            var replacedMgrTypes=  ManagerInstHelper.LoadManager(mainProjectTypes,hotfixMgrNames);
           
            //热更实体生效：主工程实体类型被覆盖
            ManagerInstHelper.RegisterType(hotfixTypes,true);
            
            //热更manager接管"主工程类型"：主工程manager被覆盖
            HotfixManagerInstHelper.RegisterMainProjectType(replacedMgrTypes,mainProjectTypes);
            
            //触发GameStart
            TriggerMainProjectGameStart(mainProjectTypes);
            TriggerHotFixGameStart(hotfixTypes);
            
            //启动管理器
            ManagerInstHelper.Start();
            HotfixManagerInstHelper.Start();
        }
        else
        {
            //启动主工程的管理器
            ManagerInstHelper.LoadManager(mainProjectTypes);
            //触发GameStart
            TriggerMainProjectGameStart(mainProjectTypes);
            TriggerHotFixGameStart(mainProjectTypes);
            //开始
            ManagerInstHelper.Start();
        }
    }

    /// <summary>
    /// 启动主工程Gamestart
    /// </summary>
    /// <param name="types"></param>
    static private void TriggerMainProjectGameStart(Type[] types)
    {
        //主工程启动
        IGameStart mainStart;
        foreach (var type in types)
        {
            if (type.IsClass && type.GetInterface(nameof(IGameStart)) != null)
            {
                BDebug.Log("【Launch】主工程 Start： " + type.FullName);
                mainStart = Activator.CreateInstance(type) as IGameStart;
                if (mainStart != null)
                {
                    //注册
                    mainStart.Start();
                    BDLauncher.OnUpdate += mainStart.Update;
                    BDLauncher. OnLateUpdate += mainStart.LateUpdate;
                    break;
                }
            }
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
            if (!type.IsClass)
            {
                continue;
            }

#if ENABLE_ILRUNTIME
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
#elif ENABLE_HCLR
            if (type.GetInterface(nameof(IHotfixGameStart)) != null)
            {
                hotfixStart = Activator.CreateInstance(type) as IHotfixGameStart;
                break;
            }
#endif

            
          

            
#if !ENABLE_ILRUNTIME && !ENABLE_HCLR
    BDebug.LogError("请打开BuildDLL面板,开启ILRUNTIME或者HCLR!");
#endif
            
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