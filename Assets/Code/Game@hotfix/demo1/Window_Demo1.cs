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
[UI((int)WinEnum.Win_Demo1,"Windows/window_demo1") ]
public class Window_Demo1 : AWindow
{
    [TransformPath("Button")]
    private Button btn_01;


    
    //[]
    public Window_Demo1(string path) : base(path)
    {
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


//        var effects = this.Transform.Find("effect");
//        var shader = Resources.Load<Shader>("builtin/default/PSU");
//
//        var renders = effects.GetComponentsInChildren<Renderer>();
//
//        foreach (var r in renders)
//        {
//            foreach (var m in r.materials)
//            {
//                //
//                Debug.Log(string.Format("{0} - shader:{1}",r.name,m.shader.name));
//                //
//                if (m.shader.name == "Particles/Standard Unlit")
//                {
////                    m.shader = null;
////                    m.shader = null;
// 
//                     Resources.UnloadAsset(m.shader);
//                     m.shader = null;
//                     m.shader = shader;
//                    
//                    BDebug.Log("替换材质成功:" + r.name);
//                }
//            }
//          
//        }

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
