using BDFramework.UFlux.Contains;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    /// <summary>
    /// 这里是渲染状态，用以描述页面渲染
    /// </summary>
    public class APropsHeroData2 : APropsBase
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

    [UI((int) WinEnum.Win_Demo6_Test006, "Windows/UFlux/demo006/Window_Reducer")]
    public class Window_Demo06 : AWindow<APropsHeroData2>
    {
        public Window_Demo06(string path) : base(path)
        {
        }


        private Store<Server_HeroData> store;

        public override void Init()
        {
            base.Init();

            store = StoreFactory.CreateStore(new Reducer_Demo06());

            store.Subscribe((s) =>
            {
                //刷新
                StateToProps(s);
            });
        }

        /// <summary>
        /// 这个是根据逻辑State
        /// 转化为渲染Props的部分
        /// 自行处理
        /// 需要注意的是，不要刷新整个页面，只要刷新部分更新的数值即可
        /// </summary>
        /// <param name="server"></param>
        public void StateToProps(Server_HeroData server)
        {
            //下面逻辑 可以写个函数 批量判断
            if (server.Name != null && this.Props.Name != server.Name)
            {
                this.Props.Name = server.Name;
                this.Props.SetPropertyChange(nameof(APropsHeroData2.Name));
            }

            if (this.Props.Hp != server.Hp)
            {
                this.Props.Hp = server.Hp;
                this.Props.SetPropertyChange(nameof(APropsHeroData2.Hp));
                //这里表现出State不一定跟Props完全一样，
                //有些ui的渲染状态，需要根据State算出来
                if (server.Hp < 50)
                {
                    this.Props.HpColor = Color.red;
                }
                else
                {
                    this.Props.HpColor = Color.blue;
                }

                this.Props.SetPropertyChange(nameof(APropsHeroData2.HpColor));
            }

            if (this.Props.MaxHp != server.MaxHp)
            {
                this.Props.MaxHp = server.MaxHp;
                this.Props.SetPropertyChange(nameof(APropsHeroData2.MaxHp));
            }

            //提交修改
            this.CommitProps();
        }


        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            //关闭
            this.Close();
        }

        [ButtonOnclick("btn_RequestNet")]
        private void btn_RequestNet()
        {
            //触发Reducer
            this.store.Dispatch(Reducer_Demo06.Reducer06.RequestHeroData);
        }
    }
}