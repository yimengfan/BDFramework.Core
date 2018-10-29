using System.Collections;
using System.Collections.Generic;
using BDFramework.ScreenView;
using UnityEngine;
using BDFramework.UI;
using DG.Tweening;
using UnityEngine.UI;
using BDFramework;
using Code.Game.Windows;
using Code.Game.Windows.MCX;

//using UnityEditor.Graphs;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int)WinEnum.Win_Demo3,"Windows/window_demo3") ]
public class Window_MVC : AWindow 
{
    public Window_MVC(string path) : base(path)
    {
        
    }

    public override void Init()
    {
        base.Init();
        //绑定一个
        var dataDriven = DataListenerServer.Create("MVCTest");
        //创建一个mvc模式的窗口
        var control = new ViewContrl_MVCTest(dataDriven);
        var view = new View_MVCTest(Transform, dataDriven);
        var mvc =  MVCBind.Create("MVCTest",control ,view);
     
    }

    public override void Close()
    {  
        base.Close();
    }

    public override void Open(WindowData data = null)
    {
        base.Open();
    }

    public override void Destroy()
    {
        base.Destroy();
    }

}
