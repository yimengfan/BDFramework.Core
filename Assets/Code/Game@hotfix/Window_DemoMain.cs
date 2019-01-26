using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BDFramework.ScreenView;
using BDFramework.Sql;
using UnityEngine;
using BDFramework.UI;
using Game.Data;
using LitJson;
using UnityEngine.UI;
using BDFramework;
using BDFramework.ResourceMgr;
using BDFramework.VersionContrller;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int) WinEnum.Win_Main, "Windows/window_demoMain")]
public class Window_DemoMain : AWindow
{
    [TransformPath("text_hotfixState")] private Text text_hotfixState;

    [TransformPath("btn_1")] private Button btn_01;
    [TransformPath("btn_2")] private Button btn_02;

    [TransformPath("btn_3")] private Button btn_03;
    [TransformPath("btn_4")] private Button btn_04;
    [TransformPath("btn_5")] private Button btn_05;
    [TransformPath("btn_6")] private Button btn_06;

    [TransformPath("btn_7")] private Button btn_07;

    //[]
    public Window_DemoMain(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();

        //提示
//        var isCodeHotfix = GameObject.Find("BDFrame").GetComponent<BDLauncher>().IsCodeHotfix;
//        text_hotfixState.text = isCodeHotfix ? "热更模式:开" : "热更模式:关";

        //demo1： screenview 切换
        this.btn_01.onClick.AddListener(() => { ScreenViewManager.Inst.MainLayer.BeginNavTo("demo1"); });

        //demo2： ui window基本操作
        this.btn_02.onClick.AddListener(() =>
        {
            ScreenViewManager.Inst.MainLayer.BeginNavTo("demo2");

            //向demo2窗口发消息
            var d = WindowData.Create("rotation");
            d.AddData("rotation", UnityEngine.Random.Range(-359, 359));
            UIManager.Inst.SendMessage((int) WinEnum.Win_Demo2, d);
        });

        //demo3： uimvc模式
        this.btn_03.onClick.AddListener(() => { ScreenViewManager.Inst.MainLayer.BeginNavTo("demo3"); });

        //demo4: uitools使用
        this.btn_04.onClick.AddListener(() =>
        {
            UIManager.Inst.LoadWindows((int) WinEnum.Win_Demo4);
            UIManager.Inst.ShowWindow((int) WinEnum.Win_Demo4);
        });
        //demo5： sqlite 查询
        this.btn_05.onClick.AddListener(() =>
        {
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").ToSearch<Hero>();

            foreach (var d in ds)
            {
                Debug.Log(JsonMapper.ToJson(d));
            }
        });
        //demo6：资源加载
        this.btn_06.onClick.AddListener(() =>
        {
            //1.同步加载
            var go = BResources.Load<GameObject>("Windows/window_demo1");

            //2.异步加载单个
            var id = BResources.AsyncLoad<GameObject>("Windows/window_demo1", (b, o) => { });
//            //取消任务
//            BResources.LoadCancel(id);6
//            
//          //3.异步加载多个
            BResources.AsyncLoad(new List<string>() {"Windows/window_demo1", "Windows/window_demo2"},
                (i, i2) => { Debug.Log(string.Format("进度 {0} / {1}", i, i2)); }, (map) =>
                {
                    BDebug.Log("加载全部完成,资源列表:");
                    foreach (var r in map)
                    {
                        BDebug.Log(string.Format("--> {0} ： {1}", r.Key, r.Value.name));
                        GameObject.Instantiate(r.Value);
                    }
                });
        });

        this.btn_07.onClick.AddListener(() =>
        {
            var path = Application.streamingAssetsPath;
        
            VersionContorller.Start("http://127.0.0.1", path,
            (i, j) =>
            {
                Debug.LogFormat("资源更新进度：{0}/{1}", i, j);
            },
            (error) =>
            {
                Debug.LogError("错误:" + error);
            });
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