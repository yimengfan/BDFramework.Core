using BDFramework.UFlux;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//这里的命名空间必须为：BDFramework.Uflux
namespace  BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Image适配器
    /// </summary>
    [ComponentBindAdaptor(nameof(Image))]
    public class ComponentBindAdaptorImage : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropComponentBindMap[nameof(Image.overrideSprite)] = SetProp_Sprite;
            setPropComponentBindMap[nameof(Image.color)]          = SetProp_Color;
            setPropComponentBindMap[nameof(Image.fillAmount)] = SetProp_Amount;
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
                img.sprite = BDFramework.UFlux.UFlux.Load<Sprite>((string) value);
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