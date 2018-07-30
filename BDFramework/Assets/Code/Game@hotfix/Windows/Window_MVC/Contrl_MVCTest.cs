using BDFramework.UI;
using UnityEngine;

namespace Code.Game.Windows.MCX
{
    public class ViewContrl_MVCTest : AViewContrlBase
    {
        public ViewContrl_MVCTest(DataDriven_Service data) : base(data)
        {
            
        }      
        /// <summary>
        /// 自动绑定到button这个组件的 onclick事件
        /// </summary>
        private void OnClick_testButton()
        {
            var clickCount = this.Model.GetData<int>("ClickCount");    
            
            this.Model.SetData("ClickCount" , ++clickCount);
           
        }
        
        /// <summary>
        /// 自动绑定
        /// </summary>
        private void OnValueChange_testSlider(float value)
        {          
            this.Model.SetData("SliderValue" ,value);
         
        }
        
        /// <summary>
        /// 自动绑定
        /// </summary>
        private void OnValueChange_testScrollBar(float value)
        {          
            this.Model.SetData("ScrollBarValue" ,value);
           
        }
        
        /// <summary>
        /// 自动绑定
        /// </summary>
        private void OnClick_testButtonAutoSetValue()
        {     
            var test = new View_MVCTest.AutoSetData()
            {
                test1 = "获得值：test1",
                test2 = "获得值：test2",
                name  = "获得值：张三"
            };
            this.Model.SetData("AutoSetValueTest" , test);
        }
        
    }
}