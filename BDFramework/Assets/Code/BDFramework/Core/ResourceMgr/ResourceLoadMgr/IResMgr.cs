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
        /// 资源管理
        /// </summary>
        Dictionary<string, AssetBundleReference> AssetbundleMap { get; set; }
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
        int AsyncLoad<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProcess"></param>
        /// <returns></returns>
        int AsyncLoad(IList<string> sources, Action<IDictionary<string, Object>> onLoadEnd,
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
