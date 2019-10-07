using System;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.Store;

namespace BDFramework.UFlux.Test
{
    public enum Login
    {
        act_GetUserData = 0,
        act_AsyncGetUserData,
        
    }

    public class UserDataReducer : AReducers<UserDataState>
    {
        public override void RegisterReducers()
        {
            AddRecucer( Test.Login.act_GetUserData,GetUserData);
            AddAsyncReducer( Test.Login.act_AsyncGetUserData,AsyncGetUserData);
        }

        /// <summary>
        /// 同步获取属性
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        private UserDataState GetUserData(UserDataState props,object @params = null)
        {

            for (int i = 0; i < 20; i++)
            {
                var item =new UserDataState.Item();
                //随机一个id
                item.Id = UnityEngine.Random.Range(0, 10);
                item.TotalNumber = UnityEngine.Random.Range(10, 100);
                item.Number = UnityEngine.Random.Range(0, item.TotalNumber);
               
                props.ItemList.Add(item);
            }
            return props;
        }

        /// <summary>
        /// 异步登录  这里注意函数签名一定要匹配上
        /// </summary>
        /// <param name="state"></param>
        /// <param name="params"></param>
        /// <param name="callback"></param>
        private void AsyncGetUserData(Store<UserDataState>.GetState getStateFunc, object @params = null, Action<UserDataState> callback = null)
        {
            
            //这个不要提前 Get，不然会有竞态问题，
            //一般是等返回时再Get
            var state = getStateFunc();
            state.SetPropertyChange(nameof(state.ItemList));
        }
    }
}