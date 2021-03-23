using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Text适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(Text))]
    public class ComponentAdaptor_Text : AComponentAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropActionMap[nameof(Text.text)] = SetProp_Text;
            setPropActionMap[nameof(Text.color)] = SetProp_Color;
        }
        /// <summary>
        /// 设置文字
        /// </summary>
        /// <param name="value"></param>
        private void SetProp_Text(UIBehaviour uiBehaviour,object value)
        {
            var text = uiBehaviour as Text;
            text.text = value.ToString();
        }
        
        
        /// <summary>
        /// 设置文字
        /// </summary>
        /// <param name="value"></param>
        private void SetProp_Color(UIBehaviour uiBehaviour,object value)
        {
            var text = uiBehaviour as Text;
            text.color = (Color) value;
        }
    }
}