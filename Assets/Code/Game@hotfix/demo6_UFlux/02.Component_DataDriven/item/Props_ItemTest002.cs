using System;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace BDFramework.UFlux.item
{
    public class Props_ItemTest002: PropsBase 
    {
       
        [TransformPath("img")]  //节点
        [ComponentValueBind(nameof(Image), nameof(Image.overrideSprite))]//数据赋值对象
        public string ItemImg = "";

        [TransformPath("img/text")] //节点
        [ComponentValueBind(nameof(Text), nameof(Text.text))]//数据赋值对象
        public string Content = "";

        [TransformPath("btn_Buy")]
        [ComponentValueBind(nameof(Button), nameof(Button.onClick))]//数据赋值对象
        public Action Action;
        
        [TransformPath("Id")] 
        [ComponentValueBind(nameof(Text), nameof(Text.text))]//数据赋值对象
        public string ID = "";

    }
}