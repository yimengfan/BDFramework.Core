using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using AssetsManager.ArtAsset.AssetBundle.V2;
using BDFramework.Core.Tools;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using LitJson;
using Object = UnityEngine.Object;

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
        private Queue<LoadTaskGroup> asyncLoadTaskGroupQueue { get; set; } = new Queue<LoadTaskGroup>(50);

        /// <summary>
        /// 全局唯一的依赖
        /// </summary>
        public AssetbundleConfigLoder AssetConfigLoder { get; private set; }

        /// <summary>
        /// 全局的ab缓存
        /// ab filename-assetbundle
        /// </summary>
        private Dictionary<string, AssetBundleWapper> AssetbundleCacheMap { get; set; } = new Dictionary<string, AssetBundleWapper>(StringComparer.OrdinalIgnoreCase);

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
            firstArtDirectory = IPath.FormatPathOnRuntime(firstArtDirectory);
            secArtDirectory = IPath.FormatPathOnRuntime(secArtDirectory);

            //3.加载ArtConfig
            this.AssetConfigLoder = new AssetbundleConfigLoder();
            string assetconfigPath;
            string assetTypePath;
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
            IEnumeratorTool.StartCoroutine(this.IE_LoadTaskUpdate());
            IEnumeratorTool.StartCoroutine(this.IE_UnLoadTaskUpdate());

            BDebug.Log($"【AssetBundleV2】 firstDir:{firstArtDirectory}", "red");
            BDebug.Log($"【AssetBundleV2】 secDir:{secArtDirectory}", "red");
        }


        #region 同步加载

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="pathType"></param>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string assetLoadPath, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            //这里首次会耗时，主要是需要关联查询依赖文件
            if (pathType == LoadPathType.GUID)
            {
                var abi = AssetConfigLoder.GetAssetBundleDataByGUID(assetLoadPath);
                if (abi != null)
                {
                    assetLoadPath = abi.LoadPath;
                }
            }

            //加载
            var obj = Load(typeof(T), assetLoadPath);
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
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns></returns>
        public Object Load(Type type, string assetLoadPath)
        {
            var retObj = GetObjectFormCache(type, assetLoadPath);

            if (!retObj)
            {
                //1.依赖路径
                var (mainAssetItem, dependAssetList) = AssetConfigLoder.GetDependAssets(assetLoadPath, type);
                //2.加载
                if (mainAssetItem != null)
                {
                    if (dependAssetList == null)
                    {
                        dependAssetList = new List<AssetBundleItem>();
                    }

                    dependAssetList.Add(mainAssetItem);
                    //加载所有ab
                    foreach (var dependABItem in dependAssetList)
                    {
                        LoadAssetBundle(dependABItem.AssetBundlePath, dependABItem.Mix);
                    }

                    //加载实例
                    retObj = LoadObjectFormAssetBundle(type, assetLoadPath, mainAssetItem);
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
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        public int AsyncLoad<T>(string assetLoadPath, Action<T> callback) where T : UnityEngine.Object
        {
            var cacheObj = GetObjectFormCache(typeof(T), assetLoadPath);
            if (!cacheObj)
            {
                var loadTask = CreateAsyncLoadTask<T>(assetLoadPath);
                if (loadTask != null)
                {
                    //添加完成回调
                    loadTask.GetAwaiter().OnCompleted(() =>
                    {
                        var obj = loadTask.GetResult<T>();
                        //回调
                        callback(obj);
                    });
                    
                    //添加到任务队列
                    AddAsyncTaskGroup(loadTask);
                    return loadTask.Id;
                }
                else
                {
                    BDebug.LogError("不存在资源:" + assetLoadPath);
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
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        public LoadTaskGroup CreateAsyncLoadTask<T>(string assetLoadPath) where T : UnityEngine.Object
        {
            var assetBundleItem = AssetConfigLoder.GetAssetBundleData<T>(assetLoadPath);

            if (assetBundleItem != null)
            {
                //取消卸载任务
                this.CancelUnloadTask(assetBundleItem.AssetBundlePath);
                //创建任务组
                var taskGroup = new LoadTaskGroup(this, typeof(T), assetLoadPath, assetBundleItem);
                taskGroup.Id = this.taskIdxCounter++;
                // AddAsyncTaskGroup(taskGroup);
                return taskGroup;
            }
            else
            {
                BDebug.LogError("不存在资源:" + assetLoadPath);
            }

            return null;
        }

        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="assetLoadPathList">资源</param>
        /// <param name="onLoadProcess">进度</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <returns>任务id列表</returns>
        public List<int> AsyncLoad(List<string> assetLoadPathList, Action<int, int> onLoadProcess, Action<IDictionary<string, Object>> onLoadComplete)
        {
            var taskIdList = new List<int>();
            int taskCounter = 0;
            var loadAssetMap = new Dictionary<string, Object>();
            assetLoadPathList = assetLoadPathList.Distinct().ToList(); //去重
            int totalNum = assetLoadPathList.Count;
            //依次添加异步任务
            foreach (var assetLoadPath in assetLoadPathList)
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
                });
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
            this.asyncLoadTaskGroupQueue.Enqueue(taskGroup);
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
            AssetBundleWapper abw = GetAssetBundleFromCache(assetbundleFileName);
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
                var loadTask = GetExsitLoadTask(assetbundleFileName);
                if (loadTask != null)
                {
                    if (loadTask.IsAsyncTask)
                    {
                        loadTask.ToSynchronizationTask();
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

            //使用
            abw.Use();
        }


        /// <summary>
        /// 添加ab到缓存
        /// </summary>
        /// <param name="assetbundleFileName">这里是ab文件名，不是路径</param>
        /// <param name="ab"></param>
        public AssetBundleWapper AddAssetBundleToCache(string assetbundleFileName, AssetBundle ab)
        {
            AssetBundleWapper abw = null;
            //
            if (!AssetbundleCacheMap.TryGetValue(assetbundleFileName, out abw))
            {
                abw = new AssetBundleWapper(ab);
                AssetbundleCacheMap[assetbundleFileName] = abw;
            }

            return abw;
        }


        /// <summary>
        /// 获取缓存中的ab
        /// </summary>
        /// <param name="assetBundleFileName"></param>
        /// <returns></returns>
        public AssetBundleWapper GetAssetBundleFromCache(string assetBundleFileName)
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
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public T LoadObjectFormAssetBundle<T>(string assetName, AssetBundleItem item) where T : UnityEngine.Object
        {
            var obj = LoadObjectFormAssetBundle(typeof(T), assetName, item);
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
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="item"></param>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public Object LoadObjectFormAssetBundle(Type loadType, string assetLoadPath, AssetBundleItem item)
        {
            Object retObj = null;

            if (AssetbundleCacheMap.TryGetValue(item.AssetBundlePath, out var abw))
            {
                retObj = abw.LoadAsset(loadType, assetLoadPath, item.AssetType);
                //加入缓存
                AddObjectToCache(loadType, assetLoadPath, retObj);
            }
            else
            {
                BDebug.Log("资源不存在:" + assetLoadPath + " - " + item.AssetBundlePath, "red");
                return null;
            }

            return retObj;
        }

        /// <summary>
        /// 加载实例资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadType"></param>
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="item"></param>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public void AsyncLoadObjectFormAssetBundle(Type loadType, string assetLoadPath, AssetBundleItem item, Action<Object> callback)
        {
            if (AssetbundleCacheMap.TryGetValue(item.AssetBundlePath, out var abw))
            {
                //item.AssetType == AssetType.TYPE_SPRITE_ATLAS
                //异步加载
                abw.AsyncLoadAsset(loadType, assetLoadPath, (o) =>
                {
                    //加入缓存
                    AddObjectToCache(loadType, assetLoadPath, o);
                    //触发回调
                    callback?.Invoke(o);
                }, item.AssetType);
            }
            else
            {
                BDebug.Log("资源不存在:" + assetLoadPath + " - " + item.AssetBundlePath, "red");
            }
        }

        #endregion

        #region 取消加载任务

        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            foreach (var tg in asyncLoadTaskGroupQueue)
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
            foreach (var tg in asyncLoadTaskGroupQueue)
            {
                tg.Cancel();
            }

            this.asyncLoadTaskGroupQueue.Clear();
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
            Debug.Log("WarmUp Shaders:\n" + JsonMapper.ToJson(ab.AssetBundle.GetAllAssetNames(), true));
#endif
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
                //1.加载任务
                if (this.asyncLoadTaskGroupQueue.Count > 0)
                {
                    //开始新任务
                    LoadTaskGroup task = null;
                    //执行加载
                    while (this.asyncLoadTaskGroupQueue.Count > 0 && task == null)
                    {
                        task = this.asyncLoadTaskGroupQueue.Dequeue();
                        if (task.IsCancel)
                        {
                            task.Dispose();
                            task = null;
                        }
                    }

                    //执行任务
                    if (task != null && !task.IsSuccess)
                    {
                        BDebug.Log("【AssetbundleV2】开始执行异步加载：" + task.MainAssetBundleLoadPath);

                        yield return task;

                        if (task.IsSuccess)
                        {
                            BDebug.Log("【AssetbundleV2】加载完成：" + task.MainAssetBundleLoadPath);
                            task.Dispose();
                        }
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
        /// 全局任务数量
        /// </summary>
        public int GlobalLoadTaskCount
        {
            get { return GLOBAL_LOAD_TASK_MAP.Count; }
        }

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
        /// 获取加载句柄
        /// </summary>
        static public LoadTask GetExsitLoadTask(string abPath)
        {
            GLOBAL_LOAD_TASK_MAP.TryGetValue(abPath, out var task);
            return task;
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载/AssetBundle
        /// </summary>
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <param name="isForceUnload">强制卸载</param>
        /// <param name="type"></param>
        public void UnloadAsset(string assetLoadPath, bool isForceUnload = false, Type type = null)
        {
            //1.AB卸载
            var (assetBundleItem, dependAssetList) = AssetConfigLoder.GetDependAssets(assetLoadPath, type);
            //添加主资源一起卸载
            dependAssetList.Add(assetBundleItem);
            //卸载
            for (int i = 0; i < dependAssetList.Count; i++)
            {
                var assetbundleFileName = dependAssetList[i].AssetBundlePath;

                if (AssetbundleCacheMap.TryGetValue(assetbundleFileName, out var abw))
                {
                    //
                    abw.Unuse();
                    //判断是否需要卸载
                    if (isForceUnload || abw.UseCounter == 0)
                    {
                        //卸载回调
                        Action onUnloadEnd = () =>
                        {
                            //移除assetbundle
                            AssetbundleCacheMap.Remove(assetbundleFileName);
                            //移除缓存
                            this.UnloadObjectCache(type, assetLoadPath);
                        };

                        //创建unload任务
                        var unloadTask = new UnLoadTask(abw, onUnloadEnd);
                        //添加卸载任务
                        this.AddUnloadTask(assetbundleFileName, unloadTask);
                    }
                }
            }
        }


        /// <summary>
        /// 强制卸载所有资源
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
        public void CancelUnloadTask(string assetbundleFileName)
        {
            foreach (var item in UnloadTaskQueue)
            {
                if (item.Key.Equals(assetbundleFileName))
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
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns></returns>
        public void AddObjectToCache(Type type, string assetLoadPath, Object obj)
        {
            var ret = GameObjectCacheMap.TryGetValue(type, out var map);
            if (!ret)
            {
                map = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);
                GameObjectCacheMap[type] = map;
            }

            map[assetLoadPath] = obj;
        }

        /// <summary>
        /// 从缓存中加载
        /// </summary>
        /// <param name="assetLoadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns></returns>
        public Object GetObjectFormCache(Type type, string assetLoadPath)
        {
            var ret = GameObjectCacheMap.TryGetValue(type, out var map);
            if (ret)
            {
                ret = map.TryGetValue(assetLoadPath, out var gobj);
                if (ret)
                {
                    Debug.Log("缓存命中成功:" + assetLoadPath);
                }

                return gobj;
            }

            return null;
        }

        /// <summary>
        /// 从缓存中卸载
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
                        //卸载
                        BResources.UnloadAsset(gobj);
                        map.Remove(assetLoadPath);
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
