#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Collections;

using UnityEditor;

using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// ab包管理器
    /// </summary>
    public class DevResourceMgr : IResMgr
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
        private List<LoadTaskGroup> allTaskList;

        /// <summary>
        /// 对象map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objsMap;

        /// <summary>
        /// 所有的资源列表
        /// </summary>
        private List<string> allResourceList;

        public DevResourceMgr()
        {
            willdoTaskSet = new HashSet<int>();
            allTaskList   = new List<LoadTaskGroup>();
            objsMap = new Dictionary<string, UnityEngine.Object>();
            //搜索所有资源
            var root = Application.dataPath + "/" + ResourceRootPath;
            //处理资源列表格式
            allResourceList = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < allResourceList.Count; i++)
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    allResourceList[i] = allResourceList[i].Replace(root + "\\", "").Replace("\\", "/");
                }

                else if(Application.platform == RuntimePlatform.OSXEditor)
                {
                    allResourceList[i] = allResourceList[i].Replace(root + "/", "");
                }

                
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

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (objsMap.ContainsKey(path))
            {
                return objsMap[path] as T;
            }
            else
            {
                var findTarget = path + ".";
                var result = this.allResourceList.Find((a) => a.Contains(findTarget));
                result = "Assets/" + this.ResourceRootPath + "/" + result;
                //
                objsMap[path] = AssetDatabase.LoadAssetAtPath<T>(result);
                return objsMap[path] as T;
            }
        }

        /// <summary>
        /// AssetDataBase 不支持异步加载
        /// </summary>
        /// <param name="objName"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int AsyncLoad<T>(string objName, Action<bool, T> callback)where T : UnityEngine.Object
        {
            this.TaskCounter++;
            var res = Load<T>(objName);
            callback(true, res);
            return this.TaskCounter;
        }




        /// <summary>
        /// assetdatabase 不支持异步，暂时先做个加载，后期用update模拟异步
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="onLoadComplete"></param>
        /// <param name="onLoadProcess"></param>
        /// <returns></returns>
        public List<int> AsyncLoad(IList<string> sources, Action<IDictionary<string, Object>> onLoadComplete,Action<int, int> onLoadProcess)
        {

            IDictionary<string ,UnityEngine.Object> map =new Dictionary<string, Object>();
            //每帧加载5个
            IEnumeratorTool.StartCoroutine(TaskUpdate( 5,sources, (s, o) =>
            {
                map[s] = o;
                //
                if (onLoadProcess != null)
                {
                    onLoadProcess(map.Count, sources.Count);
                }

                //
                if (map.Count == sources.Count)
                {
                    if (onLoadComplete != null)
                    {
                        onLoadComplete(map);
                    }
                }
            }));
            
            return new List<int>();
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
        public void LoadCalcelAll()
        {
            willdoTaskSet.Clear();
        }


        /// <summary>
        /// 任务帧
        /// </summary>
        /// <param name="loadNumPerFrame"></param>
        /// <param name="loads"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator TaskUpdate(int loadNumPerFrame,IList<string> loads ,Action<string,Object> callback)
        {
            int count = 0;
            while (count < loads.Count)
            {
                for (int  i = 0; count< loads.Count && i<loadNumPerFrame ; i++)
                {
                    var resPath = loads[count];

                    AsyncLoad<UnityEngine.Object>(resPath, (b, o) =>
                    {
                        callback(resPath, o);
                    });
                    count++;
                }
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
#endif