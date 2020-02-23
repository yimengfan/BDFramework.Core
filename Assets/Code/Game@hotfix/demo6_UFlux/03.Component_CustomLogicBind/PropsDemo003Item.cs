﻿using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace Code.Game.demo6_UFlux._05.NodeHelper
{
    public class PropsDemo003Item : PropsBase
    {
        [TransformPath("Img_Star")]
        [ComponentValueBind(typeof(Image),nameof(Image.overrideSprite))]
        public string EquipmentIconPath;
        
        
        [TransformPath("txt_EquipmentName")]
        [ComponentValueBind(typeof(Text),nameof(Text.text))]
        public string EquipmentName;
        
    }
}