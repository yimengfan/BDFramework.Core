using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr;
using System.Threading;
using BDFramework.Net;
using LitJson;
using System.Collections;
namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 增量的版本更新下载
    /// </summary>
    public class VersionControl<T> : IVersionControl  where T :IVersionData,new()
    {
        enum HttpLayer : uint
        {
            //
            HotUpdate = 99,
        }

       public  event OnVersionContrlDownload OnDownLoading;
       public  event OnVersionContrlState OnError;
       public  event OnVersionContrlState OnSuccess;
        public int AllFileCount
        {
            get;
            private set;
        }


        private HttpMgr.IHttpMgrLayer httpLayer;
        public VersionControl(string serverAddress, string localPath,string filePath)
        {
            
            //注册一个热更专用的通道
            httpLayer = HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate);

            this.serverPath = serverAddress;
            this.localPath = localPath;
            this.filePath = filePath;

            //

        }

        private string localPath, serverPath ,filePath;
        private IVersionData localConfig;
        
        public void Start()
        {
          
            var configPath = Path.Combine(localPath, filePath);

            if (File.Exists(configPath))
            {
                localConfig = JsonMapper.ToObject<T>(File.ReadAllText(configPath));
            }
            else
            {
                localConfig = new T();
            }


            //1.本地版本和服务器版本对比
            var t = HttpMgr.Task.Create( Path.Combine( serverPath ,filePath), null, (HttpMgr.Task task, WWW w) =>
            {
                 BDeBug.I.Log("下载：" + task.url);
                if ((w.error) == null)
                {
                    var strw = System.Text.Encoding.UTF8.GetString(w.bytes);
                    var serverConfig = JsonMapper.ToObject<T>(strw);

                    //获得本地与服务器的差集
                    var diffList = localConfig.CompareWithOther(serverConfig);
                    AllFileCount = diffList.Count;
                    //获取差集
                    if (AllFileCount > 0)
                    {
                        CreateDownloadTask(diffList);
                        DoNextDownloadTask();
                    }
                    else
                    {
                        //直接完成
                        OnSuccess("下载完成:");
                    }

                }
                else
                {
                    Debug.LogError(w.error);
                }

            });

            httpLayer.QueueTask(t);
        }

        /// <summary>
        /// 继续下载任务
        /// </summary>
        public  void Continue()
        {
            foreach(var t in downloadTaskQue)
            {
                t.ResetRetryCount();
            }
            DoNextDownloadTask();
        }

        private void CreateDownloadTask(List<string> list)
        {
            int counter = 0;
            var fatherPath = Path.GetDirectoryName(filePath);
            string _p = Path.Combine(this.localPath, fatherPath);
            if(Directory.Exists(_p) ==false)
            {
                Directory.CreateDirectory(_p);
            }
            foreach(var name in list)
            {
                counter++;
                var i = counter;
                var fn = name;
                var t = HttpMgr.Task.Create(Path.Combine(serverPath+"/"+ fatherPath , fn), null, (HttpMgr.Task task, WWW w) =>
                {
                     BDeBug.I.Log("下载：" + task.url);
                    var strw = System.Text.Encoding.UTF8.GetString(w.bytes);
                    var writePath = Path.Combine(_p, name);
                    File.WriteAllText(writePath, strw);


                    //
                    localConfig.FileInfoMap[fn] = HashHelper.CreateMD5ByFile(writePath);
                    //
                     BDeBug.I.Log("写入:" + writePath);
                  
                    OnDownLoading(i, "正在下载:");
                    DoNextDownloadTask();
                });

                t.Error += (task) =>
                {
                    if (task.RetryCount < 3)
                    {
                        task.AddRetryCount();
                        Debug.LogError("下载错误:" + fn + ",先跳过执行下一个");
                        DoNextDownloadTask();
                    }
                    else
                    {
                        OnError("下载失败,是否重试?");
                    }
                };
                //压入
                this.downloadTaskQue.Enqueue(t);
            }
        }
        /// <summary>
        /// 版本下载队列
        /// </summary>
        Queue<HttpMgr.Task> downloadTaskQue = new Queue<HttpMgr.Task>();

        /// <summary>
        /// 开始每个版本的下载
        /// </summary>
        /// <param name="processCallback"></param>
        public void DoNextDownloadTask()
        {
            if (downloadTaskQue.Count > 0)
            {
                var curtask = downloadTaskQue.Dequeue();
                BDeBug.I.Log("下载文件：" + curtask.url);
                httpLayer.QueueTask(curtask);
            }
            else
            {
                //配置,写入本地
                var content = JsonMapper.ToJson(this.localConfig);
                File.WriteAllText(Path.Combine(localPath, filePath), content);
                //
                OnSuccess("下载完成");
            }
        }


    }
}