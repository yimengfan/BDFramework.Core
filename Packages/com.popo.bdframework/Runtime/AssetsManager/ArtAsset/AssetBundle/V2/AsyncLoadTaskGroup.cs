using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public class AsyncLoadTaskGroup
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
        public string MainAssetName { get; private set; }

        /// <summary>
        /// 加载的manifest
        /// </summary>
        private AssetBundleItem AssetBundleItem { get; set; }

        private AssetBundleMgrV2 loder { get; set; }

        public AsyncLoadTaskGroup(AssetBundleMgrV2 loder,
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
        public void Do()
        {
            if (isCancel)
            {
                return;
            }

            //加载
            IEnumeratorTool.StartCoroutine(this.IE_LoadAssetbundles());
        }


        private bool isCancel = false;

        /// <summary>
        /// 取消 the task
        /// </summary>
        public void Cancel()
        {
            isCancel = true;
        }


        /// <summary>
        /// loding缓存表 防止重复加载
        /// </summary>
        private static Dictionary<string, AssetBundleCreateRequest> AB_LODING_LOCK_MAP = new Dictionary<string, AssetBundleCreateRequest>();

        /// <summary>
        /// 加载Asssetbundles
        /// </summary>
        /// <returns></returns>
        IEnumerator IE_LoadAssetbundles()
        {
            var loadABStatusList = new List<KeyValuePair<string, AssetBundleCreateRequest>>(ASYNC_TASK_NUM);

            //有加载任务或者任务未完成
            while (loadABStatusList.Count > 0 || taskList.Count > 0)
            {
                //1.添加任务
                while (taskList.Count > 0 && loadABStatusList.Count < ASYNC_TASK_NUM)
                {
                    var task = taskList[0];
                    taskList.RemoveAt(0);

                    //没有被加载过
                    if (!loder.AssetbundleMap.ContainsKey(task.AssetPath))
                    {
                        //判断是否在加载中
                        AB_LODING_LOCK_MAP.TryGetValue(task.AssetPath, out var exsitLoadingStatus);

                        if (exsitLoadingStatus != null)
                        {
                            loadABStatusList.Add(new KeyValuePair<string, AssetBundleCreateRequest>(task.AssetPath, exsitLoadingStatus));
                        }
                        else
                        {
                            //开始加载逻辑
                            var filePath = loder.FindMultiAddressAsset(task.AssetPath);
                            var ret = AssetBundle.LoadFromFileAsync(filePath);
                            //添加到loding表
                            AB_LODING_LOCK_MAP.Add(task.AssetPath, ret);

                            //添加到状态表
                            loadABStatusList.Add(new KeyValuePair<string, AssetBundleCreateRequest>(task.AssetPath, ret));
                        }
                    }
                }

                //2.检测加载状态
                for (int i = loadABStatusList.Count - 1; i >= 0; i--)
                {
                    var item = loadABStatusList[i];
                    var assetPath = item.Key;
                    var abcr = item.Value;
                    //判断是否成功
                    if (abcr.isDone)
                    {
                        //添加到返回列表
                        if (abcr.assetBundle != null)
                        {
                            loder.AddAssetBundle(assetPath, abcr.assetBundle);
                        }
                        else
                        {
                            BDebug.LogError("【LoadGroup】ab资源为空:" + assetPath);
                        }

                        //移除列表
                        loadABStatusList.RemoveAt(i);
                        AB_LODING_LOCK_MAP.Remove(assetPath);
                    }
                }

                BDebug.LogFormat("【AsyncLoadTaskGroup】剩余未完成任务:{0} - frame: {1}", loadABStatusList.Count + taskList.Count, Time.renderedFrameCount);
                yield return null;
            }

            BDebug.Log("【AsyncLoadTaskGroup】任务完成:" + this.MainAssetName);
            //加载完成,主资源只要保证在 实例化之前加载完毕即可
            this.IsComplete = true;
            var instObj = loder.LoadFormAssetBundle<Object>(this.MainAssetName, this.AssetBundleItem);
            OnAllTaskCompleteCallback?.Invoke(MainAssetName, instObj);
            yield return null;
        }
    }
}
