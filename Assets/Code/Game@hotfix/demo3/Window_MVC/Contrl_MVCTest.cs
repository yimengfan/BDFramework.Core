using BDFramework.UI;
using UnityEngine;
using BDFramework.DataListener;
namespace Code.Game.Windows.MCX
{
    public class ViewContrl_MVCTest : AViewContrlBase
    {
        public ViewContrl_MVCTest(DataListenerService data) : base(data)
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
        
        
    }
}