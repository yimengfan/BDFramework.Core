using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;
using BDFramework.Sql;
using BDFramework.UI;
using Code.Game;
using UnityEditor;

[ScreenView((int)ScreenViewEnum.Main)]
public class ScreenView_Main : IScreenView
{
    public int Name { get; private set; }
    public bool IsLoad { get; private set;     }

    public void BeginInit()
    {
        //一定要设置为true，否则当前是未加载状态
        this.IsLoad = true;

        //加载窗口, 0是窗口id,建议自行换成枚举
        UIManager.Inst.LoadWindows((int) WinEnum.Win_Main);
        UIManager.Inst.ShowWindow((int) WinEnum.Win_Main);
        Debug.Log("进入main");
        
        
    }

    public void BeginExit()
    {
        UIManager.Inst.CloseWindow((int) WinEnum.Win_Main);
    }

    public void Update(float delta)
    {
        
    }

    public void FixedUpdate(float delta)
    {
       
    }
}