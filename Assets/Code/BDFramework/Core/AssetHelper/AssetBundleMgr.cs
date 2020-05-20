using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using BDFramework.Helper;
using UnityEditor;
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
        public AssetBundle assetBundle;

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
            var fs    = assetBundle.GetAllAssetNames();
            var atlas = this.assetBundle.LoadAsset<SpriteAtlas>(fs[fs.Length - 1]);
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
                var fs = this.assetBundle.GetAllAssetNames().ToList();
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
            return this.assetBundle.LoadAsset(realname, type);
        }

        #endregion

        #region 引用计数

        public int counter { get; private set; }

        public void Use()
        {
            counter++;
        }

        public void Unuse()
        {
            counter--;
            if (counter <= 0)
            {
                if (assetBundle)
                {
                    assetBundle.Unload(true);
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

        public AssetBundleMgr(string root, Action onLoded)
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
            artRootPath = (root + "/" + BDUtils.GetPlatformPath(Application.platform) + "/Art").Replace("\\", "/");
            secArtRootPath =
                (Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(Application.platform) + "/Art")
                .Replace("\\", "/");
            //
            string configPath = FindAsset("Config.json");
            BDebug.Log("Art加载路径:" + configPath, "red");
            //
            this.config          = new AssetBundleManifestReference(configPath);
            this.config.OnLoaded = onLoded;
        }

        #region 异步加载单个ab

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
        HashSet<string> lockset = new HashSet<string>();

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
            if (lockset.Contains(assetHash))
            {
                callback(LoadAssetState.IsLoding, null);
                yield break;
            }

            //没被加载
            if (!AssetbundleMap.ContainsKey(assetHash))
            {
                //加锁
                lockset.Add(assetHash);
                var fullpath = FindAsset(assetHash);
                var ret      = AssetBundle.LoadFromFileAsync(fullpath);
                yield return ret;
                //解锁
                lockset.Remove(assetHash);
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
                AssetBundleWapper abr = new AssetBundleWapper()
                {
                    assetBundle = ab
                };
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
                var item     = config.Manifest.GetManifestItemByHash(res);
                var realPath = res;
                //如果有package
                if (item != null && !string.IsNullOrEmpty(item.Package))
                {
                    realPath = item.Package;
                }

                if (!AssetbundleMap.ContainsKey(realPath))
                {
                    var p  = FindAsset(realPath);
                    var ab = AssetBundle.LoadFromFile(p);

                    //添加
                    AddAssetBundle(realPath, ab);
                }
                else
                {
                    AssetBundleWapper abw = null;
                    if (AssetbundleMap.TryGetValue(realPath, out abw))
                    {
                        abw.Use();
                    }
                    else
                    {
                        return null;
                    }
                }
            }


            return LoadFormAssetBundle<T>(dependAssets.Last());
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
            if (dependAssets == null) return -1;
            //
            foreach (var r in dependAssets)
            {
                var task = new LoaderTaskData(r, typeof(Object));
                taskQueue.Add(task);
            }

            var mainAsset = dependAssets[dependAssets.Length - 1];

            //添加任务组
            LoaderTaskGroup taskGroup = new LoaderTaskGroup(mainAsset, 10, taskQueue, AsyncLoadAssetBundle,
                                                            (p, obj) => { callback(obj as T); });
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
                LoaderTaskGroup taskGroup = new LoaderTaskGroup(mainAsset, 10, taskQueue, AsyncLoadAssetBundle,
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
        public void LoadCalcelAll()
        {
            foreach (var tg in allTaskGroupList)
            {
                tg.Stop();
            }

            this.allTaskGroupList.Clear();
        }

        public string[] GetAssets(string floder, string searchPattern = null)
        {
            throw new NotImplementedException();
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
                curDoTask.SetDebugManifest(this.config.Manifest);
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
        /// <param name="name"></param>
        public void UnloadAsset(string name, bool isForceUnload = false)
        {
            if (name != null)
            {
                var res = config.Manifest.GetDirectDependenciesByName(name);
                if (res == null) return;
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
                            AssetbundleMap[r].assetBundle.Unload(true);
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
                    if (AssetbundleMap[k].counter <= 0)
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
            foreach (var v in AssetbundleMap)
            {
                UnloadAsset(v.Key);
            }

            AssetbundleMap.Clear();
            Resources.UnloadUnusedAssets();
        }

        #endregion
    }
}