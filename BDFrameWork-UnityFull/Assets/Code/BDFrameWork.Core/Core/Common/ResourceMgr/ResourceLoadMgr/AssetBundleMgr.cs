using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// manifest 
    /// </summary>
    public class AssetBundleManifestReference
    {
        AssetBundleManifest asset;

        public AssetBundleManifest Manifest
        {
            get { return asset; }

            set
            {
                asset = value;
                allAssetBundlesMap = new Dictionary<string, int>();
                var list = asset.GetAllAssetBundles();
                foreach (var l in list)
                {
                    allAssetBundlesMap[l] = 0;
                }
            }
        }

        public Dictionary<string, int> allAssetBundlesMap;
    }

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
    /// 异步任务
    /// </summary>
    public class AsyncTask
    {
        public enum state
        {
            Waiting,
            Loading,
            End,
        }

        public AsyncTask()
        {
            CurState = state.Waiting;
        }

        //任务id
        public int id;

        //当前状态
        public state CurState;

        //任务
        Action dotask;

        //注册task
        public void RegTask(Action task)
        {
            dotask = task;
        }

        //dotask
        public void DoTask()
        {
            if (dotask != null)
            {
                CurState = state.Loading;
                dotask();
            }
        }

        //任务结束
        public void EndTask()
        {
            CurState = state.End;
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
        private int id;

        /// <summary>
        /// 任务表
        /// </summary>
        private HashSet<int> taskHashSet;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private List<AsyncTask> asyncTaskList;

        /// <summary>
        /// 全局唯一的依赖
        /// </summary>
        private AssetBundleManifestReference manifest;

        public AssetBundleMgr()
        {
            this.manifest = new AssetBundleManifestReference();
            this.AssetbundleMap = new Dictionary<string, AssetBundleReference>();
            this.taskHashSet = new HashSet<int>();
            this.asyncTaskList = new List<AsyncTask>();
        }

        public Dictionary<string, AssetBundleReference> AssetbundleMap { get; set; }

        public string LocalHotUpdateResPath { get; set; }

        /// <summary>
        /// 加入ab包
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ab"></param>
        private void UseAssetBunle(string name, AssetBundle ab)
        {
            name = Path.GetFileName(name);
            if (AssetbundleMap.ContainsKey(name) == false)
            {
                AssetBundleReference abr = new AssetBundleReference() {assetBundle = ab};
                AssetbundleMap[name] = abr;
            }

            AssetbundleMap[name].Use();
        }

        #region 异步加载Manifest

        public void AsyncLoadManifest(string path, Action<bool> callback)
        {
            //如果存在 不让加载
            //if (manifest != null)
            //{
            //    callback(true);
            //    return;
            //}
            path = Path.Combine(LocalHotUpdateResPath, path);
#if UNITY_EDITOR || UNITY_IPHONE
            path = "File:///" + path;
#endif
            path = path.Replace("\\", "/");
            IEnumeratorTool.StartCoroutine(IELoadAssetBundles(path, callback, true));
        }

        //委托协程
        IEnumerator IELoadAssetBundles(string path, Action<bool> sucessCallback, bool isManiFest = false)
        {
            BDebug.Log("加载依赖");
            WWW www = new WWW(path);
            yield return www;
            if (www.error == null)
            {
                if (www.isDone)
                {
                    if (isManiFest)
                    {
                        manifest.Manifest = www.assetBundle.LoadAsset("AssetBundleManifest") as AssetBundleManifest;

                        if (manifest != null)
                        {
                            sucessCallback(true);
                        }
                        else
                        {
                            sucessCallback(false);
                            Debug.LogError("加载依赖失败!");
                        }
                    }

                    yield break;
                }
                else
                {
                    Debug.Log("loading ...");
                }
            }
            else
            {
                Debug.LogError("错误：" + www.error);
                sucessCallback(false);
            }
        }

        #endregion

        #region 异步加载ab

        /// <summary>
        /// 加载ab
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        public void AsyncLoadAssetBundle(string path, Action<bool> sucessCallback)
        {
            path = Path.Combine(LocalHotUpdateResPath, path);
            path = "file:///" + path;

            path = path.Replace("\\", "/");
            if (manifest.Manifest == null)
            {
                Debug.LogError("请先加载依赖文件！");
                return;
            }

            var res = manifest.Manifest.GetDirectDependencies(Path.GetFileName(path));
            //创建一个队列
            Queue<string> resQue = new Queue<string>();
            foreach (var r in res)
            {
                string _path = Path.GetDirectoryName(path) + "/" + Path.GetFileName(r);
                _path = _path.Replace("\\", "/");
                var key = Path.GetFileName(_path);
                if (AssetbundleMap.ContainsKey(key) == false)
                {
                    resQue.Enqueue(_path);
                }
                else
                {
                    AssetbundleMap[key].Use();
                }
            }

            if (AssetbundleMap.ContainsKey(Path.GetFileName(path)) == false)
            {
                resQue.Enqueue(path);
            }


            //开始加载队列
            IEnumeratorTool.StartCoroutine(IELoadAssetBundles(resQue, sucessCallback));
        }

        /// <summary>
        /// 递归加载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        /// <returns></returns>
        IEnumerator IELoadAssetBundles(Queue<string> resQue, Action<bool> callback)
        {
            string path;
            if (resQue.Count <= 0)
            {
                callback(true);
                yield break;
            }
            else
            {
                path = resQue.Dequeue();
            }

            BDebug.Log("加载依赖：" + path);

            WWW www = new WWW(path);
            yield return www;
            if (www.error == null)
            {
                if (www.isDone)
                {
                    UseAssetBunle(path, www.assetBundle);
                    //刷出
                    IEnumeratorTool.StartCoroutine(IELoadAssetBundles(resQue, callback));
                    yield break;
                }
            }
            else
            {
                Debug.LogError("加载失败：" + path);
            }
        }

        #endregion

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public T LoadFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
        {
            T o = default(T);

            if (AssetbundleMap.ContainsKey(abName))
            {
                o = AssetbundleMap[abName].assetBundle.LoadAsset<T>(objName);
            }

            return o;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="name"></param>
        public void UnloadAsset(string name, bool isUnloadIsUsing = false)
        {
            var path = GetExistPath(name);

            if (path != null)
            {
                var res = manifest.Manifest.GetDirectDependencies(path);
                //创建一个队列
                Queue<string> resQue = new Queue<string>();
                foreach (var r in res)
                {
                    if (AssetbundleMap.ContainsKey(r))
                    {
                        resQue.Enqueue(r);
                    }
                }

                resQue.Enqueue(path);
                foreach (var r in resQue)
                {
                    if (AssetbundleMap.ContainsKey(r))
                    {
                        if (isUnloadIsUsing)
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
                    if (AssetbundleMap[k].referenceCount <= 0)
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
        }


        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objName"></param>
        /// <returns></returns>
        public int AsyncLoadSource<T>(string objName, Action<bool, T> aciton, bool isCreateTaskid = true)
            where T : UnityEngine.Object
        {
            //创建任务序列
            int taskid = -1;

            if (isCreateTaskid)
            {
                taskid = CreateTaskHash();
                taskHashSet.Add(taskid);
            }

            AsyncTask task = new AsyncTask();
            task.id = taskid;
            task.RegTask(() =>
            {
                var path = GetExistPath<T>(objName);
                if (path != null)
                {
                    var sourceName = Path.GetFileName(objName);
                    //assetbundle 
                    AsyncLoadAssetBundle(path, (bool issuccess) =>
                    {
                        T _t = null;
                        if (issuccess)
                        {
                            _t = LoadFormAssetBundle<T>(path, sourceName);
                        }

                        //判断任务结束
                        task.EndTask();
                        //有创建taskid的，判断段taskid是否存在
                        if (isCreateTaskid == true && taskHashSet.Contains(taskid) == false)
                        {
                            BDebug.Log("没发现任务id,不执行回调");
                            return;
                        }

                        aciton(issuccess, _t);
                    });
                }
                else
                {
                    //判断任务结束
                    task.EndTask();
                    //有创建taskid的，判断段taskid是否存在
                    if (isCreateTaskid == true && taskHashSet.Contains(taskid) == false)
                    {
                        BDebug.Log("没发现任务id,不执行回调");
                        return;
                    }

                    aciton(false, null);
                }
            });

            asyncTaskList.Add(task);
            return taskid;
        }


        public T Load<T>(string objName) where T : UnityEngine.Object
        {
            return null;
        }


        public void LoadAssetBundle(string path)
        {
            path = Path.Combine(LocalHotUpdateResPath, path);
#if UNITY_EDITOR
            path = "file://" + path;
#endif
            path = path.Replace("\\", "/");
            if (manifest.Manifest == null)
            {
                Debug.LogError("请先加载依赖文件！");
                return;
            }
        }


        #region 获取资源路径

        /// <summary>
        /// 根据路径获取正确存在的资源路径
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private string GetExistPath<T>(string objName)
        {
            if (manifest.Manifest == null)
            {
                Debug.LogError("请先加载依赖文件！");
                return null;
            }

            List<string> canbeResource = new List<string>();
            //变换成ab名
            var abName = objName.Replace("\\", "/");
            abName = abName.Replace("/", "_");
            abName = abName.ToLower();

            var t = typeof(T);
            if (t == typeof(UnityEngine.Object))
            {
                return GetExistPath(objName);
            }
            else if (t == typeof(GameObject))
            {
                canbeResource.Add(abName + "@prefab");
            }
            else if (t == typeof(Image) || t == typeof(Sprite) || t == typeof(Texture) || t == typeof(Texture2D))
            {
                canbeResource.Add(abName + "@jpg");
                canbeResource.Add(abName + "@jpeg");
                canbeResource.Add(abName + "@png");
                canbeResource.Add(abName + "@tga");
            }
            else if (t == typeof(Text) || t == (typeof(TextAsset)))
            {
                canbeResource.Add(abName + "@txt");
                canbeResource.Add(abName + "@json");
                canbeResource.Add(abName + "@xml");
                canbeResource.Add(abName + "@bytes");
            }
            else if (t == typeof(AudioClip))
            {
                canbeResource.Add(abName + "@mp3");
                canbeResource.Add(abName + "@ogg");
                canbeResource.Add(abName + "@wav");
            }


            foreach (var r in canbeResource)
            {
                //开始加载
                if (manifest.allAssetBundlesMap.ContainsKey(r))
                {
                    return r;
                }
            }

            return null;
        }


        /// <summary>
        /// 根据路径获取正确存在的资源路径
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private string GetExistPath(string objName)
        {
            if (manifest.Manifest == null)
            {
                Debug.LogError("请先加载依赖文件！");
                return null;
            }

            List<string> canbeResource = new List<string>();
            //变换成ab名
            var abName = objName.Replace("\\", "/");
            abName = abName.Replace("/", "_");
            abName = abName.ToLower();
            canbeResource.Add(abName + "@prefab");
            canbeResource.Add(abName + "@jpg");
            canbeResource.Add(abName + "@jpeg");
            canbeResource.Add(abName + "@png");
            canbeResource.Add(abName + "@tga");
            canbeResource.Add(abName + "@txt");
            canbeResource.Add(abName + "@bytes");
            canbeResource.Add(abName + "@json");
            canbeResource.Add(abName + "@xml");
            canbeResource.Add(abName + "@mp3");
            canbeResource.Add(abName + "@ogg");
            canbeResource.Add(abName + "@wav");
            foreach (var r in canbeResource)
            {
                //开始加载
                if (manifest.allAssetBundlesMap.ContainsKey(r))
                {
                    return r;
                }
            }

            return null;
        }

        #endregion


        /// <summary>
        /// 异步加载表
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProcess"></param>
        /// <returns>taskid</returns>
        public int AsyncLoadSources(IList<string> sources, Action<IDictionary<string, Object>> onLoadEnd,Action<int, int> onProcess)
        {
            //task id
            var taskid = CreateTaskHash();
            taskHashSet.Add(taskid);
            IDictionary<string, UnityEngine.Object> resmap = new Dictionary<string, UnityEngine.Object>();
            //
            List<int> ids = new List<int>();
            int curTaskCount = 0;
            foreach (var obj in sources)
            {
                string curtask = obj;
                var id = AsyncLoadSource<UnityEngine.Object>(curtask, (bool b, UnityEngine.Object o) =>
                {
                    curTaskCount++;
                    resmap[curtask] = o;
                    BDebug.Log(string.Format("rescount:{0} listcount:{1}", curtask, curtask));
                    //查询是否可以继续
                    if (taskHashSet.Contains(taskid) == false)
                    {
                        foreach (var _id in ids)
                        {
                            if (taskHashSet.Contains(_id))
                            {
                                taskHashSet.Remove(_id);
                            }
                        }
                    }
                    else
                    {
                        if (onProcess != null)
                        {
                            onProcess(curTaskCount, ids.Count);
                        }
                    }
                   
                    //判断是否加载完
                    if (onLoadEnd!=null && curTaskCount >= ids.Count)
                    {
                        onLoadEnd(resmap);
                    }
                });

                ids.Add(id);
            }

            return taskid;
        }


        private int CreateTaskHash()
        {
            return id++;
        }

        public void LoadCancel(int taskid)
        {
            if (taskHashSet.Contains(taskid))
            {
                taskHashSet.Remove(taskid);
                BDebug.Log("PTResource 移除task:" + taskid);
            }
        }

        public void LoadAllCalcel()
        {
            taskHashSet.Clear();
        }

        public void Update()
        {
            //异步回调表处理
            {
                if (asyncTaskList.Count > 0)
                {
                    var curtask = asyncTaskList[0];

                    //刷出一个正在执行的任务
                    while (curtask.CurState == AsyncTask.state.End)
                    {
                        asyncTaskList.RemoveAt(0);
                        //移除表
                        if (taskHashSet.Contains(curtask.id))
                        {
                            taskHashSet.Remove(curtask.id);
                        }

                        //
                        if (asyncTaskList.Count > 0)
                            curtask = asyncTaskList[0];
                        else return;
                    }

                    switch (curtask.CurState)
                    {
                        case AsyncTask.state.Waiting:
                            //有id的task需要判断是否存在task列表中
                            if (curtask.id != -1 && taskHashSet.Contains(curtask.id) == false)
                            {
                                BDebug.Log(string.Format("当前任务：{0}，已经被移除，不执行!", curtask.id));
                                asyncTaskList.RemoveAt(0);
                                return;
                            }

                            curtask.DoTask();
                            break;
                        case AsyncTask.state.Loading:
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}