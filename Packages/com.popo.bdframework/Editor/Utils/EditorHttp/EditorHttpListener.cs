using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// EditorHttp监听器
    /// </summary>
    public class EditorHttpListener
    {
        #region 基本属性

        public string Host { get; set; }
        public string port { get; set; }
        private string _webHomeDir;
        private HttpListener listener = new HttpListener();
        private Thread listenThread;

        /// <summary>  
        /// http服务根目录  
        /// </summary>  
        public string WebHomeDir
        {
            get { return this._webHomeDir; }
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception("http服务器设置的根目录不存在!");
                this._webHomeDir = value;
            }
        }

        /// <summary>  
        /// 服务器是否在运行  
        /// </summary>  
        public bool IsRunning
        {
            get { return (listener == null) ? false : listener.IsListening; }
        }

        #endregion

        #region 处理http业务

        /// <summary>  
        /// 启动服务  
        /// </summary>  
        public void Start(string host, string port)
        {
            //http相关配置
            this.Host = host;
            this.port = port;
            //开始监听逻辑
            if (listener.IsListening)
                return;
            if (!string.IsNullOrEmpty(Host) && Host.Length > 0)
            {
                listener.Prefixes.Add("http://" + Host + ":" + this.port + "/");
            }
            else if (listener.Prefixes == null || listener.Prefixes.Count == 0)
            {
                listener.Prefixes.Add("http://localhost:" + this.port + "/");
            }

            listener.Start();
            listenThread = new Thread(AcceptClient);
            listenThread.Name = "httpserver";
            listenThread.Start();
        }

        /// <summary>  
        /// 停止服务  
        /// </summary>  
        public void Stop()
        {
            try
            {
                listener?.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        /// <summary>  
        /// /接受客户端请求  
        /// </summary>  
        void AcceptClient()
        {
            while (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    //new Thread(HandleRequest).Start(context);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleRequest), context);
                }
                catch
                {
                }
            }
        }

        #endregion


        //处理客户端请求  
        private void HandleRequest(object ctx)
        {
            HttpListenerContext context = ctx as HttpListenerContext;
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;
            try
            {
                //格式化url
                string rawUrl = Uri.UnescapeDataString(request.RawUrl);
                int paramStartIndex = rawUrl.IndexOf('?');
                if (paramStartIndex > 0)
                    rawUrl = rawUrl.Substring(0, paramStartIndex);
                else if (paramStartIndex == 0)
                    rawUrl = "";
                if (rawUrl.StartsWith("/"))
                {
                    rawUrl = rawUrl.Substring(1, rawUrl.Length - 1);
                }
                //
                rawUrl = rawUrl.Replace("//", "/");
                

                string apiFuc = "";
                string apiParams = "";
                var slashIdx = rawUrl.IndexOf("/");
                if (slashIdx > 0)
                {
                    apiFuc = rawUrl.Substring(0, slashIdx);
                    apiParams = rawUrl.Substring(slashIdx + 1);
                }
                else
                {
                    //只有参数,没有协议
                    apiParams = rawUrl;
                }


                //调用proccesor
                InvokeProccessor(apiFuc, apiParams, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                {
                    writer.WriteLine(ex.Data);
                }
            }

            try
            {
                response.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }


        /// <summary>
        /// WebApi处理器的delegate
        /// </summary>
        public delegate void WebAPIProccessor(string apiParams, HttpListenerResponse response);

        public Dictionary<string, WebAPIProccessor> WebAPIProccessorMap = new Dictionary<string, WebAPIProccessor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///  添加webAPI处理器
        /// </summary>
        public void AddWebAPIProccesor<T>() where T : IWebApiProccessor
        {
            var proccessor = Activator.CreateInstance<T>();
            if (!WebAPIProccessorMap.ContainsKey(proccessor.WebApiName))
            {
                WebAPIProccessorMap[proccessor.WebApiName] = proccessor.WebAPIProccessor;
            }
            else
            {
                Debug.Log("已经存在WebProcesssor监听! - " + typeof(T).Name);
            }
        }

        /// <summary>
        /// 触发Processsor
        /// </summary>
        /// <param name="apifunc"></param>
        /// <param name="apiParams"></param>
        /// <param name="response"></param>
        public void InvokeProccessor(string apifunc, string apiParams, HttpListenerResponse response)
        {
            var ret = WebAPIProccessorMap.TryGetValue(apifunc, out var proccessor);

            if (ret)
            {
                proccessor?.Invoke(apiParams, response);
            }
            else
            {
                throw new Exception("无webapi处理器:" + apifunc);
            }
        }
    }
}
