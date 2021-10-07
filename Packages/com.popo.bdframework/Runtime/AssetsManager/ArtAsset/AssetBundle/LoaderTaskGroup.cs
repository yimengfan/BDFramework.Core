using System;
using System.Collections;
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
        public string AssetPath { get; private set; }
        public Type LoadType { get; private set; }
        public bool IsMainAsset { get; private set; }

        public LoaderTaskData(string assetPath, Type t, bool isMainAsset = false)
        {
            this.AssetPath = assetPath;
            this.LoadType = t;
            this.IsMainAsset = isMainAsset;
        }
    }


    /// <summary>
    /// 加载任务组，每个组，负责一个load资源的操作
    /// 可能含有多个依赖资源
    /// </summary>
    public class LoaderTaskGroup
    {
        public int Id { get; set; }

        /// <summary>
        /// is complete
        /// </summary>
        public bool IsComplete { get; private set; }

        public delegate void OnTaskCompleteCallbackDelegate(string s, Object o);

        /// <summary>
        /// all  task complete  callback
        /// </summary>
        public OnTaskCompleteCallbackDelegate OnAllTaskCompleteCallback { get; set; } = null;


        /// <summary>
        /// 异步任务颗粒度，每帧执行多少个
        /// </summary>
        static readonly public int ASYNC_TASK_NUM = 5;

        /// <summary>
        /// the task queue num
        /// </summary>
        public int totalTaskNum { get; private set; } // = 0;


        /// <summary>
        /// main asset path
        /// </summary>
        private string MainAssetName { get; set; }

        /// <summary>
        /// 加载的manifest
        /// </summary>
        private AssetBundleItem AssetBundleItem { get; set; }

        private AssetBundleMgrV2 loder { get; set; }

        public LoaderTaskGroup(AssetBundleMgrV2 loder,
            string mainAssetName,
            AssetBundleItem assetBundleItem,
            List<LoaderTaskData> taskList,
            OnTaskCompleteCallbackDelegate onAllTaskCompleteCallbackDelegate)
        {
            this.loder = loder;
            this.taskList = taskList;
            //主资源
            this.MainAssetName = mainAssetName;
            this.AssetBundleItem = assetBundleItem;

            this.totalTaskNum = taskList.Count;
            this.OnAllTaskCompleteCallback += onAllTaskCompleteCallbackDelegate;
        }

        /// <summary>
        /// 任务列表
        /// </summary>
        private List<LoaderTaskData> taskList = null;

        private int curDoTaskConter = 0;

        /// <summary>
        /// 分组执行任务，每组加载ASYNC_TASK_NUM个任务
        /// </summary>
        public void DoNextTask()
        {
            if (isStop)
            {
                return;
            }

            //获取一个任务
            while (taskList.Count > 0 && curDoTaskConter < ASYNC_TASK_NUM)
            {
                var task = taskList[0];
                taskList.RemoveAt(0);
                //这一步确保主资源最后加载,防止资源自动依赖丢失
                if (task.IsMainAsset && taskList.Count != 0)
                {
                    taskList.Add(task);
                    continue;
                }
                //主资源才加载
                IEnumeratorTool.StartCoroutine(IE_AsyncLoadAssetbundle(task, (ret) =>
                {
                    curDoTaskConter--;
                    
                    switch (ret)
                    {
                        case LoadAssetState.IsLoding:
                            //正在加载，需要重新压入task
                            taskList.Add( task);
                            break;
                        case LoadAssetState.Fail:
                        case LoadAssetState.Success:
                        {
                            if (isStop)
                            {
                                return;
                            }
                            //判断任务进度
                            if (curDoTaskConter>0)
                            {
                                this.DoNextTask();
                            }
                            else
                            {
                                //总进度通知
                                IsComplete = true;
                                var instObj = loder.LoadFormAssetBundle<Object>(this.MainAssetName, this.AssetBundleItem);
                                OnAllTaskCompleteCallback?.Invoke(MainAssetName, instObj);
                            }
                        }
                            break;
                    }
                }));

                curDoTaskConter++;
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


        #region 加载接口

        /// <summary>
        /// 当前正在加载的所有AB,静态 整个共享
        /// </summary>
        static HashSet<string> lockSet = new HashSet<string>();


        /// <summary>
        ///  加载
        /// 一般来说,主资源才需要load
        /// 依赖资源只要加载ab,会自动依赖
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="isMainAsset">是否需要返回加载资源</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IEnumerator IE_AsyncLoadAssetbundle(LoaderTaskData task, Action<LoadAssetState> callback)
        {
            //正在被加载中,放入后置队列
            if (lockSet.Contains(task.AssetPath))
            {
                callback(LoadAssetState.IsLoding);
                yield break;
            }

            //没被加载
            if (!loder.AssetbundleMap.ContainsKey(task.AssetPath))
            {
                AssetBundleCreateRequest ret = null;
                string fullpath = "";

                lockSet.Add(task.AssetPath); //加锁
                {
                    fullpath = loder.FindMultiAddressAsset(task.AssetPath);
                    ret = AssetBundle.LoadFromFileAsync(fullpath);
                    yield return ret;
                }
                lockSet.Remove(task.AssetPath); //解锁
                //添加assetbundle
                if (ret.assetBundle != null)
                {
                    loder.AddAssetBundle(task.AssetPath, ret.assetBundle);
                }
                else
                {
                    callback(LoadAssetState.Fail);
                    BDebug.LogError("ab资源为空:" + fullpath);
                    yield break;
                }
            }


            callback(LoadAssetState.Success);
        }

        #endregion
    }
}