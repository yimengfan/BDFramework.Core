using System;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace BDFramework.UFlux.item
{
    public class Props_ItemTest002 : APropsBase
    {
        [ComponentValueBind("img", typeof(Image), nameof(Image.overrideSprite))] //数据赋值对象
        public string ItemImg = "";

        [ComponentValueBind("img/text", typeof(Text), nameof(Text.text))] //数据赋值对象
        public string Content = "";

        [ComponentValueBind("btn_Buy", typeof(Button), nameof(Button.onClick))] //数据赋值对象
        public Action Action;

        [ComponentValueBind("Id", typeof(Text), nameof(Text.text))] //数据赋值对象
        public string ID = "";
    }

    [Component("Windows/UFlux/demo002/item")]
    public class Component_ItemTest002 : ATComponent<Props_ItemTest002>
    {
    }
    
}