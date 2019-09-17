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
    public class AssetBundleReference
    {
        public AssetBundle assetBundle;
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
                if(assetBundle)
                assetBundle.Unload(true);
            }
        }
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
        private AssetBundleManifestReference manifest;

        /// <summary>
        /// 全局的assetbundle字典
        /// </summary>
        public Dictionary<string, AssetBundleReference> assetbundleMap { get; set; }

        /// <summary>
        /// 资源加载路径
        /// </summary>
        private string artRootPath = "";

        //第二寻址路径
        private string secArtRootPath = "";

        public AssetBundleMgr(string root, Action onLoded)
        {
            //多热更切换,需要卸载
            if (this.manifest != null)
            {
                this.UnloadAllAsset();
                GC.Collect();
            }

            this.assetbundleMap = new Dictionary<string, AssetBundleReference>();
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
            this.manifest = new AssetBundleManifestReference(configPath);
            this.manifest.OnLoaded = onLoded;
        }

        #region 异步加载单个ab

        /// <summary>
        /// 单个加载ab,会自动刷新依赖
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        private void AsyncLoadAssetBundle(string path, Action<LoadAssetState, Object> callback)
        {
            IEnumeratorTool.StartCoroutine(IEAsyncLoadAssetbundle(path, callback));
        }

        /// <summary>
        /// 当前正在加载的所有AB
        /// </summary>
        HashSet<string> lockset = new HashSet<string>();

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="res"></param>
        /// <param name="callback"></param>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        /// <returns></returns>
        IEnumerator IEAsyncLoadAssetbundle(string res, Action<LoadAssetState, Object> callback)
        {
            var mainItem = manifest.Manifest.GetManifestItem(res);
            //单ab 多资源,加载真正ab名
            if (mainItem != null && !string.IsNullOrEmpty(mainItem.PackageName))
            {
                res = mainItem.PackageName;
            }
            //正在被加载中,放入后置队列
            if (lockset.Contains(res))
            {
                callback(LoadAssetState.IsLoding, null);
                yield break;
            }
            //没被加载
            if (!assetbundleMap.ContainsKey(res))
            {
                //加锁
                lockset.Add(res);
                var resPath = FindAsset(res);
                var result = AssetBundle.LoadFromFileAsync(resPath);
                yield return result;
                //解锁
                lockset.Remove(res);
                //添加assetbundle
                if (result.assetBundle != null)
                {
                    AddAssetBundle(res, result.assetBundle);
                    callback(LoadAssetState.Success, LoadFormAssetBundle<Object>(res));
                }
                else
                {
                    callback(LoadAssetState.Fail, null);
                    BDebug.LogError("ab资源为空:" + resPath);
                }
            }
            else
            {
                callback(LoadAssetState.Success, LoadFormAssetBundle<Object>(res));
            }
        }


        /// <summary>
        /// ab包计数器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ab"></param>
        private void AddAssetBundle(string name, AssetBundle ab)
        {
            //这里将路径前缀去除
            var result = name.IndexOf("assets");
            if (result != 0 && result > 0)
            {
                name = name.Substring(result);
            }

            //
            if (!assetbundleMap.ContainsKey(name))
            {
                AssetBundleReference abr = new AssetBundleReference()
                {
                    assetBundle = ab
                };
                assetbundleMap[name] = abr;
            }

            assetbundleMap[name].Use();
        }

        #endregion


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private T LoadFormAssetBundle<T>(string objName) where T : UnityEngine.Object
        {
            return LoadFormAssetBundle(objName, typeof(T)) as T;
        }


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private Object LoadFormAssetBundle(string objName, Type t)
        {
            //判断资源结构 是单ab-单资源、单ab-多资源
            var mainItem = manifest.Manifest.GetManifestItem(objName);
            //单ab 单资源
            var abName = objName;
            //单ab 多资源
            if (mainItem != null && !string.IsNullOrEmpty(mainItem.PackageName))
            {
                abName = mainItem.PackageName;
            }

            //
            if (!string.IsNullOrEmpty(abName))
            {
                Object o = null;
                AssetBundleReference abr = null;
                if (assetbundleMap.TryGetValue(abName, out abr))
                {
                    if (abName.EndsWith(".spriteatlas"))
                    {
                        var atlas = abr.assetBundle.LoadAsset<SpriteAtlas>(abName);
                        if (!objName.EndsWith(".spriteatlas"))
                        {
                            var name = Path.GetFileNameWithoutExtension(objName);
                            o = atlas.GetSprite(name);
                        }
                       
                    }
                    else
                    {
                        o = abr.assetBundle.LoadAsset(objName, t);
                    }
                }
                else
                {
                    BDebug.Log("资源不存在:" + objName, "red");

                    return null;
                }

                return o;
            }

            return null;
        }


        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                BDebug.Log("加载路径为空:");
                return null;
            }

            //寻找ab的后缀名
            path = GetExistPath(path);
            if (path == null)
            {
              
                return null;
            }

            //1.依赖路径
            var resList = manifest.Manifest.GetDirectDependencies(path);

            // BDebug.Log("【加载】:" + path);
            //同步加载
            foreach (var res in resList)
            {
                //1.判断是否有多个ab在1个Package中
                var item = manifest.Manifest.GetManifestItem(res);
                var _res = res;
                //如果有package
                if (item != null && !string.IsNullOrEmpty(item.PackageName))
                {
                    _res = item.PackageName;
                }

                if (!assetbundleMap.ContainsKey(_res))
                {
                    var r = FindAsset(_res);
                    var ab = AssetBundle.LoadFromFile(r);
                    AddAssetBundle(_res, ab);
                }
                else
                {
                    assetbundleMap[_res].Use();
                }
            }


            return LoadFormAssetBundle<T>(path);
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

            //TODO 第三地址理论上应该是服务器端

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
            Queue<LoaderTaskData> taskQueue = new Queue<LoaderTaskData>();
            //获取依赖
            path = GetExistPath(path);
            var res = manifest.Manifest.GetDirectDependencies(path);
            foreach (var r in res)
            {
                var task = new LoaderTaskData(r, typeof(Object));
                taskQueue.Enqueue(task);
            }

            //添加任务组
            LoaderTaskGroup taskGroup =
                new LoaderTaskGroup(5, taskQueue, AsyncLoadAssetBundle, (p, obj) => { callback(obj as T); });
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
        public List<int> AsyncLoad(IList<string> assetsPath, Action<IDictionary<string, Object>> onLoadComplete,
            Action<int, int> onLoadProcess)
        {
            List<int> idList = new List<int>();
            IDictionary<string, Object> retMap = new Dictionary<string, Object>();
            assetsPath = assetsPath.Distinct().ToList(); //去重
            int total = assetsPath.Count;
            //source
            int counter=0;
            foreach (var asset in assetsPath)
            {
                var _asset = asset;
                Queue<LoaderTaskData> taskQueue = new Queue<LoaderTaskData>();
                //获取依赖
                var path = GetExistPath(_asset);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError("不存在资源:" + _asset);
                    total--;
                    continue;
                }
                var res = manifest.Manifest.GetDirectDependencies(path);
                foreach (var r in res)
                {
                    var task = new LoaderTaskData(r, typeof(Object));
                    taskQueue.Enqueue(task);
                }

                //添加任务组
                //加载颗粒度10个
      
                LoaderTaskGroup taskGroup = new LoaderTaskGroup(10, taskQueue, AsyncLoadAssetBundle,
                (p, obj) =>
                {
                    counter++;
                    //注意返回加载的id，不是具体地址的id
                    retMap[_asset] = obj;
                    if (onLoadProcess != null)
                    {
                        onLoadProcess(counter ,total);
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

        #endregion


        /// <summary>
        /// 再所有hashset中查询文件名符合的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private string GetExistPath(string objName)
        {
            objName = "assets/resource/runtime/" + objName.ToLower() + ".";
            //变换成ab名
            foreach (var ab in this.manifest.AssetBundlesSet)
            {
                if (ab.Contains(objName))
                {
                    return ab;
                }
            }

            BDebug.Log("资源不存在:" + objName, "red");
            return null;
        }


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
            if ((curDoTask == null || curDoTask.IsComplete)
                && this.allTaskGroupList.Count > 0)
            {
                curDoTask = this.allTaskGroupList[0];
                this.allTaskGroupList.RemoveAt(0);
                BDebug.LogFormat(">>>>任务组|id:{1}  count:{0}  mainasset:{2}", curDoTask.TaskQueueNum, curDoTask.Id,
                    curDoTask.MainAsset);
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
            var path = GetExistPath(name);

            if (path != null)
            {
                var res = manifest.Manifest.GetDirectDependencies(path);
                //将所有依赖,创建一个队列 倒序加载
                Queue<string> resQue = new Queue<string>();
                foreach (var r in res)
                {
                    if (assetbundleMap.ContainsKey(r))
                    {
                        resQue.Enqueue(r);
                    }
                }
                
                //判断是否有已经加载过的资源
                foreach (var r in resQue)
                {
                    if (assetbundleMap.ContainsKey(r))
                    {
                        if (isForceUnload)
                        {
                            assetbundleMap[r].assetBundle.Unload(true);
                            assetbundleMap.Remove(r);
                        }
                        else
                        {
                            assetbundleMap[r].Unuse();
                        }
                    }
                }

                //移除无用的assetbundle
                var keys = new List<string>(assetbundleMap.Keys);
                foreach (var k in keys)
                {
                    if (assetbundleMap[k].counter <= 0)
                    {
                        assetbundleMap.Remove(k);
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
            foreach (var v in assetbundleMap)
            {
                UnloadAsset(v.Key);
            }

            assetbundleMap.Clear();
        }

        #endregion
    }
}