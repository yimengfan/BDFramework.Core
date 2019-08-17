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
        public int referenceCount { get; private set; }

        public void Use()
        {
            referenceCount++;
        }

        public void Unuse()
        {
            referenceCount--;
            if (referenceCount <= 0)
            {
                assetBundle.Unload(true);
            }
        }
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
        /// 将要执行的task
        /// </summary>
        private HashSet<int> willDoTaskSet;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private List<LoadTaskGroup> allTaskList;

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
            this.willDoTaskSet = new HashSet<int>();
            this.allTaskList = new List<LoadTaskGroup>();
            //1.设置加载路径  
            artRootPath    = (root + "/" + BDUtils.GetPlatformPath(Application.platform) + "/Art").Replace("\\", "/");
            secArtRootPath = (Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(Application.platform) + "/Art").Replace("\\", "/");
            //
            string configPath = FindAsset("Config.json");
            BDebug.Log("Art加载路径:" + configPath, "red");
            //
            this.manifest = new AssetBundleManifestReference(configPath);
            this.manifest.OnLoaded = onLoded;

        }

        #region 异步加载单个ab

        /// <summary>
        /// 加载ab
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        private void AsyncLoadAssetBundle(string path, Action<bool> callback)
        {
            //ab存储的是asset下的相对目录
            path = "assets/resource/runtime/" + path.ToLower() + ".";
            //寻找ab的后缀名
            var mainAssetPath = GetExistPath(path);

            if (mainAssetPath != null)
            {
                //1.依赖资源
                var res = manifest.Manifest.GetDirectDependencies(mainAssetPath).ToList();
                //2.主体资源
                res.Add(mainAssetPath);
                //开始加载队列
                IEnumeratorTool.StartCoroutine(IEAsyncLoadAssetbundle(res, callback));
            }
            else
            {
                BDebug.LogError("没有该资源:" + path);
                //没有该资源
                callback(false);
            }
        }


        
        /// <summary>
        /// 当前正在加载的所有AB
        /// </summary>
        HashSet<string> currentLoadingABNames =new HashSet<string>();
        /// <summary>
        /// 递归加载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        /// <returns></returns>
        IEnumerator IEAsyncLoadAssetbundle(List<string> resList, Action<bool> callback)
        {
            if (resList.Count == 0)
            {
                callback(true);
            }
            else
            {
                //弹出一个任务
                var res = resList[0];
                resList.RemoveAt(0);
                if (res.EndsWith(".shader"))
                {
                    int i = 0;
                }
                var mainItem = manifest.Manifest.GetManifestItem(res);
                //单ab 多资源,加载真正ab名
                if (mainItem!=null && !string.IsNullOrEmpty(mainItem.PackageName))
                {
                    res = mainItem.PackageName;
                }

                if (!currentLoadingABNames.Contains(res) && !assetbundleMap.ContainsKey(res))
                {
                    //加入列表防止同时加载一个
                    currentLoadingABNames.Add(res);
                    //
                    var resPath = FindAsset(res);
                    //开始加载
                    var result = AssetBundle.LoadFromFileAsync(resPath);
                    yield return result;
                    //加载结束
                    if (result.isDone)
                    {
                        //移除当前列表
                        currentLoadingABNames.Remove(res);
                        //添加assetbundle
                        if (result.assetBundle != null)
                        {
                            AddAssetBundle(res, result.assetBundle);
                        }
                        else
                        {
                            BDebug.LogError("ab资源为空:" + resPath);
                        }
                    }
                }

                //开始下个任务
                IEnumeratorTool.StartCoroutine(IEAsyncLoadAssetbundle(resList, callback));
                yield break;
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
        private T LoadFormAssetBundle<T>( string objName) where T : UnityEngine.Object
        {
            //判断资源结构 是单ab-单资源、单ab-多资源
            var mainItem = manifest.Manifest.GetManifestItem(objName);
            //单ab 单资源
            var abName = objName;
            //单ab 多资源
            if (mainItem!=null&&!string.IsNullOrEmpty(mainItem.PackageName))
            {
                abName = mainItem.PackageName;
            }
            
            //
            if (!string.IsNullOrEmpty(abName))
            {
                T o = default(T);
                AssetBundleReference abr = null;
                if (assetbundleMap.TryGetValue(abName, out abr))
                {
                    o = abr.assetBundle.LoadAsset<T>(objName);
                }
                else
                {
                    BDebug.Log("资源不存在:" +objName);
                }
                return o;
            }

            return null;
        }
        

        
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private Object LoadFormAssetBundle( string objName,Type t)
        {
            //判断资源结构 是单ab-单资源、单ab-多资源
            var mainItem = manifest.Manifest.GetManifestItem(objName);
            //单ab 单资源
            var abName = objName;
            //单ab 多资源
            if (mainItem!=null&&!string.IsNullOrEmpty(mainItem.PackageName))
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
                        o = atlas.GetSprite(objName);
                    }
                    else
                    {
                        o = abr.assetBundle.LoadAsset(objName,t);
                    }
                }
                else
                {
                    BDebug.Log("资源不存在:" +objName);

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
            
            path = "assets/resource/runtime/" + path.ToLower() + ".";
            //寻找ab的后缀名
            var mainAssetPath = GetExistPath(path);

            if (mainAssetPath == null)
            {
                BDebug.Log("资源不存在:" + path, "red");
                return null;
            }

            //1.依赖路径
            var resList = manifest.Manifest.GetDirectDependencies(mainAssetPath).ToList();
            //2.主体路径
            resList.Add(mainAssetPath);
            
            BDebug.Log("【加载】:" + mainAssetPath);
            //同步加载
            foreach (var res in resList)
            {
                 //1.判断是否有多个ab在1个Package中
                 var item = manifest.Manifest.GetManifestItem(res);
                 var _res = res;
                 //如果有package
                 if (item!=null&&!string.IsNullOrEmpty(item.PackageName))
                 {
                     _res = item.PackageName;
                 }
                 
                 if (!assetbundleMap.ContainsKey(_res))
                 {
                     var r = FindAsset(_res);
                     var ab = AssetBundle.LoadFromFile(r);
                     AddAssetBundle(_res, ab);
                     
                     BDebug.Log("----->" + res);
                 }
                 else
                 {
                     assetbundleMap[_res].Use();
                     BDebug.Log("无需加载:" + _res);
                 }
            }
          
            
                return LoadFormAssetBundle<T>(mainAssetPath);

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
        /// 检测资源
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private AssetBundle CheckAsset(string res)
        {
            AssetBundleReference  abr = null;

            //获取AssetBundle
            this.assetbundleMap.TryGetValue(res, out abr);
            if (abr.assetBundle != null)
            {
                return abr.assetBundle;
            }

            return null;

        }
        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public int AsyncLoad<T>(string path, Action<bool, T> callback) where T : UnityEngine.Object
        {
            var id = this.taskIDCounter++;
            this.willDoTaskSet.Add(id);
            AsyncLoadAssetBundle(path, b =>
            {
                //异步任务取消
                if (this.willDoTaskSet.Contains(id))
                {
                    this.willDoTaskSet.Remove(id);
                }
                else
                {
                    return;
                }
                //
                var p = "assets/resource/runtime/" + path.ToLower() + ".";
                path = GetExistPath(p);
                var obj = LoadFormAssetBundle<T>(path);
                if (obj)
                {
                    callback(true, obj);
                }
                else
                {
                    callback(false, null);
                }
            });
            return id;
        }


        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="sources">资源</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <param name="onLoadProcess">进度</param>
        /// <returns>taskid</returns>
        public List<int> AsyncLoad(IList<string> sources,
            Action<IDictionary<string, UnityEngine.Object>> onLoadComplete, Action<int, int> onLoadProcess)
        {
            List<int> idList = new List<int>();
            List<LoadTaskData> tasks = new List<LoadTaskData>();
            foreach (var s in sources)
            {
                var task = new LoadTaskData(this.taskIDCounter++, s, typeof(Object));
                tasks.Add(task);
                idList.Add(task.Id);
            }

            LoadTaskGroup taskGroup = new LoadTaskGroup(tasks, onLoadProcess, onLoadComplete);
            //添加任务组
            AddTaskGroup(taskGroup);
            //开始任务
            DoNextTask();
            //
            return idList;
        }


        /// <summary>
        /// 添加一个任务组
        /// </summary>
        /// <param name="taskGroup"></param>
        public void AddTaskGroup(LoadTaskGroup taskGroup)
        {
            foreach (var t in taskGroup.TaskIdList)
            {
                willDoTaskSet.Add(t.Id);
            }

            this.allTaskList.Add(taskGroup);
        }

        #region 取消加载任务

        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            if (willDoTaskSet.Contains(taskid))
            {
                willDoTaskSet.Remove(taskid);
                BDebug.Log("BResource 移除task:" + taskid);
            }
        }


        /// <summary>
        /// 取消所有load任务
        /// </summary>
        public void LoadCalcelAll()
        {
            willDoTaskSet.Clear();
        }

        #endregion


        /// <summary>
        /// 再所有hashset中查询文件名符合的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private string GetExistPath(string objName)
        {
            //变换成ab名
            foreach (var ab in this.manifest.AssetBundlesSet)
            {
                if (ab.Contains(objName))
                {
                    return ab;
                }
            }

            return null;
        }


        #region 核心任务驱动

        /// <summary>
        /// 当前执行的任务组
        /// </summary>
        private LoadTaskGroup currentTaskGroup = null;

        /// <summary>
        /// 状态锁
        /// </summary>
        private bool isDoing = false;

        /// <summary>
        /// 核心功能,所有任务靠这个推进度
        /// 执行下个任务
        /// </summary>
        void DoNextTask()
        {
            if (isDoing) return;

            //没有东西的时候 跳出
            if (currentTaskGroup == null && this.allTaskList.Count == 0)
            {
                BDebug.Log("---------无任务组,退出----------");
                return;
            }
            else if (currentTaskGroup == null && this.allTaskList.Count > 0)
            {
              
                currentTaskGroup = this.allTaskList[0];
                BDebug.Log("---------开始执行任务组----------  |"+currentTaskGroup.GetHashCode());
                foreach (var t in currentTaskGroup.TaskIdList)
                {
                    BDebug.Log("--->" + t.ResourcePath);
                }
            }

            LoadTaskData taskData = null;
            //获取一个任务
            while (true)
            {
                taskData = currentTaskGroup.GetTask();

                //任务不存在当前可执行列表中
                if(taskData!=null &&  !willDoTaskSet.Contains(taskData.Id))
                {
                    currentTaskGroup.RemoveTask(taskData.Id);
                    continue;
                }
                //task为空，或者任务可以执行，跳出
                if (taskData == null || willDoTaskSet.Contains(taskData.Id))
                {
                    break;
                }
            }


            //当前任务组已经全部完成
            if (taskData == null)
            {
                BDebug.Log("---------加载任务组已完成---------- |"+currentTaskGroup.GetHashCode());
                currentTaskGroup = null;
                this.allTaskList.RemoveAt(0);

                DoNextTask();
            }
            else
            {
                //
                BDebug.Log("执行任务组中task:" + taskData.Id + " - " + taskData.ResourcePath);
                isDoing = true;
                //执行任务
                AsyncLoadAssetBundle(taskData.ResourcePath, b =>
                {
                    //移除任务
                    this.willDoTaskSet.Remove(taskData.Id);
                    //
                    var path = "assets/resource/runtime/" + taskData.ResourcePath.ToLower() + ".";
                    path = GetExistPath(path);
                    var obj = LoadFormAssetBundle(path,taskData.LoadType);
                    //任务完成
                    currentTaskGroup.OnOneTaskComplete(taskData.Id, taskData.ResourcePath, obj);
                    isDoing = false;
                    //继续执行
                    DoNextTask();
                });
            }
        }

        #endregion


        #region 卸载资源

        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="name"></param>
        public void UnloadAsset(string name, bool isUnloadIsUsing = false)
        {
            name = "assets/resource/runtime/" + name.ToLower() + ".";
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

                //最后加载资源本体
                resQue.Enqueue(path);
                //判断是否有已经加载过的资源
                foreach (var r in resQue)
                {
                    if (assetbundleMap.ContainsKey(r))
                    {
                        if (isUnloadIsUsing)
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
                    if (assetbundleMap[k].referenceCount <= 0)
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