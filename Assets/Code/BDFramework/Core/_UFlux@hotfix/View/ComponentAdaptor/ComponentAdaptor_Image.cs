using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Image适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(Image))]
    public class ComponentAdaptor_Image:AComponentAdaptor
    {
        /// <summary>
        /// 设置图片
        /// </summary>
        /// <param name="value"></param>
        [ComponentValueAdaptor(nameof(Image.overrideSprite))]
        private void SetProp_Sprite(UIBehaviour uiBehaviour,object value)
        {
            var img = uiBehaviour as Image;
            if (value is string)
            {
                img.sprite = UFlux.Load<Sprite>((string) value);
            }
            else if(value is Sprite)
            {
                img.sprite = (Sprite) value;
            }
        }
        
        
        [ComponentValueAdaptor(nameof(Image.color))]
        private void SetProp_Color(UIBehaviour uiBehaviour,object value)
        {
            var img = uiBehaviour as Image;
            if (value is Color)
            {
                img.color = (Color) value;
            }
        }
    }
}