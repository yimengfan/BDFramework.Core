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
using BDFramework.DataListener;
using BDFramework.ResourceMgr;
using BDFramework.UFlux;
using BDFramework.VersionContrller;
using Game;
using Game.demo_EventManager;
using Game.demo6_UFlux;

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

    [TransformPath("Grid/btn_1")]
    private Button btn_01;

    [TransformPath("Grid/btn_4")]
    private Button btn_04;

    [TransformPath("Grid/btn_5")]
    private Button btn_05;

    [TransformPath("Grid/btn_6")]
    private Button btn_06;

    [TransformPath("Grid/btn_7")]
    private Button btn_07;

    [TransformPath("Grid/btn_8")]
    private Button btn_08;

    [TransformPath("Grid/btn_9")]
    private Button btn_09;

    [TransformPath("Grid/btn_10")]
    private Button btn_10;
    //[]
    public Window_DemoMain(string path) : base(path)
    {
    }

    public enum DataListenerEnum
    {
        test,
    }

    public override void Init()
    {
        base.Init();

        //增加覆盖测试
        var service = DataListenerServer.Create(nameof(DataListenerEnum));
        service.AddListener(DataListenerEnum.test, (o) =>
        {
            Debug.Log(o.ToString());
        });
        
        

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
            //测试多个接口
            var list = new List<WinEnum>(){WinEnum.Win_Demo6};
            UIManager.Inst.LoadWindows(list);
            UIManager.Inst.ShowWindow(WinEnum.Win_Demo6);
            BDebug.Log("加载成功!");
            //
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
            List<GameObject> golist = new List<GameObject>();
            //1.同步加载
            var go = BResources.Load<GameObject>("AssetTest/Cube");
             var load1 =GameObject.Instantiate(go);
             go = BResources.Load<GameObject>("Test/Cube");
             var load2 =GameObject.Instantiate(go);
             go = BResources.Load<GameObject>("AssetTest/Particle");
             var load3 =GameObject.Instantiate(go);

             golist.Add(load1);
             golist.Add(load2);
             golist.Add(load3);
             
            //2.异步加载单个
            var id = BResources.AsyncLoad<GameObject>("Test/Cube", (o) =>
            {
                var load4 = GameObject.Instantiate(o);
                golist.Add(load4);
            });

            //3.异步加载多个
            var list = new List<string>() {"AssetTest/Cube", "Test/Cube"};
            BResources.AsyncLoad(list,
                (i, i2) =>
                {
                    Debug.Log(string.Format("进度 {0} / {1}", i, i2));
                },
                (map) =>
                {
                    BDebug.Log("加载全部完成,资源列表:");
                    foreach (var r in map)
                    {
                        BDebug.Log(string.Format("--> {0} ： {1}", r.Key, r.Value.name));
                        var _go =  GameObject.Instantiate(r.Value) as GameObject;
                        golist.Add(_go);
                    }
                });
            
            
            IEnumeratorTool.WaitingForExec(5, () =>
            {
                foreach (var _go in golist)
                {
                    GameObject.Destroy(_go);
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
            UIManager.Inst.LoadWindow( WinEnum.Win_Demo5_Atlas);
            UIManager.Inst.ShowWindow(WinEnum.Win_Demo5_Atlas);
        });
        
        
        //数据监听
        this.btn_10.onClick.AddListener(() =>
        {
            UIManager.Inst.LoadWindow( WinEnum.Win_Demo_Datalistener);
            UIManager.Inst.ShowWindow(WinEnum.Win_Demo_Datalistener);
        });
    }
    
}