using BDFramework.UI;
using UnityEngine;

namespace Code.Game.Windows.MCX
{
    public class ViewContrl_MVCTest : AViewContrlBase
    {
        public ViewContrl_MVCTest(DataDrive_Service data) : base(data)
        {
            
        }      
        /// <summary>
        /// 自动绑定到button这个组件的 onclick事件
        /// </summary>
        private void OnClick_testButton()
        {
            var clickCount = this.DataBinder.GetData<int>("ClickCount");            
            this.DataBinder.SetData("ClickCount" , ++clickCount);
            
            Debug.Log("--");
        }
        
        /// <summary>
        /// 自动绑定
        /// </summary>
        private void OnValueChange_testSlider(float value)
        {          
            this.DataBinder.SetData("SliderValue" ,value);
            Debug.Log("--");
        }
        
        /// <summary>
        /// 自动绑定
        /// </summary>
        private void OnValueChange_testScrollBar(float value)
        {          
            this.DataBinder.SetData("ScrollBarValue" ,value);
            Debug.Log("--");
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
                test3 = "获得值：test3"
            };
            this.DataBinder.SetData("AutoSetValue" , test);
            Debug.Log("-..-");
        }
        
    }
}