using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Game.Windows
{
    public class View_MVCTest : AViewBase
    {
        [SetTransform("testButton")]
        private Button testButton;
        //
        [SetTransform("testSlider")]
        private Slider testSlider;
        //
        [SetTransform("testScrollbar")]
        private Scrollbar testScrollBar;
                
        [SetTransform("testButtonAutoSetValue")]
        private Button testButtonAutoSetValue;
        //
        [SetTransform("text_click")]
        [BindData("ClickCount")]
        private Text text_click;
        
        
        //
        [SetTransform("text_sliderValue")]
        private Text text_sliderValue;
        //
        [SetTransform("text_scrollBarValue")]
        [BindData("ScrollBarValue")]
        private Text text_ScrollBarValue;
        
        public View_MVCTest(Transform t, DataDrive_Service service) : base(t, service)
        {
            
        }
        
        public override void BindModel()
        {
            base.BindModel();

//            this.Model.RegAction("ClickCount",
//            (value) =>
//            {
//                this.text_click.text = "点 击 次 数：" + value;
//            });

            this.Model.RegAction("SliderValue",
            (value) =>
            {
                this.text_sliderValue.text = "Slider Value：" + value;
            });
//            
//            this.Model.RegAction("ScrollBarValue",
//            (value) =>
//            {
//                this.text_ScrollBarValue.text = "ScrollBarValue ：" + value;
//            });
            
            //自动设置值测试
            this.Model.RegAction("AutoSetValue", AutoSetDataTest);
        }

        public class AutoSetData
        {
            [ValueType(typeof(Text))]
            public string test1;
            [ValueType(typeof(Text))]
            public string test2;
            [ValueType(typeof(Text))]
            public string name;
            
        }
        
        /// <summary>
        /// 自动测试值
        /// </summary>
        private void AutoSetDataTest(object o)
        {
            this.AutoSetTranFormData(this.Transform.Find("AutoSetValue") , o);
        }
    }
}