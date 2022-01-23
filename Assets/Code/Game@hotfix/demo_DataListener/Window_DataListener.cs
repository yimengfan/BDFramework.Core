using System;
using BDFramework.DataListener;
using BDFramework.UFlux;
using UnityEngine;
using BDFramework.UI;
using DotNetExtension;
using Game;
using Game.Demo;
using Game_hotfix.demo_DataListener;
using UnityEngine.UI;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int) WinEnum.Win_Demo_Datalistener, "Windows/Window_DataListener")]
public class Window_DataListener : AWindow
{
    [TransformPath("btn_Close")]
    private Button btn_Close;

    [TransformPath("Grid/btn_1")]
    private Button btn_1;

    [TransformPath("Grid/btn_2")]
    private Button btn_2;

    [TransformPath("Grid/btn_3")]
    private Button btn_3;

    [TransformPath("Grid/btn_4")]
    private Button btn_4;

    [TransformPath("Grid/btn_5")]
    private Button btn_5;

    [TransformPath("Grid/btn_6")]
    private Button btn_6;
    [TransformPath("text_message")]
    private Text text_message;

    //[]
    public Window_DataListener(string path) : base(path)
    {
    }


    public enum Msg_Test001
    {
        Msg1,
        Msg2
    }

    public class Msg_ParamTest
    {
        public int test1 = 1;
        public int test2 = 2;
    }

    public override void Init()
    {
        base.Init();
        //
        btn_Close.onClick.AddListener(() => { this.Close(); });


        btn_1.onClick.AddListener(() =>
        {
            //演示两种方法监听
            var s2 = StatusListenerServer.Create(nameof(Msg_Test001.Msg2));
            //主动传递参数
            s2.AddListener<Msg_ParamTest>(Msg_Test001.Msg2, triggerNum: 10, action: (o) =>
            {
                //每次自增
                Debug.Log("直接接受类型 p1 :" + o.test1);
                Debug.Log("直接接受类型 p2 :" + o.test2);
            });
            //默认传的都是object
            s2.AddListener(Msg_Test001.Msg2, triggerNum: 10, action: (o) =>
            {
                var _o = o as Msg_ParamTest;
                //每次自增
                Debug.Log("param1:" + _o.test1);
                Debug.Log("param2:" + _o.test2);
            });

            s2.TriggerEvent(Msg_Test001.Msg2, new Msg_ParamTest());
            StatusListenerServer.DelService(nameof(Msg_Test001.Msg2));

            //1.创建数据监听服务
            var service = StatusListenerServer.Create(nameof(Msg_Test001));
            text_message.text = "创建service成功:" + nameof(Msg_Test001);
        });

        btn_2.onClick.AddListener(() =>
        {
            //获取监听对象 并且注册
            var service = StatusListenerServer.GetService(nameof(Msg_Test001));
            //添加数据

            //永久监听
            int count = 0;
            //带类型参数监听
            service.AddListener<string>(Msg_Test001.Msg1, (o) =>
            {
                count++;
                text_message.text = string.Format("监听到消息:{0}  次数：{1}", o, count);
            });

            //监听1次
            //默认object监听
            service.AddListenerOnce(Msg_Test001.Msg1, (o) => { Debug.Log("监听消息1次，并移除：" + o); });

            text_message.text = "监听成功:" + nameof(Msg_Test001.Msg1);
        });

        btn_3.onClick.AddListener(() =>
        {
            //创建数据监听服务
            var service = StatusListenerServer.Create(nameof(Msg_Test001));

            service.TriggerEvent(Msg_Test001.Msg1, DateTime.Now.ToShortTimeString());
        });

        //自定义类型Value
        btn_4.onClick.AddListener(() =>
        {
            text_message.text = "自定义Value类型 Service 已经创建，查看代码";
            var s1 = StatusListenerServer.Create<ADataListenerT<float>>("x1");
            var s2 = StatusListenerServer.Create<ADataListenerT<string>>("x2");
            //获取
            var s3 = StatusListenerServer.GetService<ADataListenerT<float>>("x1");
        });


        //监听主工程对象
        var service5 = StatusListenerServer.Create(nameof(M_StatusListenerTest));
        service5.AddListener<M_StatusListenerTest>((msg) =>
        {
            //
            text_message.text = msg.TestStr;
            Debug.Log("收到主工程对象改变消息!");
        });
        //发送事件
        btn_5.onClick.AddListener(() =>
        {
            var msg = new M_StatusListenerTest();
            msg.TestStr = "测试监听主工程对象 : " + DateTimeEx.GetTotalSeconds();
            service5.TriggerEvent(msg);
        });
        
        
        
        //监听热更对象
        var service6 = StatusListenerServer.Create(nameof(StatusListenerTest));
        service6.AddListener<StatusListenerTest>((msg) =>
        {
            //
            text_message.text = msg.TestStr;
            Debug.Log("收到热更对象改变消息!");
        });
        //发送事件
        btn_6.onClick.AddListener(() =>
        {
            var msg = new StatusListenerTest();
            msg.TestStr = "测试监听热更工程对象 : " + DateTimeEx.GetTotalSeconds();
            service6.TriggerEvent(msg);
        });
    }
}
