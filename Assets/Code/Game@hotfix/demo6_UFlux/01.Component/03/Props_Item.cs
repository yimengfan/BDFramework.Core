﻿using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace Game.demo6_UFlux.Component
{
    public class Props_Item : APropsBase
    {
        [ComponentValueBind("Img_Star",typeof(Image),nameof(Image.overrideSprite))]
        public string IconPath;
        
        
        [ComponentValueBind("txt_Name",typeof(Text),nameof(Text.text))]
        public string IconName;


    }
}