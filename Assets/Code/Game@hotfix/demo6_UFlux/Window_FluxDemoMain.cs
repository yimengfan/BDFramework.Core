using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.demo6_UFlux
{
    
    [UI((int) WinEnum.Win_Demo6,"Windows/UFlux/Window_FluxMain")]
    public class Window_FluxDemoMain : AWindow<PropsBase>
    {
        public Window_FluxDemoMain(string path) : base(path)
        {
        }

        [TransformPath("btn_01")]
        private Button btn_01;
        [TransformPath("btn_02")]
        private Button btn_02;

        [TransformPath("btn_03")] 
        private Button btn_03;
        [TransformPath("btn_04")] 
        private Button btn_04;
        [TransformPath("btn_05")] 
        private Button btn_05;
        [TransformPath("btn_06")] 
        private Button btn_06;
        [TransformPath("btn_close")] 
        private Button btn_close;
        public override void Init()
        {
            base.Init();
            //
            btn_close.onClick.AddListener((() =>
            {
                this.Close();
            }));
            //测试Component同步加载
            btn_01.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo1,点击这里追踪代码!");
                
                //可以F12查看Test01Component的代码
                var com = new Component_Test001();
                //这里是同步加载 
                if (com.IsLoad)
                {
                    com.Transform.SetParent(this.Transform,false);
                    com.Open();
                }
                else
                {
                    com.AsyncLoad(() =>
                    {
                        BDebug.Log("加载完成");
                    });
                }
                
            });
            
            //demo2.窗口和自定义组件赋值
            btn_02.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo2,点击这里追踪代码!");
                UIManager.Inst.LoadWindow( WinEnum.Win_Demo6_Test002);
                UIManager.Inst.ShowWindow( WinEnum.Win_Demo6_Test002);
            });
            //3.自定义逻辑注册
            btn_03.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo3,点击这里追踪代码!");
                UIManager.Inst.LoadWindow(WinEnum.Win_Demo6_Test003);
                UIManager.Inst.ShowWindow(WinEnum.Win_Demo6_Test003);
            });
            //4.窗口,子窗口
            btn_04.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo4,点击这里追踪代码!");
                UIManager.Inst.LoadWindow(WinEnum.Win_Demo6_Test004);
                UIManager.Inst.ShowWindow(WinEnum.Win_Demo6_Test004);
            });
            
            //5.普通窗口 Props
            btn_05.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo5,点击这里追踪代码!");
                UIManager.Inst.LoadWindow(WinEnum.Win_Demo6_Test005);
                UIManager.Inst.ShowWindow(WinEnum.Win_Demo6_Test005);
            });
            
            //6.普通窗口 reducer
            btn_06.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo6,点击这里追踪代码!");
                UIManager.Inst.LoadWindow(WinEnum.Win_Demo6_Test006);
                UIManager.Inst.ShowWindow(WinEnum.Win_Demo6_Test006);
            });
        }
    }
}