using System.Collections;
using System.Collections.Generic;
using BDFramework.ScreenView;
using UnityEngine;
using BDFramework.UI;
using UnityEngine.UI;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int)WinEnum.Win_Test1,"Windows/window_test1") ]
public class Window_Test1 : AWindow 
{
    public Window_Test1(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        
        this.Transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
        {
           this.Close();
           ScreenViewMgr.I.BeginNav("sv_test2");
        });
        
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
