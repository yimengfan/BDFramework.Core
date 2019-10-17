using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Game.demo6_UFlux
{
    
    [UI((int) UFluxWindowEnum.UFluxDemoMain,"Windows/UFlux/Window_FluxMain")]
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
        public override void Init()
        {
            base.Init();
            
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
            });
            
            //demo2.窗口和自定义组件赋值
            btn_02.onClick.AddListener(() =>
            {
                Debug.Log("Flux demo2,点击这里追踪代码!");
                UIManager.Inst.LoadWindows(UFluxWindowEnum.UFluxTest002);
            });
            
            btn_03.onClick.AddListener(() =>
            {
                UIManager.Inst.LoadWindows(UFluxWindowEnum.UFluxTest005);
            });
            
        }
    }
}