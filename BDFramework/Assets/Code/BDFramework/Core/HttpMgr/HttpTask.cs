using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
namespace BDFramework.Http
{
    public interface IHttpTask
    {
        string Url { get; }

        /// <summary>
        /// 是否获取cookie
        /// </summary>
        bool IsGetCookie { get; }

        /// <summary>
        /// 重试次数
        /// </summary>
        int RetryCount { get; }
    }
    public class GetTask : IHttpTask
    {
        public string Url { get; private set; }
        public Action<byte[]> CallBack;

        public bool IsGetCookie { get; private set; }

        public int RetryCount { get; private set; }

        public GetTask(string Url , Action<byte[]> callback ,int retryCount = 5,  bool IsGetCookie =false)
        {
            this.Url = Url;
            this.CallBack = callback;
            this.IsGetCookie = IsGetCookie;
            this.RetryCount = retryCount;
        }
    }


    public class PostTask : IHttpTask
    {
        public string Url { get; private set; }
        public bool IsGetCookie { get; private set; }
        public int RetryCount { get; private set; }

        public event Action<byte[]> CallBack;

        public string UpLoadData;
        public PostTask(string Url, Action<byte[]> callback ,object o ,  int retryCount = 5,  bool IsGetCookie = false)
        {
            this.Url = Url;
            this.CallBack = callback;
            if (o is string)
            {
                this.UpLoadData = o.ToString();
            }
            else
            {    
                this.UpLoadData = JsonMapper.ToJson(o);
            }

            this.RetryCount = retryCount;
        }
    }

    public class GetFileTask : IHttpTask
    {
        public string Url { get; private set; }
        
        public string FileSavePath { get; set; }

        public bool IsGetCookie { get; private set; }

        public int RetryCount { get; private set; }

        public  Action<byte[]> CallBack;
    }
    public class PostFileTask : IHttpTask
    {
        public string Url { get; private set; }

        public bool IsGetCookie { get; private set; }

        public int RetryCount { get; private set; }

        public  Action<byte[]> CallBack;
    }
}
