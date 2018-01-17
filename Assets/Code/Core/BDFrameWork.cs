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
using UnityEditor.Graphs;

public class BDFrameWork : MonoBehaviour
{
    private IGameStart gameStart;
    private List<IMgr> mgrList;
    private void Awake()
    {
        //组件加载
        this.gameObject.AddComponent<IEnumeratorTool>();
        this.gameObject.AddComponent<BResources>();
        
        var types = Assembly.GetExecutingAssembly().GetTypes();
        mgrList = new List<IMgr>();
        //寻找所有的管理器
        foreach (var t in types)
        {
            if (t.BaseType!= null  && t.BaseType.GetInterface("IMgr") != null )
            {
                BDeBug.I.Log("加载管理器-" +  t , Styles.Color.Green);
                var i = t.BaseType.GetProperty("I").GetValue(null, null) as  IMgr;
                mgrList.Add(i);
            }
            //游戏启动器
            else if (this.gameStart == null && t.GetInterface("IGameStart") != null)
            {
                gameStart =  Activator.CreateInstance(t) as IGameStart;
            }         
        }
        
        //类型注册
        foreach (var t in types)
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
