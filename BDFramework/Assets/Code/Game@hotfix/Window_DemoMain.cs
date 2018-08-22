using System.Collections;
using System.Collections.Generic;
using BDFramework.ScreenView;
using BDFramework.Sql;
using UnityEngine;
using BDFramework.UI;
using Game.Data;
using LitJson;
using UnityEngine.UI;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int)WinEnum.Win_Main,"Windows/window_DemoMain") ]
public class Window_DemoMain : AWindow
{
    [TransformPath("text_hotfixState")]
    private Text text_hotfixState;
    
    [TransformPath("btn_1")]
    private Button btn_01;
    [TransformPath("btn_2")]
    private Button btn_02;

    [TransformPath("btn_3")]
    private Button btn_03;
    [TransformPath("btn_4")]
    private Button btn_04;
    [TransformPath("btn_5")]
    private Button btn_05;
    //[]
    public Window_DemoMain(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        
        //提示
        var isCodeHotfix = GameObject.Find("BDFrame").GetComponent<BDLauncher>().IsCodeHotfix;
        text_hotfixState.text = isCodeHotfix ? "热更模式:开" : "热更模式:关";
        
        //demo1： screenview 切换
        this.btn_01.onClick.AddListener(() =>
        {
            ScreenViewManager.Inst.MainLayer.BeginNavTo("demo1");
        });
            
        //demo2： ui window基本操作
        this.btn_02.onClick.AddListener(() =>
        {
            ScreenViewManager.Inst.MainLayer.BeginNavTo("demo2");
            
            //向demo2窗口发消息
            var d = WindowData.Create();
            d.AddData("rotation", UnityEngine.Random.Range(-359, 359));
            UIManager.Inst.SendMessage((int) WinEnum.Win_Demo2, d);
        });
        
        //demo3： uimvc模式
        this.btn_03.onClick.AddListener(() =>
        {
            ScreenViewManager.Inst.MainLayer.BeginNavTo("demo3");
        });
        
        //demo4: uitools使用
        this.btn_04.onClick.AddListener(() =>
        {
            UIManager.Inst.LoadWindows((int)WinEnum.Win_Demo4);
            UIManager.Inst.ShowWindow((int)WinEnum.Win_Demo4);
        });
        //demo5： sqlite 查询
        this.btn_05.onClick.AddListener(() =>
        {
            var ds = SqliteHelper.DB.GetTableRuntime<Hero>().Where("id > 1").ToSearch();
            foreach (var d in ds)
            {
             Debug.Log( JsonMapper.ToJson(d) );
            }
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
}
