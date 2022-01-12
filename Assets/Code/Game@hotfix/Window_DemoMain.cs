using System.Collections.Generic;
using BDFramework.Sql;
using UnityEngine;
using BDFramework.UI;
using LitJson;
using UnityEngine.UI;
using BDFramework;
using BDFramework.DataListener;
using BDFramework.Hotfix.ScreenView;
using BDFramework.ResourceMgr;
using BDFramework.UFlux;
using BDFramework.VersionContrller;
using Game;
using Game.Data.Local;
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
        var service = StatusListenerServer.Create(nameof(DataListenerEnum));
        service.AddListener(DataListenerEnum.test, (o) => { Debug.Log(o.ToString()); });
    }


    [ButtonOnclick("Grid/btn_1")]
    private void btn_01()
    {
        ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Demo1);
    }

    [ButtonOnclick("Grid/btn_4")]
    private void btn_04()
    {
        //测试多个接口
        var list = new List<int>() {(int) WinEnum.Win_UFlux};
        UIManager.Inst.AsyncLoadWindows(list, (i, j) =>
        {
            //log
            BDebug.LogFormat("加载进度:{0}-{1}", i, j);
            UIManager.Inst.ShowWindow(WinEnum.Win_UFlux);
        });
    }

    [ButtonOnclick("Grid/btn_5")]
    private void btn_05()
    {
        //单条件查询
        Debug.Log("普通查询：");
        var hero = SqliteHelper.DB.GetTableRuntime().Where("id = {0}", 1).FromAll<Hero>();
        Debug.Log(JsonMapper.ToJson(hero));

        //多条件查询
        Debug.Log("OR And查询：");
        var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").And.Where("id < 3").FromAll<Hero>();
        foreach (var d in ds)
        {
            Debug.Log(JsonMapper.ToJson(d));
        }

        //批量查询
        ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").Or.Where("id = 3").FromAll<Hero>();
        foreach (var d in ds)
        {
            Debug.Log(JsonMapper.ToJson(d));
        }

        //
        Debug.Log("Where or 批量查询：");
        ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).FromAll<Hero>();
        foreach (var d in ds)
        {
            Debug.Log(JsonMapper.ToJson(d));
        }

        //批量查询
        Debug.Log("Where and 批量查询：");
        ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).FromAll<Hero>();
        foreach (var d in ds)
        {
            Debug.Log(JsonMapper.ToJson(d));
        }
    }

    [ButtonOnclick("Grid/btn_6")]
    private void btn_06()
    {
        List<GameObject> golist = new List<GameObject>();
        //1.同步加载
        var go = BResources.Load<GameObject>("AssetTest/Cube");
        if (go)
        {
            var load1 = GameObject.Instantiate(go);
            golist.Add(load1);
        }

        go = BResources.Load<GameObject>("Test/Cube");
        if (go)
        {
            var load2 = GameObject.Instantiate(go);
            golist.Add(load2);
        }

        go = BResources.Load<GameObject>("AssetTest/Particle");
        if (go)
        {
            var load3 = GameObject.Instantiate(go);
            golist.Add(load3);
        }

        go = BResources.Load<GameObject>("Char/001");
        if (go)
        {
            var loadModel = GameObject.Instantiate(go);
            golist.Add(loadModel);
        }

        //2.异步加载单个
        var id = BResources.AsyncLoad<GameObject>("Test/Cube", (o) =>
        {
            if (o)
            {
                var load4 = GameObject.Instantiate(o);
                golist.Add(load4);
            }
        });

        //3.异步加载多个
        var list = new List<string>() {"AssetTest/Cube", "Test/Cube"};
        BResources.AsyncLoad(list, (i, i2) =>
        {
            //进度
            Debug.Log(string.Format("进度 {0} / {1}", i, i2));
        }, (map) =>
        {
            BDebug.Log("加载全部完成,资源列表:");
            foreach (var r in map)
            {
                BDebug.Log(string.Format("--> {0} ： {1}", r.Key, r.Value.name));
                if (r.Value)
                {
                    var _go = GameObject.Instantiate(r.Value);
                    golist.Add(_go as GameObject);
                }
            }
        });


        IEnumeratorTool.WaitingForExec(5, () =>
        {
            foreach (var _go in golist)
            {
                GameObject.Destroy(_go);
            }
        });
    }

    [ButtonOnclick("Grid/btn_7")]
    private void btn_07()
    {
        var path = Application.persistentDataPath;
        //开始下载
        AssetsVersionContrller.Start(UpdateMode.Repair, "http://127.0.0.1", null,
            (idx, totalNum) => { Debug.LogFormat("资源更新进度：{0}/{1}", idx, totalNum); }, //进度通知
            (status, msg) =>
            {
                //错误通知
                Debug.LogError("结果:" + status + " - " + msg);
            });
    }

    [ButtonOnclick("Grid/btn_8")]
    private void btn_08()
    {
        DemoEventManager.Inst.Do(DemoEventEnum.TestEvent2);
    }

    [ButtonOnclick("Grid/btn_9")]
    private void btn_09()
    {
        UIManager.Inst.CloseWindow(WinEnum.Win_Main);
        UIManager.Inst.LoadWindow(WinEnum.Win_Demo5_Atlas);
        UIManager.Inst.ShowWindow(WinEnum.Win_Demo5_Atlas);
    }

    [ButtonOnclick("Grid/btn_10")]
    private void btn_10()
    {
        UIManager.Inst.LoadWindow(WinEnum.Win_Demo_Datalistener);
        UIManager.Inst.ShowWindow(WinEnum.Win_Demo_Datalistener);
    }
}
