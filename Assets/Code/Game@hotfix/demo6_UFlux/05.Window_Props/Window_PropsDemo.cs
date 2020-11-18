using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    [UI((int)  WinEnum.Win_Demo6_Test005, "Windows/UFlux/demo005/Window_PropsDemo")]
    public class Window_PropsDemo:  AWindow<P_Hero>
    {
        public Window_PropsDemo(string path) : base(path)
        {
        }

        public Window_PropsDemo(Transform transform) : base(transform)
        {
        }
        
        [TransformPath("btn_Close")]
        private Button btn_Close;
        
        [TransformPath("btn_TestWindowProps")]
        private Button btn_TestWindowProps;
        
        public override void Init()
        {
            base.Init();
            
            btn_Close.onClick.AddListener(() =>
            {
                //关闭
                this.Close();
            });
            
            
            btn_TestWindowProps.onClick.AddListener(() =>
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
            });
            
        }
    }
}