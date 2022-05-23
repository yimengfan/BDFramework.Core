using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BDFramework.ResourceMgr.V2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// load path传参类型
    /// </summary>
    public enum LoadPathType
    {
        RuntimePath,
        GUID
    }
    public interface IResMgr
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="rootPath"></param>
        void Init(string rootPath);

        /// <summary>
        /// 资源管理
        /// </summary>
       // Dictionary<string, AssetBundleWapper> AssetbundleCacheMap { get; }

        /// <summary>
        /// 卸载指定ab
        /// </summary>
        /// <param name="assetPath"></param>
        void UnloadAsset(string assetPath, bool isForceUnload = false);

        /// <summary>
        /// 卸载所有ab
        /// </summary>
        void UnloadAllAsset();


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="pathType"></param>
        /// <param name="abName"></param>
        /// <returns></returns>
        T Load<T>(string path, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="assetPatharam>
        /// <returns></returns>
        UnityEngine.Object Load(Type type, string assetPath);

        /// <summary>
        /// 加载所有资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="abName"></param>
        /// <returns></returns>
        T[] LoadAll<T>(string path) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载接口
        /// 需要自行外部yield,这里不进行管理 防止逻辑冲突
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <returns>异步任务id</returns>
        LoadTaskGroup CreateAsyncLoadTask<T>(string assetName) where T : UnityEngine.Object;
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        int AsyncLoad<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPathList"></param>
        /// <param name="onLoadProcess"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        List<int> AsyncLoad(List<string> assetPathList,
            Action<int, int> onLoadProcess,
            Action<IDictionary<string, Object>> onLoadEnd);

        /// <summary>
        /// 取消一个加载任务
        /// </summary>
        /// <param name="taskid"></param>
        void LoadCancel(int taskid);

        /// <summary>
        /// 取消所有加载任务
        /// </summary>
        void LoadAllCancel();

        /// <summary>
        /// 获取某个目录下文件
        /// 以runtime为根目录
        /// </summary>
        string[] GetAssets(string floder, string searchPattern = null);

        /// <summary>
        /// 预热sharder
        /// </summary>
        void WarmUpShaders();
    }
}
