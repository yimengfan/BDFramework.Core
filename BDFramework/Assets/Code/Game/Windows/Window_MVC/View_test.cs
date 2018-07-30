
namespace Code.Game.Windows{
//工具生成代码,请勿删除标签，否则无法进行添加操作
using BDFramework.UI;
using UnityEngine;
//[using namespace]
//[Note]
//[Attribute]
public class View_test:AViewBase
{
   //------[class end]------
         [SetTransform("testButton")]
//[Attribute]
        public UnityEngine.UI.Button testButton;
      [SetTransform("testSlider")]
//[Attribute]
        public UnityEngine.UI.Slider testSlider;
      [SetTransform("testScrollbar")]
//[Attribute]
        public UnityEngine.UI.Scrollbar testScrollbar;
      [SetTransform("testButtonAutoSetValue")]
//[Attribute]
        public UnityEngine.UI.Button testButtonAutoSetValue;
      //[Attribute]
        public UnityEngine.UI.Text Text3;
      //[Attribute]
        public UnityEngine.UI.Text Text1;
      //[Attribute]
        public UnityEngine.UI.Text Text4;
//------[Field end]------
   //------[Propties end]------
   //[Note]
        public  View_test(Transform t, DataDriven_Service service) : base(t, service)
        {
            
        }
//[Note]
        public override void BindModel()
        {
            base.BindModel();
        }
//------[Method end]------
}
}//Code.Game.Windows
