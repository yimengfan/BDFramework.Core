using BDFramework;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using BDFramework.Mgr;
using System.Linq;
using BDFramework.GameStart;

public class BDLauncherBridge 
{
    static private IGameStart gameStart = null;
 
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

        var gsaType = typeof(GameStartAtrribute);
        //寻找所有的管理器
        allTypes =  allTypes.Distinct().ToList();
        foreach (var t in allTypes)
        {
            try
            {
                if (t!= null&&t.BaseType!= null  && t.BaseType.FullName!=null&&t.BaseType.FullName.Contains(".ManagerBase`2") )
                {
                    BDebug.Log("加载管理器-" +  t , "green");
                    var i = t.BaseType.GetProperty("Inst").GetValue(null, null) as  IMgr;
                    mgrs.Add(i);
                    continue;
                }
                //游戏启动器
                //这里主要寻找
                if (isCodeHotfix && gameStart == null)
                {
                    var attrs = t.GetCustomAttributes(gsaType,false);
                    if (attrs.Length > 0 )
                    {
                        if (attrs[0] is GameStartAtrribute)
                        {
                            gameStart =  Activator.CreateInstance(t) as IGameStart;
                            BDebug.Log("找到启动器" + t.FullName,"red");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }


        }

        BDebug.Log("管理器数量 =" +  mgrs.Count);
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
        BDebug.Log("管理器注册完成!");
        
        //game生命注册
        if (gameStart != null)
        {
            gameStart.Start();
            //
            BDLauncher.OnUpdate += gameStart.Update;
            BDLauncher.OnLateUpdate += gameStart.LateUpdate;
        }
        BDebug.Log("游戏生命周期准备完毕!");
        //所有管理器开始工作
        foreach (var m in mgrs)
        {
            m.Start();
        }
        BDebug.Log("管理器开始工作!");
	}
}
