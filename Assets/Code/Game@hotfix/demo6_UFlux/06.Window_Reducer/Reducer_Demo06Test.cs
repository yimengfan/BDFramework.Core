using System;
using System.Net;
using System.Threading.Tasks;
using BDFramework.UFlux.Contains;
using BDFramework.UFlux.Reducer;
using LitJson;

namespace BDFramework.UFlux.Test
{
    /// <summary>
    /// 这里是逻辑状态，
    /// 比如服务器返回，客户端计算状态数据
    /// </summary>
    public class S_HeroDataDemo6Test : StateBase
    {
        public string Name;
        public int Hp;
        public int MaxHp;
    }
    /// <summary>
    /// Reducer 函数处理的集合
    /// </summary>
    public class Reducer_Demo06Test : AReducers<S_HeroDataDemo6Test>
    {
        public enum Reducer06
        {
            //同步请求
            RequestHeroDataSynchronization,
            //异步请求
            RequestHeroDataAsync,
            //回调请求
            RequestHeroCallback,
        }
        
        /// <summary>
        /// 注册所有的Recucer
        /// </summary>
        public override void RegisterReducers()
        {
            base.RegisterReducers();
            //这里用显式注册，避免函数签名错误
            //同步方法
            this.AddRecucer(Reducer06.RequestHeroDataSynchronization, RequestServer);
            //异步方法
            this.AddAsyncRecucer(Reducer06.RequestHeroDataAsync, RequestServerByAsync);
            //回调需要考虑乱序问题
            this.AddCallbackReducer(Reducer06.RequestHeroCallback, RequestServerByCallback);
        }
        
        /// <summary>
        /// url
        /// </summary>
        readonly public string url = "https://1843236967254885.cn-shanghai.fc.aliyuncs.com/2016-08-15/proxy/BDFramework/DemoForUFlux/";

        /// <summary>
        /// 同步网络请求
        /// </summary>
        /// <param name="old"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private S_HeroDataDemo6Test RequestServer(S_HeroDataDemo6Test old, object @param)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret =  wc.DownloadString(api);
            var hero = JsonMapper.ToObject<S_HeroDataDemo6Test>(ret);
            hero.Hp -= 5;
            hero.Name += 2;
            hero.MaxHp += 10;
            return hero;
        }
        
        /// <summary>
        /// 异步网络请求
        /// </summary>
        /// <param name="old"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        async private Task<S_HeroDataDemo6Test>RequestServerByAsync(S_HeroDataDemo6Test old, object @param)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret = await  wc.DownloadStringTaskAsync(api);
            var hero = JsonMapper.ToObject<S_HeroDataDemo6Test>(ret);
            hero.Hp -= 5;
            hero.Name += 2;
            hero.MaxHp += 10;
            return hero;
        }
        

        
        /// <summary>
        /// 网络请求回调
        /// </summary>
        /// <param name="old"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private void RequestServerByCallback (Store<S_HeroDataDemo6Test>.GetState getStateFunc, object @params = null, Action<S_HeroDataDemo6Test> callback = null)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            //提前注册回调
            wc.DownloadStringCompleted += (sender,download) =>
            {
                var hero = JsonMapper.ToObject<S_HeroDataDemo6Test>(download.Result);
                hero.Hp -= 5;
                hero.Name += 2;
                hero.MaxHp += 10;
                callback?.Invoke(hero);
            };
            //开始异步下载
            wc.DownloadStringAsync(new Uri(api));
        }
        
    }
}