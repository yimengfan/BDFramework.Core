using System.Collections.Generic;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    /// <summary>
    /// 这里是渲染状态，用以描述页面渲染
    /// </summary>
    public class P_HeroData2: PropsBase
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