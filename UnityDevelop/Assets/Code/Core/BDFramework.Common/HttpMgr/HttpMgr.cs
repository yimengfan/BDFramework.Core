using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using UnityEngine;
using System.Collections;
namespace BDFramework.Net
{
    public class HttpMgr
    {
        /// <summary>
        /// 单例
        /// </summary>
        private static HttpMgr g_this;
        private HttpMgr()
        {

        }
        public static HttpMgr Instance
        {
            get
            {
                if (g_this == null)
                    g_this = new HttpMgr();
                return g_this;
            }
        }

        Dictionary<uint, HttpMgrLayer> layers = new Dictionary<uint, HttpMgrLayer>();
        /// <summary>
        /// 分层，每层独立队列
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public IHttpMgrLayer GetLayer(uint layer = 0)
        {
            if (layer > 100)
                throw new Exception("用了太多通道");
            if (layers.ContainsKey(layer) == false)
              layers[layer] = new HttpMgrLayer();
            
            return layers[layer];
        }

        public class Task
        {

            //public httpData
            private Task()
            {

            }
            public string url;
            public int RetryCount { get; private set; }
            public void AddRetryCount()
            {
                RetryCount++;
            }

            public void ResetRetryCount()
            {
                RetryCount = 0;
            }
            public HttpForm Form { get; set; }
            /// </summary>
            public Action<Task, WWW> callback;
            //public Dispatcher dispatcher;
            public static Task Create(string url, HttpForm form,
                                      Action<Task, WWW> callback)
            {
                Task task = new Task();
                task.url = url;
                task.callback = callback;          
                task.Form = form;
                return task;
            }

            public Action TimeOut;
            public Action<Task> Error;
        }

      public   class MyWebClient 
        {
            public WWW WWW
            {
                get;
                set;
            }
            //
            public int timeout = 10;
        }
        public interface IHttpMgrLayer
        {
            void QueueTask(params Task[] tasks);
            int timeout
            {
                get;
                set;
            }
        }

        class HttpMgrLayer : IHttpMgrLayer
        {
            public int timeout
            {
                get
                {
                    return webclient.timeout;
                }
                set
                {
                    webclient.timeout = value;
                }
            }
            public HttpMgrLayer()
            {
                webclient = new MyWebClient();
                httpTask = new Queue<Task>();
            }

            Queue<Task> httpTask;
            public void QueueTask(Task[] tasks)
            {
                foreach (var t in tasks)
                {
                    httpTask.Enqueue(t);
                }
              
                if (now == null)
                    DoNext();
            }
            Task now;//当前任务
            void DoNext()
            {
                if (httpTask.Count >0)
                {
                    now = httpTask.Dequeue();
                    IEnumeratorTool.StartCoroutine(DoTask(now));
                }

            }
            MyWebClient webclient;
          
            IEnumerator DoTask(Task task)
            {
                var www = webclient.WWW;
                task.url = task.url.Replace("\\", "/");
                if (task.Form == null) //get
                {                    
                    www = new WWW(task.url);
                }
                else //post
                {
                    www = new WWW(task.url, task.Form.data);
                }
                float timer = 0;
                while (www.error == null && timer < webclient.timeout && www.isDone == false)
                {
                    timer += Time.deltaTime;
                    yield return 0;
                }

               //已完成,没超时
                if (www.error == null && www.isDone)
                {
                    task.callback(task, www);
                }
                //没完成,超时
                else
                {
                    Debug.LogError("超时:" + task.url);
                    if(task.Error!=null)
                               task.Error(task);
                    yield break;
                }

                //顺利完成
                now = null;
                DoNext();

                yield break;
            }

            void OnTimeOut(Task task)
            {

               
                 BDeBug.I.Log("当前网络不稳定，是否继续？");
                //MainThreadCallTools.StartCoroutine(DoTask(task));
                
            }
        }
    }
}
