using BDFramework.ScreenView;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Game.Windows
{
    public class View_MVCTest : AViewBase
    {
        [TransformPath("testButton")]
        private Button testButton;
        //
        [TransformPath("testSlider")]
        private Slider testSlider;
        //
        [TransformPath("testScrollbar")]
        private Scrollbar testScrollBar;
                
        [TransformPath("text_click")]
        private Text text_click;
        
        
        //
        [TransformPath("text_sliderValue")]
        private Text text_sliderValue;
        //
        [TransformPath("text_scrollBarValue")]
        [BindModel("ScrollBarValue")]
        private Text text_ScrollBarValue;


        [TransformPath("Button")]
        private Button btn_back;
        public View_MVCTest(Transform t, DataDriven_Service service) : base(t, service)
        {
            
        }
        
        public override void BindModel()
        {
            base.BindModel();

            //手动注册
            this.Model.AddData("ClickCount");
            this.Model.AddListener("ClickCount",
            (value) =>
            {
                this.text_click.text = "点 击 次 数：" + value;
            });

            //手动注册
            this.Model.AddData("SliderValue");
            this.Model.AddListener("SliderValue",
            (value) =>
            {
                this.text_sliderValue.text = "Slider Value：" + value;
            });
            
            //已用标签自动注册
            this.Model.AddListener("ScrollBarValue",
            (value) =>
            {
                this.text_ScrollBarValue.text = "ScrollBarValue ：" + value;
            });
            
            //
            btn_back.onClick.AddListener(() =>
            {
                ScreenViewManager.Inst.MainLayer.BeginNavTo("main");
            });
        }
    }
}