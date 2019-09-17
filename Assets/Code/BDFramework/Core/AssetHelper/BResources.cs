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
        static public void Load(string root = "", Action onLoaded = null)
        {
            if (root != "")
            {
                resLoader = new AssetBundleMgr(root, onLoaded);
            }
            else
            {
#if UNITY_EDITOR

                resLoader = new DevResourceMgr();
                if (onLoaded != null)
                    onLoaded();
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
        public static int AsyncLoad<T>(string objName, Action<T> action) where T : UnityEngine.Object
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
        public static void UnloadAsset(string path, bool isForceUnload = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            resLoader.UnloadAsset(path, isForceUnload);
        }
        
        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="asset"></param>
        public static  void UnloadAsset(UnityEngine.Object asset)
        {
            
            if(asset is GameObject ||  asset is Component) return;
            Resources.UnloadAsset(asset);
            
        }

        /// <summary>
        /// 卸载所有的
        /// </summary>
        public static void UnloadAll()
        {
        }


        /// <summary>
        /// 删除接口
        /// </summary>
        /// <param name="trans"></param>
        public static void Destroy(Transform trans)
        {
            if (trans)
                GameObject.DestroyObject(trans.gameObject);
        }

        /// <summary>
        /// 删除接口
        /// </summary>
        /// <param name="go"></param>
        public static void Destroy(GameObject go)
        {
            if (go)
            {
                GameObject.DestroyObject(go);
                go = null;
            }
        }

        /// <summary>
        /// 取消单个任务
        /// </summary>
        public static void LoadCancel(int id)
        {
            resLoader.LoadCancel(id);
        }

        /// <summary>
        /// 取消单个任务
        /// </summary>
        public static void LoadCancel(List<int> ids)
        {
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    resLoader.LoadCancel(id);
                }
            }

         
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