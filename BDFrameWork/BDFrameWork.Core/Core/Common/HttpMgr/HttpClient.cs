using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BDFramework.Http
{
    public class HttpClient : WebClient
    {
        public HttpClient() 
        {
            this.Encoding = Encoding.GetEncoding("utf-8");
            this.Headers.Add("Content-Type", "application/json;charset=utf-8");
           // this.Headers.Add(HttpRequestHeader.Cookie, $@"");
        }
        
        /// <summary>
        /// get  异步回调
        /// </summary>
        /// <param name="url"></param>
        public void Get(string url)
        {
            this.DownloadDataAsync(new Uri(url));
        }

        
        /// <summary>
        /// get  异步堵塞
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetAsync(string url)
        {
           var  t = this.DownloadDataTaskAsync(new Uri(url));
            
            return  Encoding.UTF8.GetString( t.Result);   
        }
        
        /// <summary>
        /// post 异步回调
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        public void  PostData(string url ,string data)
        {
            this.UploadDataAsync(new Uri(url), "POST" , System.Text.Encoding.UTF8.GetBytes(data));
            
        }

        /// <summary>
        /// post 异步堵塞
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public  string PostDataAsync(string url ,string data)
        {
//            Debug.Log("");
//              this.UploadDataTaskAsync(new Uri(url), "POST" , System.Text.Encoding.UTF8.GetBytes(data) );
            
            var task =    this.UploadDataTaskAsync(new Uri(url), "POST" , System.Text.Encoding.UTF8.GetBytes(data) );
             //task.Wait();
             return  Encoding.UTF8.GetString( task.Result);   
        }

        public void DownloadFile()
        {

        }

        public void PostFile()
        {

        }

        private void SetCookie()
        {

        }
    }
}
