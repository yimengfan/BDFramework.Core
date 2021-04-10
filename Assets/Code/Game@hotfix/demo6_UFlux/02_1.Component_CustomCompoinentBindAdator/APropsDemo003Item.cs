﻿using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace Game.demo6_UFlux._05.NodeHelper
{
    public class APropsDemo003Item : APropsBase
    {
        [ComponentValueBind("Img_Star",typeof(Image),nameof(Image.overrideSprite))]
        public string EquipmentIconPath;
        
        
        [ComponentValueBind("Img_Star",typeof(Text),nameof(Text.text))]
        public string EquipmentName;


    }
}