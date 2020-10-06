#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using BDFramework.ResourceMgr.V2;
using Code.BDFramework.Core.Tools;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    // public class OnAssetInmpoter_DevResourceMgr : AssetPostprocessor
    // {
    //     /// <summary>
    //     /// 资源更新完的时候重新刷新下文件列表
    //     /// </summary>
    //     /// <param name="importedAssets"></param>
    //     /// <param name="deletedAssets"></param>
    //     /// <param name="movedAssets"></param>
    //     /// <param name="movedFromAssetPaths"></param>
    //     private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
    //                                                string[] movedAssets,    string[] movedFromAssetPaths)
    //     {
    //         //重新刷新下
    //         var mgr = BResources.ResLoader as DevResourceMgr;
    //         mgr?.Init("",null);
    //     }
    // }


    /// <summary>
    /// ab包管理器
    /// </summary>
    public class DevResourceMgr : IResMgr
    {
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
        private List<LoaderTaskGroup> allTaskList;

        /// <summary>
        /// 对象map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objsMap;


        public DevResourceMgr()
        {
            willdoTaskSet = new HashSet<int>();
            allTaskList   = new List<LoaderTaskGroup>();
            objsMap       = new Dictionary<string, UnityEngine.Object>();
        }
        
        
        //
        private List<string> allRuntimeDirectList = new List<string>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="path"></param>
        public void Init(string path)
        {
            allRuntimeDirectList = BDApplication.GetAllRuntimeDirects();
        }

        /// <summary>
        /// AssetBundle 
        /// </summary>
        public Dictionary<string, AssetBundleWapper> AssetbundleMap { get; set; }
        
        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isForceUnload"></param>
        public void UnloadAsset(string path, bool isForceUnload = false)
        {
            try
            {
                if (objsMap.ContainsKey(path))
                {
                    var obj = objsMap[path];
                    //
                    objsMap.Remove(path);
                    Resources.UnloadUnusedAssets();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 卸载所有
        /// </summary>
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

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (objsMap.ContainsKey(path))
            {
                return objsMap[path] as T;
            }
            else
            {
                var rets = FindAssets(path);
                if (rets == null)
                {
                    return null;
                }
                var resPath = rets[0];
                objsMap[path] = AssetDatabase.LoadAssetAtPath<T>(resPath);
                return objsMap[path] as T;
            }
        }


        /// <summary>
        /// 寻找资源
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> FindAssets(string path)
        {

            List<string > rets =new List<string>();
            //每个文件下判断
            foreach (var direct in this.allRuntimeDirectList)
            {
                var filePath = IPath.Combine(direct, path);
                var filename = Path.GetFileName(filePath);
                var fileDierct = Path.GetDirectoryName(filePath);
                //
                if (!Directory.Exists(fileDierct)) continue;
                //
                var res = Directory.GetFiles(fileDierct, filename + ".*", SearchOption.TopDirectoryOnly)
                    .Where((r)=>
                               !r.EndsWith(".meta"));
                rets.AddRange(res);
            }
            
            if (rets.Count == 0)
            {
                Debug.LogError("未找到资源:" + path);

                return null;
            }

            if (rets.Count > 1)
            {
                foreach (var r in rets)
                {
                    Debug.LogError("注意文件同名:" + r);
                }
            }

            return rets;
        }

        /// <summary>
        /// 加载所有
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T[] LoadAll_TestAPI_2020_5_23<T>(string path) where T : Object
        {
            var rets = FindAssets(path);
            var objs = AssetDatabase.LoadAllAssetsAtPath(rets[0]);

            List<T> list = new List<T>();
            foreach (var obj in objs)
            {
                if (obj is T)
                {
                    list.Add(obj as T);
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// AssetDataBase 不支持异步加载
        /// </summary>
        /// <param name="objName"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int AsyncLoad<T>(string objName, Action<T> callback) where T : UnityEngine.Object
        {
            this.TaskCounter++;
            if (objsMap.ContainsKey(objName))
            {
                callback(objsMap[objName] as T);
            }
            else
            {
                var res = Load<T>(objName);
                callback(res);
            }

            return this.TaskCounter;
        }


        /// <summary>
        /// assetdatabase 不支持异步，暂时先做个加载，后期用update模拟异步
        /// </summary>
        /// <param name="assetsPath"></param>
        /// <param name="onLoadComplete"></param>
        /// <param name="onLoadProcess"></param>
        /// <returns></returns>
        public List<int> AsyncLoad(IList<string>    assetsPath, Action<IDictionary<string, Object>> onLoadComplete,
                                   Action<int, int> onLoadProcess)
        {
            //var list = assetsPath.Distinct().ToList();

            IDictionary<string, UnityEngine.Object> map        = new Dictionary<string, Object>();
            int                                     curProcess = 0;
            //每帧加载1个
            IEnumeratorTool.StartCoroutine(TaskUpdate(1, assetsPath, (s, o) =>
            {
                curProcess++;
                map[s] = o;
                //
                if (onLoadProcess != null)
                {
                    onLoadProcess(curProcess, assetsPath.Count);
                }

                //
                if (curProcess == assetsPath.Count)
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
        public void LoadCancelAll()
        {
            willdoTaskSet.Clear();
        }

        /// <summary>
        /// 获取符合条件的资源名
        /// </summary>
        /// <param name="floder"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string[] GetAssets(string floder, string searchPattern = null)
        {
            //判断是否存在这个目录
           
            List<string > rets =new List<string>();
            //每个文件下判断
            foreach (var direct in this.allRuntimeDirectList)
            {
                var fileDierct = IPath.Combine(direct, floder);
                //
                if (!Directory.Exists(fileDierct)) continue;
                //
                var res = Directory.GetFiles(fileDierct, "*.*", SearchOption.TopDirectoryOnly)
                    .Where((r)=>
                               !r.EndsWith(".meta"));
                rets.AddRange(res);
            }
            //
            var splitStr = "/Runtime/";
            for (int i = 0; i < rets.Count; i++)
            {
                var r     = rets[i].Replace("\\","/");
                var index = r.IndexOf(splitStr);
                var rs    = r.Substring(index + splitStr.Length).Split('.');
                rets[i] = rs[0];
            }

            //寻找符合条件的
            if (!string.IsNullOrEmpty(searchPattern))
            {
                rets = rets.FindAll((r) =>
                {
                    var fileName = Path.GetFileName(r);

                    if (fileName.StartsWith(searchPattern))
                    {
                        return true;
                    }

                    return false;
                });
            }


            return rets.ToArray();
        }


        /// <summary>
        /// 任务帧
        /// </summary>
        /// <param name="loadNumPerFrame"></param>
        /// <param name="loads"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator TaskUpdate(int loadNumPerFrame, IList<string> loads, Action<string, Object> callback)
        {
            int count = 0;
            while (count < loads.Count)
            {
                for (int i = 0; count < loads.Count && i < loadNumPerFrame; i++)
                {
                    var resPath = loads[count];

                    AsyncLoad<UnityEngine.Object>(resPath, (o) => { callback(resPath, o); });
                    count++;
                }

                yield return new WaitForEndOfFrame();
            }
        }
    }
}
#endif