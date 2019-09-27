using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BDFramework.Http
{
    public class HttpLayer
    {
        private HttpClient client;
        private Queue<IHttpTask> httpTaskQue;
        IHttpTask curTask = null;

        public HttpLayer()
        {
            httpTaskQue = new Queue<IHttpTask>();
            client = new HttpClient();
        }


        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="task"></param>
        public async void DoTask(IHttpTask task)
        {
            if (task is GetTask)
            {
                var t = task as GetTask;
                var bytes = await client.DownloadDataTaskAsync(new Uri(t.Url));
                t.CallBack(bytes);
            }
            else if (task is GetFileTask)
            {
                var t = task as GetFileTask;
                await client.DownloadFileTaskAsync(t.Url, t.FileSavePath);
                t.CallBack(new byte[0]);
            }
            else if (task is PostFileTask)
            {
                
            }
            else if (task is PostTask)
            {
                var t = task as PostTask;
                await client.UploadDataTaskAsync(new Uri(t.Url), System.Text.Encoding.UTF8.GetBytes(t.UpLoadData));
            }

            DoNext();
        }

        private void DoNext()
        {
            //判断什么时候退出
            if (httpTaskQue.Count == 0)
            {
                curTask = null;
                return;
            }

            curTask = httpTaskQue.Dequeue();
            DoTask(curTask);
        }
    }
}