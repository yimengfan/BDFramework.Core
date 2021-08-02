using System;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace Game.demo6_UFlux.CustomCponentBindAdaptor
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

    /// <summary>
    /// 滑动列表中添加的元素
    /// </summary>
    [Component("Windows/UFlux/02CustomComponentBindAdator/01/item")]
    public class Component_ItemTest002 : ATComponent<Props_ItemTest002>
    {
    }
    
}