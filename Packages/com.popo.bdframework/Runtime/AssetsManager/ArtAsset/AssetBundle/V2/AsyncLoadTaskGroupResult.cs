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
    public struct LoaderTaskData
    {
        /// <summary>
        /// asset path
        /// </summary>
        public string AssetPath { get; private set; }

        /// <summary>
        /// 加载类型
        /// </summary>
        public Type LoadType { get; private set; }

        /// <summary>
        /// 是否为主资源
        /// </summary>
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
    public class AsyncLoadTaskGroupResult : CustomYieldInstruction, IDisposable
    {
        /// <summary>
        /// 异步任务颗粒度，每帧执行多少个
        /// </summary>
        static readonly public int ASYNC_TASK_NUM = 5;

        /// <summary>
        /// loding缓存表 防止重复加载
        /// </summary>
        private static Dictionary<string, AssetBundleCreateRequest> AB_LOAD_LOCK_MAP = new Dictionary<string, AssetBundleCreateRequest>();

        public int Id { get; set; }

        /// <summary>
        /// 是否成功
        /// 完成加载 且 没被取消
        /// </summary>
        public bool IsSuccess
        {
            get { return this.isCompleteLoad && !this.isCancel; }
        }

        /// <summary>
        /// 是否取消
        /// </summary>
        public bool IsCancel
        {
            get { return this.isCancel; }
        }

        /// <summary>
        /// 是否完成
        /// </summary>
        private bool isCompleteLoad { get; set; }

        public delegate void OnTaskCompleteCallbackDelegate(string s);

        /// <summary>
        /// 任务完成回调
        /// </summary>
        public OnTaskCompleteCallbackDelegate OnAllTaskCompleteCallback { get; set; } = null;


        /// <summary>
        /// 加载的manifest
        /// </summary>
        public AssetBundleItem AssetBundleItem { get; private set; }

        /// <summary>
        /// 加载管理器
        /// </summary>
        private AssetBundleMgrV2 loder { get; set; }

        public AsyncLoadTaskGroupResult(AssetBundleMgrV2 loder, AssetBundleItem assetBundleItem)
        {
            //赋值
            this.loder = loder;
            this.AssetBundleItem = assetBundleItem;

            //1.依赖资源队列
            this.taskQueue = new Queue<LoaderTaskData>();
            var dependAssetList = loder.AssetConfigLoder.GetDependAssets(assetBundleItem);

            foreach (var dependAsset in dependAssetList)
            {
                var task = new LoaderTaskData(dependAsset, typeof(Object));
                this.taskQueue.Enqueue(task);
            }

            //2.主资源添加队列
            var mainTask = new LoaderTaskData(assetBundleItem.AssetBundlePath, typeof(Object), true);
            this.taskQueue.Enqueue(mainTask);
        }


        /// <summary>
        /// 当前任务计数
        /// </summary>
        private int curDoTaskConter = 0;

        /// <summary>
        /// 是否取消
        /// </summary>
        private bool isCancel = false;

        /// <summary>
        /// 取消 the task
        /// </summary>
        public void Cancel()
        {
            isCancel = true;
            isCompleteLoad = true;
        }


        /// <summary>
        /// 重写CustomYieldInstruction
        /// </summary>
        public override bool keepWaiting
        {
            get
            {
                if (isCancel || IsSuccess)
                {
                    return false;
                }

                //执行加载Assetbundle
                return DoLoadAssetBundle();
            }
        }

        /// <summary>
        /// Load assetbundle状态管理
        /// </summary>
        List<KeyValuePair<string, AssetBundleCreateRequest>> loadABStatusList = new List<KeyValuePair<string, AssetBundleCreateRequest>>(ASYNC_TASK_NUM);

        /// <summary>
        /// 任务列表
        /// </summary>
        private Queue<LoaderTaskData> taskQueue = null;

        /// <summary>
        /// 执行加载 Assetbundle
        /// </summary>
        /// <returns>是否继续执行</returns>
        private bool DoLoadAssetBundle()
        {
            //有加载任务或者任务未完成
            if (loadABStatusList.Count > 0 || taskQueue.Count > 0)
            {
                //1.循环添加任务
                while (taskQueue.Count > 0 && loadABStatusList.Count < ASYNC_TASK_NUM)
                {
                    var task = taskQueue.Dequeue();
                    //没有被加载过
                    if (!loder.AssetbundleMap.ContainsKey(task.AssetPath))
                    {
                        //判断是否在加载中
                        AB_LOAD_LOCK_MAP.TryGetValue(task.AssetPath, out var exsitLoadingStatus);

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
                            AB_LOAD_LOCK_MAP.Add(task.AssetPath, ret);

                            //添加到状态表
                            loadABStatusList.Add(new KeyValuePair<string, AssetBundleCreateRequest>(task.AssetPath, ret));
                        }
                    }
                    else
                    {
                        BDebug.Log("【AsyncLoadTaskGroup】--> 已存在depend:" + task.AssetPath);
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
                        AB_LOAD_LOCK_MAP.Remove(assetPath);

                        BDebug.Log("【AsyncLoadTaskGroup】--> depend:" + assetPath);
                    }
                }

                BDebug.LogFormat("【AsyncLoadTaskGroup】剩余未完成任务:{0} - frame: {1}", loadABStatusList.Count + taskQueue.Count, Time.renderedFrameCount);
            }
            else
            {
                this.isCompleteLoad = true;
                //加载完成,主资源只要保证在 实例化之前加载完毕即可
                if (!isCancel)
                {
                    this.OnAllTaskCompleteCallback?.Invoke(this.AssetBundleItem.LoadPath);
                }
            }

            //是否继续执行
            return !this.IsSuccess;
        }


        /// <summary>
        /// 获取Instance实例
        /// </summary>
        public T GetAssetBundleInstance<T>() where T : UnityEngine.Object
        {
            if (IsSuccess)
            {
                var instObj = loder.LoadFormAssetBundle<T>(this.AssetBundleItem.LoadPath, this.AssetBundleItem);

                return instObj;
            }

            return null;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            this.AssetBundleItem = null;
            this.taskQueue = null;
            this.loadABStatusList = null;
            this.OnAllTaskCompleteCallback = null;
        }
    }
}
