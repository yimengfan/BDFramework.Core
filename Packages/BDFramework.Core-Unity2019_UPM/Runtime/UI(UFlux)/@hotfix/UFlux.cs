using System;
using BDFramework.ResourceMgr;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.UFlux
{
    static public partial class UFlux
    {
        /// <summary>
        /// 设置ComponentData
        /// </summary>
        /// <param name="t"></param>
        /// <param name="aState"></param>
        static public void SetComponentValue(Transform t ,AStateBase aState)
        {
            ComponentValueBindManager.Inst.SetComponentValue(t,aState);
        }

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        static public T Load<T>(string path) where T : Object
        {
            return BResources.Load<T>(path);
        }
        
        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        static public void AsyncLoad<T>(string path,Action<T> callback) where T : Object
        {
             BResources.AsyncLoad<T>(path,callback);
        }

        /// <summary>
        /// 删除接口
        /// </summary>
        /// <param name="go"></param>
        static public void Destroy(GameObject go)
        {
            BResources.Destroy(go);
        }

        /// <summary>
        /// 卸载，ab中需要
        /// </summary>
        /// <param name="path"></param>
        static public void Unload(string path)
        {
            BResources.UnloadAsset(path);
        }
    }
}