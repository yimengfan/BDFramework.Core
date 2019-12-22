using System;
using System.Collections;
using System.Collections.Generic;
using BDFramework.DataListener;
using BDFramework.ResourceMgr;
using BDFramework.ScreenView;
using BDFramework.UFlux;
using UnityEngine;
using BDFramework.UI;
using Code.Game;
using UnityEngine.UI;

/// <summary>
/// 这个是ui的标签，
/// index 
/// resource 目录
/// </summary>
[UI((int)WinEnum.Win_Demo_Datalistener,"Windows/Window_DataListener") ]
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
    
    public override void Init()
    {
        base.Init();
        //
        btn_Close.onClick.AddListener(() =>
        {
           this.Close();
        });
        
        
        btn_1.onClick.AddListener(() =>
        {
            //创建数据监听服务
            var service = DataListenerServer.Create(nameof(Msg_Test001));
            text_message.text="创建service成功:" +nameof(Msg_Test001);
        });

        btn_2.onClick.AddListener(() =>
        {
            //获取监听对象 并且注册
            var service = DataListenerServer.GetService(nameof(Msg_Test001));
            //添加数据
            service.AddData(Msg_Test001.Msg1);
            
            //永久监听
            int count = 0;
            service.AddListener(Msg_Test001.Msg1, (o) =>
            {
                count++;
                text_message.text = string.Format("监听到消息:{0}  次数：{1}" , o,count);
                
            });

            //监听1次
            service.AddListenerOnce(Msg_Test001.Msg1, (o) =>
            {
                Debug.Log( "监听消息1次，并移除：" +  o);
            });
            
            text_message.text="监听成功:" +nameof(Msg_Test001.Msg1);
        });
        
        btn_3.onClick.AddListener(() =>
        {
            //创建数据监听服务
            var service = DataListenerServer.Create(nameof(Msg_Test001));
            
            service.TriggerEvent(Msg_Test001.Msg1,DateTime.Now.ToShortTimeString());
        });
        
        //自定义类型Value
        btn_4.onClick.AddListener(() =>
        {
            text_message.text="自定义Value类型 Service 已经创建，查看代码";
            var s1 = DataListenerServer.Create<ADataListenerT<float>>("x1");
            var s2 = DataListenerServer.Create<ADataListenerT<string>>("x2");
            //获取
            var s3 = DataListenerServer.GetService<ADataListenerT<float>>("x1");
        });
          
    }

}
