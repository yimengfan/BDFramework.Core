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
public class SubWindow_Demo2 : SubWindow
{

    [TransformPath("Button")] 
    private Button btn_01;


    [TransformPath("text_msg")]
    private Text text_msg;
    
    
    public SubWindow_Demo2(Transform transform) : base(transform)
    {
    }
    
    public override void Init()
    {
        base.Init();
        //01按钮
        btn_01.onClick.AddListener(() =>
        {
            this.Close();
           
        });
        
        //02按钮

    }

    public override void Close()
    {
        base.Close();
    }

    public override void Open(WindowData data = null)
    {
        base.Open();
        if (data != null)
        {
            foreach (var v in data.DataMap)
            {
                text_msg.text = v.Key.ToString() + ":" + v.Value.ToString();
            }
        }
    }

    public override void Destroy()
    {
        base.Destroy();
    }


}
