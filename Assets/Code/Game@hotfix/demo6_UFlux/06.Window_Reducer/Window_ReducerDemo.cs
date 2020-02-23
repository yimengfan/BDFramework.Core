using BDFramework.UFlux.Reducer;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    [UI((int)  WinEnum.Win_Demo6_Test006, "Windows/UFlux/demo006/Window_Reducer")]
    public class Window_ReducerDemo: AWindow<Props_Hero2,State_Hero>
    {
        public Window_ReducerDemo(string path) : base(path)
        {
        }


        [TransformPath("btn_RequestNet")]
        private Button btn_RequestNet;


        public override void Init()
        {
            base.Init();
            
            this.btn_RequestNet.onClick.AddListener(() =>
            {
                //触发Reducer
                this.store.Dispatch(Reducer06Enum.MsgRequestSrver,null);
            });
        }

        /// <summary>
        /// 这个一定得重写
        /// </summary>
        /// <returns></returns>
        public override AReducers<State_Hero> CreateReducers()
        {
            return new ReducerDemo06();
        }
        

        /// <summary>
        /// 这个是根据逻辑State
        /// 转化为渲染Props的部分
        /// 自行处理
        /// 需要注意的是，不要刷新整个页面，只要刷新部分更新的数值即可
        /// </summary>
        /// <param name="state"></param>
        public override void StateToProps(State_Hero state)
        {
            //下面逻辑 可以写个函数 批量判断
            if (state.Name != null&& this.Props.Name != state.Name)
            {
                this.Props.Name = state.Name;
                this.Props.SetPropertyChange(nameof(Props_Hero2.Name));
            }
            
            if ( this.Props.Hp != state.Hp)
            {
                this.Props.Hp = state.Hp;
                this.Props.SetPropertyChange(nameof(Props_Hero2.Hp));
                //这里表现出State不一定跟Props完全一样，
                //有些ui的渲染状态，需要根据State算出来
                if (state.Hp < 50)
                {
                    this.Props.HpColor= Color.red;
                }
                else
                {
                    this.Props.HpColor = Color.blue;
                }
            }
            if (this.Props.MaxHp != state.MaxHp)
            {
                this.Props.MaxHp = state.MaxHp;
                this.Props.SetPropertyChange(nameof( Props_Hero2.MaxHp));
            }
        }
    }
}