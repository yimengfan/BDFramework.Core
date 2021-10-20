using UnityEngine;
using BDFramework.ResourceMgr;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr.V2;
using BDFramework.Core.Tools;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace BDFramework.ResourceMgr

{
    /// <summary>
    /// 资源管理类
    /// </summary>
    static public class BResources
    {
        /// <summary>
        /// 美术根目录
        /// </summary>
        readonly static public string ASSET_ROOT_PATH = "Art";
        /// <summary>
        /// 美术config配置
        /// </summary>
        readonly static public string ASSET_CONFIG_PATH = ASSET_ROOT_PATH + "/Config.Info";
        /// <summary>
        /// 资源信息
        /// </summary>
        readonly static public string ASSET_TYPE_PATH = ASSET_ROOT_PATH + "/AssetTypeConfig.Info";
        /// <summary>
        /// 构建时的信息
        /// </summary>
        readonly static public string ASSET_BUILD_INFO_PATH = ASSET_ROOT_PATH + "/Build.Info";
        readonly static public string ASSET_OLD_BUILD_INFO_PATH = ASSET_ROOT_PATH + "/OldBuild.Info";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="abModel"></param>
        /// <param name="callback"></param>
        static public void Load(AssetLoadPath loadPath, string customRoot = null)
        {
            if (loadPath == AssetLoadPath.Editor)
            {
#if UNITY_EDITOR //防止编译报错
                ResLoader = new DevResourceMgr();
                ResLoader.Init("");
#endif
            }
            else
            {
                var path = "";
                if (Application.isEditor)
                {
                    if (!string.IsNullOrEmpty(customRoot))
                    {
                        path = customRoot;
                    }
                    else
                    {
                        if (loadPath == AssetLoadPath.Persistent)
                        {
                            path = Application.persistentDataPath;
                        }
                        else if (loadPath == AssetLoadPath.StreamingAsset)
                        {
                            path = Application.streamingAssetsPath;
                        }
                    }
                }
                else
                {
                    //真机环境config在persistent，跟dll和db保持一致
                    path = Application.persistentDataPath;
                }

                //
                ResLoader = new AssetBundleMgrV2();
                ResLoader.Init(path);
            }
        }

        /// <summary>
        /// 加载器
        /// </summary>
        static public IResMgr ResLoader { get; private set; }
        
        #region 加载、取消加载

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
            return ResLoader.Load<T>(name);
        }


        /// <summary>
        /// 同步加载ALL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T[] LoadALL<T>(string name) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return ResLoader.LoadAll_TestAPI_2020_5_23<T>(name);
        }


        /// <summary>
        /// 异步加载
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="objName">名称</param>
        /// <param name="action">回调函数</param>
        public static int AsyncLoad<T>(string objName, Action<T> action) where T : UnityEngine.Object
        {
            return ResLoader.AsyncLoad<T>(objName, action);
        }

        /// <summary>
        /// 批量加载
        /// </summary>
        /// <param name="objlist"></param>
        /// <param name="onLoadEnd"></param>
        public static List<int> AsyncLoad(List<string> objlist,
            Action<int, int> onProcess = null,
            Action<IDictionary<string, UnityEngine.Object>> onLoadEnd = null)
        {
            return ResLoader.AsyncLoad(objlist, onProcess, onLoadEnd);
        }


        /// <summary>
        /// 取消单个任务
        /// </summary>
        public static void LoadCancel(int id)
        {
            ResLoader.LoadCancel(id);
        }

        /// <summary>
        /// 取消一组任务
        /// </summary>
        public static void LoadCancel(List<int> ids)
        {
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    ResLoader.LoadCancel(id);
                }
            }
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public static void LoadCancel()
        {
            ResLoader.LoadAllCancel();
        }

        #endregion
        
        #region 卸载资源

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

            ResLoader.UnloadAsset(path, isForceUnload);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="asset"></param>
        public static void UnloadAsset(UnityEngine.Object asset)
        {
            if (asset is GameObject || asset is Component)
                return;
            Resources.UnloadAsset(asset);
            asset = null;
        }

        /// <summary>
        /// 卸载所有的
        /// </summary>
        public static void UnloadAll()
        {
            ResLoader.UnloadAllAsset();
        }

        #endregion
        
        #region 实例化、删除管理

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        static public GameObject Instantiate(GameObject gameObject)
        {
            return GameObject.Instantiate(gameObject);
        }

        /// <summary>
        /// 删除接口
        /// </summary>
        /// <param name="trans"></param>
        public static void Destroy(Transform trans)
        {
            if (trans)
            {
                Destroy(trans.gameObject);
            }
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

        #endregion
    }
}
