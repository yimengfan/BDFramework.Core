using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BDFramework.Http
{
   public  class HttpLayer
   {

        private HttpClient client;
        private Queue<IHttpTask> httpTaskQue;
        IHttpTask curTask = null;
        public HttpLayer()
        {
            httpTaskQue = new Queue<IHttpTask>();
            client = new HttpClient();
            //
//            client.UploadDataCompleted += Client_UploadDataCompleted;
//            client.UploadFileCompleted += Client_UploadFileCompleted;
//            client.OpenReadCompleted += Client_OpenReadCompleted;
        }

        #region  Http回调
        /// <summary>
        /// Get请求 回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_OpenReadCompleted(object sender, System.Net.OpenReadCompletedEventArgs e)
        {
            Console.WriteLine(sender.ToString());
            DoNext();
        }

        /// <summary>
        /// 上传文件回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_UploadFileCompleted(object sender, System.Net.UploadFileCompletedEventArgs e)
        {
            Console.WriteLine(sender.ToString());
            DoNext();
        }

        /// <summary>
        /// 上传数据完成回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_UploadDataCompleted(object sender, System.Net.UploadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.Log(e.Error.ToString());
            }
            Debug.Log(Convert.ToString( System.Text.Encoding.UTF8.GetString ( e.Result)));
            DoNext();
        }
        #endregion

       /// <summary>
       /// 通过队列回调方式执行
       /// </summary>
       /// <param name="task"></param>
        public void DoTaskByCallBack(IHttpTask task)
        {
            httpTaskQue.Enqueue(task);
            if(curTask == null)
            {
                DoNext();
            }
        }

       /// <summary>
       /// 异步堵塞
       /// </summary>
       /// <param name="task"></param>
       /// <returns></returns>
       public string DoTaskAsync(IHttpTask task)
       {
           if(task is GetTask)
           {
               var data = task as GetTask;
               var   result = client.GetAsync(data.Url);
               return result;
           }
           else if(task is PostTask)
           {
               var data = task as PostTask;
               var   result = client.PostDataAsync(data.Url, data.UpLoadData);
               return result;
           }

           return "error";
       }

       /// <summary>
       /// 执行任务
       /// </summary>
       /// <param name="task"></param>
       public void DoTask(IHttpTask task)
       {
           if(task is GetTask)
           {
                
           }
           else if(task is PostTask)
           {
               var data = task as PostTask;
               client.PostData(data.Url, data.UpLoadData);
           }
       }
        private void DoNext()
        {
            //判断什么时候退出
            if(httpTaskQue.Count ==0)
            {
                curTask = null;
                return;
            }

            curTask = httpTaskQue.Dequeue();
            DoTask(curTask);
        }
   }
}
