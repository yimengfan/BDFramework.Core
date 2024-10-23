using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BResource.AssetBundle.V2;
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
        public readonly static string LogTag = "LoadTask";
        public delegate void OnTaskComplete();

        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 任务是否完成
        /// 被取消，实例化成功，或者失败 =》都会结束任务
        /// </summary>
        public bool IsComplete
        {
            get { return this.isCancel || IsFail || resultObject; }
        }

        /// <summary>
        /// 是否失败
        /// </summary>
        public bool IsFail { get; private set; } = false;

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
        private bool isLoadABFileEnd { get; set; } = false;

        /// <summary>
        /// 是否加载实例
        /// </summary>
        private bool isLoadObjectEnd { get; set; } = false;


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

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="loadType"></param>
        /// <param name="loadPath"></param>
        /// <param name="guid"></param>
        /// <param name="dependAssets"></param>
        public LoadTaskGroup(AssetBundleMgrV2 loader, Type loadType, string loadPath, string guid,
            IEnumerable<AssetBundleItem> dependAssets)
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
            isLoadABFileEnd = true;
        }


        /// <summary>
        /// 重写CustomYieldInstruction
        /// </summary>
        public override bool keepWaiting
        {
            get
            {
                //完成，则不需要等待
                if (IsComplete)
                {
                    return false;
                }
                else
                {
                    //执行加载Assetbundle
                    LoadAssets();
                    //不在当前帧返回
                    return true; //继续等待 
                }
            }
        }

        /// <summary>
        /// Load assetbundle状态管理
        /// </summary>
        private List<KeyValuePair<AssetBundleItem, LoadTask>> loadingTaskList { get; set; } = new List<KeyValuePair<AssetBundleItem, LoadTask>>(10);

        /// <summary>
        /// 当前任务计数
        /// </summary>
        private int curLoadIdx = -1;


        /// <summary>
        /// 执行加载 Assetbundle
        /// </summary>
        /// <returns>是否继续执行</returns>
        private bool LoadAssets()
        {

            if (!isCancel)
            {
                if (!this.isLoadABFileEnd)
                {
                    this.isLoadABFileEnd = AsyncLoadAssetbundleFile();
                }

                //在同一帧中继续判断
                if (this.isLoadABFileEnd && !this.isLoadObjectEnd) //完成了loadABFile
                {
                    this.isLoadObjectEnd = AsyncLoadObject();
                }
            }

            return this.IsComplete;
        }


        /// <summary>
        /// 异步加载ab文件
        /// </summary>
        /// <returns>是否加载完成</returns>
        private bool AsyncLoadAssetbundleFile()
        {
            //1.loadABFile,循环添加任务
            while (AssetBundleMgrV2.IsCanAddGlobalTask && curLoadIdx < dependAssetBundleList.Count - 1)
            {
                curLoadIdx++;
                var item = dependAssetBundleList[curLoadIdx];
                //没有被加载过
                var assetLoder = loader.GetAssetLoder(item.AssetBundlePath);
                if (assetLoder == null|| !assetLoder.IsValid)
                {
                    var abPath = loader.FindMultiAddressAsset(item.AssetBundlePath);
                    //判断是否在加载中
                    var loadTask = AssetBundleMgrV2.GetExsitLoadTask(abPath);
                    if (loadTask != null)
                    {
                        loadingTaskList.Add(new KeyValuePair<AssetBundleItem, LoadTask>(item, loadTask));
                    }
                    else
                    {
                        //创建任务
                        loadTask = new LoadTask(abPath, 0, (ulong)item.Mix);
                        //加入Global任务
                        AssetBundleMgrV2.AddGlobalLoadTask(loadTask);
                        //添加到loading表
                        loadingTaskList.Add(new KeyValuePair<AssetBundleItem, LoadTask>(item, loadTask));
                        //开始加载
                        loadTask.AysncLoad();
#if UNITY_EDITOR
                        BDebug.Log(LogTag,$"<color=red>Id:{this.Id}</color> 创建加载依赖:{item.AssetBundlePath}  - {UnityEditor.AssetDatabase.GUIDToAssetPath(item.AssetBundlePath)}");
#else
                        BDebug.Log(LogTag,$"<color=red>Id:{this.Id}</color>  创建加载依赖:{item.AssetBundlePath}");
#endif
                    }
                }
                else
                {
                    BDebug.Log(LogTag,$"<color=red>Id:{this.Id}</color> 无需加载:{item.AssetBundlePath}",Color.yellow);
                }
            }

            //2.loadABFile,检测加载状态
            if (loadingTaskList.Count > 0)
            {
                for (int i = loadingTaskList.Count - 1; i >= 0; i--)
                {
                    var loadingTask = loadingTaskList[i];
                    var item = loadingTask.Key;
                    var loadTask = loadingTask.Value;
                    //判断是否结束
                    if (loadTask.IsDone)
                    {
                        //添加到返回列表
                        if (loadTask.AssetBundle != null)
                        {
                            loader.AddAssetLoder(item, loadTask.AssetBundle);
                        }
                        else
                        {
                            BDebug.LogError(LogTag,"ab资源为空:" + item);
                        }

                        //移除成功任务
                        loadingTaskList.RemoveAt(i);
                        //解锁
                        AssetBundleMgrV2.RemoveGlobalLoadTask(loadTask);
#if UNITY_EDITOR
                        BDebug.Log(
                            LogTag,$"<color=red>Id:{this.Id}</color> --> 加载依赖完成:{item}  -  {UnityEditor.AssetDatabase.GUIDToAssetPath(item.AssetBundlePath)}!  <color=green>剩余:{loadingTaskList.Count + dependAssetBundleList.Count - (curLoadIdx + 1)}/{dependAssetBundleList.Count}</color>");
#else
                  BDebug.Log(
                            LogTag,$"<color=red>Id:{this.Id}</color> -->  加载依赖完成:{item.AssetBundlePath}!  剩余:{loadingTaskList.Count + dependAssetBundleList.Count - (curLoadIdx + 1)}/{dependAssetBundleList.Count}");
#endif
                      
                    }
                }
            }

            //3.任务执行完毕检测
            if (loadingTaskList.Count == 0 && curLoadIdx == dependAssetBundleList.Count - 1)
            {
                //加载完成,主资源只要保证在 实例化之前加载完毕即可
                //加载完则使用
#if UNITY_EDITOR
                BDebug.Log(LogTag,
                    $"<color=red>Id:{this.Id}</color> 所有依赖加载:{MainAssetBundleItem.AssetBundlePath} - {UnityEditor.AssetDatabase.GUIDToAssetPath(MainAssetBundleItem.AssetBundlePath)}",Color.green);
#else
                    BDebug.Log(LogTag,$"<color=red>Id:{this.Id}</color> 所有依赖加载:{MainAssetBundleItem.AssetBundlePath}",Color.green);
#endif


                return true;
            }

            return false;
        }

        /// <summary>
        /// 加载ab的进度
        /// </summary>
        private AssetBundleRequest assetbundleRequest = null;

        /// <summary>
        /// 异步加载AB对象
        /// </summary>
        private bool AsyncLoadObject()
        {
            //判断request 加载进度
            if (assetbundleRequest == null)
            {
                var cacheObject = loader.GetAssetObjectFromCache(LoadPath, loadType);
                //不存在缓存,加载
                if (!cacheObject)
                {
                    var assetLoder = loader.GetAssetLoder(MainAssetBundleItem.AssetBundlePath);
                    assetbundleRequest = assetLoder.LoadAssetAsync(loadType, guid, true);
                    return false;
                }
                else
                {
                    //已经存在
                    BDebug.Log(LogTag,"<color=red>Id:{this.Id}</color> 已存在cache :" + loadType);
                    this.resultObject = cacheObject;
                    return true;
                }
            }

            //异步实例化完成
            if (assetbundleRequest != null && assetbundleRequest.isDone)
            {
                //添加到缓存
                loader.AddAssetObjectToCache(loadType, LoadPath, assetbundleRequest.asset);
                this.resultObject = assetbundleRequest.asset;
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
            this.assetbundleRequest = null;
        }
    }
}