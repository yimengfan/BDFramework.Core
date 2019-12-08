using System;
using System.Collections;
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
using Boo.Lang;
using Code.Game;
using Code.Game.demo_EventManager;
using Code.Game.demo6_UFlux;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int) WinEnum.Win_Main, "Windows/window_demoMain")]
public class Window_DemoMain : AWindow
{
    [TransformPath("text_hotfixState")]
    private Text text_hotfixState;

    [TransformPath("btn_1")]
    private Button btn_01;

    [TransformPath("btn_4")]
    private Button btn_04;

    [TransformPath("btn_5")]
    private Button btn_05;

    [TransformPath("btn_6")]
    private Button btn_06;

    [TransformPath("btn_7")]
    private Button btn_07;

    [TransformPath("btn_8")]
    private Button btn_08;

    [TransformPath("btn_9")]
    private Button btn_09;

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
        //代码:
        //Game@hotfix/demo1
        this.btn_01.onClick.AddListener(() =>
        {
            ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Demo1);
        });
        
        //demo4 ： uflux窗口
        //代码:
        this.btn_04.onClick.AddListener(() =>
        {
            BDFramework.UFlux.UIManager.Inst.LoadWindow(UFluxWindowEnum.UFluxDemoMain);
            BDFramework.UFlux.UIManager.Inst.ShowWindow(UFluxWindowEnum.UFluxDemoMain);
        });


        //demo5： sqlite 查询
        this.btn_05.onClick.AddListener(() =>
        {
            //单条件查询
            Debug.Log("普通查询：");
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").ToSearch<Hero>(); 
            ds = SqliteHelper.DB.GetTableRuntime().Where("id = {0}",1).ToSearch<Hero>();
            foreach (var d in ds) Debug.Log(JsonMapper.ToJson(d));
            //多条件查询
            Debug.Log("多条件查询：");
            ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").Where("and id < 3").ToSearch<Hero>();
            foreach (var d in ds) Debug.Log(JsonMapper.ToJson(d));
            //批量查询
            Debug.Log("Where or 批量查询：");
            ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).ToSearch<Hero>();
            foreach (var d in ds) Debug.Log(JsonMapper.ToJson(d));
            //批量查询
            Debug.Log("Where and 批量查询：");
            ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).ToSearch<Hero>();
            foreach (var d in ds) Debug.Log(JsonMapper.ToJson(d));
            
        });
        //demo6：资源加载
        this.btn_06.onClick.AddListener(() =>
        {
            //1.同步加载
            var go = BResources.Load<GameObject>("Test/Cube");
            GameObject.Instantiate(go).name = "load";

            //2.异步加载单个
            var id = BResources.AsyncLoad<GameObject>("Windows/window_demo1", (o) => { });

            //3.异步加载多个
            var list = new System.Collections.Generic.List<string>() {"Windows/window_demo1", "Windows/window_demo2"};
            BResources.AsyncLoad(list,
                (i, i2) => { Debug.Log(string.Format("进度 {0} / {1}", i, i2)); },
                (map) =>
                {
                    BDebug.Log("加载全部完成,资源列表:");
                    foreach (var r in map)
                    {
                        BDebug.Log(string.Format("--> {0} ： {1}", r.Key, r.Value.name));
                        GameObject.Instantiate(r.Value);
                    }
                });
        });

        //代码:
        //Game@hotfix/demo_Manager_AutoRegister_And_Event
        this.btn_07.onClick.AddListener(() =>
        {
            var path = Application.persistentDataPath;

            VersionContorller.Start(UpdateMode.Repair, "http://127.0.0.1", path,
                (i, j) => { Debug.LogFormat("资源更新进度：{0}/{1}", i, j); },
                (error) => { Debug.LogError("错误:" + error); });
        });


        //发送消息机制
        this.btn_08.onClick.AddListener(() => { DemoEventManager.Inst.Do(DemoEventEnum.TestEvent2); });

        //图集
        this.btn_09.onClick.AddListener(() =>
        {
            UIManager.Inst.CloseWindow(WinEnum.Win_Main);
            UIManager.Inst.LoadWindows((int) WinEnum.Win_Demo5_Atlas);
            UIManager.Inst.ShowWindow((int) WinEnum.Win_Demo5_Atlas);
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