using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Image适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(Image))]
    public class ComponentAdaptor_Image : AComponentAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropActionMap[nameof(Image.overrideSprite)] = SetProp_Sprite;
            setPropActionMap[nameof(Image.color)]          = SetProp_Color;
            setPropActionMap[nameof(Image.fillAmount)] = SetProp_Amount;
        }

        /// <summary>
        /// 设置图片
        /// </summary>
        /// <param name="value"></param>
        private void SetProp_Sprite(UIBehaviour uiBehaviour, object value)
        {
            var img = uiBehaviour as Image;
            if (value is string)
            {
                img.sprite = UFlux.Load<Sprite>((string) value);
            }
            else if (value is Sprite)
            {
                img.sprite = (Sprite) value;
            }
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="uiBehaviour"></param>
        /// <param name="value"></param>
        private void SetProp_Color(UIBehaviour uiBehaviour, object value)
        {
            var img = uiBehaviour as Image;
            if (value is Color)
            {
                img.color = (Color) value;
            }
        }

        private void SetProp_Amount(UIBehaviour uiBehaviour, object value)
        {
            var img = uiBehaviour as Image;
            if (value is float)
            {
                img.fillAmount = (float) value;
            }
        }
    }
}