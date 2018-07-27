using BDFramework;
using BDFramework.Sql;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using BDFramework.Logic.GameLife;
using BDFramework.Mgr;
using BDFramework.UI;
using BDFramework.ResourceMgr;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
public class BDLauncherBridge 
{
    static private IGameStart gameStart;
 
    /// <summary>
    /// 这里注册整个游戏类型
    /// </summary>
    /// <param name="isCodeHotfix"></param>
    static  public void Start(bool isCodeHotfix = false ,bool isResourceHotfix =false)
    {   
        BDebug.Log("资源热更:" + isResourceHotfix,"yellow");
        BDebug.Log("代码热更:" + isCodeHotfix,"yellow");
        //组件加载
        List<Type> allTypes = new List<Type>();
        BResources.IsAssetBundleModel = isResourceHotfix;
        //编辑器环境下 寻找dll
        if (isCodeHotfix ==false)
        {
            //当framework 是dll形式时候需要先获取当前dll里面的所有type
            allTypes = Assembly.GetExecutingAssembly().GetTypes().ToList();
            
            //非源码形式 需要取game type
            if (Assembly.GetExecutingAssembly().GetName().Name != "Assembly-CSharp")
            {

                var assmblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
                var logicAssmbly = assmblies.Find((a) => a.GetName().Name == "Assembly-CSharp");
                
                allTypes.AddRange(logicAssmbly.GetTypes());
            }
        }
        else
        {
            
            var values = ILRuntimeHelper.AppDomain.LoadedTypes.Values.ToList();
            foreach (var v in values)
            {
                allTypes.Add(v.ReflectionType);
            }
        }
        
        //
        var mgrs =new List<IMgr>();
        //寻找所有的管理器
        foreach (var t in allTypes)
        {
            try
            {
                if (t!= null&&t.BaseType!= null  && t.BaseType.FullName!=null&&t.BaseType.FullName.Contains(".ManagerBase`2") )
                {
                    BDebug.Log("加载管理器-" +  t , "green");
                    var i = t.BaseType.GetProperty("Inst").GetValue(null, null) as  IMgr;
                    mgrs.Add(i);
             
                }
                //游戏启动器
                else if (gameStart == null && t.GetInterface("IGameStart") != null)
                {
                    gameStart =  Activator.CreateInstance(t) as IGameStart;
                }
            }
            catch (Exception e)
            {
               Debug.LogError(e.Message);
            }

        }

        BDebug.Log("管理器数量：" +  mgrs.Count);
        //类型注册
        foreach (var t in allTypes)
        {
            foreach (var iMgr in mgrs)
            {            
                iMgr.CheckType(t);
            }
        }
        
        //管理器初始化
        foreach (var m in mgrs)
        {
            m.Init();
        }

        //game生命注册s
        if (gameStart != null)
        {
            BDLauncher.OnStart = gameStart.Start;
            BDLauncher.OnUpdate =()=>
            {
                // TODO 后期干掉管理类的 update
                foreach (var v in mgrs)
                {
                    v.Update();
                }        
                gameStart.Update();
            };
            BDLauncher.OnLateUpdate = gameStart.LateUpdate;
        }
        
        //所有管理器开始工作
        foreach (var m in mgrs)
        {
            m.Start();
        }
	}
}
