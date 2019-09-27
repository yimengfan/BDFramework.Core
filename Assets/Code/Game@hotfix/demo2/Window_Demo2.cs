using System.Collections;
using System.Collections.Generic;
using BDFramework.ScreenView;
using UnityEngine;
using BDFramework.UI;
using DG.Tweening;
using UnityEngine.UI;
using BDFramework;
//using UnityEditor.Graphs;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int)WinEnum.Win_Demo2,"Windows/window_demo2") ]
public class Window_Demo2 : AWindow
{

    [TransformPath("Button")] 
    private Button btn_01;
    [TransformPath("002/Button")] 
    private Button btn_02;
    
    public Window_Demo2(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        
        //添加子窗口
        var subwin =  new SubWindow_Demo2(this.Transform.Find("SubWindow"));
        subwin.Init();
        
        this.AddSubWindow("subwin1",subwin);
        //注册消息监听
        RegisterAction("rotation", OnMsg_Rotation);
        //01按钮
        btn_01.onClick.AddListener(() =>
        {
            this.Close();
            ScreenViewManager.Inst.MainLayer.BeginNavTo("main");
        });
        
        //02按钮
        btn_02.onClick.AddListener(() =>
        {
            var data = WindowData.Create("testkey");
            data.AddData("testkey","testvalue");
            this.OpenSubWindow("subwin1",data);
        });
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


    public void OnMsg_Rotation(WindowData o)
    {
        float value = o.GetData<int>("rotation");
        BDebug.Log("监听到rotation ：" +  value , "yellow");
        var trans = this.Transform.Find("001/Image");
        trans.DOKill();

        //
        trans.DOLocalRotate(trans.localEulerAngles + new Vector3(0f, 0f, value), 3, RotateMode.WorldAxisAdd).SetEase( Ease.Linear);
    }
}
