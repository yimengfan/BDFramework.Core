using System.Collections;
using System.Collections.Generic;
using BDFramework.ScreenView;
using UnityEngine;
using BDFramework.UI;
using DG.Tweening;
using UnityEngine.UI;
using BDFramework;
using BDFramework.UFlux;

//using UnityEditor.Graphs;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int)WinEnum.Win_Demo5_Atlas,"Windows/window_demo5_SpriteAtlas") ]
public class Window_Demo5 : AWindow
{

    [ButtonOnclick("btn_Close")]
    private void btn_close()
    {
        this.Close();
        UIManager.Inst.ShowWindow(WinEnum.Win_Main);
    }

    public Window_Demo5(string path) : base(path)
    {
    }

   
    




   
}