using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    
    /// <summary>
    /// HeroData组件
    /// </summary>
    [Component("Windows/UFlux/demo006/Com_HeroData")]
    public class Com_HeroData : ATComponent<Com_HeroData.Props_Demo6HeroData>
    {
        public class Props_Demo6HeroData : APropsBase
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
        /// <param name="propsDemo6"></param>
        public void SetData(Props_Demo6HeroData propsDemo6)
        {
            this.Props.Hp = propsDemo6.Hp;
            this.Props.HpColor = propsDemo6.HpColor;
            this.Props.Name = propsDemo6.Name;
            this.Props.MaxHp = propsDemo6.MaxHp;
            //提交属性修改
            this.CommitProps();
        }
        
        
    }
}
