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
    /// 加载任务组，每个组，负责一个load资源的操作
    /// 可能含有多个依赖资源
    /// </summary>
    public class LoadTaskGroup : CustomYieldInstruction, IDisposable
    {
        public delegate void OnTaskComplete();

        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 是否加载文件成功
        /// 完成加载 且 没被取消 且 资源实例化完成
        /// </summary>
        public bool IsSuccess
        {
            get { return !this.isCancel && this.isLoadABFile && isLoadObject; }
        }

        /// <summary>
        /// 是否取消
        /// </summary>
        private bool isCancel = false;

        /// <summary>
        /// 是否取消
        /// </summary>
        public bool IsCancel
        {
            get { return this.isCancel; }
        }

        /// <summary>
        /// 返回的object
        /// </summary>
        private Object @resultObject;

        /// <summary>
        /// 加载结果
        /// </summary>
        public T GetResult<T>() where T : Object
        {
            return this.resultObject as T;
        }


        /// <summary>
        /// 是否加载AB文件
        /// </summary>
        private bool isLoadABFile { get; set; }

        /// <summary>
        /// 是否加载实例
        /// </summary>
        private bool isLoadObject { get; set; } = false;


        // /// <summary>
        // /// 任务完成回调
        // /// </summary>
        // public OnTaskComplete OnComplete { get; set; } = null;


        /// <summary>
        /// 加载的资源信息
        /// </summary>
        private AssetBundleItem MainAssetBundleItem { get; set; }

        /// <summary>
        /// 加载的路径名
        /// </summary>
        public string MainAssetBundleLoadPath { get; private set; }

        /// <summary>
        /// 主资源类型
        /// </summary>
        private Type MainAssetType { get; set; }

        /// <summary>
        /// 等待加载ab的列表
        /// </summary>
        private List<AssetBundleItem> waitingLoadAssetBundleList;

        /// <summary>
        /// 加载管理器
        /// </summary>
        private AssetBundleMgrV2 loder { get; set; }

        public LoadTaskGroup(AssetBundleMgrV2 loder, Type type, string mainAssetLoadPath, AssetBundleItem mainAssetBundleItem)
        {
            //赋值
            this.loder = loder;
            this.MainAssetType = type;
            this.MainAssetBundleLoadPath = mainAssetLoadPath;
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
        /// 取消 the task
        /// </summary>
        public void Cancel()
        {
            isCancel = true;
            isLoadABFile = true;
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
                    //不再等待，表示当前任务已完成/被取消
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
            if (!isCancel)
            {
                if (!this.isLoadABFile)
                {
                    this.isLoadABFile = AsyncLoadAssetbundleFile();
                }
                else if (!this.isLoadObject) //完成了loadABFile
                {
                    this.isLoadObject = AsyncLoadObject();
                }
            }

            //没成功则继续
            return !this.IsSuccess;
        }


        /// <summary>
        /// 加载assetbundle 文件
        /// </summary>
        private bool AsyncLoadAssetbundleFile()
        {
            //1.loadABFile,循环添加任务
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

            //2.loadABFile,检测加载状态
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

            //3.任务执行完毕检测
            if (loadingTaskList.Count == 0 && curLoadIdx == waitingLoadAssetBundleList.Count - 1)
            {
                //加载完成,主资源只要保证在 实例化之前加载完毕即可
                //加载完则使用
                foreach (var abi in waitingLoadAssetBundleList)
                {
                    var abw = loder.GetAssetBundleFromCache(abi.AssetBundlePath);

                    if (abw != null && abw.AssetBundle != null)
                    {
                        abw.Use();
                    }
                    else
                    {
                        BDebug.LogError($"【AsyncLoadTaskGroup】未获取ab:{abi.AssetBundlePath}");
                    }
                }


                BDebug.Log($"<color=green>【AsyncLoadTaskGroup】所有加载完成:{MainAssetBundleItem.AssetBundlePath}</color>");

                return true;
            }

            return false;
        }

        private AssetBundleRequest abRequest = null;

        /// <summary>
        /// 异步加载对象
        /// </summary>
        private bool AsyncLoadObject()
        {
            //判断request 加载进度
            if (abRequest == null)
            {
                //加载实例对象
                var cacheObject = loder.GetObjectFormCache(MainAssetType, MainAssetBundleLoadPath);
                if (!cacheObject)
                {
                    var abw = loder.GetAssetBundleFromCache(MainAssetBundleItem.AssetBundlePath);
                    abRequest = abw.CreateAsyncInstantiateTask(MainAssetType, MainAssetBundleLoadPath, true);
                    return false;
                }
                else
                {
                    //已经存在
                    this.resultObject = cacheObject;
                    return true;
                }
            }

            //完成
            if (abRequest != null && abRequest.isDone)
            {
                //添加到缓存
                loder.AddObjectToCache(MainAssetType, MainAssetBundleLoadPath, abRequest.asset);
                this.resultObject = abRequest.asset;
                return true;
            }

            return false;
        }


        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            this.loder = null;
            this.MainAssetBundleItem = null;
            this.MainAssetType = null;
            this.MainAssetBundleLoadPath = null;
            this.loadingTaskList = null;
            this.waitingLoadAssetBundleList = null;
            this.abRequest = null;
        }
    }
}
