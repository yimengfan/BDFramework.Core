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
[UI((int)WinEnum.Win_XVC,"Windows/window_xvc") ]
public class Window_MVC : AWindow 
{
    public Window_MVC(string path) : base(path)
    {
        
    }

    public override void Init()
    {
        base.Init();
        //绑定一个
        var dataDriver = DataDriveServer.Create("XVCTest");
        var mvc =  MVCBind.Create("XVCTest", new ViewContrl_MVCTest(dataDriver), new View_MVCTest(Transform, dataDriver));
     
    }

    public override void Close()
    {  
        base.Close();
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Destroy()
    {
        base.Destroy();
    }

}
