using System.Collections;
using System.Collections.Generic;
using BDFramework.ResourceMgr;
using BDFramework.ScreenView;
using UnityEngine;
using BDFramework.UI;
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
           ScreenViewManager.Inst.MainLayer.BeginNavTo("main");
        });

        var o = BResources.Load<GameObject>("effect");
        var e = GameObject.Instantiate(o);
        e.transform.SetParent(GameObject.Find("UIRoot/Top").transform,false);

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
