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
[UI((int)WinEnum.Win_Test2,"Windows/window_test2") ]
public class Window_Test2 : AWindow 
{
    public Window_Test2(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        
        this.Transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
        {
            this.Close();
            ScreenViewManager.Inst.MainLayer.BeginNavTo("demo3");
        });
        
        //注册消息监听
        RegisterAction("rotation", OnRecive_Rotation);
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


    public void OnRecive_Rotation(object o)
    {
        float value = (int)o;
        BDebug.Log("监听到rotation ：" +  value , "yellow");
        var trans = this.Transform.Find("Image");
        trans.DOKill();

        //
        trans.DOLocalRotate(trans.localEulerAngles + new Vector3(0f, 0f, value), 3, RotateMode.WorldAxisAdd).SetEase( Ease.Linear);
    }
}
