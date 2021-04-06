using System.Collections;
using System.Collections.Generic;
using BDFramework.ResourceMgr;
using BDFramework.ScreenView;
using BDFramework.UFlux;
using UnityEngine;
using BDFramework.UI;
using Game;
using UnityEngine.UI;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int) WinEnum.Win_Demo1, "Windows/window_demo1")]
public class Window_Demo1 : AWindow
{
    //[]
    public Window_Demo1(string path) : base(path)
    {
    }

    [ButtonOnclick("Button")]
    private void btn_01()
    {
        this.Close();
        ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Main);
    }
}