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
        /// <summary>
        /// 设置图片
        /// </summary>
        /// <param name="value"></param>
        [ComponentValueAdaptor(nameof(Text.text))]
        private void SetProp_Text(UIBehaviour uiBehaviour,object value)
        {
            var text = uiBehaviour as Text;
            text.text = (string) value;
        }
    }
}