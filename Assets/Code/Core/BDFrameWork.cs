using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using BDFramework.Logic.GameLife;
using BDFramework.Mgr;
using BDFramework.UI;
using BDFramework.Logic.Item;
public class BDFrameWork : MonoBehaviour
{
    private IGameStart gameStart;
    private List<IMgr> mgrList;
    private void Awake()
    {
        //组件加载
        this.gameObject.AddComponent<IEnumeratorTool>();
        
        var types = Assembly.GetExecutingAssembly().GetTypes();
        mgrList = new List<IMgr>();
        //寻找所有的管理器
        foreach (var t in types)
        {
            if (t.BaseType!= null  && t.BaseType.GetInterface("IMgr") != null )
            {
                var xxx = t.BaseType.GetProperties();
                foreach (var x in xxx)
                {
                    Debug.Log(x.Name);
                }
                Debug.Log("加载管理器-" +  t);
                var i = t.BaseType.GetProperty("I").GetValue(null, null) as  IMgr;
                mgrList.Add(i);
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
	}

    private void Start()
    {
        
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
    }
}
