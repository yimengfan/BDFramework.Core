using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using Object = UnityEngine.Object;
using Utils = BDFramework.Helper.Utils;

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
        private string path = "";

        public AssetBundleMgr()
        {
            this.assetbundleMap = new Dictionary<string, AssetBundleReference>();
            this.willDoTaskSet = new HashSet<int>();
            this.allTaskList = new List<LoadTaskGroup>();
            //1.设置加载路径  
            //persistent 和 streaming同时只能存在一个，
            path =(Application.persistentDataPath+"/"+Utils.GetPlatformPath(Application.platform)+"/Art").Replace("\\", "/");
            var configPath = this.path+ "/Config.json";
            this.path = File.Exists(configPath) ? path : Application.streamingAssetsPath+"/"+ Utils.GetPlatformPath(Application.platform)+"/Art".Replace("\\", "/");
            this.manifest = new AssetBundleManifestReference(configPath);
            
            BDebug.Log("Art加载路径:" + path,"red");
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
            path = "assets/resource/runtime/" + path.ToLower()+".";
            //寻找ab的后缀名
            var assetPath = GetExistPath(path);

            if (assetPath != null)
            {
                var res = manifest.Manifest.GetDirectDependencies(assetPath);
                //1.创建依赖加载队列
                Stack<string> loadQueue = new Stack<string>();
                foreach (var asset in res)
                {
                    //依赖队列需要加上resourcepath
                    var fullPath = this.path+"/" +asset;
                    //判断是否已经加载过
                    if (assetbundleMap.ContainsKey(asset) == false)
                    {
                        loadQueue.Push(fullPath);
                    }
                    else
                    {
                        assetbundleMap[asset].Use();
                    }
                }

                //2.加载主体
//                if (assetbundleMap.ContainsKey(assetPath) == false)
//                {
//                    var fullPath = Path.Combine(this.path, assetPath);
//                    loadQueue.Enqueue(fullPath);
//                }

                //开始加载队列
                IEnumeratorTool.StartCoroutine(IEAsyncLoadAssetbundle(loadQueue, callback));
            }
            else
            {
                BDebug.LogError("没有该资源:" + path);
                //没有该资源
                callback(false);
            }
        }

        /// <summary>
        /// 递归加载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        /// <returns></returns>
        IEnumerator IEAsyncLoadAssetbundle(Stack<string> loadQueue, Action<bool> callback)
        {
            if (loadQueue.Count <= 0)
            {
                callback(true);
                yield break;
            }
            else
            {
                var path = loadQueue.Pop();
                var result = AssetBundle.LoadFromFileAsync(path);
                yield return result;
                if (result.isDone)
                {
                    //添加assetbundle
                    if (result.assetBundle != null)
                    {
                        AddAssetBundle(result.assetBundle.name, result.assetBundle);
                    }

                    //开始下个任务
                    IEnumeratorTool.StartCoroutine(IEAsyncLoadAssetbundle(loadQueue, callback));
                    yield break;
                }
                else
                {
                    BDebug.LogError("加载失败:" + path);
                }
            }
        }


        /// <summary>
        /// ab包计数器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ab"></param>
        private void AddAssetBundle(string name, AssetBundle ab)
        {
            if (assetbundleMap.ContainsKey(name) == false)
            {
                AssetBundleReference abr = new AssetBundleReference() {assetBundle = ab};
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
        private T LoadFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
        {
            //
            T o = default(T);
            AssetBundleReference abr = null;
            if (assetbundleMap.TryGetValue(abName, out abr))
            {
                o = abr.assetBundle.LoadAsset<T>(objName);
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
            path = "assets/resource/runtime/" + path.ToLower();
            //寻找ab的后缀名
            var assetPath = GetExistPath(path);

            var res = manifest.Manifest.GetDirectDependencies(assetPath);
            //1.创建依赖加载队列
            List<string> loadList = new List<string>();
            foreach (var r in res)
            {
                //依赖队列需要加上resourcepath
                var dir = this.path+"/"+ r;
                //判断是否已经加载过
                if (assetbundleMap.ContainsKey(r) == false)
                {
                    loadList.Add(dir);
                }
                else
                {
                    assetbundleMap[r].Use();
                }
            }

            //加载列表
            foreach (var l in loadList)
            {
                var ab = AssetBundle.LoadFromFile(l);
                AddAssetBundle(ab.name, ab);
            }

            //2.加载主体
            if (assetbundleMap.ContainsKey(assetPath) == false)
            {
                var fullname =this.path+"/"+ assetPath;
                var ab = AssetBundle.LoadFromFile(fullname);
                AddAssetBundle(ab.name, ab);
            }

            //3.加载具体资源
            return LoadFormAssetBundle<T>(assetPath, assetPath);
        }


        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public int AsyncLoad<T>(string path, Action<bool, T> callback) where T : UnityEngine.Object
        {
            var task = new LoadTask()
            {
                Id = this.taskIDCounter++,
                ResourcePath = path,
            };
            //创建任务组
            LoadTaskGroup taskGroup = new LoadTaskGroup(new List<LoadTask>() {task}, null,
                (map) =>
                {
                    if (map.Keys.Count > 0)
                    {
                        //只取第一个返回
                        foreach (var v in map.Values)
                        {
                            callback(true, v as T);
                            return;
                        }
                    }
                    else
                    {
                        callback(false, null);
                    }
                });
            AddTaskGroup(taskGroup);
            //开始任务
            DoNextTask();
            return task.Id;
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
            List<LoadTask> tasks = new List<LoadTask>();
            foreach (var s in sources)
            {
                var task = new LoadTask()
                {
                    Id = this.taskIDCounter++,
                    ResourcePath = s,
                };

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
            if(isDoing) return;
            
            //没有东西的时候 跳出
            if (currentTaskGroup == null && this.allTaskList.Count == 0) 
            {
                BDebug.Log("---------无任务组,退出----------");
                return;
            }
            else if (currentTaskGroup == null && this.allTaskList.Count > 0)
            {
                BDebug.Log("---------开始执行任务组----------");
                currentTaskGroup = this.allTaskList[0];
            }
            
            LoadTask task = null;
            //获取一个任务
            for (;;)
            {
                task = currentTaskGroup.GetTask();
                //task为空，或者任务可以执行，跳出
                if (task == null || willDoTaskSet.Contains(task.Id))
                {
                    break;
                }
            }


            //当前任务组已经全部完成
            if (task == null)
            {
                BDebug.Log("---------一组加载任务组已完成----------");
                currentTaskGroup = null;
                this.allTaskList.RemoveAt(0);

                DoNextTask();        
            }
            else
            {
                //
                BDebug.Log("执行任务组中task:" + task.Id + " - " + task.ResourcePath);
                isDoing = true;
                //执行任务
                AsyncLoadAssetBundle(task.ResourcePath, b =>
                {
                    //移除任务
                    this.willDoTaskSet.Remove(task.Id);
                    //
                    var path = "assets/resource/runtime/" + task.ResourcePath.ToLower()+".";
                    path = GetExistPath(path);
                    var obj = LoadFormAssetBundle<Object>(path, path);
                    //任务完成
                    currentTaskGroup.OnOneTaskComplete(task.Id, task.ResourcePath, obj);
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
            name = "assets/resource/runtime/" + name.ToLower();
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