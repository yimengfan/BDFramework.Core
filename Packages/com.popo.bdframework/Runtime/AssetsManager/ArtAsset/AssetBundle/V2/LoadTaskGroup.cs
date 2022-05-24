using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AssetsManager.ArtAsset.AssetBundle.V2;
using BDFramework.ResourceMgr.V2;
using Cysharp.Threading.Tasks;
using LitJson;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 单个任务的数据存储
    /// </summary>
    // public struct LoaderTaskData
    // {
    //     /// <summary>
    //     /// asset path
    //     /// </summary>
    //     public AssetBundleItem AssetBundleItem { get; private set; }
    //
    //     /// <summary>
    //     /// 加载类型
    //     /// </summary>
    //     public Type LoadType { get; private set; }
    //
    //     /// <summary>
    //     /// 是否为主资源
    //     /// </summary>
    //     public bool IsMainAsset { get; private set; }
    //
    //     public LoaderTaskData(AssetBundleItem assetBundleItem, Type t, bool isMainAsset = false)
    //     {
    //         this.AssetBundleItem = assetBundleItem;
    //         this.LoadType = t;
    //         this.IsMainAsset = isMainAsset;
    //     }
    // }


    /// <summary>
    /// 加载任务组，每个组，负责一个load资源的操作
    /// 可能含有多个依赖资源
    /// </summary>
    public class LoadTaskGroup : CustomYieldInstruction, IDisposable
    {
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
        public AssetBundleItem MainAssetBundleItem { get; private set; }

        /// <summary>
        /// 等待加载ab的列表
        /// </summary>
        private List<AssetBundleItem> waitingLoadAssetBundleList;

        /// <summary>
        /// 加载管理器
        /// </summary>
        private AssetBundleMgrV2 loder { get; set; }

        public LoadTaskGroup(AssetBundleMgrV2 loder, AssetBundleItem mainAssetBundleItem)
        {
            //赋值
            this.loder = loder;
            this.MainAssetBundleItem = mainAssetBundleItem;
            
            //1.依赖资源队列
            var dependAssetList = loder.AssetConfigLoder.GetDependAssets(mainAssetBundleItem);
            if (dependAssetList != null)
            {
                waitingLoadAssetBundleList = new List<AssetBundleItem>(dependAssetList.Count + 1);
                //添加依赖
                waitingLoadAssetBundleList.AddRange(dependAssetList);
            }
            else
            {
                waitingLoadAssetBundleList = new List<AssetBundleItem>();
            }

            //添加主资源
            waitingLoadAssetBundleList.Add(mainAssetBundleItem);
        }


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
        List<KeyValuePair<string, LoadTask>> loadingTaskList = new List<KeyValuePair<string, LoadTask>>(10);

        /// <summary>
        /// 当前任务计数
        /// </summary>
        private int curLoadIdx = -1;


        /// <summary>
        /// 执行加载 Assetbundle
        /// </summary>
        /// <returns>是否继续执行</returns>
        private bool DoLoadAssetBundle()
        {
            //1.循环添加任务
            while (AssetBundleMgrV2.IsCanAddGlobalTask && curLoadIdx < waitingLoadAssetBundleList.Count - 1)
            {
                curLoadIdx++;

                var abi = waitingLoadAssetBundleList[curLoadIdx];
                //没有被加载过
                var abw = loder.GetAssetBundleFromCache(abi.AssetBundlePath);
                if (abw == null)
                {
                    //判断是否在加载中
                    var loadTask = AssetBundleMgrV2.GetExsitLoadTask(abi.AssetBundlePath);
                    if (loadTask != null)
                    {
                        loadingTaskList.Add(new KeyValuePair<string, LoadTask>(abi.AssetBundlePath, loadTask));
                    }
                    else
                    {
                        //创建任务
                        var abPath = loder.FindMultiAddressAsset(abi.AssetBundlePath);
                        loadTask = new LoadTask(abPath, 0, (ulong) abi.Mix);
                        //加入Global任务
                        AssetBundleMgrV2.AddGlobalLoadTask(loadTask);
                        //添加到loading表
                        loadingTaskList.Add(new KeyValuePair<string, LoadTask>(abi.AssetBundlePath, loadTask));
                        //开始加载
                        loadTask.AysncLoad();
                        BDebug.Log($"【AsyncLoadTaskGroup】 加    载: {abi.AssetBundlePath}");
                    }
                }
                else
                {
                        BDebug.Log($"【AsyncLoadTaskGroup】 无需加载: {abi.AssetBundlePath}");
                }
            }

            //2.检测加载状态
            if (loadingTaskList.Count > 0)
            {
                for (int i = loadingTaskList.Count - 1; i >= 0; i--)
                {
                    var loadingTask = loadingTaskList[i];
                    var assetbundleFileName = loadingTask.Key;
                    var loadTask = loadingTask.Value;
                    //判断是否结束
                    if (loadTask.IsDone)
                    {
                        //添加到返回列表
                        if (loadTask.AssetBundle != null)
                        {
                            loder.AddAssetBundleToCache(assetbundleFileName, loadTask.AssetBundle);
                        }
                        else
                        {
                            BDebug.LogError("【AsyncLoadTaskGroup】ab资源为空:" + assetbundleFileName);
                        }

                        //移除成功任务
                        loadingTaskList.RemoveAt(i);
                        //解锁
                        AssetBundleMgrV2.RemoveGlobalLoadTask(loadTask);
                        BDebug.Log($"【AsyncLoadTaskGroup】--> 加载完成:{assetbundleFileName}  剩余:{loadingTaskList.Count + waitingLoadAssetBundleList.Count - (curLoadIdx + 1)}/{waitingLoadAssetBundleList.Count}");
                    }
                }
            }

            //任务执行完毕
            if (loadingTaskList.Count == 0 && curLoadIdx == waitingLoadAssetBundleList.Count - 1)
            {
                BDebug.Log($"<color=green>【AsyncLoadTaskGroup】所有加载完成:{MainAssetBundleItem.AssetBundlePath}</color>");
                this.isCompleteLoad = true;
                //加载完成,主资源只要保证在 实例化之前加载完毕即可
                if (!isCancel)
                {
                    //加载完则使用
                    foreach (var waiting in waitingLoadAssetBundleList)
                    {
                        var abw = loder.GetAssetBundleFromCache(waiting.AssetBundlePath);
                        if (abw != null && abw.AssetBundle != null)
                        {
                            abw.Use();
                        }
                        else
                        {
                            BDebug.LogError($"【AsyncLoadTaskGroup】未获取ab:{waiting.AssetBundlePath}");
                        }
                    }

                    this.OnAllTaskCompleteCallback?.Invoke(this.MainAssetBundleItem.LoadPath);
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
                var instObj = loder.LoadObjectFormAssetBundle<T>(this.MainAssetBundleItem.LoadPath, this.MainAssetBundleItem);

                return instObj;
            }

            return null;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            this.MainAssetBundleItem = null;
            this.loadingTaskList = null;
            this.OnAllTaskCompleteCallback = null;
            this.waitingLoadAssetBundleList = null;
        }
    }
}
