using UnityEngine;
using BDFramework.ResourceMgr;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr.V2;
using BDFramework.Core.Tools;
using BDFramework.VersionController;
using JetBrains.Annotations;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace BDFramework.ResourceMgr

{
    /// <summary>
    /// 资源管理类
    /// </summary>
    static public partial class BResources
    {
        #region 美术资源相关

        /// <summary>
        /// 美术根目录
        /// </summary>
        readonly static public string ART_ASSET_ROOT_PATH = "Art";

        /// <summary>
        /// 美术资源config配置
        /// </summary>
        readonly static public string ART_ASSET_CONFIG_PATH = ART_ASSET_ROOT_PATH + "/ArtConfig.Info";

        /// <summary>
        /// 资源信息
        /// </summary>
        readonly static public string ART_ASSET_TYPES_PATH = ART_ASSET_ROOT_PATH + "/AssetTypeConfig.Info";

        /// <summary>
        /// 构建时的信息(Editor用)
        /// </summary>
        readonly static public string EDITOR_ART_ASSET_BUILD_INFO_PATH = ART_ASSET_ROOT_PATH + "/EditorBuild.Info";

        /// <summary>
        /// 旧打包资源配置
        /// </summary>
        readonly static public string AIR_ASSET_OLD_BUILD_INFO_PATH = ART_ASSET_ROOT_PATH + "/OldBuild.Info";

        /// <summary>
        /// ShaderVariant加载地址
        /// </summary>
        readonly public static string ALL_SHADER_VARAINT_RUNTIME_PATH = "Shader/AllShaders";

        /// <summary>
        /// Shadervariant资源地址
        /// </summary>
        readonly public static string ALL_SHADER_VARAINT_ASSET_PATH = "Assets/Resource/Runtime/" + ALL_SHADER_VARAINT_RUNTIME_PATH + ".shadervariants";

        /// <summary>
        /// 混淆ab的资源路径
        /// </summary>
        readonly static public string MIX_SOURCE_FOLDER = "Assets/Resource/Runtime/MIX_AB_SOURCE";

        #endregion


        #region 所有资源相关配置

        /// <summary>
        /// 客户端-资源包服务器信息
        /// </summary>
        readonly static public string ART_ASSETS_INFO_PATH = "Assets.Info";

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
        /// 母包构建信息路径
        /// </summary>
        readonly static public string PACKAGE_BUILD_INFO_PATH = "PackageBuild.Info";

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
        /// 远程资源地址
        /// </summary>
        private static string RemoteAssetsUrl = "";

        /// <summary>
        /// 网络寻址模式
        /// </summary>
        static public void SetRemoteAssetsUrl(string url)
        {
            RemoteAssetsUrl = url;
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
            AssetsVersionController.GetServerSubPackageInfos(serverUrl, callback);
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
            Action<AssetsVersionController.RetStatus, string> onTaskEndCallback = null)
        {
            AssetsVersionController.UpdateAssets(updateMode, serverUrl, assetsPackageName, onDownloadProccess, onTaskEndCallback);
        }

        #endregion

        #region 对象池

        #endregion

        #region 资源校验、判断

        /// <summary>
        /// 是否存在资源文件
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="serverAsset"></param>
        /// <returns></returns>
        static public bool IsExsitAsset(RuntimePlatform platform, string assetName, string assetHashName)
        {
            //本地是否下载过hash文件(之前下到一半就中止了)
            var persistentHashPath = IPath.Combine(BApplication.persistentDataPath, BApplication.GetPlatformPath(platform), assetHashName);
            if (File.Exists(persistentHashPath))
            {
                var hash = FileHelper.GetMurmurHash3(persistentHashPath);
                if (assetHashName.Equals(hash))
                {
                    return true;
                }
                else
                {
                    File.Delete(persistentHashPath);
                }
            }

            //persistent判断
            var persistentAssetPath = IPath.Combine(BApplication.persistentDataPath, BApplication.GetPlatformPath(platform), assetName);
            if (File.Exists(persistentAssetPath))
            {
                return true;
            }

            /************母包资源的判断*************/
           
            if (Application.isEditor && BDLauncher.Inst.GameConfig.ArtRoot == AssetLoadPathType.DevOpsPublish)
            {
                //devops
                var devopsAssetPath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformPath(platform), assetName);
                if (File.Exists(devopsAssetPath))
                {
                    return true;
                }
            }
            else
            {
                //Streaming 文件判断,无需Streaming前缀
                var streamingAssetPath = IPath.Combine(BApplication.GetPlatformPath(platform), assetName);
                if (BetterStreamingAssets.FileExists(streamingAssetPath))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 是否存在资源.并且校验hash
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="serverAsset"></param>
        /// <returns></returns>
        static public bool IsExsitAssetWithCheckHash(RuntimePlatform platform, string assetName, string assetHash)
        {
            //本地是否下载过hash文件(之前下到一半就中止了),hash文件只会在
            var persistentHashPath = IPath.Combine(BApplication.persistentDataPath, BApplication.GetPlatformPath(platform), assetHash);
            if (File.Exists(persistentHashPath))
            {
                var hash = FileHelper.GetMurmurHash3(persistentHashPath);
                if (assetHash.Equals(hash))
                {
                    BDebug.Log($"hash文件存在 - {assetName} | hash - {assetHash}");
                    return true;
                }
                else
                {
                    File.Delete(persistentHashPath);
                }
            }

            //persistent判断
            var persistentAssetPath = IPath.Combine(BApplication.persistentDataPath, BApplication.GetPlatformPath(platform), assetName);
            if (File.Exists(persistentAssetPath))
            {
                var hash = FileHelper.GetMurmurHash3(persistentAssetPath);
                if (assetHash.Equals(hash))
                {
                    BDebug.Log($"persistent存在 - {assetName} | hash - {assetHash}");
                    return true;
                }
            }

            
            /************母包资源的判断*************/
            if (Application.isEditor && BDLauncher.Inst.GameConfig.ArtRoot == AssetLoadPathType.DevOpsPublish)
            {
                //devops
                var devopsAssetPath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformPath(platform), assetName);
                if (File.Exists(devopsAssetPath))
                {
                    var hash = FileHelper.GetMurmurHash3(devopsAssetPath);
                    if (assetHash.Equals(hash))
                    {
                        BDebug.Log($"devops存在 - {assetName} | hash - {assetHash}");
                        return true;
                    }
                }
            }
            else
            {
                //Streaming 文件判断,无需Streaming前缀
                var streamingAssetPath = IPath.Combine(BApplication.GetPlatformPath(platform), assetName);
                if (BetterStreamingAssets.FileExists(streamingAssetPath))
                {
                    var bytes = BetterStreamingAssets.ReadAllBytes(streamingAssetPath);
                    var hash = FileHelper.GetMurmurHash3(bytes);
                    if (assetHash.Equals(hash))
                    {
                        BDebug.Log($"streaming存在 - {assetName} | hash - {assetHash}");
                        return true;
                    }
                }
            }


            return false;
        }

        #endregion

        #region 配置相关路径

        /// <summary>
        /// 获取版本配置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetServerAssetsVersionInfoPath(string rootPath, RuntimePlatform platform)
        {
            return IPath.Combine(rootPath, BApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_VERSION_INFO_PATH);
        }

        /// <summary>
        /// 获取资源信息路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetAssetsInfoPath(string rootPath, RuntimePlatform platform)
        {
            return IPath.Combine(rootPath, BApplication.GetPlatformPath(platform), BResources.ART_ASSETS_INFO_PATH);
        }

        /// <summary>
        /// 获取分包设置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetAssetsSubPackageInfoPath(string rootPath, RuntimePlatform platform, string subPackageName)
        {
            //旧版本兼容逻辑
            if (subPackageName.StartsWith("ServerAssetsSubPackage_"))
            {
                return IPath.Combine(rootPath, BApplication.GetPlatformPath(platform), subPackageName);
            }
            else
            {
                var subPackagePath = string.Format(BResources.SERVER_ART_ASSETS_SUB_PACKAGE_INFO_PATH, subPackageName);
                return IPath.Combine(rootPath, BApplication.GetPlatformPath(platform), subPackagePath);
            }
        }

        #endregion
    }
}
