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
                
        [TransformPath("testButtonAutoSetValue")]
        private Button testButtonAutoSetValue;
        //
        [TransformPath("text_click")]
        private Text text_click;
        
        
        //
        [TransformPath("text_sliderValue")]
        private Text text_sliderValue;
        //
        [TransformPath("text_scrollBarValue")]
        [BindModel("ScrollBarValue")]
        private Text text_ScrollBarValue;
        
        
        public View_MVCTest(Transform t, DataDriven_Service service) : base(t, service)
        {
            
        }
        
        public override void BindModel()
        {
            base.BindModel();

            //手动注册
            this.Model.RegisterData("ClickCount");
            this.Model.RegAction("ClickCount",
            (value) =>
            {
                this.text_click.text = "点 击 次 数：" + value;
            });

            //手动注册
            this.Model.RegisterData("SliderValue");
            this.Model.RegAction("SliderValue",
            (value) =>
            {
                this.text_sliderValue.text = "Slider Value：" + value;
            });
            
            //已用标签自动注册
            this.Model.RegAction("ScrollBarValue",
            (value) =>
            {
                this.text_ScrollBarValue.text = "ScrollBarValue ：" + value;
            });
            
            //自动设置值测试
            this.Model.RegisterData("AutoSetValueTest");
            this.Model.RegAction("AutoSetValueTest", AutoSetDataTest);
        }

        //
        public class AutoSetData
        {
            [UIComponentType( UIComponentEnum.Text)]
            public string test1;
            [UIComponentType(UIComponentEnum.Text)]
            public string test2;
            [UIComponentType(UIComponentEnum.Text)]
            public string name;
            
        }
        
        public class Item
        {
            [UIComponentType(UIComponentEnum.Image)]
            public string Icon = "xxxx.png";
            [UIComponentType(UIComponentEnum.Text)]
            public string ItemName = "体力丹";
            [UIComponentType(UIComponentEnum.Text)]
            public string ItemDes = "使用后可增加xx体力";
            [UIComponentType(UIComponentEnum.Text)]
            public string ItemCount = "2";
        }
        
        /// <summary>
        /// 自动测试值
        /// </summary>
        private void AutoSetDataTest(object o)
        {
            UITools.AutoSetValue(this.Transform.Find("AutoSetValue") , o);
        }
    }
}