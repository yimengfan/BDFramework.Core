using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.Editor.EditorPipeline.PublishPipeline
{
    public class EditorHttpListener
    {
        #region

        public string Host { get; set; }
        public string port { get; set; }
        private string _webHomeDir;
        private HttpListener listener = new HttpListener();
        private Thread listenThread;
        private string directorySeparatorChar = Path.DirectorySeparatorChar.ToString();

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

        #region

        /// <summary>  
        /// 启动服务  
        /// </summary>  
        public void Start(string host, string port, string webHomeDir)
        {
            //http相关配置
            this.Host = host;
            this.port = port;
            this._webHomeDir = webHomeDir;
            Debug.Log("文件服务器:" + _webHomeDir);
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

        #region HandleRequest

        //处理客户端请求  
        private void HandleRequest(object ctx)
        {
            HttpListenerContext context = ctx as HttpListenerContext;
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;
            try
            {
                string rawUrl = Uri.UnescapeDataString(request.RawUrl);
                int paramStartIndex = rawUrl.IndexOf('?');
                if (paramStartIndex > 0)
                    rawUrl = rawUrl.Substring(0, paramStartIndex);
                else if (paramStartIndex == 0)
                    rawUrl = "";

                #region 文件请求

                {
                    string filePath = WebHomeDir + rawUrl;
                    //替换
                    // var platforms = BDApplication.GetSupportPlatform();
                    // foreach (var platform in platforms)
                    // {
                    //     var platformStr = BDApplication.GetPlatformPath(platform);
                    //     filePath = filePath.Replace(platformStr, platformStr + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX);
                    // }

                    if (!File.Exists(filePath))
                    {
                        response.ContentLength64 = 0;
                        response.StatusCode = 404;
                        response.Abort();
                    }
                    else
                    {
                        response.StatusCode = 200;
                        string exeName = Path.GetExtension(filePath);
                        response.ContentType = GetContentType(exeName);
                        FileStream fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                        int byteLength = (int) fileStream.Length;
                        byte[] fileBytes = new byte[byteLength];
                        fileStream.Read(fileBytes, 0, byteLength);
                        fileStream.Close();
                        fileStream.Dispose();
                        response.ContentLength64 = byteLength;
                        response.OutputStream.Write(fileBytes, 0, byteLength);
                        response.OutputStream.Close();
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                {
                    writer.WriteLine("接收完成！");
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

        #endregion

        #region GetContentType

        /// <summary>  
        /// 获取文件对应MIME类型  
        /// </summary>  
        /// <param name="fileExtention">文件扩展名,如.jpg</param>  
        /// <returns></returns>  
        protected string GetContentType(string fileExtention)
        {
            if (string.Compare(fileExtention, ".html", true) == 0 || string.Compare(fileExtention, ".htm", true) == 0)
                return "text/html;charset=utf-8";
            else if (string.Compare(fileExtention, ".js", true) == 0)
                return "application/javascript";
            else if (string.Compare(fileExtention, ".css", true) == 0)
                return "application/javascript";
            else if (string.Compare(fileExtention, ".png", true) == 0)
                return "image/png";
            else if (string.Compare(fileExtention, ".jpg", true) == 0 || string.Compare(fileExtention, ".jpeg", true) == 0)
                return "image/jpeg";
            else if (string.Compare(fileExtention, ".gif", true) == 0)
                return "image/gif";
            else if (string.Compare(fileExtention, ".swf", true) == 0)
                return "application/x-shockwave-flash";
            else
                return ""; //application/octet-stream
        }

        #endregion

        #region WriteStreamToFile

        //const int ChunkSize = 1024 * 1024;
        private void WriteStreamToFile(BinaryReader br, string fileName, long length)
        {
            byte[] fileContents = new byte[] { };
            var bytes = new byte[length];
            int i = 0;
            while ((i = br.Read(bytes, 0, (int) length)) != 0)
            {
                byte[] arr = new byte[fileContents.LongLength + i];
                fileContents.CopyTo(arr, 0);
                Array.Copy(bytes, 0, arr, fileContents.Length, i);
                fileContents = arr;
            }

            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(fileContents);
                }
            }
        }

        #endregion
    }
}