using BDFramework.UFlux.item;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.Store;
using Code.Game.demo6_UFlux;
using ILRuntime.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
    [UI((int)UFluxWindowEnum.UFluxTest003,"Windows/UFlux/demo004/Window_FluxTest003")]
    public class Window_GetUserData : AWindow<Props_UserBag, UserDataState>
    {
        /// <summary>
        /// 获取reducer
        /// </summary>
        /// <returns></returns>
        public override AReducers<UserDataState> GetReducers()
        {
            return new UserDataReducer();
        }

        public Window_GetUserData(string path) : base(path)
        {
        }

        [TransformPath("btn_Login")]
        private Button btn_Login;
        
        public override void Init()
        {
            base.Init();
            btn_Login.onClick.AddListener(() => { this.store.Dispatch(Login.act_GetUserData); });

           
        }

        /// <summary>
        /// 当Store中State变化的回调
        /// </summary>
        /// <param name="state"></param>
        public override void StateToProps(UserDataState state)
        {
            //这个函数主要的作用是:
            //State描述的是 数据状态，而非渲染状态
            //当state数据不足以描述渲染状态时，则需要进行转换
            //比如服务器  发出，id 1  num 20，客户端需要num低于100的显示红色 等。。。
            while (state.IsChanged())
            {
                switch (state.GetPropertyChange())
                {
                    case nameof(state.ItemList):
                    {
                        //更新列表
                        for (int i = 0; i < state.ItemList.Count; i++)
                        {
                            var stateItem = state.ItemList[i];
                            if(this.Props.ItemList.Count < i)  //新增模式 
                            {
                                var prop_item = new Props_ItemTest003();
                                prop_item.ComponentType = typeof(Component_ItemTest003);
                                prop_item.ID = "ID:" + stateItem.Id;
                                prop_item.ItemImg = "Image/" + stateItem.Id;
                                prop_item.Content = stateItem.Number + "/" + stateItem.TotalNumber;
                                prop_item.ImgColor = stateItem.Number < 20 ? Color.black : Color.red;
                                //设置所有属性改变
                                prop_item.SetAllPropertyChanged();
                                Props.ItemList.Add(prop_item);
                            }
                            else //更新
                            {
                                if (stateItem.IsChanged())
                                {
                                    var prop_Item = this.Props.ItemList[i];
                                    while (stateItem.IsChanged())//更新所有字段
                                    {
                                        var name = stateItem.GetPropertyChange();
                                        prop_Item.SetValue(name,stateItem.GetValue(name));
                                    }
                                }
                            }
                        }
                        
                        state.SetPropertyChange(nameof(state.ItemList));
                       
                    } 
                        break;
                }
            }
        }
    }
}