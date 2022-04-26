using BDFramework.UFlux.Contains;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    /// <summary>
    /// 这里是渲染状态，用以描述页面渲染
    /// </summary>
    public class Props_HeroDataContent : APropsBase
    {
        [ComponentValueBind("Hero/Content/t_Name", typeof(Text), nameof(Text.text))]
        public string Name;

        [ComponentValueBind("Hero/Content/t_Hp", typeof(Text), nameof(Text.text))]
        public int Hp;

        [ComponentValueBind("Hero/Content/t_MaxHp", typeof(Text), nameof(Text.text))]
        public int MaxHp;

        [ComponentValueBind("Hero/Content/t_Hp", typeof(Text), nameof(Text.color))]
        public Color HpColor;

        //这里只是演示用，其实content1最好也是这样用类型包装，而不是重写一遍成员变量
        [ComponentValueBind("Hero/Content2", typeof(UFluxBindLogic), nameof(UFluxBindLogic.BindChild))]
        public Props_HeroDataContent2 Content2 = null;
    }

    /// <summary>
    /// Content2的props
    /// </summary>
    public class Props_HeroDataContent2 : APropsBase
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

    [UI((int) WinEnum.Win_UFlux_Test006, "Windows/UFlux/demo006/Window_Reducer")]
    public class Window_Demo06 : AWindow<Props_HeroDataContent>
    {
        public Window_Demo06(string path) : base(path)
        {
        }


        private Store<Server_HeroData> store;
        private StoreWrapper storeWrapper;

        public override void Init()
        {
            base.Init();

            //单Reducer监听
            store = StoreFactory.CreateStore(new Reducer_Demo06());
            store.Subscribe((newState) =>
            {
                //刷新
                StateToProps(newState);
            });


            //多Reducer 监听演示
            storeWrapper = StoreFactory.CreateStore(new Reducer_Demo06(), new Reducer_Demo06Test());
            //监听State:S_HeroDataDemo6Test
            storeWrapper.Subscribe<S_HeroDataDemo6Test>((newState) =>
            {
                //设置Content2
                State2ToContent2(newState);
            });
            // //监听State:Server_HeroData
            storeWrapper.Subscribe<Server_HeroData>((newState) =>
            {
                //刷新
                StateToProps(newState);
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
            this.Props.Name  = server.Name;
            this.Props.Hp = server.Hp;
            this.Props.MaxHp = server.MaxHp;
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

            //提交修改
            this.CommitProps();
        }


        /// <summary>
        /// Content2的渲染
        /// </summary>
        public void State2ToContent2(S_HeroDataDemo6Test server)
        {
            if (this.Props.Content2 == null)
            {
                this.Props.Content2 = new Props_HeroDataContent2();
            }

            this.Props.Content2.Name  = server.Name;
            this.Props.Content2.Hp = server.Hp;
            this.Props.Content2.MaxHp = server.MaxHp;
            //这里表现出State不一定跟Props完全一样，
            //有些ui的渲染状态，需要根据State算出来
            if (server.Hp < 50)
            {
                this.Props.Content2.HpColor = Color.green;
            }
            else
            {
                this.Props.Content2.HpColor = Color.yellow;
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

        [ButtonOnclick("btn_InvokeAsyncTest")]
        private void btn_RequestServerAsync()
        {
            //异步Reducer测试
            this.store.Dispatch(Reducer_Demo06.Reducer06.InvokeAsyncTest);
        }

        [ButtonOnclick("btn_InvokeSynchronizationTest")]
        private void btn_InvokeSynchronizationTest()
        {
            //触发Reducer
            this.store.Dispatch(Reducer_Demo06.Reducer06.InvokeSynchronizationTest);
        }

        [ButtonOnclick("btn_InvokeCallbackTest")]
        private void btn_InvokeCallbackTest()
        {
            //触发Reducer
            this.store.Dispatch(Reducer_Demo06.Reducer06.InvokeCallbackTest);
        }

        //多监听 reducer 按钮
        [ButtonOnclick("btn_InvokeMultipleReducer")]
        private void btn_InvokeMultipleReducer()
        {
            Debug.Log("发出多Reducer信息!");
            //触发Reducer1
            this.storeWrapper.Dispatch(Reducer_Demo06.Reducer06.InvokeAsyncTest);
            
            //触发Reducer2
            this.storeWrapper.Dispatch(Reducer_Demo06Test.Reducer06.RequestHeroDataAsync);
        }
    }
}
