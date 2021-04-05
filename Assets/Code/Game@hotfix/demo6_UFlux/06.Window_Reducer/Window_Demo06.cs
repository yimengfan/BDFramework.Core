using BDFramework.UFlux.Contains;
using BDFramework.UFlux.Reducer;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    [UI((int)  WinEnum.Win_Demo6_Test006, "Windows/UFlux/demo006/Window_Reducer")]
    public class Window_Demo06: AWindow<P_HeroData2>
    {
        public Window_Demo06(string path) : base(path)
        {
        }
        
        [TransformPath("btn_RequestNet")]
        private Button btn_RequestNet;

        private Store<S_HeroData> store;
        
        public override void Init()
        {
            base.Init();

            store = StoreFactory.CreateStore(new Reducer_Demo06());
            
            store.Subscribe((s) =>
            {
                //刷新
                StateToProps(s);
                //提交修改
                this.CommitProps();
            });
            
            this.btn_RequestNet.onClick.AddListener(() =>
            {
                //触发Reducer
                this.store.Dispatch(Reducer_Demo06.Reducer06.RequestHeroData);
            });
        }

        /// <summary>
        /// 这个一定得重写
        /// </summary>
        /// <returns></returns>
        public  AReducers<S_HeroData> CreateReducers()
        {
            return new Reducer_Demo06();
        }
        

        /// <summary>
        /// 这个是根据逻辑State
        /// 转化为渲染Props的部分
        /// 自行处理
        /// 需要注意的是，不要刷新整个页面，只要刷新部分更新的数值即可
        /// </summary>
        /// <param name="s"></param>
        public void StateToProps(S_HeroData s)
        {
            //下面逻辑 可以写个函数 批量判断
            if (s.Name != null&& this.Props.Name != s.Name)
            {
                this.Props.Name = s.Name;
                this.Props.SetPropertyChange(nameof(P_HeroData2.Name));
            }
            
            if ( this.Props.Hp != s.Hp)
            {
                this.Props.Hp = s.Hp;
                this.Props.SetPropertyChange(nameof(P_HeroData2.Hp));
                //这里表现出State不一定跟Props完全一样，
                //有些ui的渲染状态，需要根据State算出来
                if (s.Hp < 50)
                {
                    this.Props.HpColor= Color.red;
                }
                else
                {
                    this.Props.HpColor = Color.blue;
                }
                this.Props.SetPropertyChange(nameof(P_HeroData2.HpColor));
            }
            if (this.Props.MaxHp != s.MaxHp)
            {
                this.Props.MaxHp = s.MaxHp;
                this.Props.SetPropertyChange(nameof( P_HeroData2.MaxHp));
            }
            

        }
    }
}