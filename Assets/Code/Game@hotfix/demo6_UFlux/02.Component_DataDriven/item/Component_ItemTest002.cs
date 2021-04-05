using System;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace BDFramework.UFlux.item
{
    public class Props_ItemTest002: PropsBase 
    {
       
        [TransformPath("img")]  //节点
        [ComponentValueBind(typeof(Image), nameof(Image.overrideSprite))]//数据赋值对象
        public string ItemImg = "";

        [TransformPath("img/text")] //节点
        [ComponentValueBind(typeof(Text), nameof(Text.text))]//数据赋值对象
        public string Content = "";

        [TransformPath("btn_Buy")]
        [ComponentValueBind(typeof(Button), nameof(Button.onClick))]//数据赋值对象
        public Action Action;
        
        [TransformPath("Id")] 
        [ComponentValueBind(typeof(Text), nameof(Text.text))]//数据赋值对象
        public string ID = "";

    }
    
    [Component("Windows/UFlux/demo002/item")] 
    public class Component_ItemTest002: ATComponent<Props_ItemTest002>
    {
        
    }
}