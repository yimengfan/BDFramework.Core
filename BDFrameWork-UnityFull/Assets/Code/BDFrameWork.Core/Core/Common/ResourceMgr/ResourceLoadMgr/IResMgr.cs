using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    public interface IResMgr
    {

        /// <summary>
        /// 本地加载根目录
        /// </summary>
        string LocalHotUpdateResPath { get; set; }
        /// <summary>
        /// 资源管理
        /// </summary>
        Dictionary<string, AssetBundleReference> AssetbundleMap { get; set; }

        /// <summary>
        /// 加载全局的依赖文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        void AsyncLoadManifest(string path,Action<bool> callback);
        /// <summary>
        /// 加载ab
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sucessCallback"></param>
        void AsyncLoadAssetBundle(string path,Action<bool>sucessCallback);
        /// <summary>
        /// 同步加载ab
        /// </summary>
        /// <param name="path"></param>
        void LoadAssetBundle(string path);
        /// <summary>
        /// 卸载指定ab
        /// </summary>
        /// <param name="name"></param>
        void UnloadAsset(string name ,bool isUnloadIsUsing =false);

        /// <summary>
        /// 卸载所有ab
        /// </summary>
        void UnloadAllAsset();

        /// <summary>
        /// 从ab加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        T LoadFormAssetBundle<T>(string abName, string objName) where T : UnityEngine.Object;
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abName"></param>
        /// <param name="objName"></param>
        /// <returns></returns>
        T Load<T>(string objName) where T : UnityEngine.Object;
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        int AsyncLoadSource<T>(string objName, Action<bool, T> action, bool isCreateTaskid = true) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProcess"></param>
        /// <returns></returns>
        int AsyncLoadSources(IList<string> sources, Action<IDictionary<string, Object>> onLoadEnd,
            Action<int, int> onProcess);

        /// <summary>
        /// 取消一个加载任务
        /// </summary>
        /// <param name="taskid"></param>
        void  LoadCancel(int taskid);
        /// <summary>
        /// 取消所有加载任务
        /// </summary>
        void LoadAllCalcel();

        /// <summary>
        /// 帧循环
        /// </summary>
        void Update();
         
    }
}
