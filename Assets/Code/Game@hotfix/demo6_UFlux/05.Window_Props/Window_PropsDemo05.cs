using BDFramework.UFlux;
using BDFramework.UFlux.Test;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    public class Props_HeroData : APropsBase
    {
        [ComponentValueBind("Hero/Content/t_Name",typeof(Text), nameof(Text.text))]
        public string Name;
        
        [ComponentValueBind("Hero/Content/t_Hp",typeof(Text), nameof(Text.text))]
        public int Hp;
        
        [ComponentValueBind("Hero/Content/t_MaxHp",typeof(Text), nameof(Text.text))]
        public int MaxHp;
        
        [ComponentValueBind("Hero/Content/t_Hp",typeof(Text), nameof(Text.color))]
        public Color HpColor;
    }


    /// <summary>
    /// Props=》Windows 的演示
    /// 设置props 就能刷新Windows
    /// </summary>
    [UI((int) WinEnum.Win_Demo6_Test005, "Windows/UFlux/demo005/Window_PropsDemo")]
    public class Window_PropsDemo05 : AWindow<Props_HeroData>
    {
        public Window_PropsDemo05(string path) : base(path)
        {
        }

        public Window_PropsDemo05(Transform transform) : base(transform)
        {
        }

        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            //关闭
            this.Close();
        }

        [ButtonOnclick("btn_TestWindowProps")]
        private void btn_TestWindowProps()
        {
            //触发属性变动
            this.Props.Name = "吕布";
            this.Props.Hp = Random.Range(1, 100);
            this.Props.MaxHp = 100;
            if (this.Props.Hp < 50)
            {
                this.Props.HpColor = Color.red;
            }
            else
            {
                this.Props.HpColor = Color.blue;
            }

            this.CommitProps();
        }
    }
}