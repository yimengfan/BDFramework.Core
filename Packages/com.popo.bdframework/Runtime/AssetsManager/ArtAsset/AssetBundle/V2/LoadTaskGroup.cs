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
        /// 或 已有资源已经存在
        /// </summary>
        public bool IsSuccess
        {
            get { return (!this.isCancel && this.isLoadABFile && isLoadObject) || (resultObject); }
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
        public string LoadPath { get; private set; }

        private string guid { get; set; }
        /// <summary>
        /// 主资源类型
        /// </summary>
        private Type loadType { get; set; }

        /// <summary>
        /// 等待加载ab的列表
        /// </summary>
        private List<AssetBundleItem> dependAssetBundleList;

        /// <summary>
        /// 加载管理器
        /// </summary>
        private AssetBundleMgrV2 loader { get; set; }

        public LoadTaskGroup(AssetBundleMgrV2 loader, Type loadType, string loadPath,string guid, List<AssetBundleItem> dependAssets)
        {
            //赋值
            this.loader = loader;
            this.loadType = loadType;
            this.LoadPath = loadPath;
            this.guid = guid;
            this.MainAssetBundleItem = dependAssets.Last();
            //1.依赖资源队列
            dependAssetBundleList = new List<AssetBundleItem>(dependAssets);
        }

        /// <summary>
        /// 一般用于统一返回结构时使用
        /// </summary>
        /// <param name="loder"></param>
        /// <param name="type"></param>
        /// <param name="mainAssetLoadPath"></param>
        /// <param name="mainAssetBundleItem"></param>
        public LoadTaskGroup(Object exsitObject)
        {
            this.resultObject = exsitObject;
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
                // Debug.Log("yield Task frame:" + Time.frameCount);
                //不再等待，表示当前任务已完成/被取消
                if (isCancel || IsSuccess || resultObject != null)
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
            
            BDebug.Log($"【keepAwait】 {this.LoadPath} - {Time.realtimeSinceStartup}  /   <color=red>frame:{Time.frameCount} </color>");

            if (!isCancel)
            {
                if (!this.isLoadABFile)
                {
                    this.isLoadABFile = AsyncLoadAssetbundleFile();
                    if (this.isLoadABFile)
                    {
                        BDebug.Log("【LoadTaskGroup】加载AB文件成功:" +this.LoadPath);
                    }
                }
                
                //在同一帧中继续判断
                if (this.isLoadABFile && !this.isLoadObject) //完成了loadABFile
                {
                    this.isLoadObject = AsyncLoadObject();
                    BDebug.Log("【LoadTaskGroup】AB实例化完成:" +this.LoadPath);
                }
            }
//            BDebug.Log($"<color=yellow> IsSuccess:{this.IsSuccess} </color>");
           //没成功则继续
            return !this.IsSuccess;
        }


        /// <summary>
        /// 加载assetbundle 文件
        /// </summary>
        private bool AsyncLoadAssetbundleFile()
        {
            //1.loadABFile,循环添加任务
            while (!isCancel && AssetBundleMgrV2.IsCanAddGlobalTask && curLoadIdx < dependAssetBundleList.Count - 1)
            {
                curLoadIdx++;

                var abi = dependAssetBundleList[curLoadIdx];
                //没有被加载过
                var abw = loader.GetAssetBundleFromCache(abi.AssetBundlePath);
                if (abw == null)
                {
                    var abPath = loader.FindMultiAddressAsset(abi.AssetBundlePath);
                    //判断是否在加载中
                    var loadTask = AssetBundleMgrV2.GetExsitLoadTask(abPath);
                    if (loadTask != null)
                    {
                        loadingTaskList.Add(new KeyValuePair<string, LoadTask>(abi.AssetBundlePath, loadTask));
                    }
                    else
                    {
                        //创建任务
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
                    BDebug.Log($"【AsyncLoadTaskGroup】 <color=green>无需加载</color>: {abi.AssetBundlePath}");
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
                            loader.AddAssetBundleToCache(assetbundleFileName, loadTask.AssetBundle);
                        }
                        else
                        {
                            BDebug.LogError("【AsyncLoadTaskGroup】ab资源为空:" + assetbundleFileName);
                        }

                        //移除成功任务
                        loadingTaskList.RemoveAt(i);
                        //解锁
                        AssetBundleMgrV2.RemoveGlobalLoadTask(loadTask);
                        BDebug.Log($"【AsyncLoadTaskGroup】--> 加载完成:{assetbundleFileName}  剩余:{loadingTaskList.Count + dependAssetBundleList.Count - (curLoadIdx + 1)}/{dependAssetBundleList.Count}");
                    }
                }
            }

            //3.任务执行完毕检测
            if (!isCancel && loadingTaskList.Count == 0 && curLoadIdx == dependAssetBundleList.Count - 1)
            {
                //加载完成,主资源只要保证在 实例化之前加载完毕即可
                //加载完则使用
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
            if (!isCancel && abRequest == null)
            {
                //加载实例对象
                var cacheObject = loader.GetObjectFormCache(LoadPath, loadType);
                if (!cacheObject)
                {
                    var abw = loader.GetAssetBundleFromCache(MainAssetBundleItem.AssetBundlePath);
                    abRequest = abw.CreateAsyncInstantiateTask(loadType,guid ,true);
                    return false;
                }
                else
                {
                    //已经存在
                    Debug.Log("【LoadTaskGroup】已存在cache :" + loadType);
                    this.resultObject = cacheObject;
                    return true;
                }
            }

            //异步实例化完成
            if (!isCancel && abRequest != null && abRequest.isDone)
            {
                //添加到缓存
                loader.AddObjectToCache(loadType, LoadPath, abRequest.asset);
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
            this.loader = null;
            this.MainAssetBundleItem = null;
            this.loadType = null;
            this.LoadPath = null;
            this.loadingTaskList = null;
            this.dependAssetBundleList = null;
            this.abRequest = null;
        }
    }
}
