using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Collections;
using Image = UnityEngine.UI.Image;

namespace BDFramework.ResourceMgr
{

    /// <summary>
    /// ab包管理器
    /// </summary>
    public class ResourcesMgr : IResMgr
    {


        /// <summary>
        /// 全局的任务id
        /// </summary>
        private int id = 0;
        /// <summary>
        /// 任务表
        /// </summary>
        private HashSet<int> taskHashSet;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private List<AsyncTask> asyncTaskList;

        /// <summary>
        /// 控件map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objsMap;

        public ResourcesMgr()
        {
            taskHashSet = new HashSet<int>();
            asyncTaskList = new List<AsyncTask>();
            objsMap = new Dictionary<string, UnityEngine.Object>();
        }

        public string LocalHotUpdateResPath
        {
            get;
            set;
        }

        public Dictionary<string, AssetBundleReference> AssetbundleMap
        {
            get;
            set;
        }

        public void LoadManifestAsync(string path, Action<bool> callback)
        {
            BDebug.Log("res 模式不需要加载依赖");
        }

        public void LoadAssetBundleAsync(string path, Action<bool> sucessCallback)
        {

        }

        public void LoadAssetBundle(string path)
        {

        }

        public void UnloadAsset(string name, bool isUnloadIsUsing = false)
        {
            try
            {
                if (objsMap.ContainsKey(name))
                {

                    var obj = objsMap[name];

//                    if (obj is GameObject)
//                    {
//                        GameObject.DestroyImmediate(obj);
//                    }
//                    //
                    objsMap.Remove(name);
                    // GC.Collect();
                    Resources.UnloadUnusedAssets();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
          

        }

        public void UnloadAllAsset()
        {
            objsMap.Clear();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public T LoadResFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
        {
            return null;
        }

        public T Load<T>(string objName) where T : UnityEngine.Object
        {
            if (objsMap.ContainsKey(objName))
            {

                return objsMap[objName] as T;
            }
            else
            {
                objsMap[objName] = Resources.Load<T>(objName);
                return objsMap[objName] as T;
            }

        }

        public int LoadAsync<T>(string objName, Action<bool, T> action, bool isCreateTaskid = true) where T : UnityEngine.Object
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
                if (objsMap.ContainsKey(objName))
                {
                    //判断任务结束
                    task.EndTask();
                    //有创建taskid的，判断段taskid是否存在
                    if (isCreateTaskid == true && taskHashSet.Contains(taskid) == false)
                    {
                        BDebug.Log("没发现任务id,不执行回调");
                        return;
                    }
                    action(true, objsMap[objName] as T);
                }
                else
                {
                    IEnumeratorTool.StartCoroutine(IELoadAsync<T>(objName, (bool b, T t) =>
                     {
                        //判断任务结束
                        task.EndTask();
                        //有创建taskid的，判断段taskid是否存在
                        if (isCreateTaskid == true && taskHashSet.Contains(taskid) == false)
                         {
                             BDebug.Log("没发现任务id,不执行回调");
                             return;
                         }
                         action(b, t);
                     }));
                }
            });

            asyncTaskList.Add(task);
            return taskid;
        }


        IEnumerator IELoadAsync<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object
        {
            // JDeBug.Inst.Log("执行：" + objName);
            var res = Resources.LoadAsync<T>(objName);
            yield return res;
            if (res.isDone)
            {

                //JDeBug.Inst.LogFormat("耗时:{0}  创建:{1}", watch.ElapsedMilliseconds, objName);
                objsMap[objName] = res.asset;
                action(true, res.asset as T);
            }
        }


        public int LoadAsync(IList<string> objNames, Action<IDictionary<string, UnityEngine.Object>> action)
        {
            var taskid = CreateTaskHash();
            taskHashSet.Add(taskid);

            IDictionary<string, UnityEngine.Object> resmap = new Dictionary<string, UnityEngine.Object>();
            //
            List<int> ids = new List<int>();
            //标记任务回调
            int taskIndex = 0;
            int count = objNames.Count;
            foreach (var obj in objNames)
            {

                var curtask = obj;
                var id = LoadAsync<UnityEngine.Object>(curtask, (bool b, UnityEngine.Object o) =>
                {
                      //加入列表
                      resmap[curtask] = o;
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

                    taskIndex++;
                  //判断是否加载完
                    if (taskIndex == count)
                    {
                        action(resmap);
                    }

                });

                ids.Add(id);
            }

            return taskid;
        }
        /// <summary>
        /// 实时返回进度
        /// </summary>
        /// <param name="objName"></param>
        /// <param name="processAction"></param>
        /// <returns></returns>
        public int LoadAsync(IList<string> objNames, Action<string, UnityEngine.Object> processAction)
        {
            var taskid = CreateTaskHash();
            taskHashSet.Add(taskid);
            //所有任务的id
            List<int> ids = new List<int>();
            //标记任务回调
            int taskIndex = 0;
            int count = objNames.Count;
            foreach (var obj in objNames)
            {

                var curtask = obj;
                var id = LoadAsync<UnityEngine.Object>(curtask, (bool b, UnityEngine.Object o) =>
                {
                    //查询是否可以继续
                    if (taskHashSet.Contains(taskid) == false)
                    {
                        //移除所有任务的id
                        foreach (var _id in ids)
                        {
                            if (taskHashSet.Contains(_id))
                            {
                                taskHashSet.Remove(_id);
                            }
                        }

                    }

                    taskIndex++;
                    //判断是否加载完
                    processAction(curtask, o);

                });

                ids.Add(id);
            }

            return taskid;
        }
        public void Update()
        {
            //异步回调表处理
            if (asyncTaskList.Count > 0)
            {
                var curtask = asyncTaskList[0];

                //刷出一个正在执行的任务
                while (curtask.mCurState == AsyncTask.state.End)
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
                //
                switch (curtask.mCurState)
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

        /// <summary>
        /// 取消所有
        /// </summary>
        public void LoadAllCalcel()
        {
            taskHashSet.Clear();
        }


    }
}
