using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using BDFramework.Core.Tools;
using LitJson;
using UnityEngine;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// Editor http 返回的数据
    /// </summary>
    public class EditorHttpResonseData
    {
        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool err = false;
        /// <summary>
        /// 返回的结构
        /// </summary>
        public string content="";
    }
    /// <summary>
    /// EditorHttp监听器
    /// </summary>
    public class EditorHttpListener
    {
        #region 基本属性

        public string Host { get; set; }
        public string port { get; set; }
        //
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
        /// <param name="host">主机地址</param>
        /// <param name="ports">端口号,多传参则为备用</param>
        public void Start(string host, params string[] ports)
        {
            if (listener.IsListening)
            {
                return;
            }


            for (int i = 0; i < ports.Length; i++)
            {
                var tryPort = ports[i];
                //开始监听逻辑
                try
                {
                    if (!string.IsNullOrEmpty(host) && host.Length > 0)
                    {
                        listener.Prefixes.Add("http://" + host + ":" + tryPort + "/");
                    }
                    else if (listener.Prefixes == null || listener.Prefixes.Count == 0)
                    {
                        listener.Prefixes.Add("http://+:" + tryPort + "/");
                    }
                    listener.Start();
                    //赋值本地
                    this.Host = host;
                    this.port = tryPort;
                    if (i > 0)
                    {
                        Debug.Log($"【EditorHttpService】备用端口号生效 - http://{host}:{tryPort}");
                    }
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }



            listenThread = new Thread(AcceptClient);
            listenThread.Name = "httpserver";
            listenThread.Start();
        }


        private void StartHttpServer()
        {
            
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

                var retdata = new EditorHttpResonseData();
                retdata.err = true;
                retdata.content = ex.Message;
                
                //返回
                response.StatusCode = 400;
                response.ContentType = "text/plain";
                using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                {
                    writer.WriteLine(JsonMapper.ToJson(retdata));
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
