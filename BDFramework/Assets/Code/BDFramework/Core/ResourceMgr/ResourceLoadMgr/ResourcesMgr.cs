#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Collections;
using Mono.Cecil;
using Mono.Cecil.PE;
using UnityEditor;
using UnityEditor.VersionControl;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// ab包管理器
    /// </summary>
    public class ResourcesMgr : IResMgr
    {
        /// <summary>
        /// 资源的根目录
        /// </summary>
        private string ResourceRootPath = "Resource/Runtime";

        /// <summary>
        /// 全局的任务计数器
        /// </summary>
        private int TaskCounter = 0;

        /// <summary>
        /// 等待执行的列表
        /// </summary>
        private HashSet<int> willdoTaskSet;

        /// <summary>
        /// 所有任务的集合
        /// </summary>
        private List<LoadTask> allTaskList;

        /// <summary>
        /// 对象map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objsMap;

        /// <summary>
        /// 所有的资源列表
        /// </summary>
        private List<string> allResourceList;

        public ResourcesMgr()
        {
            willdoTaskSet = new HashSet<int>();
            allTaskList = new List<LoadTask>();
            objsMap = new Dictionary<string, UnityEngine.Object>();
            //搜索所有资源
            var root = Application.dataPath + "/" + ResourceRootPath;
            //处理资源列表格式
            allResourceList = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < allResourceList.Count; i++)
            {
                allResourceList[i] = allResourceList[i].Replace(root + "\\", "").Replace("\\", "/");
            }
        }


        public Dictionary<string, AssetBundleReference> assetbundleMap { get; set; }

       

        public void UnloadAsset(string name, bool isUnloadIsUsing = false)
        {
            try
            {
                if (objsMap.ContainsKey(name))
                {
                    var obj = objsMap[name];
                    //
                    objsMap.Remove(name);
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

        public T LoadFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
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
                var findTarget = objName + ".";
                var result = this.allResourceList.Find((a) => a.Contains(findTarget));
                result = "Assets/" + this.ResourceRootPath + "/" + result;
                Debug.Log("加载:" + objName);
                Debug.Log("find:" + result);
                //
                objsMap[objName] = AssetDatabase.LoadAssetAtPath<T>(result);
                return objsMap[objName] as T;
            }
        }

        public int AsyncLoad<T>(string objName, Action<bool, T> action)
            where T : UnityEngine.Object
        {
            //创建任务序列
            int taskid = -1;

          
                taskid = AddTaskCounter();
                willdoTaskSet.Add(taskid);
            

            LoadTask task = new LoadTask();
            task.id = taskid;
            task.RegisterTask(() =>
            {
                if (objsMap.ContainsKey(objName))
                {
                    //判断任务结束
                    task.EndTask();
                    //有创建taskid的，判断段taskid是否存在
                    if (willdoTaskSet.Contains(taskid) == false)
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
                        if ( willdoTaskSet.Contains(taskid) == false)
                        {
                            BDebug.Log("没发现任务id,不执行回调");
                            return;
                        }

                        action(b, t);
                    }));
                }
            });

            allTaskList.Add(task);
            return taskid;
        }


        IEnumerator IELoadAsync<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object
        {
            var res = Load<T>(objName);
            yield return new WaitForEndOfFrame();
            action(true, res);
        }


        /// <summary>
        /// 批量加载
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProcess"></param>
        /// <returns></returns>
        public int AsyncLoad(IList<string> sources, Action<IDictionary<string, Object>> onLoadEnd,
            Action<int, int> onProcess)
        {
            var taskid = AddTaskCounter();
            willdoTaskSet.Add(taskid);

            IDictionary<string, UnityEngine.Object> resmap = new Dictionary<string, UnityEngine.Object>();
            //
            List<int> ids = new List<int>();
            //标记任务回调
            int curTaskCount = 0;
            foreach (var obj in sources)
            {
                var curtask = obj;
                var id = AsyncLoad<Object>(curtask, (b, o) =>
                {
                    curTaskCount++;
                    //加入列表
                    resmap[curtask] = o;
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
                    //进度通知
                    else if (onProcess != null)
                    {
                        onProcess(curTaskCount, sources.Count);
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

        /// <summary>
        /// 任务id自增长
        /// </summary>
        /// <returns></returns>
        private int AddTaskCounter()
        {
            return TaskCounter++;
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            if (willdoTaskSet.Contains(taskid))
            {
                willdoTaskSet.Remove(taskid);
            }
        }

        /// <summary>
        /// 取消所有
        /// </summary>
        public void LoadAllCalcel()
        {
            willdoTaskSet.Clear();
        }
    }
}
#endif