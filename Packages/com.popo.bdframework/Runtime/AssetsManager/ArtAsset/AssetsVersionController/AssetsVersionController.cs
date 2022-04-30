using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Cysharp.Text;
using LitJson;
using MurmurHash.Net;
using ServiceStack.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.VersionController
{
    /// <summary>
    /// 服务器Asset的Config
    /// </summary>
    public class ServerAssetsVersionInfo
    {
        /// <summary>
        /// 平台
        /// </summary>
        public string Platfrom { get; set; } = "";

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = "0.0.1";

        /// <summary>
        /// 资源子包的版本
        /// packname - verion
        /// </summary>
        public Dictionary<string, string> SubPckMap { get; private set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 服务器Asset的描述
    /// </summary>
    public class ServerAssetItem
    {
        public enum AssetType
        {
            AssetBundle,
            DLL,
            Sqlite
        }

        /// <summary>
        /// 资源id
        /// 这里id 会和artasset保持一致
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// hash名 -服务器存储文件名
        /// </summary>
        public string HashName { get; set; }

        /// <summary>
        /// 本地存储名
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// 文件大小 KB
        /// </summary>
        public float FileSize { get; set; }

        /// <summary>
        /// 重写比较
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is ServerAssetItem _SAI)
            {
                return (this.HashName.Equals(_SAI.HashName) && this.LocalPath.Equals(_SAI.LocalPath));
            }

            return false;
        }
    }

    public enum UpdateMode
    {
        Compare, //对比版本
        Repair, //修复模式
    }

    /// <summary>
    /// 资源分包配置
    /// </summary>
    public class SubPackageConfigItem
    {
        /// <summary>
        /// 分包名
        /// </summary>
        public string PackageName { get; set; } = "Null";

        /// <summary>
        /// 资源目录名
        /// </summary>
        public List<int> ArtAssetsIdList { get; set; } = new List<int>();

        /// <summary>
        /// 热更代码
        /// </summary>
        public List<string> HotfixCodePathList { get; set; } = new List<string>();

        /// <summary>
        /// 数据库热更
        /// </summary>
        public List<string> TablePathList { get; set; } = new List<string>();

        /// <summary>
        /// 必须存在的相关配置表
        /// </summary>
        public List<string> ConfAndInfoList { get; set; } = new List<string>();
    }

    /// <summary>
    /// 版本控制
    /// </summary>
    public partial class AssetsVersionController
    {
        /// <summary>
        /// 服务器错误码
        /// </summary>
        public enum VersionControllerStatus
        {
            Error,
            Success
        }

        public AssetsVersionController()
        {
            //BSA初始化
            BetterStreamingAssets.Initialize();
        }

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="updateMode"></param>
        /// <param name="serverConfigUrl"></param>
        /// <param name="assetsPackageName">分包名,如果不填则为下载所有</param>
        /// <param name="onProccess">下载进度</param>
        /// <param name="onTaskEndCallback">结果回调</param>
        public void UpdateAssets(UpdateMode updateMode, string serverConfigUrl, string assetsPackageName = "", Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess = null,
            Action<VersionControllerStatus, string> onTaskEndCallback = null)
        {
            //下载资源位置必须为Persistent
            IEnumeratorTool.StartCoroutine(IE_StartVersionControl(updateMode, serverConfigUrl, Application.persistentDataPath, assetsPackageName, onDownloadProccess, onTaskEndCallback));
        }


        /// <summary>
        /// 获取服务器子包信息
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="callback"></param>
        public void GetServerSubPacks(string serverUrl, Action<Dictionary<string, string>> callback)
        {
            IEnumeratorTool.StartCoroutine(this.IE_GetServerVersionInfo(serverUrl, callback));
        }


        /// <summary>
        /// 重试次数
        /// </summary>
        private static int RETRY_COUNT = 6;


        /// <summary>
        ///获取服务器版本信息
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator IE_GetServerVersionInfo(string serverUrl, Action<Dictionary<string, string>> callback)
        {
            //本地、服务器版本信息的路径
            var serverAssetsVersionInfoUrl = GetServerAssetsVersionInfoPath(serverUrl, Application.platform);
            //开始下载服务器配置
            var serverAssetsVersionConfigWebReq = UnityWebRequest.Get(serverAssetsVersionInfoUrl);
            for (int i = 0; i < RETRY_COUNT; i++)
            {
                yield return serverAssetsVersionConfigWebReq.SendWebRequest();
                if (serverAssetsVersionConfigWebReq.error == null)
                {
                    break;
                }
            }

            if (serverAssetsVersionConfigWebReq.error == null)
            {
                BDebug.Log("服务器资源配置:" + serverAssetsVersionConfigWebReq.downloadHandler.text);
            }
            else
            {
                BDebug.LogError(serverAssetsVersionConfigWebReq.error);
                yield break;
            }

            var serverVersionInfo = JsonMapper.ToObject<ServerAssetsVersionInfo>(serverAssetsVersionConfigWebReq.downloadHandler.text);
            callback?.Invoke(serverVersionInfo.SubPckMap);
        }

        /// <summary>
        /// 开始版本控制逻辑
        /// </summary>
        /// <param name="serverUrl">服务器配置根目录</param>
        /// <param name="localSaveAssetsPath">本地根目录</param>
        /// <param name="onDownloadProccess"></param>
        /// <param name="onError">返回失败后，只需要重试重新调用该函数即可</param>
        /// 返回码: -1：error  0：success
        private IEnumerator IE_StartVersionControl(UpdateMode mode, string serverUrl, string localSaveAssetsPath, string subPackageName, Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess,
            Action<VersionControllerStatus, string> onTaskEndCallback)
        {
            //目录准备
            var platform = BDApplication.GetPlatformPath(Application.platform);
            var platformPath = IPath.Combine(localSaveAssetsPath, platform);
            if (!Directory.Exists(platformPath))
            {
                Directory.CreateDirectory(platformPath);
            }

            //子包模式判断
            bool isDownloadSubPackageMode = !string.IsNullOrEmpty(subPackageName);
            //本地、服务器版本信息的路径
            var serverAssetsVersionInfoUrl = GetServerAssetsVersionInfoPath(serverUrl, Application.platform);
            var localAssetsVersionInfoPath = GetServerAssetsVersionInfoPath(localSaveAssetsPath, Application.platform);
            BDebug.Log("server version config:" + serverAssetsVersionInfoUrl);
            /**********************获取差异列表**********************/
            Queue<ServerAssetItem> diffDownloadQueue = null;
            //1.下载服务器version config

            #region 版本信息加载

            //开始下载服务器配置
            var serverAssetsVersionConfigWebReq = UnityWebRequest.Get(serverAssetsVersionInfoUrl);
            for (int i = 0; i < RETRY_COUNT; i++)
            {
                yield return serverAssetsVersionConfigWebReq.SendWebRequest();
                if (serverAssetsVersionConfigWebReq.error == null)
                {
                    break;
                }
            }

            if (serverAssetsVersionConfigWebReq.error == null)
            {
                BDebug.Log("服务器资源配置:" + serverAssetsVersionConfigWebReq.downloadHandler.text);
            }
            else
            {
                BDebug.LogError(serverAssetsVersionConfigWebReq.error);
                onTaskEndCallback(VersionControllerStatus.Error, serverAssetsVersionConfigWebReq.error);
                yield break;
            }

            var serverVersionInfo = JsonMapper.ToObject<ServerAssetsVersionInfo>(serverAssetsVersionConfigWebReq.downloadHandler.text);

            //本地配置
            var localVersionInfo = new ServerAssetsVersionInfo();
            if (File.Exists(localAssetsVersionInfoPath))
            {
                localVersionInfo = JsonMapper.ToObject<ServerAssetsVersionInfo>(File.ReadAllText(localAssetsVersionInfoPath));
                if (localVersionInfo == null)
                {
                    localVersionInfo = new ServerAssetsVersionInfo();
                }
            }

            #endregion

            //服务器的AssetInfo
            string serverAssetInfosUrl = "";
            var serverAssetsInfoList = new List<ServerAssetItem>();

            //本地的AssetInfo
            string localAssetInfoPath = "";
            var localAssetsInfoList = new List<ServerAssetItem>();

            //2.服务器AssetInfo 本地AssetInfo对比

            #region 版本信息对比

            /**********处理服务器配置*************/
            if (mode == UpdateMode.Compare)
            {
            }

            //分包下载
            if (isDownloadSubPackageMode)
            {
                BDebug.Log("【版本控制】子包模式:" + subPackageName);
                var ret = serverVersionInfo.SubPckMap.TryGetValue(subPackageName, out var serverSubPckVersion);
                if (!ret)
                {
                    onTaskEndCallback(VersionControllerStatus.Error, "【版本控制】服务器不存在子包:" + subPackageName);
                    yield break;
                }

                localVersionInfo.SubPckMap.TryGetValue(subPackageName, out var localSubPckVersion);

                if (serverSubPckVersion != localSubPckVersion)
                {
                    //服务器路径
                    serverAssetInfosUrl = GetAssetsSubPackageInfoPath(serverUrl, Application.platform, subPackageName);
                    BDebug.Log($"【版本控制】分包下载模式! server:{serverSubPckVersion} local:{localSubPckVersion} ", "red");
                }
                else
                {
                    BDebug.Log("【版本控制】分包版本一致,无需下载!");
                    onTaskEndCallback(VersionControllerStatus.Success, "");
                    yield break;
                }
            }
            //全量包下载
            else
            {
                //判断版本号
                if (localVersionInfo.Version != serverVersionInfo.Version)
                {
                    serverAssetInfosUrl = GetAssetsInfoPath(serverUrl, Application.platform);
                    BDebug.Log($"【版本控制】全量下载模式! server:{serverVersionInfo.Version} local:{localVersionInfo.Version} ", "red");
                }
                else
                {
                    BDebug.Log("【版本控制】全量版本一致,无需下载!");
                    onTaskEndCallback(VersionControllerStatus.Success, "");
                    yield break;
                }
            }

            #endregion

            #region 服务器资源配置、本地配置加载

            //下载服务器配置
            var serverAssetsInfoWebReq = UnityWebRequest.Get(serverAssetInfosUrl);
            if (!string.IsNullOrEmpty(serverAssetInfosUrl))
            {
                for (int i = 0; i < RETRY_COUNT; i++)
                {
                    yield return serverAssetsInfoWebReq.SendWebRequest();
                    if (serverAssetsInfoWebReq.error == null)
                    {
                        break;
                    }
                }

                //下载完成
                if (serverAssetsInfoWebReq.error == null)
                {
                    serverAssetsInfoList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(serverAssetsInfoWebReq.downloadHandler.text);
                }
                else
                {
                    BDebug.LogError(serverAssetsInfoWebReq.error);
                    onTaskEndCallback(VersionControllerStatus.Error, serverAssetsInfoWebReq.error);
                    yield break;
                }
            }


            /**************处理本地配置*************/
            if (isDownloadSubPackageMode)
            {
                //本地路径
                localAssetInfoPath = GetAssetsSubPackageInfoPath(localSaveAssetsPath, Application.platform, subPackageName);
            }
            else
            {
                //读到一个Assets.info即可,全量只有1个描述文件
                localAssetInfoPath = GetAssetsInfoPath(localSaveAssetsPath, Application.platform);
            }

            if (File.Exists(localAssetInfoPath))
            {
                var content = File.ReadAllText(localAssetInfoPath);
                localAssetsInfoList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
            }

            #endregion

            //3.生成差异列表

            #region 生成差异文件

            //根据加载模式不同,寻找不同目录下的其他配置
            //打包时，本地会带一份ServerAssets.info以标记当前包携带的资源
            var loadArtRoot = BDLauncher.Inst.GameConfig.ArtRoot;
            switch (loadArtRoot)
            {
                case AssetLoadPathType.Persistent:
                case AssetLoadPathType.StreamingAsset:
                {
                    //BSA 读取，不需要前缀
                    var steamingAssetsInfoPath = IPath.Combine(platform, BResources.ASSETS_INFO_PATH);
                    if (BetterStreamingAssets.FileExists(steamingAssetsInfoPath))
                    {
                        var content = BetterStreamingAssets.ReadAllText(steamingAssetsInfoPath);
                        var assetInfoList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
                        localAssetsInfoList.AddRange(assetInfoList);
                    }
                }
                    break;
                case AssetLoadPathType.DevOpsPublish:
                {
                    var path = GameConfig.GetLoadPath(loadArtRoot);
                    var devopsAssetInfoPath = GetAssetsInfoPath(path, Application.platform);
                    if (File.Exists(devopsAssetInfoPath))
                    {
                        var content = File.ReadAllText(devopsAssetInfoPath);
                        var assetInfoList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
                        localAssetsInfoList.AddRange(assetInfoList);
                    }
                }
                    break;
            }


            switch (mode)
            {
                case UpdateMode.Compare:
                    diffDownloadQueue = Compare(localAssetsInfoList, serverAssetsInfoList);
                    break;
                case UpdateMode.Repair:
                    diffDownloadQueue = Repair(localSaveAssetsPath, localAssetsInfoList, serverAssetsInfoList);
                    break;
            }

            #endregion

            //4.开始下载

            #region 根据差异文件下载

            var downloadCacheList = diffDownloadQueue.ToList();
            var failDownloadList = new List<ServerAssetItem>();
            while (diffDownloadQueue.Count > 0)
            {
                var downloadItem = diffDownloadQueue.Dequeue();
                bool isSkip = false;
                //本地存在hash文件,且hash一致,跳过
                var localDownloadFile = IPath.Combine(localSaveAssetsPath, platform, downloadItem.HashName);
                if (File.Exists(localDownloadFile))
                {
                    var hash = FileHelper.GetMurmurHash3(localDownloadFile);
                    if (downloadItem.HashName.Equals(hash))
                    {
                        isSkip = true;
                    }
                    else
                    {
                        File.Delete(localDownloadFile);
                    }
                }

                //本地存在原资源，且hash一致，跳过
                var localRealPath = IPath.Combine(localSaveAssetsPath, platform, downloadItem.LocalPath);
                if (File.Exists(localRealPath))
                {
                    var hash = FileHelper.GetMurmurHash3(localRealPath);
                    if (downloadItem.HashName.Equals(hash))
                    {
                        isSkip = true;
                    }
                }

                //下载
                UnityWebRequest assetDownloadWebReq = null;
                if (!isSkip)
                {
                    //先进行下载hash文件,所有的完成后再进行rename成资源
                    var serverAssetUrl = IPath.Combine(serverUrl, platform, downloadItem.HashName);
                    //下载具体资源 ,任务会重试5次
                    assetDownloadWebReq = UnityWebRequest.Get(serverAssetUrl);
                    for (int i = 0; i < RETRY_COUNT; i++)
                    {
                        yield return assetDownloadWebReq.SendWebRequest();
                        if (assetDownloadWebReq.error == null)
                        {
                            //对比hash
                            var downloadFileHash = FileHelper.GetMurmurHash3(assetDownloadWebReq.downloadHandler.data);
                            if (downloadFileHash == downloadItem.HashName)
                            {
                                BDebug.Log("下载成功：" + serverAssetUrl);
                                break;
                            }
                            else
                            {
                                BDebug.LogError("【版本控制】重下, hash校验失败! server-" + downloadItem.HashName + " local-" + downloadFileHash);
                            }
                        }
                    }
                }

                //通知
                if (isSkip)
                {
                    onDownloadProccess(downloadItem, downloadCacheList);
                }
                else if (assetDownloadWebReq.error == null)
                {
                    onDownloadProccess(downloadItem, downloadCacheList);
                    FileHelper.WriteAllBytes(localDownloadFile, assetDownloadWebReq.downloadHandler.data);
                }
                else
                {
                    //这边需要继续下载,最后统计失败文件
                    failDownloadList.Add(downloadItem);
                    BDebug.LogError("下载失败:" + assetDownloadWebReq.error);
                }

                assetDownloadWebReq?.Dispose();
            }

            if (failDownloadList.Count > 0)
            {
                onTaskEndCallback(VersionControllerStatus.Error, "部分资源未下载完毕!");
                yield break;
            }

            #endregion

            //5.重命名资源,并写入配置到本地

            #region 资源写入本地，整理为加载资源

            foreach (var assetItem in downloadCacheList)
            {
                var localHashPath = IPath.Combine(localSaveAssetsPath, platform, assetItem.HashName);
                var localRealPath = IPath.Combine(localSaveAssetsPath, platform, assetItem.LocalPath);
                if (File.Exists(localHashPath))
                {
                    if (File.Exists(localRealPath))
                    {
                        File.Delete(localRealPath);
                    }

                    //移动(重命名)
                    FileHelper.Move(localHashPath, localRealPath);
                }
            }

            #endregion


            if (downloadCacheList.Count == 0)
            {
                BDebug.Log("【版本控制】对比差异，无可下载资源~");
            }

            //6.写入配置到本地

            #region 存储配置到本地

            //写入AssetInfo
            if (!string.IsNullOrEmpty(serverAssetsInfoWebReq.downloadHandler.text))
            {
                File.WriteAllText(localAssetInfoPath, serverAssetsInfoWebReq.downloadHandler.text);
            }

            //写入VersionInfo
            if (isDownloadSubPackageMode)
            {
                localVersionInfo.Platfrom = serverVersionInfo.Platfrom;
                //子包版本信息
                localVersionInfo.SubPckMap[subPackageName] = serverVersionInfo.SubPckMap[subPackageName];
            }
            else
            {
                localVersionInfo.Platfrom = serverVersionInfo.Platfrom;
                //全量包信息
                localVersionInfo.Version = serverVersionInfo.Version;
            }

            File.WriteAllText(localAssetsVersionInfoPath, JsonMapper.ToJson(localVersionInfo));

            #endregion


            //the end.
            serverAssetsVersionConfigWebReq.Dispose();
            serverAssetsInfoWebReq.Dispose();
            onTaskEndCallback(VersionControllerStatus.Success, null);
            //TODO 删除冗余资源
        }


        /// <summary>
        /// 对比版本配置
        /// 原则上认为StreamingAsset资源为母包携带,且完整
        /// </summary>
        private Queue<ServerAssetItem> Compare(List<ServerAssetItem> localAssetsInfo, List<ServerAssetItem> serverAssetsInfo)
        {
            if (localAssetsInfo == null || localAssetsInfo.Count == 0)
            {
                return new Queue<ServerAssetItem>(serverAssetsInfo);
            }

            var diffQueue = new Queue<ServerAssetItem>();
            //比对平台
            foreach (var serverAsset in serverAssetsInfo)
            {
                //比较本地是否有 hash、文件名一致的资源
                var result = localAssetsInfo.FindIndex((a) => a.HashName == serverAsset.HashName && a.LocalPath == serverAsset.LocalPath);
                //不存在
                if (result == -1)
                {
                    diffQueue.Enqueue(serverAsset);
                }
            }


            return diffQueue;
        }


        /// <summary>
        /// 修复模式
        /// Persistent资源和Streaming资源全量进行对比：文件名和hash
        /// </summary>
        private Queue<ServerAssetItem> Repair(string localRootPath, List<ServerAssetItem> localAssetsInfo, List<ServerAssetItem> serverAssetsInfo)
        {
            if (localAssetsInfo == null || localAssetsInfo.Count == 0)
            {
                return new Queue<ServerAssetItem>(serverAssetsInfo);
            }

            var diffQueue = new Queue<ServerAssetItem>();

            //平台
            var platform = BDApplication.GetPlatformPath(Application.platform);
            //
            foreach (var serverAsset in serverAssetsInfo)
            {
                //比较本地是否有 hash、文件名一致的资源
                var result = serverAssetsInfo.FindIndex((a) => a.HashName == serverAsset.HashName && a.LocalPath == serverAsset.LocalPath);
                //配置不存在
                if (result == -1)
                {
                    diffQueue.Enqueue(serverAsset);
                }
                else
                {
                    //配置存在，判断文件存不存在,存在还要判断hash         
                    var fs = IPath.Combine(localRootPath, platform, serverAsset.LocalPath);
                    if (!File.Exists(fs))
                    {
                        diffQueue.Enqueue(serverAsset);
                        continue;
                    }
                }
            }


            return diffQueue;
        }


        /// <summary>
        /// 获取版本配置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        private string GetServerAssetsVersionInfoPath(string rootPath, RuntimePlatform platform)
        {
            return IPath.Combine(rootPath, BDApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_VERSION_INFO_PATH);
        }

        /// <summary>
        /// 获取资源信息路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        private string GetAssetsInfoPath(string rootPath, RuntimePlatform platform)
        {
            return IPath.Combine(rootPath, BDApplication.GetPlatformPath(platform), BResources.ASSETS_INFO_PATH);
        }

        /// <summary>
        /// 获取分包设置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        private string GetAssetsSubPackageInfoPath(string rootPath, RuntimePlatform platform, string subPackageName)
        {
            //旧版本兼容逻辑
            if (subPackageName.StartsWith("ServerAssetsSubPackage_"))
            {
                return IPath.Combine(rootPath, BDApplication.GetPlatformPath(platform), subPackageName);
            }
            else
            {
                var subPackagePath = string.Format(BResources.SERVER_ART_ASSETS_SUB_PACKAGE_INFO_PATH, subPackageName);
                return IPath.Combine(rootPath, BDApplication.GetPlatformPath(platform), subPackagePath);
            }
        }
    }
}
