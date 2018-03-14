using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Game.Windows
{
    public class View_XVCTest : AViewBase
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
        private Text text_click;
        //
        [BSetTransform("text_sliderValue")]
        private Text text_sliderValue;
        //
        [BSetTransform("text_scrollBarValue")]
        [BBindData("test")]
        private Text text_ScrollBarValue;
        
        public View_XVCTest(Transform t, DataDrive_Service service) : base(t, service)
        {
            
        }
        
        public override void BindData()
        {
            base.BindData();

            this.DataBinder.RegAction_WhenDataChange("ClickCount",
            (value) =>
            {
                this.text_click.text = "点 击 次 数：" + value;
            });

            this.DataBinder.RegAction_WhenDataChange("SliderValue",
            (value) =>
            {
                this.text_sliderValue.text = "Slider Value：" + value;
            });
            
            this.DataBinder.RegAction_WhenDataChange("ScrollBarValue",
            (value) =>
            {
                this.text_ScrollBarValue.text = "ScrollBarValue ：" + value;
            });
            
            //自动设置值测试
            this.DataBinder.RegAction_WhenDataChange("AutoSetValue", AutoSetDataTest);
        }

        public class AutoSetData
        {
            [BValueType(typeof(Text))]
            public string test1;
            [BValueType(typeof(Text))]
            public string test2;
            [BValueType(typeof(Text))]
            public string test3;
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