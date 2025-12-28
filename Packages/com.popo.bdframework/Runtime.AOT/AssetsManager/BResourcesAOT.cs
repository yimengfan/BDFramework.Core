using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// BResources 版本控制扩展
    /// </summary>
    static public partial class BResourcesAOT
    {
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

        #endregion
        
        
        #region 资源配置相关路径

        /// <summary>
        /// 获取版本配置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetServerAssetsVersionInfoPath(string rootPath, RuntimePlatform platform)
        {
            return IPath.Combine(rootPath, BApplication.GetPlatformLoadPath(platform), BResourcesAOT.SERVER_ASSETS_VERSION_INFO_PATH);
        }

        /// <summary>
        /// 获取资源信息路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetAssetsInfoPath(string rootPath)
        {
            return IPath.Combine(rootPath, BResourcesAOT.ASSETS_INFO_PATH);
        }

        /// <summary>
        /// 获取资源信息路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetAssetsInfoPath(string rootPath, RuntimePlatform platform)
        {
            return IPath.Combine(rootPath, BApplication.GetPlatformLoadPath(platform), BResourcesAOT.ASSETS_INFO_PATH);
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
                return IPath.Combine(rootPath, BApplication.GetPlatformLoadPath(platform), subPackageName);
            }
            else
            {
                var subPackagePath = string.Format(BResourcesAOT.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH, subPackageName);
                return IPath.Combine(rootPath, BApplication.GetPlatformLoadPath(platform), subPackagePath);
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
        static public void StartAssetsVersionControl(UpdateMode updateMode, string serverUrl,
            string assetsPackageName = "", Action<AssetItem, List<AssetItem>> onDownloadProccess = null,
            Action<AssetsVersionController.RetStatus, string> onTaskEndCallback = null)
        {
            AssetsVersionController.UpdateAssets(updateMode, serverUrl, assetsPackageName, onDownloadProccess, onTaskEndCallback);
        }

        #endregion
    }
}
