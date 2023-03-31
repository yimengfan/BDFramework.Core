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
        private List<LoadTaskGroup> allTaskList;

        /// <summary>
        /// 对象map
        /// </summary>
        Dictionary<string, UnityEngine.Object> objsCacheMap;


        public DevResourceMgr()
        {
            willdoTaskSet = new HashSet<int>();
            allTaskList = new List<LoadTaskGroup>();
            objsCacheMap = new Dictionary<string, UnityEngine.Object>();
        }


        //
        private List<string> allRuntimeDirectList = new List<string>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="rootPath"></param>
        public void Init(string rootPath, RuntimePlatform platform)
        {
            allRuntimeDirectList = BApplication.GetAllRuntimeDirects();
        }

        /// <summary>
        /// AssetBundle 
        /// </summary>
        public Dictionary<string, AssetLoder> AssetbundleCacheMap { get; set; }

        /// <summary>
        /// 寻找一个shader
        /// </summary>
        /// <param name="shaderName"></param>
        /// <returns></returns>
        public Shader FindShader(string shaderName)
        {
            return Shader.Find(shaderName);
        }

        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="assetLoadPath">资源名</param>
        /// <param name="type">指定类型</param>
        public void UnloadAsset(string assetLoadPath, Type type = null)
        {
            try
            {
                if (objsCacheMap.ContainsKey(assetLoadPath))
                {
                    var obj = objsCacheMap[assetLoadPath];
                    objsCacheMap.Remove(assetLoadPath);
                    //
                    //Resources.UnloadAsset(obj);
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
            objsCacheMap.Clear();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1)
        {
        }

        public T LoadFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object
        {
            return null;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="loadPathType"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            //guid转短路径

            if (loadPathType == LoadPathType.GUID)
            {
                loadPath = AssetDatabase.GUIDToAssetPath(loadPath);
            }


            return Load(typeof(T), loadPath) as T;
        }

        /// <summary>
        /// 加载
        /// Type版本
        /// </summary>
        /// <param name="type"></param>
        /// <param name="assetPatharam>
        /// <returns></returns>
        public Object Load(Type type, string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath)
        {
            //读取缓存
            var ret = objsCacheMap.TryGetValue(loadPath, out var retobj);
            //走新加载逻辑
            if (!ret)
            {
                string assetPath = null;
                //全路径
                if (loadPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = loadPath;
                }
                //短路径
                else
                {
                    var assetPaths = FindAssets(loadPath);
                    if (assetPaths == null)
                    {
                        return null;
                    }


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
                }

                retobj = AssetDatabase.LoadAssetAtPath(assetPath, type);
                objsCacheMap[loadPath] = retobj;
            }

            return retobj;
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
                BDebug.LogError("资源加载","未找到资源:" + path);

                return null;
            }

            // if (rets.Count > 1)
            // {
            //     foreach (var r in rets)
            //     {
            //         Debug.LogError("注意文件同名:" + r);
            //     }
            // }

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


        public LoadTaskGroup AsyncLoad<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : Object
        {
            return null;
        }


        /// <summary>
        /// AssetDataBase 不支持异步加载
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int AsyncLoad<T>(string loadPath, Action<T> callback, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            this.TaskCounter++;


            var res = Load<T>(loadPath);
            callback(res);


            return this.TaskCounter;
        }


        /// <summary>
        /// assetdatabase 不支持异步，暂时先做个加载，后期用update模拟异步
        /// </summary>
        /// <param name="loadPathList"></param>
        /// <param name="onLoadProcess"></param>
        /// <param name="onLoadComplete"></param>
        /// <returns></returns>
        public List<int> AsyncLoad(List<string> loadPathList, Action<int, int> onLoadProcess, Action<IDictionary<string, Object>> onLoadComplete, LoadPathType loadPathType = LoadPathType.RuntimePath)
        {
            //var list = assetsPath.Distinct().ToList();

            IDictionary<string, UnityEngine.Object> map = new Dictionary<string, Object>();
            int curProcess = 0;
            //每帧加载n个
            IEnumeratorTool.StartCoroutine(TaskUpdate(5, loadPathList, (s, o) =>
            {
                curProcess++;
                map[s] = o;
                //
                if (onLoadProcess != null)
                {
                    onLoadProcess(curProcess, loadPathList.Count);
                }

                //
                if (curProcess == loadPathList.Count)
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
        public bool LoadCancel(int taskid)
        {
            if (willdoTaskSet.Contains(taskid))
            {
                willdoTaskSet.Remove(taskid);
                return true;
            }

            return false;
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

                    AsyncLoad<UnityEngine.Object>(resPath, (o) =>
                    {
                        //异步加载回调
                        callback(resPath, o);
                    });
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
            if (index > 0)
            {
                var rs = fullAssetPath.Substring(index + RUNTIME_STR.Length).Split('.');
                return rs[0];
            }
            else
            {
                return fullAssetPath;
            }
        }
    }
}
#endif
