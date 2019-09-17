using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LitJson;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 单个任务的数据存储
    /// </summary>
    public class LoaderTaskData
    {
        public LoaderTaskData(string resourcePath, Type t)
        {
            this.ResourcePath = resourcePath;
            this.LoadType = t;
        }

        public string ResourcePath { get; private set; }
        public Type LoadType { get; private set; }
    }


    /// <summary>
    /// 这是加载任务的组,每组任务可能有1个以上taskid组成
    /// </summary>
    public class LoaderTaskGroup
    {
        public int Id { get; set; }

        /// <summary>
        /// is complete
        /// </summary>
        public bool IsComplete { get; private set; }


        public delegate void OnTaskCompleteCallback(string s, Object o);

        /// <summary>
        /// all  task complete  callback
        /// </summary>
        public OnTaskCompleteCallback onTaskCompleteCallback = null;

        /// <summary>
        /// all res callback
        /// </summary>
        Dictionary<string, Object> objectsMap = new Dictionary<string, Object>();

        /// <summary>
        /// load function
        /// </summary>
        /// <returns></returns>
        private Action<string, Action<LoadAssetState, Object>> loadAssetAction = null;

        /// <summary>
        /// the max do task number
        /// </summary>
        private int asyncTaskMaxNum = 1;

        /// <summary>
        /// the task queue num
        /// </summary>
        public int TaskQueueNum { get; private set; } // = 0;


        /// <summary>
        /// main asset path
        /// </summary>
        public string MainAsset { get; private set; }


        public LoaderTaskGroup(int asyncTaskMaxNum, Queue<LoaderTaskData> taskQueue,
            Action<string, Action<LoadAssetState, Object>> loadAssetAction,
            OnTaskCompleteCallback onTaskCompleteCallback)
        {
            this.asyncTaskMaxNum = asyncTaskMaxNum;
            this.TaskQueue = taskQueue;
            this.MainAsset = this.TaskQueue.Last().ResourcePath;
            this.TaskQueueNum = taskQueue.Count;
            this.loadAssetAction = loadAssetAction;
            this.onTaskCompleteCallback += onTaskCompleteCallback;
        }

        /// <summary>
        /// 任务列表
        /// </summary>
        private Queue<LoaderTaskData> TaskQueue = null;


        private int curDoTaskNum = 0;

        /// <summary>
        /// 开始执行任务
        /// </summary>
        public void DoNextTask()
        {
            if (isStop) return;
            if (MainAsset == "assets/resource/runtime/char/026.prefab")
            {
                int i = 0;
            }

            //获取一个任务
            while (TaskQueue.Count > 0 && curDoTaskNum < asyncTaskMaxNum)
            {
                var task = TaskQueue.Dequeue();
                
                //执行任务
                loadAssetAction(task.ResourcePath, (state, obj) =>
                {
                    curDoTaskNum--;
                    switch (state)
                    {
                        case LoadAssetState.IsLoding:  //正在加载，需要重新执行一次task
                            TaskQueue.Enqueue(task);
                            break;
                        case LoadAssetState.Fail:
                        case LoadAssetState.Success:
                            OnTaskComplete(task.ResourcePath, obj);
                            break;
                    }
                });
                curDoTaskNum++;
                
//               BDebug.Log("加载：" + task.ResourcePath);
            }
        }

        /// <summary>
        /// 一个任务完成的回调
        /// </summary>
        /// <param name="resPath"></param>
        /// <param name="obj"></param>
        private void OnTaskComplete(string resPath, Object obj)
        {
            this.objectsMap[resPath] = obj;
            if (isStop) return;
            if (this.objectsMap.Count != TaskQueueNum)
            {
                this.DoNextTask();
            }
            else //所有任务完成
            {
                IsComplete = true;
                //总进度通知
                if (onTaskCompleteCallback != null)
                {
                    onTaskCompleteCallback(MainAsset, this.objectsMap[MainAsset]);
                }

//                BDebug.Log("当前任务组完成----------------------------------------");
            }
        }

        private bool isStop = false;

        /// <summary>
        /// stop the task
        /// </summary>
        public void Stop()
        {
            isStop = true;
        }
    }
}