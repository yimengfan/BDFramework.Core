using System;
using UnityEngine;
using System.Collections.Generic;
using System.Data;
using System.IO;
using BDFramework.Configure;
using BDFramework.ResourceMgr.V2;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgrV2;
using BDFramework.VersionController;
using Cysharp.Text;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 资源管理类
    /// </summary>
    static public partial class BResources
    {
        #region 美术资源相关路径

        /// <summary>
        /// 美术根目录
        /// </summary>
        readonly static public string ART_ASSET_ROOT_PATH = "art_assets";

        /// <summary>
        /// 美术资源config配置
        /// </summary>
        readonly static public string ART_ASSET_INFO_PATH = ART_ASSET_ROOT_PATH + "/art_assets.info";

        /// <summary>
        /// 资源信息
        /// </summary>
        readonly static public string ART_ASSET_TYPES_PATH = ART_ASSET_ROOT_PATH + "/art_asset_type.info";

        /// <summary>
        /// 构建时的信息(Editor用)
        /// </summary>
        readonly static public string EDITOR_ART_ASSET_BUILD_INFO_PATH = ART_ASSET_ROOT_PATH + "/EditorBuild.Info";

        /// <summary>
        /// SBP build信息
        /// </summary>
        readonly static public string SBPBuildLog = "buildlogtep.json";
        readonly static public string SBPBuildLog2 = "build_result.info";
        /// <summary>
        /// 旧打包资源配置
        /// </summary>
        //readonly static public string AIR_ASSET_OLD_BUILD_INFO_PATH = ART_ASSET_ROOT_PATH + "/OldBuild.Info";

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

        #region 所有资源相关配置路径

        /// <summary>
        /// 客户端-资源包服务器信息
        /// </summary>
        readonly static public string ASSETS_INFO_PATH = "assets.info";

        /// <summary>
        /// 客户端-资源分包信息
        /// </summary>
        readonly static public string ASSETS_SUB_PACKAGE_CONFIG_PATH = "assets_subpack.info";

        /// <summary>
        /// 服务器-资源包版本配置
        /// </summary>
        readonly static public string SERVER_ASSETS_VERSION_INFO_PATH = "server_assets_version.info";

        /// <summary>
        /// 服务器-资源分包信息
        /// </summary>
        readonly static public string SERVER_ASSETS_SUB_PACKAGE_INFO_PATH = "server_assets_subpack_{0}.info";

        /// <summary>
        /// 包体构建信息路径
        /// </summary>
        readonly static public string PACKAGE_BUILD_INFO_PATH = "package_build.info";

        #endregion

        /// <summary>
        /// 加载器
        /// </summary>
        static public IResMgr ResLoader { get; private set; }

        /// <summary>
        /// loder缓存
        /// </summary>
        static private Dictionary<string, IResMgr> loaderCacheMap = new Dictionary<string, IResMgr>();

        /// <summary>
        /// 资产加载路径类型
        /// </summary>
        static public AssetLoadPathType AssetPathType { get; private set; } = AssetLoadPathType.Editor;

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
                ResLoader.Init(null, RuntimePlatform.WindowsEditor);
#endif
            }
            else
            {
                var path = GameBaseConfigProcessor.GetLoadPath(loadPathType);
                ResLoader = new AssetBundleMgrV2();
                ResLoader.Init(path, BApplication.RuntimePlatform);
            }

            AssetPathType = loadPathType;
            //初始化对象池
            InitObjectPools();
        }


        /// <summary>
        /// 初始化加载Assetbundle的环境
        /// 该接口,一般用于测试
        /// </summary>
        /// <param name="abModel"></param>
        /// <param name="callback"></param>
        static public void InitLoadAssetBundleEnv(string path, RuntimePlatform platform)
        {
            var key = ZString.Concat(path, "_", platform);
            if (!loaderCacheMap.TryGetValue(key, out var loder))
            {
                ResLoader = new AssetBundleMgrV2();
                ResLoader.Init(path, platform);
                loaderCacheMap[key] = ResLoader;
            }
            else
            {
                ResLoader = loder;
            }

            //初始化对象池
            InitObjectPools();
        }

        /// <summary>
        /// 远程资源地址
        /// </summary>
        private static string RemoteAssetsUrl = "";

        /// <summary>
        /// 设置网络寻址
        /// 网络寻址模式
        /// </summary>
        static public void SetRemoteAssetsUrl(string url)
        {
            RemoteAssetsUrl = url;
        }


        #region 加载、取消加载

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="assetLoadPath">资源路径</param>
        /// <param name="pathType">加载类型：路径名还是GUID</param>
        /// <param name="groupName">加载组,用以对资源加载分组</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Load<T>(string assetLoadPath, LoadPathType pathType = LoadPathType.RuntimePath, string groupName = null) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetLoadPath))
            {
                return null;
            }

            //添加到资源组
            AddAssetsPathToGroup(groupName, assetLoadPath);
            //加载
            return ResLoader.Load<T>(assetLoadPath, pathType);
        }

        /// <summary>
        /// 同步加载
        /// Load<T>的type形式
        /// </summary>
        /// <typeparam name="type">类型</typeparam>
        /// <param name="assetLoadPath">加载路径</param>
        /// <returns></returns>
        private static UnityEngine.Object Load(Type type, string assetLoadPath)
        {
            if (string.IsNullOrEmpty(assetLoadPath))
            {
                return null;
            }

            return ResLoader.Load(type, assetLoadPath);
        }

        /// <summary>
        /// 同步加载文件夹下所有资源ALL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("已废弃,不建议项目使用!")]
        public static T[] LoadALL<T>(string assetLoadPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetLoadPath))
            {
                return null;
            }

            return ResLoader.LoadAll<T>(assetLoadPath);
        }


        /// <summary>
        /// 创建异步任务
        /// 该接口主要作为加载测试用，非内部创建任务不接受AssetbundleV2系统调度
        /// </summary>
        /// <param name="assetLoadPath">加载路径</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static LoadTaskGroup AsyncLoad<T>(string assetLoadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object
        {
            return ResLoader.AsyncLoad<T>(assetLoadPath);
        }


        /// <summary>
        /// 异步加载
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="objName">名称</param>
        /// <param name="action">加载回调</param>
        /// <param name="groupName">分组名，用于统一管理，如：卸载等</param>
        ///  <param name="loadPathType">路径类型：路径或者guid</param>
        public static int AsyncLoad<T>(string assetLoadPath, Action<T> action, LoadPathType loadPathType = LoadPathType.RuntimePath, string groupName = null) where T : UnityEngine.Object
        {
            //添加到资源组
            AddAssetsPathToGroup(groupName, assetLoadPath);
            //异步加载
            return ResLoader.AsyncLoad<T>(assetLoadPath, action, loadPathType);
        }

        /// <summary>
        /// 批量加载
        /// </summary>
        /// <param name="assetlist"></param>
        /// <param name="onProcess"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="loadPathType"></param>
        /// <param name="groupName"></param>
        public static List<int> AsyncLoad(List<string> assetlist, Action<int, int> onProcess = null, Action<IDictionary<string, Object>> onLoadEnd = null, LoadPathType loadPathType = LoadPathType.RuntimePath, string groupName = null)
        {
            if (assetlist.Count != 0)
            {
                //添加到资源组
                AddAssetsPathToGroup(groupName, assetlist.ToArray());
                //异步加载
                return ResLoader.AsyncLoad(assetlist, onProcess, onLoadEnd, loadPathType);
            }
            else
            {
                //==0 容错！
                onLoadEnd?.Invoke(new Dictionary<string, Object>());
                return new List<int>();
            }
        }


        /// <summary>
        /// 取消一个任务
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool LoadCancel(int id)
        {
            return ResLoader.LoadCancel(id);
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
        /// 卸载资源 / Assetbundle
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="isForceUnload"></param>
        /// <param name="type">带具体类型，有些项目喜欢用重名资源,如a.prefab、a.mat，此时卸载就需要指定类型</param>
        public static void UnloadAsset(string assetPath, bool isForceUnload = false, Type type = null)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            ResLoader.UnloadAsset(assetPath, type);
        }

        /// <summary>
        /// 卸载实例化资源
        /// </summary>
        /// <param name="obj"></param>
        public static void UnloadAsset(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                Destroy(go);
                Resources.UnloadAsset(go);
            }
            else if (obj is Sprite sp)
            {
                Resources.UnloadAsset(sp.texture);
            }
            else
            {
                Resources.UnloadAsset(obj);
            }
        }

        /// <summary>
        /// 卸载资源/Assetbundle
        /// </summary>
        /// <param name="o"></param>
        public static void UnloadAssets(params string[] assetPaths)
        {
            foreach (var assetPath in assetPaths)
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    return;
                }

                ResLoader.UnloadAsset(assetPath);
            }
        }


        /// <summary>
        /// 卸载所有的AssetBundle
        /// </summary>
        public static void UnloadAll()
        {
            ResLoader.UnloadAllAsset();
        }

        #endregion


        #region Shader操作

        /// <summary>
        /// 预热shader
        /// </summary>
        public static void WarmUpShaders()
        {
            ResLoader.WarmUpShaders();
        }


        /// <summary>
        /// 寻找一个shader
        /// 类似Shader.Find用法
        /// </summary>
        /// <param name="shaderName"></param>
        public static Shader FindShader(string shaderName)
        {
            return ResLoader.FindShader(shaderName);
        }

        #endregion

        // #region 资源缓存
        //
        // /// <summary>
        // /// 全局的资源缓存
        // /// </summary>
        // static private Dictionary<string, UnityEngine.Object> GameObjectCacheMap { get; set; } = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
        //
        // /// <summary>
        // /// 从缓存中加载
        // /// </summary>
        // /// <param name="assetPath"></param>
        // /// <returns></returns>
        // static public void AddObjectToCache(Type type, string assetPath, Object obj)
        // {
        //     GameObjectCacheMap[assetPath] = obj;
        // }
        //
        // /// <summary>
        // /// 从缓存中加载
        // /// </summary>
        // /// <param name="assetPath"></param>
        // /// <returns></returns>
        // static public Object GetObjectFormCache(Type type, string assetPath)
        // {
        //     Object obj = null;
        //     GameObjectCacheMap.TryGetValue(assetPath, out obj);
        //     return obj;
        // }
        //
        // /// <summary>
        // /// 从缓存中加载
        // /// </summary>
        // /// <param name="assetPath"></param>
        // /// <returns></returns>
        // static public Object UnloadObjectCache(string assetPath)
        // {
        //     var ret = GameObjectCacheMap.TryGetValue(assetPath, out var obj);
        //     if (ret)
        //     {
        //         GameObject.Destroy(obj);
        //         GameObjectCacheMap.Remove(assetPath);
        //     }
        //
        //
        //     return obj;
        // }
        //
        // #endregion

        #region 资源组，用于加载资源分组,方便卸载(Assetbundle)

        /// <summary>
        /// 加载资源组缓存
        /// </summary>
        static private Dictionary<string, List<string>> loadAssetGroupMap = new Dictionary<string, List<string>>();


        /// <summary>
        /// 添加到资源组
        /// </summary>
        public static void AddAssetsPathToGroup(string groupName, params string[] assetPath)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                if (!loadAssetGroupMap.TryGetValue(groupName, out var list))
                {
                    list = new List<string>(10);
                    loadAssetGroupMap[groupName] = list;
                }

                list.AddRange(assetPath);
            }
        }

        /// <summary>
        /// 获取资源组资源
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static string[] GetAssetsPathByGroup(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                if (loadAssetGroupMap.TryGetValue(groupName, out var list))
                {
                    return list.ToArray();
                }
            }

            return new string[0];
        }

        /// <summary>
        /// 获取资源组资源
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static void ClearAssetGroup(string groupName)
        {
            loadAssetGroupMap.Remove(groupName);
        }

        /// <summary>
        /// 通过组卸载
        /// </summary>
        /// <param name="groupName"></param>
        static public void UnloadAssetByGouroup(string groupName)
        {
            var assets = GetAssetsPathByGroup(groupName);
            UnloadAssets(assets);
            ClearAssetGroup(groupName);
        }

        #endregion

        #region 实例化、删除管理

        /// <summary>
        /// 删除接口
        /// </summary>
        /// <param name="transform"></param>
        public static void Destroy(Transform transform)
        {
            if (transform)
            {
                Destroy(transform.gameObject);
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
                GameObject.Destroy(go);
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
        static public void StartAssetsVersionControl(UpdateMode updateMode, string serverUrl, string assetsPackageName = "", Action<AssetItem, List<AssetItem>> onDownloadProccess = null,
            Action<AssetsVersionController.RetStatus, string> onTaskEndCallback = null)
        {
            AssetsVersionController.UpdateAssets(updateMode, serverUrl, assetsPackageName, onDownloadProccess, onTaskEndCallback);
        }

        #endregion

        #region 对象池

        private static bool isInitedPools = false;

        /// <summary>
        /// 初始化对象池
        /// </summary>
        static private void InitObjectPools()
        {
            if (Application.isPlaying && !isInitedPools)
            {
                isInitedPools = true;
                GameObject pool = new GameObject("GameobjectPools");
                pool.AddComponent<GameObjectPoolManager>();
            }
        }


        /// <summary>
        /// 预热对象池
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="size"></param>
        static public void WarmPool(string assetPath, int size = 5)
        {
            var obj = Load<GameObject>(assetPath);
            GameObjectPoolManager.WarmPool(obj, size);
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="size"></param>
        static public void DestroyPool(string assetPath)
        {
            var obj = Load<GameObject>(assetPath);
            GameObjectPoolManager.DestoryPool(obj);
        }

        /// <summary>
        /// 异步预热对象池
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="size"></param>
        static public void AsyncWarmPool(string assetPath, int size = 5)
        {
            //异步加载后初始化
            AsyncLoad<GameObject>(assetPath, (obj) =>
            {
                //异步加载完
                GameObjectPoolManager.WarmPool(obj, size);
            });
        }

        /// <summary>
        /// 从对象池加载
        /// </summary>
        /// <returns></returns>
        static public GameObject LoadFormPool(string assetPath)
        {
            var obj = Load<GameObject>(assetPath);
            return GameObjectPoolManager.SpawnObject(obj);
        }

        /// <summary>
        /// 从对象池加载
        /// </summary>
        /// <returns></returns>
        static public GameObject LoadFormPool(string assetPath, Vector3 position, Quaternion rotation)
        {
            var obj = Load<GameObject>(assetPath);
            return GameObjectPoolManager.SpawnObject(obj, position, rotation);
        }

        /// <summary>
        /// 释放Gameobj
        /// </summary>
        /// <param name="gobjClone"></param>
        static public void ReleaseToPool(GameObject gobjClone)
        {
            GameObjectPoolManager.ReleaseObject(gobjClone);
        }

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

            if (Application.isEditor && BDLauncher.Inst.Config.ArtRoot == AssetLoadPathType.DevOpsPublish)
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
                    BDebug.Log($"【AB校验】persistent存在 - {assetName} | hash - {assetHash}");
                    return true;
                }
            }


            /************母包资源的判断*************/
            if (Application.isEditor && BDLauncher.Inst.Config.ArtRoot == AssetLoadPathType.DevOpsPublish)
            {
                //devops
                var devopsAssetPath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformPath(platform), assetName);
                if (File.Exists(devopsAssetPath))
                {
                    var hash = FileHelper.GetMurmurHash3(devopsAssetPath);
                    if (assetHash.Equals(hash))
                    {
                        BDebug.Log($"【AB校验】devops存在 - {assetName} | hash - {assetHash}");
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
                        BDebug.Log($"【AB校验】streaming存在 - {assetName} | hash - {assetHash}");
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
            return IPath.Combine(rootPath, BApplication.GetPlatformPath(platform), BResources.ASSETS_INFO_PATH);
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
                var subPackagePath = string.Format(BResources.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH, subPackageName);
                return IPath.Combine(rootPath, BApplication.GetPlatformPath(platform), subPackagePath);
            }
        }

        #endregion

        #region AUP设置

        public enum AUPLevel
        {
            /// <summary>
            /// 低render 情况下，aup可以设置比较高
            /// </summary>
            LowRender,
            Height,
            Normal,
            Low
        }


        /// <summary>
        /// 设置AUP等级
        /// </summary>
        static public void SetAUPLEvel(AUPLevel level)
        {
            QualitySettings.asyncUploadPersistentBuffer = true;
            switch (level)
            {
                case AUPLevel.LowRender:
                {
                    //低渲染、高加载时候

                    QualitySettings.asyncUploadBufferSize = 32;
                    QualitySettings.asyncUploadTimeSlice = 8;
                    Application.backgroundLoadingPriority = ThreadPriority.High;
                }
                    break;
                case AUPLevel.Height:
                {
                    //最高配置
                    QualitySettings.asyncUploadBufferSize = 32;
                    QualitySettings.asyncUploadTimeSlice = 4;
                    Application.backgroundLoadingPriority = ThreadPriority.Normal;
                }
                    break;
                case AUPLevel.Normal:
                {
                    //中等配置
                    QualitySettings.asyncUploadBufferSize = 16;
                    QualitySettings.asyncUploadTimeSlice = 4;
                    Application.backgroundLoadingPriority = ThreadPriority.Normal;
                }
                    break;
                case AUPLevel.Low:
                {
                    //低配置
                    QualitySettings.asyncUploadBufferSize = 16;
                    QualitySettings.asyncUploadTimeSlice = 2;
                    Application.backgroundLoadingPriority = ThreadPriority.Low;
                }
                    break;
            }
        }

        /// <summary>
        /// 设置配置信息
        /// </summary>
        static public void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1)
        {
            ResLoader.SetLoadConfig(maxLoadTaskNum, maxUnloadTaskNum);
        }

        #endregion
    }
}
