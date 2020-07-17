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

namespace BDFramework.ResourceMgr
{
    /// <summary>
    ///ab包引用计数类
    /// </summary>
    public class AssetBundleWapper
    {
        public AssetBundle AssetBundle;

        #region 各种加载接口

        Dictionary<string, string> assetNameMap = new Dictionary<string, string>();

        /// <summary>
        /// 加载图集资源
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        public Object LoadTextureFormAtlas(string texName)
        {
            //默认一个ab中只有一个atlas
            var fs    = AssetBundle.GetAllAssetNames();
            var atlas = this.AssetBundle.LoadAsset<SpriteAtlas>(fs[fs.Length - 1]);
            texName = Path.GetFileName(texName);
            var sp = atlas.GetSprite(texName);
            return sp;
        }

        /// <summary>
        /// 加载普通资源
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object LoadAsset(string name, Type type)
        {
            string realname = "";
            if (!assetNameMap.TryGetValue(name, out realname))
            {
                var fs = this.AssetBundle.GetAllAssetNames().ToList();
                if (fs.Count == 1)
                {
                    realname = fs[0];
                }
                else
                {
                    var _name = name.ToLower() + ".";
                    realname = fs.Find((p) => p.Contains(_name));
                }

                assetNameMap[name] = realname;
            }

            if (realname == null)
                return null;
            return this.AssetBundle.LoadAsset(realname, type);
        }

        #endregion

        #region 引用计数

        public int Counter { get; private set; }

        public void Use()
        {
            Counter++;
        }

        public void Unuse()
        {
            Counter--;
            if (Counter <= 0)
            {
                if (AssetBundle)
                {
                    AssetBundle.Unload(true);
                }
            }
        }

        #endregion
    }

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
    public class AssetBundleMgr : IResMgr
    {
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
        private AssetBundleManifestReference config;
        
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
            if (this.config != null)
            {
                this.UnloadAllAsset();
                GC.Collect();
            }

            this.AssetbundleMap   = new Dictionary<string, AssetBundleWapper>();
            this.allTaskGroupList = new List<LoaderTaskGroup>();
            //1.设置加载路径  
            artRootPath = (path + "/" + BApplication.GetPlatformPath(Application.platform) + "/Art").Replace("\\", "/");
            secArtRootPath = (Application.streamingAssetsPath + "/" + BApplication.GetPlatformPath(Application.platform) + "/Art")
                .Replace("\\", "/");
            //
            string configPath = FindAsset("Config.json");
            BDebug.Log("AssetBundle Version: V2", "red");
            BDebug.Log("Art加载路径:" + configPath, "red");
            //
            this.config = new AssetBundleManifestReference();
            this.config.Load(configPath, onInitEnd);
        }

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
                var p  = FindAsset(path);
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
        private void AsyncLoadAssetBundle(string                         assetHash, bool isLoadObj = false,
                                          Action<LoadAssetState, Object> callback = null)
        {
            IEnumeratorTool.StartCoroutine(IEAsyncLoadAssetbundle(assetHash, isLoadObj, callback));
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
        /// <param name="assetHash"></param>
        /// <param name="isLoadObj">是否需要返回加载资源</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IEnumerator IEAsyncLoadAssetbundle(string assetHash, bool isLoadObj, Action<LoadAssetState, Object> callback)
        {
            //
            var mainItem = config.Manifest.GetManifestItemByHash(assetHash);
            //单ab 多资源,加载真正ab名
            if (mainItem != null && !string.IsNullOrEmpty(mainItem.Package))
            {
                assetHash = mainItem.Package;
            }

            //正在被加载中,放入后置队列
            if (lockSet.Contains(assetHash))
            {
                callback(LoadAssetState.IsLoding, null);
                yield break;
            }

            //没被加载
            if (!AssetbundleMap.ContainsKey(assetHash))
            {
                AssetBundleCreateRequest ret      = null;
                string                   fullpath = "";
                //加锁
                lockSet.Add(assetHash);
                {
                    fullpath = FindAsset(assetHash);
                    ret      = AssetBundle.LoadFromFileAsync(fullpath);
                    yield return ret;
                }
                //解锁
                lockSet.Remove(assetHash);
                //添加assetbundle
                if (ret.assetBundle != null)
                {
                    AddAssetBundle(assetHash, ret.assetBundle);
                    if (isLoadObj)
                    {
                        callback(LoadAssetState.Success, LoadFormAssetBundle<Object>(assetHash));
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
                    callback(LoadAssetState.Success, LoadFormAssetBundle<Object>(assetHash));
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


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private T LoadFormAssetBundle<T>(string assetHash) where T : UnityEngine.Object
        {
            ManifestItem item = this.config.Manifest.GetManifestItemByHash(assetHash);

            if (item != null)
            {
                return LoadFormAssetBundle(item, typeof(T)) as T;
            }

            Debug.LogError("不存在:" + assetHash);
            return null;
        }


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private T LoadFormAssetBundleByName<T>(string assetName) where T : UnityEngine.Object
        {
            ManifestItem item = this.config.Manifest.GetManifestItemByName(assetName);
            if (item != null)
            {
                return LoadFormAssetBundle(item, typeof(T)) as T;
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
        private Object LoadFormAssetBundle(ManifestItem item, Type t)
        {
            //判断资源结构 是单ab-单资源、单ab-多资源
            //单ab 单资源
            var sourceName = item.Name;
            //单ab 多资源
            if (!string.IsNullOrEmpty(item.Package))
            {
                item = this.config.Manifest.GetManifestItemByHash(item.Package);
            }

            Object            o   = null;
            AssetBundleWapper abr = null;
            if (AssetbundleMap.TryGetValue(item.Hash, out abr))
            {
                switch ((ManifestItem.AssetTypeEnum) item.Type)
                {
                    //暂时需要特殊处理的只有一个
                    case ManifestItem.AssetTypeEnum.SpriteAtlas:
                    {
                        o = abr.LoadTextureFormAtlas(sourceName);
                    }
                        break;
                    default:
                    {
                        o = abr.LoadAsset(sourceName, t);
                    }
                        break;
                }
            }
            else
            {
                BDebug.Log("资源不存在:" + sourceName, "red");

                return null;
            }

            return o;
        }


        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            //BDebug.Log("加载:" + path);
            //1.依赖路径
            var dependAssets = config.Manifest.GetDirectDependenciesByName(path);
            if (dependAssets == null)
            {
                return null;
            }

            //同步加载
            foreach (var res in dependAssets)
            {
                //1.判断是否有多个ab在1个Package中
                var    item     = config.Manifest.GetManifestItemByHash(res);
                string realPath = string.IsNullOrEmpty(item.Package) ? item.Hash : item.Package;
                LoadAssetBundle(realPath);
            }


            return LoadFormAssetBundle<T>(dependAssets.Last());
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
            var    item     = config.Manifest.GetManifestItemByName(path);
            string realPath = string.IsNullOrEmpty(item.Package) ? item.Hash : item.Package;

            //加载assetbundle
            AssetBundle ab = LoadAssetBundle(realPath);

            if (ab != null)
            {
                var    assetNames = ab.GetAllAssetNames();
                string relname    = "";
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


        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public int AsyncLoad<T>(string path, Action<T> callback) where T : UnityEngine.Object
        {
            List<LoaderTaskData> taskQueue = new List<LoaderTaskData>();
            //获取依赖
            var dependAssets = config.Manifest.GetDirectDependenciesByName(path);
            if (dependAssets == null)
                return -1;
            //
            foreach (var r in dependAssets)
            {
                var task = new LoaderTaskData(r, typeof(Object));
                taskQueue.Add(task);
            }

            var mainAsset = dependAssets[dependAssets.Length - 1];

            //添加任务组
            LoaderTaskGroup taskGroup = new LoaderTaskGroup(mainAsset, 10, taskQueue, AsyncLoadAssetBundle, //Loader接口
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


        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="assetsPath">资源</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <param name="onLoadProcess">进度</param>
        /// <returns>taskid</returns>
        public List<int> AsyncLoad(IList<string>    assetsPath, Action<IDictionary<string, Object>> onLoadComplete,
                                   Action<int, int> onLoadProcess)
        {
            List<int>                   idList = new List<int>();
            IDictionary<string, Object> retMap = new Dictionary<string, Object>();
            assetsPath = assetsPath.Distinct().ToList(); //去重
            int total = assetsPath.Count;
            //source
            int counter = 0;
            foreach (var asset in assetsPath)
            {
                var                  _asset       = asset;
                List<LoaderTaskData> taskQueue    = new List<LoaderTaskData>();
                var                  dependAssets = config.Manifest.GetDirectDependenciesByName(asset);
                //获取依赖
                if (dependAssets == null)
                {
                    total--;
                    continue;
                }

                foreach (var r in dependAssets)
                {
                    var task = new LoaderTaskData(r, typeof(Object));
                    taskQueue.Add(task);
                }

                var mainAsset = dependAssets[dependAssets.Length - 1];
                //添加任务组
                //加载颗粒度10个
                LoaderTaskGroup taskGroup = new LoaderTaskGroup(mainAsset, 10, taskQueue, AsyncLoadAssetBundle, //Load接口
                                                                (p, obj) =>
                                                                {
                                                                    counter++;
                                                                    //注意返回加载的id，不是具体地址的id
                                                                    retMap[_asset] = obj;
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
            var          str  = floder + "/";
            foreach (var key in this.config.Manifest.Manifest_NameKey.Keys)
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
                var item = config.Manifest.GetManifestItemByHash(curDoTask.MainAsset);
                BDebug.LogFormat(">>>>任务组|id:{0} 任务数:{1} - {2}", curDoTask.Id, curDoTask.TaskQueueNum, item.Name);
                //开始task
                curDoTask.DoNextTask();
                //注册完成回调
                curDoTask.onTaskCompleteCallback += (a, b) => { DoNextTask(); };
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
                var res = config.Manifest.GetDirectDependenciesByName(path);
                if (res == null)
                    return;
                //将所有依赖,创建一个队列 倒序加载
                Queue<string> resQue = new Queue<string>();
                foreach (var r in res)
                {
                    if (AssetbundleMap.ContainsKey(r))
                    {
                        resQue.Enqueue(r);
                    }
                }

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