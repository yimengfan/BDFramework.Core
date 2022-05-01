﻿using UnityEngine;
using BDFramework.ResourceMgr;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr.V2;
using BDFramework.Core.Tools;
using BDFramework.VersionController;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace BDFramework.ResourceMgr

{
    /// <summary>
    /// 资源管理类
    /// </summary>
    static  public partial class BResources
    {
        #region 目录
        /// <summary>
        /// 美术根目录
        /// </summary>
        readonly static public string ASSET_ROOT_PATH = "Art";

        /// <summary>
        /// 美术资源config配置
        /// </summary>
        readonly static public string ASSET_CONFIG_PATH = ASSET_ROOT_PATH + "/ArtConfig.Info";

        /// <summary>
        /// 资源信息
        /// </summary>
        readonly static public string ASSET_TYPES_PATH = ASSET_ROOT_PATH + "/AssetTypeConfig.Info";

        /// <summary>
        /// 构建时的信息(Editor用)
        /// </summary>
        readonly static public string EDITOR_ASSET_BUILD_INFO_PATH = ASSET_ROOT_PATH + "/EditorBuild.Info";

        /// <summary>
        /// 旧打包资源配置
        /// </summary>
        readonly static public string ASSET_OLD_BUILD_INFO_PATH = ASSET_ROOT_PATH + "/OldBuild.Info";

        /// <summary>
        /// 客户端-资源包服务器信息
        /// </summary>
        readonly static public string ASSETS_INFO_PATH = "Assets.Info";

        /// <summary>
        /// 客户端-资源分包信息
        /// </summary>
        readonly static public string ASSETS_SUB_PACKAGE_CONFIG_PATH = "AssetsSubPackage.Info";

        /// <summary>
        /// 服务器-资源包版本配置
        /// </summary>
        readonly static public string SERVER_ASSETS_VERSION_INFO_PATH = "ServerAssetsVersion.Info";

        /// <summary>
        /// 服务器-资源分包信息
        /// </summary>
        readonly static public string SERVER_ART_ASSETS_SUB_PACKAGE_INFO_PATH = "ServerAssetsSubPackage_{0}.Info";

        /// <summary>
        /// ShaderVariant加载地址
        /// </summary>
        readonly public static string ALL_SHADER_VARAINT_RUNTIME_PATH = "Shader/AllShaders";

        /// <summary>
        /// Shadervariant资源地址
        /// </summary>
        readonly public static string ALL_SHADER_VARAINT_ASSET_PATH = "Assets/Resource/Runtime/" + ALL_SHADER_VARAINT_RUNTIME_PATH + ".shadervariants";

        /// <summary>
        /// 包构建信息路径
        /// </summary>
        readonly static public string PACKAGE_BUILD_INFO_PATH = "PackageBuild.Info";

        /// <summary>
        /// 混淆ab的资源路径
        /// </summary>
        readonly static public string MIX_SOURCE_FOLDER = "Assets/Resource/Runtime/MIX_AB_SOURCE";

        #endregion
        
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="abModel"></param>
        /// <param name="callback"></param>
        static public void Init(AssetLoadPathType loadPathType)
        {
            BDebug.Log("【BResource】加载路径:" + loadPathType.ToString());
            if (loadPathType == AssetLoadPathType.Editor)
            {
#if UNITY_EDITOR //防止编译报错
                ResLoader = new DevResourceMgr();
                ResLoader.Init("");
#endif
            }
            else
            {
                var path = GameConfig.GetLoadPath(loadPathType);
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
        public static T Load<T>(string name, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return ResLoader.Load<T>(name, pathType);
        }

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static UnityEngine.Object Load(Type type, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return ResLoader.Load(type, name);
        }

        /// <summary>
        /// 同步加载文件夹下所有资源ALL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("已废弃,不建议项目使用!")]
        public static T[] LoadALL<T>(string assetName) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            return ResLoader.LoadAll<T>(assetName);
        }


        /// <summary>
        /// 创建异步任务
        /// 该接口主要作为加载测试用，非内部创建任务不接受AssetbundleV2系统调度
        /// </summary>
        /// <param name="assetName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AsyncLoadTaskGroupResult CreateAsyncLoadTask<T>(string assetName) where T : UnityEngine.Object
        {
            return ResLoader.CreateAsyncLoadTask<T>(assetName);
        }


        /// <summary>
        /// 异步加载
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="objName">名称</param>
        /// <param name="action">回调函数</param>
        public static int AsyncLoad<T>(string assetName, Action<T> action) where T : UnityEngine.Object
        {
            return ResLoader.AsyncLoad<T>(assetName, action);
        }

        /// <summary>
        /// 批量加载
        /// </summary>
        /// <param name="assetlist"></param>
        /// <param name="onLoadEnd"></param>
        public static List<int> AsyncLoad(List<string> assetlist, Action<int, int> onProcess = null, Action<IDictionary<string, UnityEngine.Object>> onLoadEnd = null)
        {
            return ResLoader.AsyncLoad(assetlist, onProcess, onLoadEnd);
        }


        /// <summary>
        /// 取消一组任务
        /// </summary>
        public static void LoadCancel(params int[] ids)
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
            {
                return;
            }

            Resources.UnloadAsset(asset);
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

        #region 资源版本控制

        /// <summary>
        /// 版本控制器
        /// </summary>
        static public AssetsVersionController AssetsVersionController { get; private set; } = new AssetsVersionController();

        /// <summary>
        /// 获取子包的信息
        /// </summary>
        static public void GetServerSubPacks(string serverUrl, Action<Dictionary<string, string>> callback)
        {
            AssetsVersionController.GetServerSubPacks(serverUrl, callback);
        }

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="updateMode"></param>
        /// <param name="serverUrl"></param>
        /// <param name="assetsPackageName">分包名,如果不填则为下载所有</param>
        /// <param name="onProccess">下载进度</param>
        /// <param name="onTaskEndCallback">结果回调</param>
        static public void StartAssetsVersionControl(UpdateMode updateMode, string serverUrl, string assetsPackageName = "", Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess = null,
            Action<AssetsVersionController.VersionControllerStatus, string> onTaskEndCallback = null)
        {
            AssetsVersionController.UpdateAssets(updateMode, serverUrl, assetsPackageName, onDownloadProccess, onTaskEndCallback);
        }

        #endregion

        #region 对象池

        #endregion
    }
}