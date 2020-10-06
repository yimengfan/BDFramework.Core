using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BDFramework.ResourceMgr.V2;
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

        public delegate void LoderFunc(string name, bool isLoadObj, Action<LoadAssetState, Object> callback);

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
        private LoderFunc loadAssetAction = null;

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


        public LoaderTaskGroup(string mainAsset, int asyncTaskMaxNum, List<LoaderTaskData> taskList,
            LoderFunc loadAssetAction,
            OnTaskCompleteCallback onTaskCompleteCallback)
        {
            this.asyncTaskMaxNum = asyncTaskMaxNum;
            this.taskList = taskList;
            this.MainAsset = mainAsset;
            this.TaskQueueNum = taskList.Count;
            this.loadAssetAction = loadAssetAction;
            this.onTaskCompleteCallback += onTaskCompleteCallback;
        }

        /// <summary>
        /// 任务列表
        /// </summary>
        private List<LoaderTaskData> taskList = null;


        private int curDoTaskNum = 0;

        
        /// <summary>
        /// 开始执行任务
        /// </summary>
        public void DoNextTask()
        {
            if (isStop) return;
            //获取一个任务
            while (taskList.Count > 0 && curDoTaskNum < asyncTaskMaxNum)
            {
                var task = taskList[0];
                taskList.RemoveAt(0);
                //这一步确保主资源最后加载,防止资源自动依赖丢失
                if (task.ResourcePath == MainAsset && objectsMap.Count != this.TaskQueueNum-1)
                {
                    taskList.Add(task);
                    break;
                }
                //主资源才加载
                var isLoadObj = task.ResourcePath == MainAsset;
                //执行任务
                loadAssetAction(task.ResourcePath, isLoadObj, (state, obj) =>
                {
                    curDoTaskNum--;
                    switch (state)
                    {
                        case LoadAssetState.IsLoding: //正在加载，需要重新执行一次task
                            //插入在倒数第二个位置
                            taskList.Insert(taskList.Count-2,task); 
                            break;
                        case LoadAssetState.Fail:
                        case LoadAssetState.Success:
                            OnTaskComplete(task.ResourcePath, obj);
                            break;
                    }
                });
                curDoTaskNum++;
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
            //BDebug.Log("加载：" + this.config.GetManifestItemByHash(resPath).Name);
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