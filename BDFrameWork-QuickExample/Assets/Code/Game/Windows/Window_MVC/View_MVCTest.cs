using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Game.Windows
{
    public class View_MVCTest : AViewBase
    {
        [BSetTransform("testButton")]
        private Button testButton;
        //
        [BSetTransform("testSlider")]
        private Slider testSlider;
        //
        [BSetTransform("testScrollbar")]
        private Scrollbar testScrollBar;
                
        [BSetTransform("testButtonAutoSetValue")]
        private Button testButtonAutoSetValue;
        //
        [BSetTransform("text_click")]
        [BBindData("ClickCount")]
        private Text text_click;
        
        
        //
        [BSetTransform("text_sliderValue")]
        private Text text_sliderValue;
        //
        [BSetTransform("text_scrollBarValue")]
        [BBindData("ScrollBarValue")]
        private Text text_ScrollBarValue;
        
        public View_MVCTest(Transform t, DataDrive_Service service) : base(t, service)
        {
            
        }
        
        public override void BindData()
        {
            base.BindData();

//            this.DataBinder.RegAction("ClickCount",
//            (value) =>
//            {
//                this.text_click.text = "点 击 次 数：" + value;
//            });

            this.DataBinder.RegAction("SliderValue",
            (value) =>
            {
                this.text_sliderValue.text = "Slider Value：" + value;
            });
//            
//            this.DataBinder.RegAction("ScrollBarValue",
//            (value) =>
//            {
//                this.text_ScrollBarValue.text = "ScrollBarValue ：" + value;
//            });
            
            //自动设置值测试
            this.DataBinder.RegAction("AutoSetValue", AutoSetDataTest);
        }

        public class AutoSetData
        {
            [BValueType(typeof(Text))]
            public string test1;
            [BValueType(typeof(Text))]
            public string test2;
            [BValueType(typeof(Text))]
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