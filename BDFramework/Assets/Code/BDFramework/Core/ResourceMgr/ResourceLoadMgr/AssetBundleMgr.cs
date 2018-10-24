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
        private int taskID;

        /// <summary>
        /// 将要执行的task
        /// </summary>
        private HashSet<int> willdoTaskSet;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private List<LoadTask> allTaskList;

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
        private string resourcePath = "";

        public AssetBundleMgr()
        {
            this.assetbundleMap = new Dictionary<string, AssetBundleReference>();
            this.willdoTaskSet = new HashSet<int>();
            this.allTaskList = new List<LoadTask>();
            //1.设置加载路径
            string path = "";
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                path = "Windows/Art";
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                path = "Android/Art";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                path = "iOS/Art";
            }

            var persistent = Path.Combine(Application.persistentDataPath, path).Replace("\\", "/");
            var streamingAsset = Path.Combine(Application.streamingAssetsPath, path).Replace("\\", "/");
            //
            resourcePath = File.Exists(persistent) ? persistent : streamingAsset;

            //2.加载menifest
            //persistent 和 streaming同时只能存在一个，
            //streaming是给appstore审核用,
            //过审后开始下载,则切回persistent模式
            this.manifest = new AssetBundleManifestReference(resourcePath + "/Art");
        }

        #region 异步加载ab
        /// <summary>
        /// 加载ab
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        private void AsyncLoadAssetBundle(string path, Action<bool> callback)
        {
            path = Path.Combine(resourcePath, path);
            path = path.Replace("\\", "/");

            var res = manifest.Manifest.GetDirectDependencies(Path.GetFileName(path));
            //创建一个队列
            Queue<string> resQue = new Queue<string>();
            foreach (var r in res)
            {
                string _path = Path.GetDirectoryName(path) + "/" + Path.GetFileName(r);
                _path = _path.Replace("\\", "/");
                var key = Path.GetFileName(_path);
                //判断是否已经加载过
                if (assetbundleMap.ContainsKey(key) == false)
                {
                    resQue.Enqueue(_path);
                }
                else
                {
                    assetbundleMap[key].Use();
                }
            }

            if (assetbundleMap.ContainsKey(Path.GetFileName(path)) == false)
            {
                resQue.Enqueue(path);
            }
            
            //开始加载队列
            IEnumeratorTool.StartCoroutine(IELoadAssetbundle(resQue, callback));
        }

        /// <summary>
        /// 递归加载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        /// <returns></returns>
        IEnumerator IELoadAssetbundle(Queue<string> resQue, Action<bool> callback)
        {
            if (resQue.Count <= 0)
            {
                callback(true);
                yield break;
            }
            else
            {
                var path = resQue.Dequeue();
                var result =  AssetBundle.LoadFromFileAsync(path);
                yield return result;
                if (result.isDone)
                {
                    AddAssetBundle(path, result.assetBundle);
                    //开始下个任务
                    IEnumeratorTool.StartCoroutine(IELoadAssetbundle(resQue, callback));
                    yield break;          
                }
                else
                {
                    Debug.LogError("加载失败:"+ path);
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
            T o = default(T);
            AssetBundleReference abr = null;         
            if (assetbundleMap.TryGetValue(abName, out abr))
            {
                o = abr.assetBundle.LoadAsset<T>(objName);
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


        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objName"></param>
        /// <returns></returns>
        public int AsyncLoad<T>(string objName, Action<bool, T> aciton)
            where T : UnityEngine.Object
        {
            //创建任务id
            int id;
            id = this.taskID++;
            willdoTaskSet.Add(id);
            //开始异步任务
            LoadTask task = new LoadTask();
            task.id = id;
            task.RegisterTask(() =>
            {
                var path = GetExistPath(objName);
                if (path != null)
                {
                    //assetbundle 
                    AsyncLoadAssetBundle(path, (bool issuccess) =>
                    {
                        T _t = null;
                        if (issuccess)
                        {
                            var fileName = Path.GetFileName(path);
                            _t = LoadFormAssetBundle<T>(path, fileName);
                        }

                        //判断任务结束
                        task.EndTask();
                        //判断是否取消了任务
                        if (willdoTaskSet.Contains(id) == false)
                        {
                            BDebug.Log("没发现任务id,不执行回调");
                        }
                        else
                        {                        
                            aciton(issuccess, _t);
                        }
                    });
                }
                else
                {
                    //判断任务结束
                    task.EndTask();
                    //判断是否取消了任务
                    if ( willdoTaskSet.Contains(id) == false)
                    {
                        BDebug.Log("没发现任务id,不执行回调");
                    }
                    else
                    {                     
                        aciton(false, null);
                    }
                }
            });

            allTaskList.Add(task);
            return id;
        }


        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="objName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string objName) where T : UnityEngine.Object
        {
            //ab存储的是asset下的相对目录
            objName = "assets/resource/runtime/" + objName.ToLower();
            //寻找ab的全目录
            objName = GetExistPath(objName);
            
            var res = manifest.Manifest.GetDirectDependencies(objName);
            //1.创建依赖加载队列
            List<string> loadList = new List<string>();
            foreach (var r in res)
            {
                //依赖队列需要加上resourcepath
                var dir = Path.Combine(this.resourcePath, r);
                //判断是否已经加载过
                if (assetbundleMap.ContainsKey(dir) == false)
                {
                    loadList.Add(dir);
                }
                else
                {
                    assetbundleMap[dir].Use();
                }
            }

            //加载列表
            foreach (var l in loadList)
            {
                AddAssetBundle(l, AssetBundle.LoadFromFile(l));
            }
                      
            //2.加载主体
            var fullname = Path.Combine(resourcePath, objName);
            if (assetbundleMap.ContainsKey(Path.GetFileName(fullname)) == false)
            {
                var ab = AssetBundle.LoadFromFile(fullname);
                AddAssetBundle(fullname, ab);
            }
            //3.加载具体资源
            return LoadFormAssetBundle<T>(fullname, objName);
        }


        #region 获取资源路径

        /// <summary>
        /// 再所有hash中查询文件名符合的
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

        #endregion


        /// <summary>
        /// 异步加载表
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProcess"></param>
        /// <returns>taskid</returns>
        public int AsyncLoad(IList<string> sources, Action<IDictionary<string, Object>> onLoadEnd,
            Action<int, int> onProcess)
        {
            //task id
            var taskid = this.taskID++;
            willdoTaskSet.Add(taskid);
            IDictionary<string, UnityEngine.Object> resmap = new Dictionary<string, UnityEngine.Object>();
            //
            List<int> ids = new List<int>();
            int curTaskCount = 0;
            foreach (var obj in sources)
            {
                string curtask = obj;
                var id = AsyncLoad<UnityEngine.Object>(curtask, (bool b, UnityEngine.Object o) =>
                {
                    curTaskCount++;
                    resmap[curtask] = o;
                    BDebug.Log(string.Format("rescount:{0} listcount:{1}", curtask, curtask));
                    //查询是否可以继续
                    if (willdoTaskSet.Contains(taskid) == false)
                    {
                        foreach (var _id in ids)
                        {
                            if (willdoTaskSet.Contains(_id))
                            {
                                willdoTaskSet.Remove(_id);
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
                    if (onLoadEnd != null && curTaskCount >= ids.Count)
                    {
                        onLoadEnd(resmap);
                    }
                });

                ids.Add(id);
            }

            return taskid;
        }


        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            if (willdoTaskSet.Contains(taskid))
            {
                willdoTaskSet.Remove(taskid);
                BDebug.Log("BResource 移除task:" + taskid);
            }
        }

        /// <summary>
        /// 取消所有load任务
        /// </summary>
        public void LoadAllCalcel()
        {
            willdoTaskSet.Clear();
        }

        public void Update()
        {
            int count = 0;
            //异步回调表处理
            if (allTaskList.Count > 0)
            {
                //每帧最多处理10个
                for (int i = 0; i < allTaskList.Count && i < 10; i++)
                {
                    var task = allTaskList[i];
                    //
                    switch (task.CurState)
                    {
                        //等待执行
                        case LoadTask.state.Waiting:
                        {
                            //有id的task需要判断是否存在task集合中
                            if (task.id != -1 && willdoTaskSet.Contains(task.id) == false)
                            {
                                BDebug.Log(string.Format("当前任务：{0}，已经被移除，不执行!", task.id));
                                allTaskList.RemoveAt(i);
                                //当前任务没有执行,继续
                                i--;
                            }
                            else
                            {
                                //开始
                                task.DoTask();
                            }
                        }
                            break;
                        case LoadTask.state.Loading:
                        {
                        }
                            break;
                        case LoadTask.state.End:
                        {
                            //完成
                            allTaskList.RemoveAt(i);
                            willdoTaskSet.Remove(task.id);
                            //当前任务没有执行,继续
                            i--;
                        }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}