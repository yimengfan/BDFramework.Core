using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using BDFramework.Core.Tools;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// 加载资源的返回状态
    /// </summary>
    public enum LoadAssetState
    {
        Success = 0,
        Fail,
        IsLoding,
    }


    /// <summary>
    /// ab包管理器
    /// </summary>
    public class AssetBundleMgrV2 : IResMgr
    {
        /// <summary>
        /// 特殊的前缀
        /// </summary>
        static readonly public string RUNTIME = "runtime/{0}";


        /// <summary>
        /// 全局的任务id
        /// </summary>
        private int taskIDCounter;

        /// <summary>
        /// 异步回调表
        /// </summary>
        private List<LoaderTaskGroup> allTaskGroupList;

        /// <summary>
        /// 全局唯一的依赖
        /// </summary>
        private ManifestLoder loder;

        /// <summary>
        /// 全局的assetbundle字典
        /// </summary>
        public Dictionary<string, AssetBundleWapper> AssetbundleMap { get; private set; }

        /// <summary>
        /// 资源加载路径
        /// </summary>
        private string firstArtDirectory = "";

        //第二寻址路径
        private string secArtDirectory = "";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="path"></param>
        public void Init(string path)
        {
            //多热更切换,需要卸载
            if (this.loder != null)
            {
                this.UnloadAllAsset();
                GC.Collect();
            }

            this.AssetbundleMap = new Dictionary<string, AssetBundleWapper>();
            this.allTaskGroupList = new List<LoaderTaskGroup>();
            //1.设置加载路径  
            firstArtDirectory = string.Format("{0}/{1}/Art", path, BDApplication.GetPlatformPath(Application.platform))
                .Replace("\\", "/");
            //当路径为persistent时，第二路径生效
            secArtDirectory = string.Format("{0}/{1}/Art", Application.streamingAssetsPath,
                    BDApplication.GetPlatformPath(Application.platform)) //
                .Replace("\\", "/");

            //加载Config
            var configPath = "";
            this.loder = new ManifestLoder();
            if (Application.isEditor)
            {
                configPath = string.Format("{0}/{1}/{2}", path, BDApplication.GetPlatformPath(Application.platform),
                    BResources.CONFIGPATH);
            }
            else
            {
                //真机环境config在persistent，跟dll和db保持一致
                configPath = string.Format("{0}/{1}/{2}", Application.persistentDataPath,
                    BDApplication.GetPlatformPath(Application.platform), BResources.CONFIGPATH);
            }

            this.loder.Load(configPath);
        }


        #region 对外加载接口

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="fullPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (!this.loder.Manifest.IsHashName)
            {
                path = string.Format(RUNTIME, path.ToLower());
            }
            else
            {
                path = path.ToLower();
            }

            //1.依赖路径
            var item = loder.Manifest.GetManifest(path);
            if (item != null)
            {
                //加载依赖
                foreach (var dependAsset in item.Depend)
                {
                    LoadAssetBundle(dependAsset);
                }
                //加载主资源

                LoadAssetBundle(item.Path);
                //
                return LoadFormAssetBundle<T>(path, item);
            }

            return null;
        }

        /// <summary>
        /// load ALL TestAPI
        /// 这个有一定局限性，这里是返回某个Ab中的所有资源
        /// 简单处理一些简单资源情况：目前只解决图集
        /// 仅作为某些项目补坑用
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T[] LoadAll_TestAPI_2020_5_23<T>(string path) where T : Object
        {
            if (!this.loder.Manifest.IsHashName)
            {
                path = string.Format(RUNTIME, path.ToLower());
            }
            else
            {
                path = path.ToLower();
            }

            var item = loder.Manifest.GetManifest(path);
            //加载assetbundle
            AssetBundle ab = LoadAssetBundle(item.Path);

            if (ab != null)
            {
                var assetNames = ab.GetAllAssetNames();
                string relname = "";
                if (assetNames.Length == 1)
                {
                    relname = assetNames[0];
                }
                else
                {
                    var f = path + ".";
                    relname = assetNames.First((s) => s.Contains(f));
                }

                return ab.LoadAssetWithSubAssets<T>(relname);
            }

            return null;
        }


        /// <summary>
        /// 异步加载接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public int AsyncLoad<T>(string assetName, Action<T> callback) where T : UnityEngine.Object
        {
            if (!this.loder.Manifest.IsHashName)
            {
                assetName = string.Format(RUNTIME, assetName.ToLower());
            }
            else
            {
                assetName = assetName.ToLower();
            }


            List<LoaderTaskData> taskQueue = new List<LoaderTaskData>();
            //获取依赖
            var mainItem = loder.Manifest.GetManifest(assetName);
            if (mainItem != null)
            {
                //依赖资源
                foreach (var r in mainItem.Depend)
                {
                    var task = new LoaderTaskData(r, typeof(Object));
                    taskQueue.Add(task);
                }
                //主资源，
                var mainTask = new LoaderTaskData(mainItem.Path, typeof(Object), true);
                taskQueue.Add(mainTask);
                //添加任务组
                var taskGroup = new LoaderTaskGroup(this, assetName, mainItem, taskQueue, //Loader接口
                (p, obj) =>
                {
                    //完成回调
                    callback(obj as T);
                });
                taskGroup.Id = this.taskIDCounter++;
                AddTaskGroup(taskGroup);

                //开始任务
                DoNextTask();
                return taskGroup.Id;
            }
            else
            {
                BDebug.LogError("资源不存在:" + assetName);
            }

            return -1;
        }


        /// <summary>
        /// 异步加载 多个
        /// </summary>
        /// <param name="assetNameList">资源</param>
        /// <param name="onLoadProcess">进度</param>
        /// <param name="onLoadComplete">加载结束</param>
        /// <returns>taskid</returns>
        public List<int> AsyncLoad(List<string> assetNameList,
            Action<int, int> onLoadProcess,
            Action<IDictionary<string, Object>> onLoadComplete)
        {
            var taskIdList = new List<int>();
            int taskCounter = 0;
            var loadAssetMap = new Dictionary<string, Object>();
            assetNameList = assetNameList.Distinct().ToList(); //去重
            int total = assetNameList.Count;
            //source
            foreach (var assetName in assetNameList)
            {
                var taskid =  AsyncLoad<Object>(assetName, (o) =>
                {
                    loadAssetMap[assetName] = o;
                    //进度回调
                    onLoadProcess?.Invoke(loadAssetMap.Count,total);
                    //完成回调
                    if (loadAssetMap.Count == total)
                    {
                        onLoadComplete?.Invoke(loadAssetMap);
                    }

                });

                taskIdList.Add(taskid);
            }

            //开始任务
            DoNextTask();
            //
            return taskIdList;
        }


        /// <summary>
        /// 添加一个任务组
        /// </summary>
        /// <param name="taskGroup"></param>
        public void AddTaskGroup(LoaderTaskGroup taskGroup)
        {
            this.allTaskGroupList.Add(taskGroup);
        }

        /// <summary>
        /// 寻找可加载的地址
        /// </summary>
        /// <param name="assetFileName"></param>
        /// <returns></returns>
        public string FindAsset(string assetFileName)
        {
            //第一地址
            var p = IPath.Combine(this.firstArtDirectory, assetFileName);
            //寻址到第二路径,第二地址没有就放弃
            if (!File.Exists(p))
            {
                p = IPath.Combine(this.secArtDirectory, assetFileName);
            }

            return p;
        }

        #endregion

        #region 加载AssetsBundle

        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundle(string path)
        {
            if (AssetbundleMap.ContainsKey(path))
            {
                AssetBundleWapper abw = null;
                if (AssetbundleMap.TryGetValue(path, out abw))
                {
                    abw.Use();
                    return abw.AssetBundle;
                }
            }
            else
            {
                var p = FindAsset(path);
                var ab = AssetBundle.LoadFromFile(p);
                //添加
                AddAssetBundle(path, ab);
                return ab;
            }

            return null;
        }


        /// <summary>
        /// ab包计数器
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="ab"></param>
        public void AddAssetBundle(string assetPath, AssetBundle ab)
        {
            //
            if (!AssetbundleMap.ContainsKey(assetPath))
            {
                AssetBundleWapper abr = new AssetBundleWapper() {AssetBundle = ab};
                AssetbundleMap[assetPath] = abr;
            }

            AssetbundleMap[assetPath].Use();
        }

        #endregion

        #region 从AB中加载资源

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        public T LoadFormAssetBundle<T>(string assetName, ManifestItem item) where T : UnityEngine.Object
        {
            if (item != null)
            {
                return LoadFormAssetBundle(assetName, item, typeof(T)) as T;
            }

            BDebug.LogError("不存在:" + assetName);
            return null;
        }


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        private Object LoadFormAssetBundle(string assetName, ManifestItem item, Type t)
        {
            Object o = null;
            AssetBundleWapper abr = null;
            if (AssetbundleMap.TryGetValue(item.Path, out abr))
            {
                switch ((ManifestItem.AssetTypeEnum) item.Type)
                {
                    //暂时需要特殊处理的只有一个
                    case ManifestItem.AssetTypeEnum.SpriteAtlas:
                    {
                        o = abr.LoadTextureFormAtlas(assetName);
                    }
                        break;
                    case ManifestItem.AssetTypeEnum.Prefab:
                    case ManifestItem.AssetTypeEnum.Texture:
                    case ManifestItem.AssetTypeEnum.Others:
                    default:
                    {
                        o = abr.LoadAsset(assetName, t);
                    }
                        break;
                }
            }
            else
            {
                BDebug.Log("资源不存在:" + assetName + " - " + item.Path, "red");

                return null;
            }

            return o;
        }

        #endregion

        #region 取消加载任务

        /// <summary>
        /// 取消load任务
        /// </summary>
        /// <param name="taskid"></param>
        public void LoadCancel(int taskid)
        {
            foreach (var tg in allTaskGroupList)
            {
                if (tg.Id == taskid)
                {
                    tg.Stop();
                    allTaskGroupList.Remove(tg);
                    break;
                }
            }
        }


        /// <summary>
        /// 取消所有load任务
        /// </summary>
        public void LoadAllCancel()
        {
            foreach (var tg in allTaskGroupList)
            {
                tg.Stop();
            }

            this.allTaskGroupList.Clear();
        }

        /// <summary>
        /// 获取路径下所有资源
        /// </summary>
        /// <param name="floder"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public string[] GetAssets(string floder, string searchPattern = null)
        {
            List<string> rets = new List<string>();
            string str;

            if (this.loder.Manifest.IsHashName)
            {
                str = (floder + "/").ToLower();
            }
            else
            {
                str = string.Format(RUNTIME, (floder + "/").ToLower());
            }
               


            searchPattern = searchPattern?.ToLower();
            foreach (var key in this.loder.Manifest.ManifestMap.Keys)
            {
                if (key.StartsWith(str))
                {
                    rets.Add(key);
                }
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

            if (!this.loder.Manifest.IsHashName)
            {
                var count = "runtime/".Length;
                for (int i = 0; i < rets.Count; i++)
                {
                    rets[i] = rets[i].Substring(count);
                }
            }


            return rets.ToArray();
        }

        #endregion

        #region 工具类

        #endregion

        #region 核心任务驱动

        /// <summary>
        /// 当前执行的任务组
        /// </summary>
        private LoaderTaskGroup curDoTask = null;

        /// <summary>
        /// 核心功能,所有任务靠这个推进度
        /// 执行下个任务
        /// </summary>
        void DoNextTask()
        {
            if (this.allTaskGroupList.Count == 0)
            {
                return;
            }

            //当前任务组执行完毕，执行下一个
            if ((curDoTask == null || curDoTask.IsComplete) && this.allTaskGroupList.Count > 0)
            {
                curDoTask = this.allTaskGroupList[0];
                this.allTaskGroupList.RemoveAt(0);
                //开始task
                curDoTask.DoNextTask();
                //注册完成回调
                curDoTask.OnAllTaskCompleteCallback += (a, b) =>
                {
                    //
                    DoNextTask();
                };
            }
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载
        /// 废弃接口，现在ab管理只需要 使用者管理好实例化出来的资源即可
        /// AB本身只有一个头的消耗
        /// </summary>
        /// <param name="path"></param>
        [Obsolete]
        public void UnloadAsset(string path, bool isForceUnload = false)
        {
            
        }


        /// <summary>
        /// 卸载
        /// 废弃接口，现在ab管理只需要 使用者管理好实例化出来的资源即可
        /// AB本身只有一个头的消耗
        /// </summary>
        /// <param name="path"></param>
        [Obsolete]
        public void UnloadAllAsset()
        {
            
        }

        #endregion
    }
}