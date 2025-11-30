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
    /// 
    /// </summary>
    public class Props_HeroDataContent : APropsBase
    {
        /// <summary>
        /// 左边的英雄信息
        /// 这里只是演示流程，才将组件数据存在着，实际上应该组件自身保存，处理
        /// </summary>
        public Com_HeroData.Props_Demo6HeroData Content { get; set; } = new Com_HeroData.Props_Demo6HeroData();
        /// <summary>
        /// 右边的英雄信息
        /// 这里只是演示流程，才将组件数据存在着，实际上应该组件自身保存，处理
        /// </summary>
        public Com_HeroData.Props_Demo6HeroData Content2 { get; set; }  = new Com_HeroData.Props_Demo6HeroData();

    }


    [UI((int) WinEnum.Win_UFlux_Test006, "Windows/UFlux/demo006/Window_Reducer")]
    public class Window_Demo06 : AWindow<Props_HeroDataContent>
    {
        /// <summary>
        /// 左边的英雄信息组件
        /// </summary>
        [UfluxComponentPath("Hero/Content")]
        private Com_HeroData com_HeroDataContent { get; set; }
        /// <summary>
        /// 左边的英雄信息组件
        /// </summary>
        [UfluxComponentPath("Hero/Content2")]
        private Com_HeroData com_HeroDataContent2{ get; set; }
        
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
            //自动订阅
            store.ScanThisSubscribe(this);
            
            //多Reducer 监听演示
            storeWrapper = StoreFactory.CreateStore(new Reducer_Demo06(), new Reducer_Demo06Copy());
            
            //监听State:S_HeroDataDemo6Test
            storeWrapper.Subscribe<S_HeroDataDemo6Copy>((newState) =>
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
            this.Props.Content.Name  = server.Name;
            this.Props.Content.Hp = server.Hp;
            this.Props.Content.MaxHp = server.MaxHp;
            //这里表现出State不一定跟Props完全一样，
            //有些ui的渲染状态，需要根据State算出来
            if (server.Hp < 50)
            {
                this.Props.Content.HpColor = Color.red;
            }
            else
            {
                this.Props.Content.HpColor = Color.blue;
            }

            //提交修改
            //this.CommitProps();
            //这里只是演示流程，实际上组件本身的逻辑，组件自己处理，最要不要依赖父窗口
            this.com_HeroDataContent.SetData(this.Props.Content);
        }


        /// <summary>
        /// Content2的渲染
        /// </summary>
        public void State2ToContent2(S_HeroDataDemo6Copy server)
        {
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
            //this.CommitProps();
            
            //这里只是演示流程，实际上组件本身的逻辑，组件自己处理，最要不要依赖父窗口
            this.com_HeroDataContent2.SetData(this.Props.Content2);
        }


        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            //关闭
            this.Close();
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        [ButtonOnclick("btn_InvokeAsyncTest")]
        private void btn_RequestServerAsync()
        {
            //异步Reducer测试
            this.store.Dispatch(Reducer_Demo06.Reducer06.InvokeAsyncTest);
        }
        
        



        /// <summary>
        /// 同步请求
        /// </summary>
        [ButtonOnclick("btn_InvokeSynchronizationTest")]
        private void btn_InvokeSyncTest()
        {
            //触发Reducer
            this.store.Dispatch(Reducer_Demo06.Reducer06.InvokeSyncTest);
        }
        /// <summary>
        /// 同步请求,带参数
        /// </summary>
        [ButtonOnclick("btn_InvokeSyncTest2")]
        private void btn_RequestServerSyncWithParams()
        {
            BDebug.Log("同步-带参数请求,类型,int");
            int @param = Random.Range(1,1000);
            //异步Reducer测试
            this.store.Dispatch(Reducer_Demo06.Reducer06.InvokeSyncTest,@param);
        }
        
        //多监听 reducer 按钮
        [ButtonOnclick("btn_InvokeMultipleReducer")]
        private void btn_InvokeMultipleReducer()
        {
            Debug.Log("发出多Reducer信息!");
            //触发Reducer1
            this.storeWrapper.Dispatch(Reducer_Demo06.Reducer06.InvokeAsyncTest);
            //触发Reducer2
            this.storeWrapper.Dispatch(Reducer_Demo06Copy.Reducer06.RequestHeroDataAsync);
        }

        /// <summary>
        /// 自动订阅
        /// </summary>
        /// <param name="state"></param>
        [Subscribe((int)Reducer_Demo06.Reducer06.InvokeAsyncTest)]
        private void SubscribeInvokeAsyncTest(Server_HeroData state)
        {
            BDebug.Log($"订阅InvokeAsyncTest 返回成功!:{JsonMapper.ToJson(state,true)}");
        }
    }
}
