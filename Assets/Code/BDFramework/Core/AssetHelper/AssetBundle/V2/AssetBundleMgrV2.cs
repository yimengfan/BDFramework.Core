using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using Code.BDFramework.Core.Tools;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// ab包管理器
    /// </summary>
    public class AssetBundleMgrV2 : IResMgr
    {
        /// <summary>
        /// 特殊的前缀
        /// </summary>
        static readonly public string RUNTIME = "runtime/{0}";

        /// <summary>
        /// 异步任务颗粒度，每帧执行多少个
        /// </summary>
        static readonly public int ASYNC_TASK_UNIT = 5;

        /// <summary>
        /// 全局的任务id
        /// </summary>
        private int taskIDCounter;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private List<LoaderTaskGroup> allTaskGroupList;

        /// <summary>
        /// 全局唯一的依赖
        /// </summary>
        private ManifestLoder loder;

        /// <summary>
        /// 全局的assetbundle字典
        /// </summary>
        public Dictionary<string, AssetBundleWapper> AssetbundleMap { get; private set; }

        /// <summary>
        /// 资源加载路径
        /// </summary>
        private string artRootPath = "";

        //第二寻址路径
        private string secArtRootPath = "";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onInitEnd"></param>
        public void Init(string path, Action onInitEnd)
        {
            //多热更切换,需要卸载
            if (this.loder != null)
            {
                this.UnloadAllAsset();
                GC.Collect();
            }

            this.AssetbundleMap = new Dictionary<string, AssetBundleWapper>();
            this.allTaskGroupList = new List<LoaderTaskGroup>();
            //1.设置加载路径  
            artRootPath = (path + "/" + BApplication.GetPlatformPath(Application.platform) + "/Art").Replace("\\", "/");
            secArtRootPath = (Application.streamingAssetsPath + "/" +
                              BApplication.GetPlatformPath(Application.platform) + "/Art")
                .Replace("\\", "/");
            //
            string configPath = FindAsset("Config.json");
            BDebug.Log("AssetBundle Version: V2", "red");
            BDebug.Log("Art加载路径:" + configPath, "red");
            //
            this.loder = new ManifestLoder();
            this.loder.Load(configPath, onInitEnd);
        }


        #region 对外加载接口

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            path = string.Format(RUNTIME, path.ToLower());
            //1.依赖路径
            var item = loder.Manifest.GetManifest(path);
            if (item != null)
            {
                //加载依赖
                foreach (var dependAsset in item.Depend)
                {
                    LoadAssetBundle(dependAsset);
                }
                //加载主资源

                LoadAssetBundle(item.Path);
                //
                return LoadFormAssetBundle<T>(path);
            }

            return null;
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
        public T[] LoadAll_TestAPI_2020_5_23<T>(string path) where T : Object
        {
            path = string.Format(RUNTIME, path.ToLower());
            var item = loder.Manifest.GetManifest(path);
            //加载assetbundle
            AssetBundle ab = LoadAssetBundle(item.Path);

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
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public int AsyncLoad<T>(string path, Action<T> callback) where T : UnityEngine.Object
        {
            path = string.Format(RUNTIME, path.ToLower());
            List<LoaderTaskData> taskQueue = new List<LoaderTaskData>();
            //获取依赖
            var mainItem = loder.Manifest.GetManifest(path);
            if (mainItem != null)
            {
                //依赖任务
                foreach (var r in mainItem.Depend)
                {
                    var task = new LoaderTaskData(r, typeof(Object));
                    taskQueue.Add(task);
                }

                //主任务
                var mainTask = new LoaderTaskData(mainItem.Path, typeof(Object));
                taskQueue.Add(mainTask);

                //添加任务组
                LoaderTaskGroup taskGroup = new LoaderTaskGroup(mainItem.Path, ASYNC_TASK_UNIT, taskQueue,
                    AsyncLoadAssetBundle, //Loader接口
                    (p, obj) =>
                    {
                        //完成回调
                        callback(obj as T);
                    });
                taskGroup.Id = this.taskIDCounter++;
                AddTaskGroup(taskGroup);

                //开始任务
                DoNextTask();
                return taskGroup.Id;
            }

            return -1;
        }


        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="assetsPath">资源</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <param name="onLoadProcess">进度</param>
        /// <returns>taskid</returns>
        public List<int> AsyncLoad(IList<string> assetsPath,
            Action<IDictionary<string, Object>> onLoadComplete,
            Action<int, int> onLoadProcess)
        {
            List<int> idList = new List<int>();
            IDictionary<string, Object> retMap = new Dictionary<string, Object>();
            assetsPath = assetsPath.Distinct().ToList(); //去重
            int total = assetsPath.Count;
            //source
            int counter = 0;
            foreach (var assetPath in assetsPath)
            {
                var path = string.Format(RUNTIME, assetPath.ToLower());
                //
                var mainItem = loder.Manifest.GetManifest(path);
                List<LoaderTaskData> taskQueue = new List<LoaderTaskData>();
                //获取依赖
                if (mainItem == null)
                {
                    BDebug.LogError("不存在:" + path);
                    total--;
                    continue;
                }

                //依赖任务
                foreach (var r in mainItem.Depend)
                {
                    var task = new LoaderTaskData(r, typeof(Object));
                    taskQueue.Add(task);
                }

                //主任务
                var mainTask = new LoaderTaskData(mainItem.Path, typeof(Object));
                taskQueue.Add(mainTask);

                //添加任务组
                //加载颗粒度10个
                LoaderTaskGroup taskGroup = new LoaderTaskGroup(mainItem.Path, ASYNC_TASK_UNIT, taskQueue,
                    AsyncLoadAssetBundle, //Load接口
                    (p, obj) =>
                    {
                        counter++;
                        //注意返回加载的id，不是具体地址的id
                        retMap[assetPath] = obj;
                        if (onLoadProcess != null)
                        {
                            onLoadProcess(counter, total);
                        }

                        //完成
                        if (retMap.Count == total)
                        {
                            onLoadComplete(retMap);
                        }
                    });
                taskGroup.Id = this.taskIDCounter++;
                AddTaskGroup(taskGroup);
                idList.Add(taskGroup.Id);
            }

            //开始任务
            DoNextTask();
            //
            return idList;
        }


        /// <summary>
        /// 添加一个任务组
        /// </summary>
        /// <param name="taskGroup"></param>
        public void AddTaskGroup(LoaderTaskGroup taskGroup)
        {
            this.allTaskGroupList.Add(taskGroup);
        }

        /// <summary>
        /// 检测
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private string FindAsset(string res)
        {
            //第一地址
            var p = IPath.Combine(this.artRootPath, res);
            //寻址到第二路径,第二地址没有就放弃
            if (!File.Exists(p))
            {
                p = IPath.Combine(this.secArtRootPath, res);
            }

            return p;
        }

        #endregion

        #region 加载AssetsBundle

        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private AssetBundle LoadAssetBundle(string path)
        {
            if (AssetbundleMap.ContainsKey(path))
            {
                AssetBundleWapper abw = null;
                if (AssetbundleMap.TryGetValue(path, out abw))
                {
                    abw.Use();
                    return abw.AssetBundle;
                }
            }
            else
            {
                var p = FindAsset(path);
                var ab = AssetBundle.LoadFromFile(p);
                //添加
                AddAssetBundle(path, ab);
                return ab;
            }

            return null;
        }

        /// <summary>
        /// 单个加载ab,会自动刷新依赖
        /// </summary>
        /// <param name="assetHash"></param>
        /// <param name="callback"></param>
        private void AsyncLoadAssetBundle(string assetHash,
            bool isLoadObj = false,
            Action<LoadAssetState, Object> callback = null)
        {
            IEnumeratorTool.StartCoroutine(IE_AsyncLoadAssetbundle(assetHash, isLoadObj, callback));
        }

        /// <summary>
        /// 当前正在加载的所有AB
        /// </summary>
        HashSet<string> lockSet = new HashSet<string>();

        /// <summary>
        ///  加载
        /// 一般来说,主资源才需要load
        /// 依赖资源只要加载ab,会自动依赖
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isLoadObj">是否需要返回加载资源</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IEnumerator IE_AsyncLoadAssetbundle(string name, bool isLoadObj, Action<LoadAssetState, Object> callback)
        {
            //
            // var mainItem = loder.Manifest.GetManifest(name);
            // //单ab 多资源,加载真正ab名
            // if (mainItem != null && !string.IsNullOrEmpty(mainItem.AB))
            // {
            //     name = mainItem.AB;
            // }

            //正在被加载中,放入后置队列
            if (lockSet.Contains(name))
            {
                callback(LoadAssetState.IsLoding, null);
                yield break;
            }

            //没被加载
            if (!AssetbundleMap.ContainsKey(name))
            {
                AssetBundleCreateRequest ret = null;
                string fullpath = "";
                //加锁
                lockSet.Add(name);
                {
                    fullpath = FindAsset(name);
                    ret = AssetBundle.LoadFromFileAsync(fullpath);
                    yield return ret;
                }
                //解锁
                lockSet.Remove(name);
                //添加assetbundle
                if (ret.assetBundle != null)
                {
                    AddAssetBundle(name, ret.assetBundle);
                    if (isLoadObj)
                    {
                        callback(LoadAssetState.Success, LoadFormAssetBundle<Object>(name));
                    }
                    else
                    {
                        callback(LoadAssetState.Success, null);
                    }
                }
                else
                {
                    callback(LoadAssetState.Fail, null);
                    BDebug.LogError("ab资源为空:" + fullpath);
                }
            }
            else
            {
                if (isLoadObj)
                {
                    callback(LoadAssetState.Success, LoadFormAssetBundle<Object>(name));
                }
                else
                {
                    callback(LoadAssetState.Success, null);
                }
            }
        }


        /// <summary>
        /// ab包计数器
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="ab"></param>
        private void AddAssetBundle(string hash, AssetBundle ab)
        {
            //
            if (!AssetbundleMap.ContainsKey(hash))
            {
                AssetBundleWapper abr = new AssetBundleWapper() {AssetBundle = ab};
                AssetbundleMap[hash] = abr;
            }

            AssetbundleMap[hash].Use();
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
        private T LoadFormAssetBundle<T>(string assetName) where T : UnityEngine.Object
        {
            ManifestItem item = this.loder.Manifest.GetManifest(assetName);

            if (item != null)
            {
                return LoadFormAssetBundle(assetName, item, typeof(T)) as T;
            }

            Debug.LogError("不存在:" + assetName);
            return null;
        }


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private Object LoadFormAssetBundle(string assetName, ManifestItem item, Type t)
        {
            Object o = null;
            AssetBundleWapper abr = null;
            if (AssetbundleMap.TryGetValue(item.Path, out abr))
            {
                switch ((ManifestItem.AssetTypeEnum) item.Type)
                {
                    //暂时需要特殊处理的只有一个
                    case ManifestItem.AssetTypeEnum.SpriteAtlas:
                    {
                        o = abr.LoadTextureFormAtlas(assetName);
                    }
                        break;
                    case ManifestItem.AssetTypeEnum.Prefab:
                    case ManifestItem.AssetTypeEnum.Texture:
                    case ManifestItem.AssetTypeEnum.Others:
                    default:
                    {
                        o = abr.LoadAsset(assetName, t);
                    }
                        break;
                }
            }
            else
            {
                BDebug.Log("资源不存在:" + assetName + " - " + item.Path, "red");

                return null;
            }

            return o;
        }

        #endregion

        #region 取消加载任务

        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            foreach (var tg in allTaskGroupList)
            {
                if (tg.Id == taskid)
                {
                    tg.Stop();
                    allTaskGroupList.Remove(tg);
                    break;
                }
            }
        }


        /// <summary>
        /// 取消所有load任务
        /// </summary>
        public void LoadCancelAll()
        {
            foreach (var tg in allTaskGroupList)
            {
                tg.Stop();
            }

            this.allTaskGroupList.Clear();
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
            var str = string.Format(RUNTIME,  (floder + "/").ToLower());
            
            searchPattern = searchPattern?.ToLower();
            foreach (var key in this.loder.Manifest.ManifestMap.Keys)
            {
                if (key.StartsWith(str))
                {
                    rets.Add(key);
                }
            }

            //寻找符合条件的
            if (!string.IsNullOrEmpty(searchPattern))
            {
                rets = rets.FindAll((r) =>
                {
                    var fileName = Path.GetFileName(r);

                    if (fileName.StartsWith(searchPattern))
                    {
                        return true;
                    }

                    return false;
                });
            }

            var count = "runtime/".Length;
            for (int i = 0; i < rets.Count; i++)
            {
                rets[i] = rets[i].Substring(count);
            }
            return rets.ToArray();
        }

        #endregion

        #region 工具类

        #endregion

        #region 核心任务驱动

        /// <summary>
        /// 当前执行的任务组
        /// </summary>
        private LoaderTaskGroup curDoTask = null;

        /// <summary>
        /// 核心功能,所有任务靠这个推进度
        /// 执行下个任务
        /// </summary>
        void DoNextTask()
        {
            if (this.allTaskGroupList.Count == 0)
            {
                return;
            }

            //当前任务组执行完毕，执行下一个
            if ((curDoTask == null || curDoTask.IsComplete) && this.allTaskGroupList.Count > 0)
            {
                curDoTask = this.allTaskGroupList[0];
                this.allTaskGroupList.RemoveAt(0);
                var item = loder.Manifest.GetManifest(curDoTask.MainAsset);
                BDebug.LogFormat(">>>>任务组|id:{0} 任务数:{1} - {2}", curDoTask.Id, curDoTask.TaskQueueNum, item.Path);
                //开始task
                curDoTask.DoNextTask();
                //注册完成回调
                curDoTask.onTaskCompleteCallback += (a, b) =>
                {
                    //
                    DoNextTask();
                };
            }
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="path"></param>
        public void UnloadAsset(string path, bool isForceUnload = false)
        {
            if (path != null)
            {
                path = string.Format(RUNTIME, path.ToLower());
                var item = loder.Manifest.GetManifest(path);
                if (item == null) return;
                //将所有依赖,创建一个队列 倒序加载
                Queue<string> resQue = new Queue<string>();
                foreach (var r in item.Depend)
                {
                    if (AssetbundleMap.ContainsKey(r))
                    {
                        resQue.Enqueue(r);
                    }
                }
                resQue.Enqueue(item.Path);
                //判断是否有已经加载过的资源
                foreach (var r in resQue)
                {
                    if (AssetbundleMap.ContainsKey(r))
                    {
                        if (isForceUnload)
                        {
                            AssetbundleMap[r].AssetBundle.Unload(true);
                            AssetbundleMap.Remove(r);
                        }
                        else
                        {
                            AssetbundleMap[r].Unuse();
                        }
                    }
                }

                //移除无用的assetbundle
                var keys = new List<string>(AssetbundleMap.Keys);
                foreach (var k in keys)
                {
                    if (AssetbundleMap[k].Counter <= 0)
                    {
                        AssetbundleMap.Remove(k);
                    }
                }
            }
            else
            {
                BDebug.Log("路径不存在");
            }
        }


        /// <summary>
        /// 卸载所有资源
        /// </summary>
        public void UnloadAllAsset()
        {
            AssetBundle.UnloadAllAssetBundles(true);
            // foreach (var v in AssetbundleMap)
            // {
            //     UnloadAsset(v.Key);
            // }


            //AssetbundleMap.Clear();
            Resources.UnloadUnusedAssets();
        }

        #endregion
    }
}