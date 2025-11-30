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
        /// <summary>
        /// 位于 */Runtime/xxxx 的路径
        /// </summary>
        RuntimePath,
        /// <summary>
        /// 通过guid加载
        /// </summary>
        GUID,
        /// <summary>
        /// 从Assets开始的路径
        /// </summary>
        AssetsPath,
    }

    public interface IResMgr
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="rootPath"></param>
        void Init(string rootPath, RuntimePlatform platform);

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <param name="loadPathType"></param>
        /// <param name="abName"></param>
        /// <returns></returns>
        T Load<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="assetPatharam>
        /// <returns></returns>
        UnityEngine.Object Load(Type type, string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath);

        /// <summary>
        /// 加载所有资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="abName"></param>
        /// <returns></returns>
        T[] LoadAll<T>(string path) where T : UnityEngine.Object;

        // <summary>
        /// 异步加载接口
        /// 未加载则返回LoadTask自行驱动，否则返回已加载的内容
        /// 一般作为Editor验证使用，不作为Runtime正式API
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath">api传入的加载路径,Runtime下的相对路径</param>
        /// <returns>返回Task</returns>
        LoadTaskGroup AsyncLoad<T>(string loadPath, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        int AsyncLoad<T>(string loadPath, Action<T> callback, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPathList"></param>
        /// <param name="onLoadProcess"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        List<int> AsyncLoad(List<string> loadPathList,
            Action<int, int> onLoadProcess,
            Action<IDictionary<string, Object>> onLoadEnd,LoadPathType pathType = LoadPathType.RuntimePath);

        /// <summary>
        /// 取消一个加载任务
        /// </summary>
        /// <param name="taskid"></param>
        bool LoadCancel(int taskid);

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

        /// <summary>
        ///  加载shader
        /// 传参跟Shader.Find一致
        /// </summary>
        /// <returns></returns>
        Shader FindShader(string shaderName);
    

        /// <summary>
        /// 卸载指定ab
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <param name="type"></param>
        void UnloadAsset(string assetLoadPath, Type type = null);

        /// <summary>
        /// 卸载所有ab
        /// </summary>
        void UnloadAllAsset();

        /// <summary>
        /// 设置加载配置
        /// </summary>
        /// <param name="maxLoadTaskNum"></param>
        /// <param name="maxUnloadTaskNum"></param>
        void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1);
    }
}
