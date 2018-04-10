using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using BDFramework.Logic.GameLife;
using BDFramework.Mgr;
using BDFramework.UI;
using BDFramework.Logic.Item;
using BDFramework.ResourceMgr;
using System.IO;
//using UnityEditor.Graphs;

public class BDFrameWork : MonoBehaviour
{
    private IGameStart gameStart;
    private List<IMgr> mgrList;
    private void Awake()
    {
        Debug.Log("start bdframe");
      
        //组件加载
        this.gameObject.AddComponent<IEnumeratorTool>();
        this.gameObject.AddComponent<BResources>();
        Type[] frameTypes = Assembly.GetExecutingAssembly().GetTypes(); ;
        Type[] logicTypes = null;
        List<Type> allTypes = new List<Type>();
        
        //编辑器环境下 寻找dll
        if (Application.isEditor)
        {
            Debug.Log("Edidor Get Types...");
            var assmblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            var logicAssmbly = assmblies.Find((a) => a.GetName().Name == "Assembly-CSharp");
            logicTypes = logicAssmbly.GetTypes();
        }

        allTypes.AddRange(frameTypes);
        allTypes.AddRange(logicTypes);
   //其他环境用热更模型进行加载

        //
        mgrList = new List<IMgr>();
        //寻找所有的管理器
        foreach (var t in allTypes)
        {
            if (t.BaseType!= null  && t.BaseType.GetInterface("IMgr") != null )
            {
                BDebug.Log("加载管理器-" +  t , "green");
                var i = t.BaseType.GetProperty("Inst").GetValue(null, null) as  IMgr;
                mgrList.Add(i);
             
            }
            //游戏启动器
            else if (this.gameStart == null && t.GetInterface("IGameStart") != null)
            {
                gameStart =  Activator.CreateInstance(t) as IGameStart;
            }

        }
        
        //类型注册
        foreach (var t in allTypes)
        {
            foreach (var iMgr in mgrList)
            {
               
                iMgr.CheckType(t);
            }
        }
        
        //管理器唤醒
        foreach (var _imgr in mgrList)
        {
            _imgr.Awake();
        }

        if (gameStart != null)
        {
            gameStart.Awake();
        }
	}

    private void Start()
    {
        if (gameStart != null)
        {
            gameStart.Start();
        }
    }


    private void Update()
    {
        if (this.mgrList != null)
        {
            foreach (var iMgr in this.mgrList)
            {
                iMgr.Update();
            }
        }
        //
        if (gameStart != null)
        {
            gameStart.Update();
        }
    }
}
