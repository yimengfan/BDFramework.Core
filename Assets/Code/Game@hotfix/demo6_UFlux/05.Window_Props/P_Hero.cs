using System.Collections.Generic;
using BDFramework.UFlux.View.Props;
using BDFramework.UI.Demo_ScreenRect;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    public class P_Hero: PropsBase
    {
        
        [TransformPath("Hero/Content/t_Name")]
        [ComponentValueBind(typeof(Text),nameof(Text.text))]
        public string Name;
        [TransformPath("Hero/Content/t_Hp")]
        [ComponentValueBind(typeof(Text),nameof(Text.text))]
        public int Hp;
        [TransformPath("Hero/Content/t_MaxHp")]
        [ComponentValueBind(typeof(Text),nameof(Text.text))]
        public int MaxHp;
        [TransformPath("Hero/Content/t_Hp")]
        [ComponentValueBind(typeof(Text),nameof(Text.color))]
        public Color HpColor;
    }
}