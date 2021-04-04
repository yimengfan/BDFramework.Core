using System.Collections.Generic;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    /// <summary>
    /// 这里是渲染状态，用以描述页面渲染
    /// </summary>
    public class P_Hero2: PropsBase
    {
        
        [TransformPath("Hero/Content/t_Name")]
        [ComponentValueBind(nameof(Text),nameof(Text.text))]
        public string Name;
        [TransformPath("Hero/Content/t_Hp")]
        [ComponentValueBind(nameof(Text),nameof(Text.text))]
        public int Hp;
        [TransformPath("Hero/Content/t_MaxHp")]
        [ComponentValueBind(nameof(Text),nameof(Text.text))]
        public int MaxHp;
        [TransformPath("Hero/Content/t_Hp")]
        [ComponentValueBind(nameof(Text),nameof(Text.color))]
        public Color HpColor;
    }
}