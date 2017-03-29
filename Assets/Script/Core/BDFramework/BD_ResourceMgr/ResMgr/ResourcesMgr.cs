using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Collections;

namespace BDFramework.ResourceMgr
{

  /// <summary>
  /// ab包管理器
  /// </summary>
  public class ResourcesMgr :IResMgr
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
        /// 控件map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objMaps ;

        public ResourcesMgr()
        {
            mTaskHashTable = new HashSet<string>();
            mAsyncTaskList = new List<AsyncTask>();
            objMaps = new Dictionary<string, UnityEngine.Object>();
        }

      public string mLocalHotUpdateResPath
      {
          get;
          set;
      }

      public Dictionary<string, AssetBundleReference> assetbundleMap
      {
          get;
          set;
      }

        public void LoadManifestAsync(string path, Action<bool> callback)
        {
            Debug.Log("res 模式不需要加载依赖");
        }

        public void LoadAssetBundleAsync(string path, Action<bool> sucessCallback)
        {
            
        }

        public void LoadAssetBundle(string path)
        {
            
        }

        public void UnloadAsset(string name,bool isUnloadIsUsing = false)
        {
            if (objMaps.ContainsKey(name))
            {
                objMaps.Remove(name);
                Resources.UnloadUnusedAssets();
                GC.Collect();
            }
            
        }

        public void UnloadAllAsset()
        {
            objMaps.Clear();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public T LoadResFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
        {
            return null;
        }

        public T Load<T>(string objName) where T : UnityEngine.Object
        {
            if (objMaps.ContainsKey(objName))
            {

                return objMaps[objName] as T;
            }
            else
            {
                objMaps[objName] = Resources.Load<T>(objName);
                return  objMaps[objName] as T;
            }

        }

        public string LoadAsync<T>(string objName, Action<bool, T> action, bool isCreateTaskid = true) where T : UnityEngine.Object
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
                    if (objMaps.ContainsKey(objName))
                    {
                        //判断任务结束
                        task.EndTask();
                        //有创建taskid的，判断段taskid是否存在
                        if (isCreateTaskid == true && mTaskHashTable.Contains(taskid) == false)
                        {

                            Debug.Log("没发现任务id,不执行回调");
                            return;
                        }

                        action(true, objMaps[objName] as T);
                    }
                    else
                    {
                        IEnumeratorLaunch.Instance.Enqueue(IELoadAsync<T>(objName,(bool b, T t)=>
                            {
                                //判断任务结束
                                task.EndTask();
                                //有创建taskid的，判断段taskid是否存在
                                if (isCreateTaskid == true && mTaskHashTable.Contains(taskid) == false)
                                {

                                    Debug.Log("没发现任务id,不执行回调");
                                    return;
                                }
                                action(b, t);
                            }));
                    }
                });

            mAsyncTaskList.Add(task);
            return taskid;
        }


      IEnumerator IELoadAsync<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object
      {
          var res = Resources.LoadAsync<T>(objName);
          yield return res;


          if (res.isDone)
          {
              objMaps[objName] = res.asset;
              action(true, res.asset as T);
          }
      }


      public string LoadAsync(IList<string> objName, Action<IDictionary<string, UnityEngine.Object>> action)
      {
          var taskid = CreateTaskHash();
          mTaskHashTable.Add(taskid);

          IDictionary<string, UnityEngine.Object> resmap = new Dictionary<string, UnityEngine.Object>();
          //
          List<string> ids = new List<string>();
          foreach (var obj in objName)
          {

              var curtask = obj;
              var id = LoadAsync<UnityEngine.Object>(curtask, (bool b, UnityEngine.Object o) =>
              {
                  //加入列表
                  resmap[curtask] = o;
                  //查询是否可以继续
                  if (mTaskHashTable.Contains(taskid) == false)
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
                  else if (resmap.Count == objName.Count)
                  {
                      action(resmap);
                  }

              });

              ids.Add(id);
          }

          return taskid;
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
                          if (curtask.id != null && mTaskHashTable.Contains(curtask.id) == false)
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
    }
}
