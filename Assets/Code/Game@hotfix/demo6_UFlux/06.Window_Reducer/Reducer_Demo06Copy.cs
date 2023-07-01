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
    public class S_HeroDataDemo6Copy : StateBase
    {
        public string Name;
        public int Hp;
        public int MaxHp;
    }
    /// <summary>
    /// Reducer 函数处理的集合
    /// </summary>
    public class Reducer_Demo06Copy : AReducers<S_HeroDataDemo6Copy>
    {
        public enum Reducer06
        {
            //同步请求
            RequestHeroDataSync,
            //异步请求
            RequestHeroDataAsync,
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
        [Reducer((int)Reducer06.RequestHeroDataSync)]
        private S_HeroDataDemo6Copy RequestServer(S_HeroDataDemo6Copy old)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret =  wc.DownloadString(api);
            var hero = JsonMapper.ToObject<S_HeroDataDemo6Copy>(ret);
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
        [Reducer((int)Reducer06.RequestHeroDataAsync)]
        async private Task<S_HeroDataDemo6Copy>RequestServerByAsync(S_HeroDataDemo6Copy old)
        {
            var api = url + "api/bdframework/getherodata";
            WebClient  wc=new WebClient();
            string ret = await  wc.DownloadStringTaskAsync(api);
            var hero = JsonMapper.ToObject<S_HeroDataDemo6Copy>(ret);
            hero.Hp -= 5;
            hero.Name += 2;
            hero.MaxHp += 10;
            return hero;
        }
        
        
    }
}