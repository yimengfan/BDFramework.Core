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
[UI((int)WinEnum.Win_Demo5_Atlas,"Windows/window_demo5_SpriteAtlas") ]
public class Window_Demo5 : AWindow
{
 

    public Window_Demo5(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        //01按钮
       
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
