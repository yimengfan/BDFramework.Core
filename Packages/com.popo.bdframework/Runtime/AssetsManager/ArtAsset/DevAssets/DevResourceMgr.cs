#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using BDFramework.ResourceMgr.V2;
using BDFramework.Core.Tools;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 开发模式下的加载
    /// </summary>
    public class DevResourceMgr : IResMgr
    {
        static string RUNTIME_STR = "/Runtime/";

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
        private List<AsyncLoadTaskGroup> allTaskList;

        /// <summary>
        /// 对象map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objsMap;


        public DevResourceMgr()
        {
            willdoTaskSet = new HashSet<int>();
            allTaskList = new List<AsyncLoadTaskGroup>();
            objsMap = new Dictionary<string, UnityEngine.Object>();
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
        /// <param name="assetName"></param>
        /// <param name="isForceUnload"></param>
        public void UnloadAsset(string assetName, bool isForceUnload = false)
        {
            try
            {
                if (objsMap.ContainsKey(assetName))
                {
                    var obj = objsMap[assetName];
                    //
                    objsMap.Remove(assetName);
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

        private Dictionary<string, string> guidPathChacheMap = new Dictionary<string, string>();

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathType"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            //guid转短路径

            if (pathType == LoadPathType.GUID)
            {
                var ret = guidPathChacheMap.TryGetValue(path, out var guidpath);
                if (!ret)
                {
                    guidpath = AssetDatabase.GUIDToAssetPath(path);
                    path = FullAssetPathToRuntimePath(guidpath);
                }
            }


            return Load(typeof(T), path) as T;
        }

        /// <summary>
        /// 加载
        /// Type版本
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public Object Load(Type type, string path)
        {
            var assetPaths = FindAssets(path);
            if (assetPaths == null)
            {
                return null;
            }

            string assetPath = null;
            if (assetPaths.Count == 1)
            {
                assetPath = assetPaths[0];
            }
            else
            {
                //这里是有同名文件依次匹配类型
                foreach (var p in assetPaths)
                {
                    var assetType = AssetDatabase.GetMainAssetTypeAtPath(p);
                    if (type == assetType)
                    {
                        assetPath = p;
                        break;
                    }
                }
            }

            var obj = AssetDatabase.LoadAssetAtPath(assetPath, type);
            objsMap[path] = obj;
            return obj;
        }


        /// <summary>
        /// 寻找资源
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> FindAssets(string path)
        {
            List<string> rets = new List<string>();
            //每个文件下判断
            foreach (var direct in this.allRuntimeDirectList)
            {
                var filePath = IPath.Combine(direct, path);
                var filename = Path.GetFileName(filePath);
                var fileDierct = Path.GetDirectoryName(filePath);
                //
                if (!Directory.Exists(fileDierct))
                {
                    continue;
                }

                //
                var res = Directory.GetFiles(fileDierct, filename + ".*", SearchOption.TopDirectoryOnly).Where((r) => !r.EndsWith(".meta"));
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
        public T[] LoadAll<T>(string path) where T : Object
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
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int AsyncLoad<T>(string assetName, Action<T> callback) where T : UnityEngine.Object
        {
            this.TaskCounter++;
            if (objsMap.ContainsKey(assetName))
            {
                callback(objsMap[assetName] as T);
            }
            else
            {
                var res = Load<T>(assetName);
                callback(res);
            }

            return this.TaskCounter;
        }


        /// <summary>
        /// assetdatabase 不支持异步，暂时先做个加载，后期用update模拟异步
        /// </summary>
        /// <param name="assetNameList"></param>
        /// <param name="onLoadProcess"></param>
        /// <param name="onLoadComplete"></param>
        /// <returns></returns>
        public List<int> AsyncLoad(List<string> assetNameList, Action<int, int> onLoadProcess, Action<IDictionary<string, Object>> onLoadComplete)
        {
            //var list = assetsPath.Distinct().ToList();

            IDictionary<string, UnityEngine.Object> map = new Dictionary<string, Object>();
            int curProcess = 0;
            //每帧加载1个
            IEnumeratorTool.StartCoroutine(TaskUpdate(1, assetNameList, (s, o) =>
            {
                curProcess++;
                map[s] = o;
                //
                if (onLoadProcess != null)
                {
                    onLoadProcess(curProcess, assetNameList.Count);
                }

                //
                if (curProcess == assetNameList.Count)
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
        public void LoadAllCancel()
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

            List<string> rets = new List<string>();
            //每个文件下判断
            foreach (var direct in this.allRuntimeDirectList)
            {
                var fileDierct = IPath.Combine(direct, floder);
                //
                if (!Directory.Exists(fileDierct)) continue;
                //
                var res = Directory.GetFiles(fileDierct, "*.*", SearchOption.TopDirectoryOnly).Where((r) => !r.EndsWith(".meta"));
                rets.AddRange(res);
            }

            //
            for (int i = 0; i < rets.Count; i++)
            {
                rets[i] = FullAssetPathToRuntimePath(rets[i]);
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
        /// 预热shader
        /// </summary>
        public void WarmUpShaders()
        {
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
        
        /// <summary>
        /// 全路径转 runtime短路径
        /// </summary>
        /// <returns></returns>
        public static string FullAssetPathToRuntimePath(string fullAssetPath)
        {
            fullAssetPath = fullAssetPath.Replace("\\", "/");
            var index = fullAssetPath.IndexOf(RUNTIME_STR);
            var rs = fullAssetPath.Substring(index + RUNTIME_STR.Length).Split('.');
            return rs[0];
        }


    }
}
#endif