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
[UI((int)WinEnum.Win_Demo1,"Windows/window_demo1") ]
public class Window_Demo1 : AWindow
{
    [TransformPath("Button")]
    private Button btn_01;


    
    //[]
    public Window_Demo1(string path) : base(path)
    {
        var c = new Camera();
    }

    public override void Init()
    {
        base.Init();


        //
        btn_01.onClick.AddListener(() =>
        {
           this.Close();
           ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Main);
        });
        

    }

}
