using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using BDFramework.Core.Tools;
using Cysharp.Text;
using LitJson;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// 加载资源的返回状态
    /// </summary>
    public enum LoadAssetState
    {
        Success = 0,
        Fail,
        IsLoding,
    }


    /// <summary>
    /// ab包管理器
    /// </summary>
    public class AssetBundleMgrV2 : IResMgr
    {
        /// <summary>
        /// 非Hash命名时，runtime目录的都放在一起，方便调试
        /// </summary>
        // static readonly public string DEBUG_RUNTIME = "runtime/{0}";


        /// <summary>
        /// 全局的任务id
        /// </summary>
        private int taskIdxCounter;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private Queue<AsyncLoadTaskGroupResult> asyncTaskGroupQueue { get; set; } = new Queue<AsyncLoadTaskGroupResult>(50);

        /// <summary>
        /// 全局唯一的依赖
        /// </summary>
        public AssetbundleConfigLoder AssetConfigLoder { get; private set; }

        /// <summary>
        /// 全局的ab缓存
        /// </summary>
        public Dictionary<string, AssetBundleWapper> AssetbundleCacheMap { get; private set; } = new Dictionary<string, AssetBundleWapper>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 全局的资源缓存
        /// </summary>
        public Dictionary<string, Object> GameObjectCacheMap { get; private set; } = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 资源加载路径
        /// </summary>
        private string firstArtDirectory;

        //第二寻址路径
        private string secArtDirectory;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="rootPath"></param>
        public void Init(string rootPath)
        {
            //多热更切换,需要卸载
            if (this.AssetConfigLoder != null)
            {
                this.UnloadAllAsset();
            }

            var platformPath = BApplication.GetRuntimePlatformPath();
            //1.设置加载路径  
            if (Application.isEditor)
            {
                firstArtDirectory = IPath.Combine(rootPath, platformPath, BResources.ART_ASSET_ROOT_PATH);
                secArtDirectory = IPath.Combine(Application.streamingAssetsPath, platformPath, BResources.ART_ASSET_ROOT_PATH); //
            }
            else
            {
                firstArtDirectory = IPath.Combine(Application.persistentDataPath, platformPath, BResources.ART_ASSET_ROOT_PATH);
                secArtDirectory = IPath.Combine(Application.streamingAssetsPath, platformPath, BResources.ART_ASSET_ROOT_PATH); //
            }

            //2.路径替换
            firstArtDirectory = IPath.ReplaceBackSlash(firstArtDirectory);
            secArtDirectory = IPath.ReplaceBackSlash(secArtDirectory);

            //3.加载ArtConfig
            this.AssetConfigLoder = new AssetbundleConfigLoder();
            var assetconfigPath = "";
            var assetTypePath = "";
            if (Application.isEditor)
            {
                assetconfigPath = IPath.Combine(rootPath, platformPath, BResources.ART_ASSET_CONFIG_PATH);
                assetTypePath = IPath.Combine(rootPath, platformPath, BResources.ART_ASSET_TYPES_PATH);
            }
            else
            {
                //真机环境config在persistent，跟dll和db保持一致
                assetconfigPath = IPath.Combine(Application.persistentDataPath, platformPath, BResources.ART_ASSET_CONFIG_PATH);
                assetTypePath = IPath.Combine(Application.persistentDataPath, platformPath, BResources.ART_ASSET_TYPES_PATH);
            }

            this.AssetConfigLoder.Load(assetconfigPath, assetTypePath);
            //开始异步任务刷新
            IEnumeratorTool.StartCoroutine(this.IE_AsyncTaskListUpdte());
            BDebug.Log($"【AssetBundleV2】 firstDir:{firstArtDirectory}", "red");
            BDebug.Log($"【AssetBundleV2】 secDir:{secArtDirectory}", "red");
        }


        #region 对外加载接口

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathType"></param>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            //这里首次会耗时，主要是需要关联查询依赖文件
            if (pathType == LoadPathType.GUID)
            {
                var abi = AssetConfigLoder.GetAssetBundleDataByGUID(path);
                if (abi != null)
                {
                    path = abi.LoadPath;
                }
            }

            //加载
            var obj = Load(typeof(T), path);
            if (obj)
            {
                return obj as T;
            }

            return null;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public Object Load(Type type, string assetPath)
        {
            var retObj = GetObjectFormCache(assetPath);

            if (!retObj)
            {
                //1.依赖路径
                var (assetBundleItem, dependAssetList) = AssetConfigLoder.GetDependAssets(assetPath, type);
                //2.加载
                if (assetBundleItem != null)
                {
                    //加载依赖AB
                    foreach (var dependABItem in dependAssetList)
                    {
                        LoadAssetBundle(dependABItem.AssetBundlePath, dependABItem.Mix);
                    }

                    //加载主资源AB
                    LoadAssetBundle(assetBundleItem.AssetBundlePath, assetBundleItem.Mix);
                    //加载实例
                    retObj = LoadAssetFormAssetBundle(type, assetPath, assetBundleItem);
                }
            }


            return retObj;
        }

        /// <summary>
        /// load ALL TestAPI
        /// 这个有一定局限性，这里是返回某个Ab中的所有资源
        /// 简单处理一些简单资源情况：目前只解决图集
        /// 仅作为某些项目补坑用
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T[] LoadAll<T>(string path) where T : Object
        {
            //非hash模式，需要debugRuntime
            // if (!this.AssetConfigLoder.IsHashName)
            // {
            //     path = ZString.Format(DEBUG_RUNTIME, path);
            // }


            var item = AssetConfigLoder.GetAssetBundleData<T>(path);
            //加载assetbundle
            AssetBundle ab = LoadAssetBundle(item.AssetBundlePath);

            if (ab != null)
            {
                var assetNames = ab.GetAllAssetNames();
                string relname = "";
                if (assetNames.Length == 1)
                {
                    relname = assetNames[0];
                }
                else
                {
                    var f = path + ".";
                    relname = assetNames.First((s) => s.Contains(f));
                }

                return ab.LoadAssetWithSubAssets<T>(relname);
            }

            return null;
        }


        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath"></param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        public int AsyncLoad<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var retObj = GetObjectFormCache(assetPath);
            if (!retObj)
            {
                var loadTask = CreateAsyncLoadTask<T>(assetPath);
                if (loadTask != null)
                {
                    //添加完成回调
                    loadTask.OnAllTaskCompleteCallback += (p) =>
                    {
                        var obj = loadTask.GetAssetBundleInstance<T>();
                        //回调
                        callback(obj);
                    };
                    //添加到任务队列
                    AddAsyncTaskGroup(loadTask);
                    return loadTask.Id;
                }
                else
                {
                    BDebug.LogError("不存在资源:" + assetPath);
                }
            }
            else
            {
                //返回缓存的数据
                var tObj = retObj as T;
                callback?.Invoke(tObj);
            }

            return -1;
        }

        /// <summary>
        /// 异步加载接口
        /// 外部创建 需要自己管理yield,防止逻辑冲突
        /// 开放该接口，主要用于各种批量测试控制逻辑，一般情况下无需调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        public AsyncLoadTaskGroupResult CreateAsyncLoadTask<T>(string assetName) where T : UnityEngine.Object
        {
            var assetBundleItem = AssetConfigLoder.GetAssetBundleData<T>(assetName);

            if (assetBundleItem != null)
            {
                //添加任务组
                var taskGroup = new AsyncLoadTaskGroupResult(this, assetBundleItem);
                taskGroup.Id = this.taskIdxCounter++;
                // AddAsyncTaskGroup(taskGroup);
                return taskGroup;
            }
            else
            {
                BDebug.LogError("不存在资源:" + assetName);
            }

            return null;
        }

        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="assetPathList">资源</param>
        /// <param name="onLoadProcess">进度</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <returns>任务id列表</returns>
        public List<int> AsyncLoad(List<string> assetPathList, Action<int, int> onLoadProcess, Action<IDictionary<string, Object>> onLoadComplete)
        {
            var taskIdList = new List<int>();
            int taskCounter = 0;
            var loadAssetMap = new Dictionary<string, Object>();
            assetPathList = assetPathList.Distinct().ToList(); //去重
            int totalNum = assetPathList.Count;
            //依次添加异步任务
            foreach (var assetPath in assetPathList)
            {
                var taskid = AsyncLoad<Object>(assetPath, (o) =>
                {
                    loadAssetMap[assetPath] = o;
                    //进度回调
                    onLoadProcess?.Invoke(loadAssetMap.Count, totalNum);
                    //完成回调
                    if (loadAssetMap.Count == totalNum)
                    {
                        onLoadComplete?.Invoke(loadAssetMap);
                    }
                });
                taskIdList.Add(taskid);
            }

            //
            return taskIdList;
        }


        /// <summary>
        /// 添加一个任务组
        /// </summary>
        /// <param name="taskGroupResult"></param>
        public void AddAsyncTaskGroup(AsyncLoadTaskGroupResult taskGroupResult)
        {
            this.asyncTaskGroupQueue.Enqueue(taskGroupResult);
        }

        /// <summary>
        /// 多路径寻址
        /// </summary>
        /// <param name="assetFileName"></param>
        /// <returns></returns>
        public string FindMultiAddressAsset(string assetFileName)
        {
            //第一地址
            var p = IPath.Combine(this.firstArtDirectory, assetFileName);
            //寻址到第二路径
            if (!File.Exists(p))
            {
                p = IPath.Combine(this.secArtDirectory, assetFileName);
            }

            return p;
        }

        #endregion

        #region 加载AssetsBundle

        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        /// <param name="abPath"></param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundle(string abPath, int offset = 0)
        {
            AssetBundleWapper abw = null;
            if (AssetbundleCacheMap.TryGetValue(abPath, out abw))
            {
                abw.Use();
                return abw.AssetBundle;
            }
            else
            {
                //寻找加载路径
                var loadPath = FindMultiAddressAsset(abPath);
#if UNITY_EDITOR
                if (!File.Exists(loadPath))
                {
                    return null;
                }
#endif
                //TODO 这里需要判断Lock列表，异步转同步

                var ab = AssetBundle.LoadFromFile(loadPath, 0u, (ulong) offset);
                //添加
                this.AddAssetBundle(abPath, ab);
                return ab;
            }

            return null;
        }


        /// <summary>
        /// ab包计数器
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="ab"></param>
        public void AddAssetBundle(string assetPath, AssetBundle ab)
        {
            AssetBundleWapper abw = null;
            //
            if (!AssetbundleCacheMap.TryGetValue(assetPath, out abw))
            {
                abw = new AssetBundleWapper(ab);
                AssetbundleCacheMap[assetPath] = abw;
            }

            abw.Use();
        }

        #endregion


        #region 资源缓存

        /// <summary>
        /// 从缓存中加载
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public void AddObjectToCache(string assetPath, Object obj)
        {
            this.GameObjectCacheMap[assetPath] = obj;
        }

        /// <summary>
        /// 从缓存中加载
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public Object GetObjectFormCache(string assetPath)
        {
            Object obj = null;
            this.GameObjectCacheMap.TryGetValue(assetPath, out obj);
            return obj;
        }

        /// <summary>
        /// 从缓存中加载
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public Object UnloadObjectCache(string assetPath)
        {
            Object obj = null;
            this.GameObjectCacheMap.TryGetValue(assetPath, out obj);
            if (obj)
            {
                Resources.UnloadAsset(obj);
                this.GameObjectCacheMap.Remove(assetPath);
            }

            return obj;
        }

        #endregion

        #region 从AB中加载资源

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public T LoadFormAssetBundle<T>(string assetName, AssetBundleItem item) where T : UnityEngine.Object
        {
            var obj = LoadAssetFormAssetBundle(typeof(T), assetName, item);
            if (obj)
            {
                return (obj as T);
            }

            return null;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="assetPath"></param>
        /// <param name="item"></param>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private Object LoadAssetFormAssetBundle(Type type, string assetPath, AssetBundleItem item)
        {
            Object obj = null;
            AssetBundleWapper abr = null;
            if (AssetbundleCacheMap.TryGetValue(item.AssetBundlePath, out abr))
            {
                //优先处理图集
                if (item.AssetType == this.AssetConfigLoder.TYPE_SPRITE_ATLAS)
                {
                    obj = abr.LoadTextureFormAtlas(assetPath);
                }
                //其他需要处理的资源类型，依次判断.
                else
                {
                    obj = abr.LoadAsset(type, assetPath);
                }

                AddObjectToCache(assetPath, obj);
            }
            else
            {
                BDebug.Log("资源不存在:" + assetPath + " - " + item.AssetBundlePath, "red");

                return null;
            }

            return obj;
        }

        #endregion

        #region 取消加载任务

        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            foreach (var tg in asyncTaskGroupQueue)
            {
                if (tg.Id == taskid)
                {
                    tg.Cancel();
                    break;
                }
            }
        }


        /// <summary>
        /// 取消所有load任务
        /// </summary>
        public void LoadAllCancel()
        {
            foreach (var tg in asyncTaskGroupQueue)
            {
                tg.Cancel();
            }

            this.asyncTaskGroupQueue.Clear();
        }

        /// <summary>
        /// 获取路径下所有资源
        /// </summary>
        /// <param name="floder"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public string[] GetAssets(string floder, string searchPattern = null)
        {
            List<string> rets = new List<string>();
            string str;

            str = ZString.Concat(floder, "/");
            // if (!this.AssetConfigLoder.IsHashName)
            // {
            //     str = ZString.Format(DEBUG_RUNTIME, str);
            // }


            foreach (var abItem in this.AssetConfigLoder.AssetbundleItemList)
            {
                if (abItem.LoadPath.StartsWith(str, StringComparison.OrdinalIgnoreCase))
                {
                    rets.Add(abItem.LoadPath);
                }
            }

            //寻找符合条件的
            if (!string.IsNullOrEmpty(searchPattern))
            {
                rets = rets.FindAll((r) =>
                {
                    var fileName = Path.GetFileName(r);

                    if (fileName.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                });
            }

            // if (!this.AssetConfigLoder.IsHashName)
            // {
            //     var count = "runtime/".Length;
            //     for (int i = 0; i < rets.Count; i++)
            //     {
            //         rets[i] = rets[i].Substring(count);
            //     }
            // }


            return rets.ToArray();
        }

        /// <summary>
        /// 预热shader
        /// </summary>
        public void WarmUpShaders()
        {
            var svc = this.Load<ShaderVariantCollection>(BResources.ALL_SHADER_VARAINT_RUNTIME_PATH);
            if (!svc.isWarmedUp)
            {
                svc.WarmUp();
            }
#if UNITY_EDITOR
            var (abd, list) = this.AssetConfigLoder.GetDependAssets(BResources.ALL_SHADER_VARAINT_RUNTIME_PATH);
            this.AssetbundleCacheMap.TryGetValue(abd.AssetBundlePath, out var ab);
            Debug.Log("Shaders:\n" + JsonMapper.ToJson(ab.AssetBundle.GetAllAssetNames(), true));
#endif
        }

        #endregion

        #region 异步任务检测

        /// <summary>
        /// 核心功能,所有任务靠这个推进度
        /// 执行下个任务
        /// </summary>
        IEnumerator IE_AsyncTaskListUpdte()
        {
            while (true)
            {
                if (this.asyncTaskGroupQueue.Count > 0)
                {
                    //开始新任务
                    AsyncLoadTaskGroupResult task = null;

                    do
                    {
                        task = this.asyncTaskGroupQueue.Dequeue();
                    } while (task.IsCancel && this.asyncTaskGroupQueue.Count > 0);

                    //执行任务
                    if (!task.IsCancel)
                    {
                        BDebug.Log("【AssetbundleV2】开始执行异步加载：" + task.AssetBundleItem.LoadPath);

                        yield return task;

                        if (task.IsSuccess)
                        {
                            BDebug.Log("【AssetbundleV2】加载完成：" + task.AssetBundleItem.LoadPath);
                            task.Dispose();
                        }
                    }
                }

                // BDebug.Log("【Assetbundlev2】检测 剩余任务:" + this.asyncTaskGroupList.Count + "   " + curDoTask.MainAssetName);
                yield return null;
            }
        }

        #endregion

        #region 加载任务锁

        /// <summary>
        /// loder缓存表 防止重复加载
        /// </summary>
        private static Dictionary<string, AssetBundleCreateRequest> AB_LOAD_LOCK_MAP = new Dictionary<string, AssetBundleCreateRequest>();

        /// <summary>
        /// 锁住加载AB
        /// </summary>
        static public void LockLoadAssetBundle(string abPath, AssetBundleCreateRequest abcr = null)
        {
            if (AB_LOAD_LOCK_MAP.ContainsKey(abPath))
            {
                BDebug.LogError("【AssetbundleV2】 重复加锁 " + abPath);
                return;
            }

            AB_LOAD_LOCK_MAP[abPath] = abcr;
        }

        /// <summary>
        /// 解锁加载ab
        /// </summary>
        /// <param name="abPath"></param>
        /// <param name="abcr"></param>
        static public void UnLockLoadAssetBundle(string abPath, AssetBundleCreateRequest abcr = null)
        {
            AB_LOAD_LOCK_MAP.Remove(abPath);
        }

        /// <summary>
        /// 获取加载句柄
        /// </summary>
        static public AssetBundleCreateRequest GetLoaderHandle(string abPath)
        {
            AB_LOAD_LOCK_MAP.TryGetValue(abPath, out var abcr);
            return abcr;
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="assetPath">根据加载路径卸载</param>
        /// <param name="isForceUnload">强制卸载</param>
        public void UnloadAsset(string assetPath, bool isForceUnload = false)
        {
            //1.AB卸载
            var (assetBundleItem, dependAssetList) = AssetConfigLoder.GetDependAssets(assetPath);
            //添加主资源一起卸载
            dependAssetList.Add(assetBundleItem);
            //卸载
            for (int i = 0; i < dependAssetList.Count; i++)
            {
                var abPath = dependAssetList[i].AssetBundlePath;
                AssetBundleWapper abw = null;

                if (AssetbundleCacheMap.TryGetValue(abPath, out abw))
                {
                    if (isForceUnload)
                    {
                        abw.UnLoad();
                    }
                    else
                    {
                        abw.Unuse();
                    }
                }
            }

            //2.资源实例卸载
            UnloadObjectCache(assetPath);
        }


        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="path"></param>
        public void UnloadAllAsset()
        {
            AssetbundleCacheMap.Clear();
            GameObjectCacheMap.Clear();
            AssetBundle.UnloadAllAssetBundles(true);
            Resources.UnloadUnusedAssets();
        }

        #endregion
    }
}
