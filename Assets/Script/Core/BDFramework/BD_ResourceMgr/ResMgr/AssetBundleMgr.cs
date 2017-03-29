using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.UI;
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
            get
            {
                return asset;
            }

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
        public int referenceCount;

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
            iswaiting,
            isloading,
            isend,
        }

        public AsyncTask()
        {
            mCurState = state.iswaiting;
        }
        //任务id
        public string id;
        //当前状态
        public state mCurState;
        //任务
        Action dotask;
        //注册task
        public void ResTask(Action task)
        {
            dotask = task;
        }
        //dotask
        public void DoTask()
        {
            if (dotask != null)
            {
                mCurState = state.isloading;
                dotask();
            }
        }
        //任务结束
        public void EndTask()
        {
            mCurState = state.isend;
        }
    }
  /// <summary>
  /// ab包管理器
  /// </summary>
  public class AssetBundleMgr :IResMgr
    {
      /// <summary>
      /// 全局的任务id
      /// </summary>
      private int id;
      /// <summary>
      /// 任务表
      /// </summary>
      private HashSet<string> mTaskHashTable;

      /// <summary>
      /// 异步回调表
      /// </summary>
      private List<AsyncTask> mAsyncTaskList;

      /// <summary>
      /// 全局唯一的依赖
      /// </summary>
      private AssetBundleManifestReference manifest;

      public AssetBundleMgr()
      {
          this.manifest       = new AssetBundleManifestReference();
          this.assetbundleMap = new Dictionary<string, AssetBundleReference>();
          this.mTaskHashTable = new HashSet<string>();
          this.mAsyncTaskList  = new List<AsyncTask>();

      }
      public Dictionary<string, AssetBundleReference> assetbundleMap
      {
          get;
          set;
      }

      public string mLocalHotUpdateResPath
      {
          get;
          set;
      }

      /// <summary>
      /// 加入ab包
      /// </summary>
      /// <param name="name"></param>
      /// <param name="ab"></param>
      private void UseAssetBunle(string name , AssetBundle ab)
      {
          name = Path.GetFileName(name);
          if (assetbundleMap.ContainsKey(name) ==false)
          {
              AssetBundleReference abr = new AssetBundleReference() { assetBundle = ab};
              assetbundleMap[name] = abr;
          }

          assetbundleMap[name].Use();

          
      }

      #region 异步加载Manifest
      public void LoadManifestAsync(string path,Action<bool> callback)
      {
          //如果存在 不让加载
          //if (manifest != null)
          //{
          //    callback(true);
          //    return;
          //}
          path = Path.Combine(mLocalHotUpdateResPath, path);
#if UNITY_EDITOR || UNITY_IPHONE
          path = "File:///" + path;
#endif
          path = path.Replace("\\", "/");
          IEnumeratorLaunch.Instance.Enqueue(IELoadAssetBundles(path, callback, true));
      }

      //委托协程
      IEnumerator IELoadAssetBundles(string path, Action<bool> sucessCallback, bool isManiFest = false)
      {

			if (File.Exists (path)) {
			
				Debug.Log (" file is exists!");
			} else {
				Debug.Log ("file  is not exists!");
			}
          Debug.Log("加载依赖");
          WWW www = new WWW(path);
          yield return www;
          if (www.error == null)
          {
				if (www.isDone) {
					if (isManiFest) {

						manifest.Manifest = www.assetBundle.LoadAsset ("AssetBundleManifest") as AssetBundleManifest;
						var xx = manifest.Manifest.GetAllAssetBundles ();

						if (manifest != null) {
							sucessCallback (true);

						} else {
							sucessCallback (false);
							Debug.LogError ("加载依赖失败!");
						}

					}
					yield break;
				} else {
					Debug.Log ("loading ...");
				}

          }
          else
          {
              Debug.LogError("错误："+www.error);
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
        public void LoadAssetBundleAsync(string path, Action<bool> sucessCallback)
        {
            path = Path.Combine(mLocalHotUpdateResPath, path);
#if UNITY_EDITOR || UNITY_IPHONE
            path = "file:///" + path;
#endif
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
                string _path = Path.GetDirectoryName(path) +"/"+Path.GetFileName(r);
                _path = _path.Replace("\\", "/");
                var key = Path.GetFileName(_path);
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
            IEnumeratorLaunch.Instance.Enqueue(IELoadAssetBundles(resQue, sucessCallback));


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
            Debug.Log("加载依赖：" + path);

            WWW www = new WWW(path);
            yield return www;
            if (www.error == null)
            {
                if (www.isDone)
                {
                    UseAssetBunle(path, www.assetBundle);
                    //递归刷出
                    IEnumeratorLaunch.Instance.Enqueue(IELoadAssetBundles(resQue, callback));
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
        public T LoadResFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
        {
            T o = default(T);

            if (assetbundleMap.ContainsKey(abName))
            {
                o = assetbundleMap[abName].assetBundle.LoadAsset<T>(objName);
            }
            return o;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="name"></param>
        public void UnloadAsset(string name,bool isUnloadIsUsing = false)
        {
            var path = GetExistPath(name);

            if (path != null)
            {
                var res = manifest.Manifest.GetDirectDependencies(path);
                //创建一个队列
                Queue<string> resQue = new Queue<string>();
                foreach (var r in res)
                {
                    if (assetbundleMap.ContainsKey(r))
                    {
                        resQue.Enqueue(r);
                    }
                }

                resQue.Enqueue(path);
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
                Debug.Log("路径不存在");
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
       public string LoadAsync<T>(string objName,Action<bool,T> aciton,bool isCreateTaskid = true) where T : UnityEngine.Object
        {

            //创建任务序列
            string taskid = null;
            
            if (isCreateTaskid)
            {
                taskid = CreateTaskHash();
                mTaskHashTable.Add(taskid);
            }

            AsyncTask task = new AsyncTask();
            task.id = taskid;
            task.ResTask(() =>
                {

                    var path = GetExistPath<T>(objName);
                    if (path != null)
                    {

                        var sourceName = Path.GetFileName(objName);
                        //assetbundle 
                        LoadAssetBundleAsync(path, (bool issuccess) =>
                        {
                            T _t = null;
                            if (issuccess)
                            {
                                _t = LoadResFormAssetBundle<T>(path, sourceName);
                            }
                            //判断任务结束
                            task.EndTask();
                            //有创建taskid的，判断段taskid是否存在
                            if (isCreateTaskid == true && mTaskHashTable.Contains(taskid) == false)
                            {

                                Debug.Log("没发现任务id,不执行回调");
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
                        if (isCreateTaskid == true && mTaskHashTable.Contains(taskid) == false)
                        {
                            Debug.Log("没发现任务id,不执行回调");
                            return;
                        }
                        aciton(false, null);
                        
                    }
                
                });

            mAsyncTaskList.Add(task);
            return taskid;
        }


        public T Load<T>(string objName) where T : UnityEngine.Object
        {
            return null;
        }



        public void LoadAssetBundle(string path)
        {
            path = Path.Combine(mLocalHotUpdateResPath, path);
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
                return  null;
            }

            List<string> canbeResource = new List<string>();
            //变换成ab名
            var abName = objName.Replace("\\", "/");
            abName = abName.Replace("/", "_");
             abName = abName.ToLower();

             var t =  typeof(T);
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
            else if (t == typeof(Text)|| t==(typeof(TextAsset)))
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
        /// <param name="objlist"></param>
        /// <param name="action"></param>
        /// <returns>taskid</returns>
        public string LoadAsync( IList<string> objlist, Action< IDictionary< string,UnityEngine.Object>> action)
        {
            //task id
            var taskid = CreateTaskHash();
            mTaskHashTable.Add(taskid);
            IDictionary<string, UnityEngine.Object> resmap = new Dictionary<string, UnityEngine.Object>();
            //
            List<string> ids = new List<string>();
            foreach(var obj in objlist)
            {
                string curtask = obj;
                var id = LoadAsync<UnityEngine.Object>(curtask, (bool b, UnityEngine.Object o) =>
                {
                    resmap[curtask] = o;
                    Debug.Log(string.Format("rescount:{0} listcount:{1}", curtask, curtask));
                    //查询是否可以继续
                    if( mTaskHashTable.Contains(taskid) == false)
                    {
                        foreach (var _id in ids)
                        {
                            if (mTaskHashTable.Contains(_id))
                            {
                                mTaskHashTable.Remove(_id);
                            }
                        }
                    }
             
                    //判断是否加载完
                    else if (resmap.Count == objlist.Count)
                    {
                        action(resmap);
                    }

                });

                ids.Add(id);
            }
           

            
            return taskid;
        }

        private string CreateTaskHash()
        {
            id++;
            return HashHelper.CreateMD5Hash(id.ToString());
        }

        public void LoadCancel(string taskid)
        {
            if (mTaskHashTable.Contains(taskid))
            {
                mTaskHashTable.Remove(taskid);
                Debug.Log("PTResource 移除task:" + taskid);
            }
        }

        public void LoadAllCalcel()
        {
            mTaskHashTable.Clear();
        }

        public void Update()
        {
            //异步回调表处理
            {
                if (mAsyncTaskList.Count > 0)
                {
                    var curtask = mAsyncTaskList[0];
                    switch (curtask.mCurState)
                    {
                        case AsyncTask.state.iswaiting:
                            //有id的task需要判断是否存在task列表中
                            if (curtask.id != null && mTaskHashTable.Contains(curtask.id) ==false)
                            {
                                Debug.Log(string.Format("当前任务：{0}，已经被移除，不执行!", curtask.id));
                                mAsyncTaskList.RemoveAt(0);
                                return;
                            }
                            curtask.DoTask();
                            break;
                        case AsyncTask.state.isloading:
                            break;
                        case AsyncTask.state.isend:
                            if (mTaskHashTable.Contains(curtask.id))
                            {
                                mTaskHashTable.Remove(curtask.id);
                            }
                            mAsyncTaskList.RemoveAt(0);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

}
