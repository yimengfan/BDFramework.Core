using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using AssetsManager.ArtAsset.AssetBundle.V2;
using BDFramework.Core.Tools;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using LitJson;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// ab包管理器
    /// </summary>
    public class AssetBundleMgrV2 : IResMgr
    {
        /// <summary>
        /// 最大加载任务数量
        /// </summary>
        static public int MAX_LOAD_TASK_NUM = 10;

        /// <summary>
        /// 最大的卸载任务数量
        /// </summary>
        private static int MAX_UNLOAD_TASK_NUM = 5;


        /// <summary>
        /// 异步加载表
        /// </summary>
        private List<LoadTaskGroup> asyncLoadTaskList { get; set; } = new List<LoadTaskGroup>(50); //<LoadTaskGroup>(50);

        /// <summary>
        /// 全局唯一的依赖
        /// </summary>
        public AssetBundleConfigLoader AssetBundleInfoLoader { get; private set; }

        /// <summary>
        /// 全局的ab缓存
        /// ab filename-assetbundle
        /// </summary>
        private Dictionary<string, AssetBundleWrapper> AssetbundleCacheMap { get; set; } = new Dictionary<string, AssetBundleWrapper>(StringComparer.OrdinalIgnoreCase);

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
        public void Init(string rootPath, RuntimePlatform platform = RuntimePlatform.Android)
        {
            //多热更切换,需要卸载
            if (this.AssetBundleInfoLoader != null)
            {
                this.UnloadAllAsset();
            }

            //平台
            string platformStr = "";
            //1.设置加载路径  
            if (Application.isEditor)
            {
                //editor下可以自行输入
                platformStr = BApplication.GetPlatformPath(platform);
                firstArtDirectory = IPath.Combine(rootPath, platformStr, BResources.ART_ASSET_ROOT_PATH);
                secArtDirectory = IPath.Combine(Application.streamingAssetsPath, platformStr, BResources.ART_ASSET_ROOT_PATH); //
            }
            else
            {
                //runtime只能使用当前平台
                platformStr = BApplication.GetRuntimePlatformPath();
                firstArtDirectory = IPath.Combine(Application.persistentDataPath, platformStr, BResources.ART_ASSET_ROOT_PATH);
                secArtDirectory = IPath.Combine(Application.streamingAssetsPath, platformStr, BResources.ART_ASSET_ROOT_PATH); //
                rootPath = Application.persistentDataPath;
            }

            //2.寻址路径格式化
            firstArtDirectory = IPath.FormatPathOnRuntime(firstArtDirectory);
            secArtDirectory = IPath.FormatPathOnRuntime(secArtDirectory);

            //3.加载ArtConfig
            this.AssetBundleInfoLoader = GetAssetbundleConfigLoder(rootPath, platformStr);

            //开始异步任务刷新
            IEnumeratorTool.StartCoroutine(this.IE_LoadTaskUpdate());
            IEnumeratorTool.StartCoroutine(this.IE_UnLoadTaskUpdate());

            BDebug.Log($"【AssetBundleV2】 firstDir:{firstArtDirectory}", "red");
            BDebug.Log($"【AssetBundleV2】 secDir:{secArtDirectory}", "red");
        }

        /// <summary>
        /// 获取AssetbundleConfigLoder
        /// </summary>
        /// <returns></returns>
        static public AssetBundleConfigLoader GetAssetbundleConfigLoder(string rootPath, string platformStr)
        {
            //路径
            var platformPath = IPath.Combine(rootPath, platformStr);
            //实例化
            var configLoader = new AssetBundleConfigLoader();
            configLoader.Load(platformPath);
            return configLoader;
        }


        /// <summary>
        /// 获取loadpath
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loadPathType"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        // private string GetLoadPath<T>(string path, LoadPathType loadPathType)
        // {
        //     //这里首次会耗时，主要是需要关联查询依赖文件
        //     switch (loadPathType)
        //     {
        //         case LoadPathType.RuntimePath:
        //             return path;
        //         case LoadPathType.GUID:
        //         {
        //             var abi = AssetBundleInfoLoader.GetAssetBundleItemByGUID(path, typeof(T));
        //             if (abi != null)
        //             {
        //                 return abi.LoadPath;
        //             }
        //         }
        //             break;
        //     }
        //
        //     return null;
        // }

        #region 同步加载

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="loadPathType"></param>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            // loadPath = GetLoadPath<T>(loadPath, loadPathType);

            //加载
            var obj = Load(typeof(T), loadPath, loadPathType);
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
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns></returns>
        public Object Load(Type type, string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath)
        {
            var retObj = GetObjectFormCache(loadPath, type);

            if (!retObj)
            {
                AssetBundleItem mainItem = null;
                List<AssetBundleItem> dependItems = null;
                //1.依赖路径
                switch (loadPathType)
                {
                    case LoadPathType.RuntimePath:
                        (mainItem, dependItems) = AssetBundleInfoLoader.GetDependAssets(loadPath, type);
                        break;
                    case LoadPathType.GUID:
                        (mainItem, dependItems) = AssetBundleInfoLoader.GetDependAssetsByGUID(loadPath, type);
                        break;
                }

                //2.加载
                if (dependItems?.Count > 0)
                {
                    //加载所有ab
                    foreach (var dependABItem in dependItems)
                    {
                        LoadAssetBundle(dependABItem.AssetBundlePath, dependABItem.Mix);
                    }

                    //使用ab
                    UseAssetBundle(loadPath, type);
                    //加载实例
                    var abFileItem = dependItems.Last();
                    retObj = LoadObjectFormAssetBundle(type, loadPath, mainItem.GUID, abFileItem);
                }
                else
                {
                    BDebug.LogError($"依赖获取失败,art_assets.info不存在资产配置,传入路径:" + loadPath);
                }
            }


            return retObj;
        }

        /// <summary>
        /// 获取某个目录下的所有资源
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [Obsolete]
        public T[] LoadAll<T>(string path) where T : Object
        {
            //非hash模式，需要debugRuntime
            // if (!this.AssetConfigLoder.IsHashName)
            // {
            //     path = ZString.Format(DEBUG_RUNTIME, path);
            // }
            // var item = AssetConfigLoder.GetAssetBundleData<T>(path);
            // //加载assetbundle
            // var task= LoadAssetBundleFile(item.AssetBundlePath);
            //
            // if (ab != null)
            // {
            //     var assetNames = ab.GetAllAssetNames();
            //     string relname = "";
            //     if (assetNames.Length == 1)
            //     {
            //         relname = assetNames[0];
            //     }
            //     else
            //     {
            //         var f = path + ".";
            //         relname = assetNames.First((s) => s.Contains(f));
            //     }
            //
            //     return ab.LoadAssetWithSubAssets<T>(relname);
            // }

            return null;
        }

        /// <summary>
        /// 多路径寻址
        /// </summary>
        /// <param name="assetbundleFileName">assetbundle文件名</param>
        /// <returns></returns>
        public string FindMultiAddressAsset(string assetbundleFileName)
        {
            //第一地址
            var p = IPath.Combine(this.firstArtDirectory, assetbundleFileName);
            //寻址到第二路径
            if (!File.Exists(p))
            {
                p = IPath.Combine(this.secArtDirectory, assetbundleFileName);
            }

            return p;
        }

        #endregion


        #region 异步加载

        /// <summary>
        /// 异步加载接口
        /// 未加载则返回LoadTask自行驱动，否则返回已加载的内容
        /// 一般作为Editor验证使用，不作为Runtime正式API
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns>返回Task</returns>
        public LoadTaskGroup AsyncLoad<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            LoadTaskGroup loadTask = null;
            var objCache = GetObjectFormCache(loadPath, typeof(T));
            if (!objCache)
            {
                loadTask = CreateAsyncLoadTask<T>(loadPath, loadPathType);
                if (loadTask == null)
                {
                    BDebug.LogError("不存在资源:" + loadPath);
                }
            }
            else
            {
                loadTask = new LoadTaskGroup(objCache);
            }

            return loadTask;
        }

        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        public int AsyncLoad<T>(string loadPath, Action<T> callback, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            //转换path
            // loadPath = GetLoadPath<T>(loadPath, loadPathType);

            var cacheObj = GetObjectFormCache(loadPath, typeof(T));
            if (!cacheObj)
            {
                var loadTask = CreateAsyncLoadTask<T>(loadPath, loadPathType);
                if (loadTask != null)
                {
                    //添加完成回调
                    loadTask.GetAwaiter().OnCompleted(() =>
                    {
                        if (!loadTask.IsCancel)
                        {
                            this.UseAssetBundle(loadPath, typeof(T));
                            //回调
                            var obj = loadTask.GetResult<T>();
                            callback(obj);
                        }
                    });

                    //添加到任务队列
                    AddAsyncTaskGroup(loadTask);
                    return loadTask.Id;
                }
                else
                {
                    BDebug.LogError("不存在资源:" + loadPath);
                }
            }
            else
            {
                //返回缓存的数据
                var obj = cacheObj as T;
                callback?.Invoke(obj);
            }

            return -1;
        }


        /// <summary>
        /// 全局的任务id
        /// </summary>
        private int taskIdxCounter;

        /// <summary>
        /// 异步加载接口
        /// 外部创建 需要自己管理yield,防止逻辑冲突
        /// 开放该接口，主要用于各种批量测试控制逻辑，一般情况下无需调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        private LoadTaskGroup CreateAsyncLoadTask<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            //
            AssetBundleItem mainItem = null;
            List<AssetBundleItem> dependItems = null;
            //1.依赖路径
            switch (loadPathType)
            {
                case LoadPathType.RuntimePath:
                    (mainItem, dependItems) = AssetBundleInfoLoader.GetDependAssets(loadPath, typeof(T));
                    break;
                case LoadPathType.GUID:
                    (mainItem, dependItems) = AssetBundleInfoLoader.GetDependAssetsByGUID(loadPath, typeof(T));
                    break;
            }

            if (dependItems != null && dependItems.Count > 0)
            {
                //取消卸载任务
                var abPaths = dependItems.Select((d) => d.AssetBundlePath).ToArray();
                this.CancelUnloadTask(abPaths);
                //创建任务组
                var taskGroup = new LoadTaskGroup(this, typeof(T), loadPath, mainItem.GUID, dependItems);
                taskGroup.Id = this.taskIdxCounter++;
                // AddAsyncTaskGroup(taskGroup);
                return taskGroup;
            }
            else
            {
                BDebug.LogError("不存在资源:" + loadPath);
            }

            return null;
        }

        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="loadPathList">资源</param>
        /// <param name="onLoadProcess">进度</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <returns>任务id列表</returns>
        public List<int> AsyncLoad(List<string> loadPathList, Action<int, int> onLoadProcess, Action<IDictionary<string, Object>> onLoadComplete, LoadPathType loadPathType = LoadPathType.RuntimePath)
        {
            var taskIdList = new List<int>();
            int taskCounter = 0;
            var loadAssetMap = new Dictionary<string, Object>();
            loadPathList = loadPathList.Distinct().ToList(); //去重
            int totalNum = loadPathList.Count;
            //依次添加异步任务
            foreach (var assetLoadPath in loadPathList)
            {
                var taskid = AsyncLoad<Object>(assetLoadPath, (o) =>
                {
                    loadAssetMap[assetLoadPath] = o;
                    //进度回调
                    onLoadProcess?.Invoke(loadAssetMap.Count, totalNum);
                    //完成回调
                    if (loadAssetMap.Count == totalNum)
                    {
                        onLoadComplete?.Invoke(loadAssetMap);
                    }
                }, loadPathType);
                taskIdList.Add(taskid);
            }

            //
            return taskIdList;
        }


        /// <summary>
        /// 添加一个任务组
        /// </summary>
        /// <param name="taskGroup"></param>
        public void AddAsyncTaskGroup(LoadTaskGroup taskGroup)
        {
            this.asyncLoadTaskList.Add(taskGroup);
        }

        #endregion


        #region AssetbundleFile 处理

        /// <summary>
        /// 加载AssetBundle文件
        /// </summary>
        /// <param name="assetbundleFileName">ab文件名</param>
        /// <returns></returns>
        public void LoadAssetBundle(string assetbundleFileName, int offset = 0)
        {
            AssetBundleWrapper abw = GetAssetBundleFromCache(assetbundleFileName);
            if (abw == null)
            {
                //取消加载任务
                this.CancelUnloadTask(assetbundleFileName);
                //寻找加载路径
                var abLocalPath = FindMultiAddressAsset(assetbundleFileName);
#if UNITY_EDITOR
                if (!File.Exists(abLocalPath))
                {
                    return;
                }
#endif

                //这里需要判断task列表，异步转同步
                var loadTask = GetExsitLoadTask(abLocalPath);
                if (loadTask != null)
                {
                    if (loadTask.IsAsyncTask)
                    {
                        loadTask.ToSynchronizationTask();
                        BDebug.Log("【AssetbundleV2】异步转同步:" + loadTask.LocalPath);
                    }
                    else
                    {
                        BDebug.LogError("【AssetbundleV2】同步任务调度错误~");
                    }
                }
                else
                {
                    //同步也要创建任务
                    loadTask = new LoadTask(abLocalPath, 0u, (ulong) offset);
                    AddGlobalLoadTask(loadTask);
                    {
                        //同步加载
                        loadTask.Load();
                    }
                    RemoveGlobalLoadTask(loadTask);
                }


#if UNITY_EDITOR
                if (loadTask.AssetBundle == null)
                {
                    Debug.LogError($"【AssetBundleV2】 ab加载失败{loadTask.LocalPath}");
                }
#endif
                //添加
                abw = this.AddAssetBundleToCache(assetbundleFileName, loadTask.AssetBundle);
            }
        }


        /// <summary>
        /// 添加ab到缓存
        /// </summary>
        /// <param name="assetbundleFileName">这里是ab文件名，不是路径</param>
        /// <param name="ab"></param>
        public AssetBundleWrapper AddAssetBundleToCache(string assetbundleFileName, AssetBundle ab)
        {
            AssetBundleWrapper abw = null;
            //
            if (!AssetbundleCacheMap.TryGetValue(assetbundleFileName, out abw))
            {
                abw = new AssetBundleWrapper(ab);
                AssetbundleCacheMap[assetbundleFileName] = abw;
            }

            return abw;
        }


        /// <summary>
        /// 获取缓存中的ab
        /// </summary>
        /// <param name="assetBundleFileName"></param>
        /// <returns></returns>
        public AssetBundleWrapper GetAssetBundleFromCache(string assetBundleFileName)
        {
            AssetbundleCacheMap.TryGetValue(assetBundleFileName, out var abw);
            return abw;
        }

        #endregion

        #region 从AB中加载Object

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <param name="item"></param>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public T LoadObjectFormAssetBundle<T>(string loadPath, string guid, AssetBundleItem item) where T : UnityEngine.Object
        {
            var obj = LoadObjectFormAssetBundle(typeof(T), loadPath, guid, item);
            if (obj)
            {
                return (obj as T);
            }

            return null;
        }

        /// <summary>
        /// 加载实例资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadType"></param>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="guid"></param>
        /// <param name="abFileItem"></param>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public Object LoadObjectFormAssetBundle(Type loadType, string loadPath, string guid, AssetBundleItem abFileItem)
        {
            Object retObj = null;
            if (AssetbundleCacheMap.TryGetValue(abFileItem.AssetBundlePath, out var abw))
            {
                retObj = abw.LoadAsset(loadType, guid);
                //加入缓存
                AddObjectToCache(loadType, loadPath, retObj);
            }
            else
            {
                BDebug.Log("资源不存在:" + loadPath + " - " + abFileItem.AssetBundlePath, "red");
                return null;
            }

            return retObj;
        }

        /// <summary>
        /// 加载实例资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadType"></param>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="item"></param>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public void AsyncLoadObjectFormAssetBundle(Type loadType, string loadPath, LoadPathType loadPathType, AssetBundleItem item, Action<Object> callback)
        {
            if (AssetbundleCacheMap.TryGetValue(item.AssetBundlePath, out var abw))
            {
                //item.AssetType == AssetType.TYPE_SPRITE_ATLAS
                //异步加载
                abw.AsyncLoadAsset(loadType, loadPath, (o) =>
                {
                    //加入缓存
                    AddObjectToCache(loadType, loadPath, o);
                    //触发回调
                    callback?.Invoke(o);
                }, item.AssetType);
            }
            else
            {
                BDebug.Log("资源不存在:" + loadPath + " - " + item.AssetBundlePath, "red");
            }
        }

        #endregion

        #region 取消加载任务

        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public bool LoadCancel(int taskid)
        {
            foreach (var tg in asyncLoadTaskList)
            {
                if (tg.Id == taskid)
                {
                    tg.Cancel();
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 取消所有load任务
        /// </summary>
        public void LoadAllCancel()
        {
            foreach (var tg in asyncLoadTaskList)
            {
                tg.Cancel();
            }

            this.asyncLoadTaskList.Clear();
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
            foreach (var abItem in this.AssetBundleInfoLoader.AssetbundleItemList)
            {
                if (abItem.LoadPath != null && abItem.LoadPath.StartsWith(str, StringComparison.OrdinalIgnoreCase))
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

            return rets.ToArray();
        }

        private AssetBundleWrapper ALLSHADER = null;

        /// <summary>
        /// 预热shader
        /// </summary>
        public void WarmUpShaders()
        {
            var svc = this.Load<ShaderVariantCollection>(BResources.ALL_SHADER_VARAINT_RUNTIME_PATH);
            if (!svc.isWarmedUp)
            {
                svc.WarmUp();
                //加载shaders
                if (this.ALLSHADER == null)
                {
                    var abItem = this.AssetBundleInfoLoader.GetAssetBundleItem(BResources.ALL_SHADER_VARAINT_RUNTIME_PATH);
                    var abContentItem = this.AssetBundleInfoLoader.GetAssetBundleItem(abItem.DependAssetIds.Last());
                    if (abContentItem.AssetBundlePath == null)
                    {
                        BDebug.LogError("Shader AssetBundlePath 为空！！！！ ");
                        return;
                    }
                   
                    this.AssetbundleCacheMap.TryGetValue(abContentItem.AssetBundlePath, out ALLSHADER);
                    if (ALLSHADER != null)
                    {
                        ALLSHADER.LoadAllShaders();
                    }
                }
            }
#if UNITY_EDITOR
            var (_, dependList) = this.AssetBundleInfoLoader.GetDependAssets(BResources.ALL_SHADER_VARAINT_RUNTIME_PATH);
            var svcABItem = dependList.Last();
            this.AssetbundleCacheMap.TryGetValue(svcABItem.AssetBundlePath, out var ab);
            BDebug.Log("WarmUp Shaders:\n" + JsonMapper.ToJson(ab.AssetBundle.GetAllAssetNames(), true));
#endif
        }

        /// <summary>
        /// 寻找一个shader
        /// </summary>
        /// <param name="shaderName"></param>
        /// <returns></returns>
        public Shader FindShader(string shaderName)
        {
            return ALLSHADER?.FindShader(shaderName);
        }

        #endregion

        #region 加载任务

        /// <summary>
        /// 加载任务的驱动
        /// 目前只处理异步任务
        /// </summary>
        IEnumerator IE_LoadTaskUpdate()
        {
            while (true)
            {
                //执行任务，因为要兼容IEnumerator,所以直接调用keepWaiting
                // for (int i = 0; i < asyncLoadTaskList.Count; i++)
                // {
                //     var asyncTask = asyncLoadTaskList[i];
                //     var iskeep = asyncTask.keepWaiting;
                // }

                //移除已完成
                for (int i = asyncLoadTaskList.Count - 1; i >= 0; i--)
                {
                    var asyncTask = asyncLoadTaskList[i];
                    if (asyncTask.IsCancel || asyncTask.IsSuccess)
                    {
                        asyncTask.Dispose();
                        asyncLoadTaskList.RemoveAt(i);
                    }
                }

                yield return null;
            }
        }

        #endregion

        #region 全局加载任务控制

        /// <summary>
        /// loder缓存表 防止重复加载
        /// </summary>
        private static Dictionary<string, LoadTask> GLOBAL_LOAD_TASK_MAP = new Dictionary<string, LoadTask>();

        /// <summary>
        /// 是否能添加全局任务
        /// </summary>
        /// <returns></returns>
        static public bool IsCanAddGlobalTask
        {
            get { return GLOBAL_LOAD_TASK_MAP.Count < MAX_LOAD_TASK_NUM; }
        }


        /// <summary>
        /// 锁住加载AB
        /// </summary>
        static public void AddGlobalLoadTask(LoadTask loadTask)
        {
            if (GLOBAL_LOAD_TASK_MAP.ContainsKey(loadTask.LocalPath))
            {
                BDebug.LogError("【AssetbundleV2】 重复任务 " + loadTask.LocalPath);
                return;
            }

            GLOBAL_LOAD_TASK_MAP[loadTask.LocalPath] = loadTask;
        }

        /// <summary>
        /// 移除加载任务
        /// </summary>
        /// <param name="abPath"></param>
        /// <param name="abcr"></param>
        static public void RemoveGlobalLoadTask(LoadTask loadTask)
        {
            if (loadTask.IsDone)
            {
                GLOBAL_LOAD_TASK_MAP.Remove(loadTask.LocalPath);
            }
        }

        /// <summary>
        ///  获取加载句柄
        /// </summary>
        /// <param name="abPath">ab加载全路径</param>
        /// <returns></returns>
        static public LoadTask GetExsitLoadTask(string abPath)
        {
            GLOBAL_LOAD_TASK_MAP.TryGetValue(abPath, out var task);
            return task;
        }

        #endregion


        #region 引用计数辅助

        /// <summary>
        /// 加载计数
        /// 防止对某个具体资产 一直unse 导致ab被卸载
        /// </summary>
        private Dictionary<string, int> loadCounterMap = new Dictionary<string, int>();

        /// <summary>
        /// 引用计数辅助,use
        /// </summary>
        private void UseAssetBundle(string loadPath, Type type = null)
        {
            if (!loadCounterMap.TryGetValue(loadPath, out var counter))
            {
                counter = 0;
                loadCounterMap[loadPath] = 0;
            }

            //自增
            loadCounterMap[loadPath] = counter++;
            //1.ab自增
            var mainAssetItem = AssetBundleInfoLoader.GetAssetBundleItem(loadPath, type,true);
            var dependAssetList = AssetBundleInfoLoader.GetDependAssets(mainAssetItem);
            foreach (var abi in dependAssetList)
            {
                if (AssetbundleCacheMap.TryGetValue(abi.AssetBundlePath, out var abw))
                {
                    //
                    abw.Use();
                }
            }
        }

        /// <summary>
        /// 引用计数辅助,unuse
        /// </summary>
        private void UnUseAssetBundle(string loadPath, Type type = null)
        {
            if (loadCounterMap.TryGetValue(loadPath, out var counter))
            {
                if (counter > 0)
                {
                    BDebug.Log($"AssetMgrV2: <color=yellow>UnUse</color> {loadPath}");
                    //1.AB卸载
                    var mainAssetItem = AssetBundleInfoLoader.GetAssetBundleItem(loadPath, type,true);
                    var dependAssetList = AssetBundleInfoLoader.GetDependAssets(mainAssetItem);
                    foreach (var abi in dependAssetList)
                    {
                        var abPath = abi.AssetBundlePath;
                        //unuse
                        if (AssetbundleCacheMap.TryGetValue(abPath, out var abw))
                        {
                            abw.Unuse();

                            //判断是否需要卸载
                            if (abw.UseCounter <= 0)
                            {
                                //卸载回调
                                Action onUnloadEnd = () =>
                                {
                                    //移除assetbundle
                                    AssetbundleCacheMap.Remove(abPath);
                                    //移除缓存
                                    this.UnloadObjectCache(type, loadPath);
                                };

                                //创建unload任务
                                var unloadTask = new UnLoadTask(abw, onUnloadEnd);
                                //添加卸载任务
                                this.AddUnloadTask(abPath, unloadTask);

#if UNITY_EDITOR
                                BDebug.Log($"AssetMgrV2: <color=red>Unload</color> {loadPath} - AB: {AssetDatabase.GUIDToAssetPath(abw.AssetBundle.name)}");
#else
                                BDebug.Log($"AssetMgrV2: <color=red>Unload</color> {loadPath} - AB: { abw.AssetBundle.name}");
#endif
                            }
                        }
                    }
                }
                else
                {
                    BDebug.LogError($"loader计数=0，无法继续卸载:{loadPath}");
                }
            }
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载/AssetBundle
        /// </summary>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="type">为空则卸载所有同名资源，为空则卸载指定类型资源</param>
        public void UnloadAsset(string loadPath, Type type = null)
        {
            UnUseAssetBundle(loadPath);
        }


        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        /// <param name="path"></param>
        public void UnloadAllAsset()
        {
            AssetbundleCacheMap.Clear();
            GameObjectCacheMap.Clear();
            loadCounterMap.Clear();
            AssetBundle.UnloadAllAssetBundles(true);
            Resources.UnloadUnusedAssets();
        }

        #endregion

        #region 卸载任务

        /// <summary>
        /// 卸载任务集合
        /// </summary>
        /// <returns></returns>
        private Queue<KeyValuePair<string, UnLoadTask>> UnloadTaskQueue = new Queue<KeyValuePair<string, UnLoadTask>>(50);

        /// <summary>
        /// 执行卸载任务
        /// </summary>
        public void AddUnloadTask(string assetbundleFileName, UnLoadTask unloadTask)
        {
            UnloadTaskQueue.Enqueue(new KeyValuePair<string, UnLoadTask>(assetbundleFileName, unloadTask));
        }

        /// <summary>
        /// 取消卸载任务
        /// </summary>
        public void CancelUnloadTask(params string[] paths)
        {
            foreach (var item in UnloadTaskQueue)
            {
                if (paths.Contains(item.Key))
                {
                    //取消
                    item.Value.Cancel();
                }
            }
        }


        /// <summary>
        /// unload计数
        /// </summary>
        private int unloadCounter = 0;

        /// <summary>
        /// 卸载任务的驱动
        /// </summary>
        /// <returns></returns>
        IEnumerator IE_UnLoadTaskUpdate()
        {
            while (true)
            {
                if (UnloadTaskQueue.Count > 0 && unloadCounter < MAX_UNLOAD_TASK_NUM)
                {
                    unloadCounter++;
                    var unloadTask = UnloadTaskQueue.Dequeue();
                    unloadTask.Value.Unload();
                }

                unloadCounter = 0;
                yield return null;
            }
        }

        #endregion


        #region 资源缓存

        /// <summary>
        /// 全局的资源缓存
        /// </summary>
        private Dictionary<Type, Dictionary<string, UnityEngine.Object>> GameObjectCacheMap { get; set; } = new Dictionary<Type, Dictionary<string, Object>>();

        /// <summary>
        /// 从缓存中加载
        /// </summary>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns></returns>
        public void AddObjectToCache(Type type, string loadPath, Object obj)
        {
            //部分类型不进行缓存~
            if (type == typeof(TextAsset))
            {
                return;
            }

            //
            var ret = GameObjectCacheMap.TryGetValue(type, out var map);
            if (!ret)
            {
                map = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);
                GameObjectCacheMap[type] = map;
            }

            BDebug.Log($"<color=yellow>缓存添加成功</color>:{loadPath}");
            map[loadPath] = obj;
        }

        /// <summary>
        /// 从缓存中加载
        /// </summary>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object GetObjectFormCache(string loadPath, Type type)
        {
            var ret = GameObjectCacheMap.TryGetValue(type, out var map);
            if (ret)
            {
                ret = map.TryGetValue(loadPath, out var gobj);
                if (ret)
                {
                    BDebug.Log($"<color=yellow>缓存命中成功</color>:{loadPath}");
                }

                return gobj;
            }
            return null;
        }
        

        /// <summary>
        /// 从缓存中卸载 实例化的资源
        /// </summary>
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns></returns>
        public void UnloadObjectCache(Type type, string assetLoadPath)
        {
            if (type != null)
            {
                //卸载指定类型的缓存资源
                var ret = GameObjectCacheMap.TryGetValue(type, out var map);
                if (ret)
                {
                    ret = map.TryGetValue(assetLoadPath, out var gobj);
                    if (ret)
                    {
                        //卸载实例化
                        BResources.UnloadAsset(gobj);
                        map.Remove(assetLoadPath);

                        BDebug.Log($"<color=red>删除缓存</color>:{assetLoadPath}");
                    }
                }
            }
            else
            {
                //卸载所有同名的缓存资产
                foreach (var typeMap in GameObjectCacheMap.Values)
                {
                    var ret = typeMap.TryGetValue(assetLoadPath, out var gobj);
                    if (ret)
                    {
                        //卸载
                        BResources.UnloadAsset(gobj);
                        typeMap.Remove(assetLoadPath);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 设置加载配置
        /// </summary>
        /// <param name="maxLoadTaskNum"></param>
        /// <param name="maxUnloadTaskNum"></param>
        public void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1)
        {
            if (maxLoadTaskNum > 0)
            {
                MAX_LOAD_TASK_NUM = maxLoadTaskNum;
            }

            if (maxUnloadTaskNum > 0)
            {
                MAX_UNLOAD_TASK_NUM = maxUnloadTaskNum;
            }
        }
    }
}
