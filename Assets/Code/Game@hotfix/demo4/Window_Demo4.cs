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
[UI((int)WinEnum.Win_Demo4,"Windows/window_demo4") ]
public class Window_Demo4 : AWindow
{

    [TransformPath("Button")] 
    private Button button_back;
    [TransformPath("btn_01")] 
    private Button btn_01;
    
    [TransformPath("btn_02")] 
    private Button btn_02;
    public Window_Demo4(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        //01按钮
        button_back.onClick.AddListener(() =>
        {
            this.Close();
        });
        
        //02：自动赋值按钮
        btn_01.onClick.AddListener(() =>
        {
            var data = new TestData()
            {
                 test12 = "清明上河图,",
                test23 = "锄禾日当午",
                name1213 = "拖拽Tag式发现节点"
            };
            UITools.AutoSetComValue(this.Transform.Find("AutoSetValue") ,data);
        });
        
        //02：自动赋值按钮
        btn_02.onClick.AddListener(() =>
        {
            var data = new TestData2()
            {
                test12 = "Hello World,",
                test23 = "Hello Kitty",
                name1213 = "全代码配置式发现节点"
            };
            UITools.AutoSetComValue(this.Transform.Find("AutoSetValue2") ,data);
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


    //赋值方式1 数据
    public class TestData
    {
        [Component("test1",ComponentType.Text,"text")]
        public string test12;
        [Component("test2",ComponentType.Text,"text")]
        public string test23;
        [Component("name",ComponentType.Text,"text")]
        public string name1213;
        [Component("name2",ComponentType.Text,CustomField.GameObjectActive)]
        public string name1;
        [Component("test001", ComponentType.Text, CustomField.ComponentEnable)]
        public string name2;
    }
        
    
    // 赋值方式2数据
    public class TestData2
    {
        //节点名
        [TransformPath("Text3")]
        [Component("test1",ComponentType.Text,"text")]
        public string test12;
        
        //节点名
        [TransformPath("Text1")]
        [Component("test2",ComponentType.Text,"text")]
        public string test23;
        
        //节点名
        [TransformPath("1/2/3/4/Text4")]
        [Component("name",ComponentType.Text,"text")]
        public string name1213;
    }
}
