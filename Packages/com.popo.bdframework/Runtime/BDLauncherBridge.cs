using BDFramework;
using System;
using BDFramework.Mgr;
using BDFramework.GameStart;

public class BDLauncherBridge
{
    static private IHotfixGameStart hotfixStart = null;

    /// <summary>
    /// 整个游戏的启动器
    /// </summary>
    /// <param name="mainProjectTypes">TODO 废弃的传参，回头删掉</param>
    static public void Start()
    {
       //list
       var  mainProjectTypes = ManagerInstHelper.GetMainProjectTypes();
        //启动主工程的管理器
        ManagerInstHelper.LoadManager(mainProjectTypes);
        //触发GameStart
        TriggerMainProjectGameStart(mainProjectTypes);
        TriggerHotFixGameStart(mainProjectTypes);
        //开始
        ManagerInstHelper.Start();
        
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


            if (type.GetInterface(nameof(IHotfixGameStart)) != null)
            {
                hotfixStart = Activator.CreateInstance(type) as IHotfixGameStart;
                break;
            }
            
#if !ENABLE_ILRUNTIME && !ENABLE_HYCLR
    BDebug.LogError("请开启ILRUNTIME或者HyCLR!");
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