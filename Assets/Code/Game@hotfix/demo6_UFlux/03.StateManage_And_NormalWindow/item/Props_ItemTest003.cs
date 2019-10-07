using System;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    public class Props_ItemTest003 : PropsBase
    {
        [TransformPath("img")]  //节点
        [ComponentValueBind(typeof(Image), nameof(Image.overrideSprite))]//数据赋值对象
        public string ItemImg = "";

        [TransformPath("img")]  //节点
        [ComponentValueBind(typeof(Image), nameof(Image.color))]//数据赋值对象
        public Color ImgColor ;
        
        [TransformPath("img/text")] //节点
        [ComponentValueBind(typeof(Text), nameof(Text.text))]//数据赋值对象
        public string Content = "";
        
        
        [TransformPath("Id")] 
        [ComponentValueBind(typeof(Text), nameof(Text.text))]//数据赋值对象
        public string ID = "";

    }
}