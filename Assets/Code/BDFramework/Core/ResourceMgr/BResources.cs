using UnityEngine;
using BDFramework.ResourceMgr;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace BDFramework.ResourceMgr

{
    static public class BResources
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="abModel"></param>
        /// <param name="callback"></param>
        static public void Load(string root = "")
        {
            if (root != "")
            {
                resLoader = new AssetBundleMgr(root);
            }
            else
            {
#if UNITY_EDITOR
                
                resLoader = new DevResourceMgr();
                BDebug.Log("资源加载:AssetDataBase editor only");
#endif
            }
        }

        static private IResMgr resLoader { get; set; }


        /// <summary>
        /// 同步加载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T Load<T>(string name) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return resLoader.Load<T>(name);
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="objName">名称</param>
        /// <param name="action">回调函数</param>
        public static int AsyncLoad<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object
        {
            return resLoader.AsyncLoad<T>(objName, action);
        }

        /// <summary>
        /// 批量加载
        /// </summary>
        /// <param name="objlist"></param>
        /// <param name="onLoadEnd"></param>
        public static List<int> AsyncLoad(IList<string> objlist, Action<int, int> onProcess = null,
            Action<IDictionary<string, UnityEngine.Object>> onLoadEnd = null)
        {
            return resLoader.AsyncLoad(objlist, onLoadEnd, onProcess);
        }

        /// <summary>
        /// 卸载某个gameobj
        /// </summary>
        /// <param name="o"></param>
        public static void UnloadAsset(string path, bool isUnloadIsUsing = false)
        {
            if (string.IsNullOrEmpty(path))
                return;
            resLoader.UnloadAsset(path, isUnloadIsUsing);
        }

        /// <summary>
        /// 卸载所有的
        /// </summary>
        public static void UnloadAll()
        {
          
        }


        public static void Destroy(Transform trans)
        {
            if (trans)
                GameObject.DestroyObject(trans.gameObject);
        }
        
        public static void Destroy(GameObject go)
        {
            if (go)
                GameObject.DestroyObject(go);
        }

        /// <summary>
        /// 取消单个任务
        /// </summary>
        public static void LoadCancel(int id)
        {
            resLoader.LoadCancel(id);
        }


        /// <summary>
        /// 取消所有任务
        /// </summary>
        public static void LoadCancel()
        {
            resLoader.LoadCalcelAll();
        }
    }
}