using System;
using System.Net;
using System.Threading.Tasks;
using BDFramework.UFlux.Contains;
using BDFramework.UFlux.Reducer;
using LitJson;

namespace BDFramework.UFlux.Test
{
    /// <summary>
    /// Reducer 函数处理的集合
    /// </summary>
    public class Reducer_Demo06 : AReducers<Server_HeroData>
    {
        public enum Reducer06
        {
            //同步请求
            InvokeSyncTest,
            //异步请求
            InvokeAsyncTest,
        }
        

        
        /// <summary>
        /// url
        /// </summary>
        readonly public string url = "https://1843236967254885.cn-shanghai.fc.aliyuncs.com/2016-08-15/proxy/BDFramework/DemoForUFlux/";

        public override void RegisterReducers()
        {
            base.RegisterReducers();
        }

        /// <summary>
        /// 同步网络请求
        /// </summary>
        /// <param name="old"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        [Reducer((int)Reducer06.InvokeSyncTest)]
        private Server_HeroData RequestServer(Server_HeroData old)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret =  wc.DownloadString(api);
            var hero = JsonMapper.ToObject<Server_HeroData>(ret);
            return hero;
        }
        /// <summary>
        /// 同步网络请求
        /// </summary>
        /// <param name="old"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        [Reducer((int)Reducer06.InvokeSyncTest)]
        private Server_HeroData RequestServer2(Server_HeroData old,int @params)
        {
            BDebug.Log($"匹配参数响应InvokeSyncTest, params:{@params}");
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret =  wc.DownloadString(api);
            var hero = JsonMapper.ToObject<Server_HeroData>(ret);
            hero.Name += $"-params:{@params}";
            return hero;
        }
        /// <summary>
        /// 异步网络请求
        /// </summary>
        /// <param name="old"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        [Reducer((int)Reducer06.InvokeAsyncTest)]
        async private Task<Server_HeroData>RequestServerByAsync(Server_HeroData old)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret = await  wc.DownloadStringTaskAsync(api);
            var hero = JsonMapper.ToObject<Server_HeroData>(ret);
            return hero;
        }

    }
}