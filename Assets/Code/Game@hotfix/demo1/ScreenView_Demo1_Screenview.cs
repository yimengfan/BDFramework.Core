using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;
using BDFramework.Sql;
using BDFramework.UI;

[ScreenView("demo1")]
public class ScreenView_Demo1_Screenview : IScreenView
{
    public string Name { get; private set; }
    public bool IsLoad { get; private set;     }

    public void BeginInit()
    {
        //一定要设置为true，否则当前是未加载状态
        this.IsLoad = true;
        //加载窗口, 0是窗口id,建议自行换成枚举
        UIManager.Inst.LoadWindows((int) WinEnum.Win_Demo1);
        UIManager.Inst.ShowWindow((int) WinEnum.Win_Demo1);
        Debug.Log("进入demo1");
    }

    public void BeginExit()
    {
        //退出设置为false，否则下次进入不会调用begininit
        this.IsLoad = false;
        //
        Debug.Log("退出Test Screen 1");
    }

    public void Update(float delta)
    {
        
    }

    public void FixedUpdate(float delta)
    {
       
    }
}