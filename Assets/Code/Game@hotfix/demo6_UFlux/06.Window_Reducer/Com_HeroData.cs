using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    
    /// <summary>
    /// HeroData组件
    /// </summary>
    [Component("Windows/UFlux/demo006/Com_HeroData")]
    public class Com_HeroData : ATComponent<Com_HeroData.RD_Demo6HeroData>
    {
        public class RD_Demo6HeroData : ARenderDataBase
        {
            [ComponentValueBind("t_Name", typeof(Text), nameof(Text.text))]
            public string Name;

            [ComponentValueBind("t_Hp", typeof(Text), nameof(Text.text))]
            public int Hp;

            [ComponentValueBind("t_MaxHp", typeof(Text), nameof(Text.text))]
            public int MaxHp;

            [ComponentValueBind("t_Hp", typeof(Text), nameof(Text.color))]
            public Color HpColor;
        }


        public Com_HeroData(Transform trans) : base(trans)
        {
        }

        public Com_HeroData(string resPath) : base(resPath)
        {
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="renderDataDemo6"></param>
        public void SetData(RD_Demo6HeroData renderDataDemo6)
        {
            this.RenderData.Hp = renderDataDemo6.Hp;
            this.RenderData.HpColor = renderDataDemo6.HpColor;
            this.RenderData.Name = renderDataDemo6.Name;
            this.RenderData.MaxHp = renderDataDemo6.MaxHp;
            //提交属性修改
            this.CommitRenderData();
        }
        
        
    }
}
