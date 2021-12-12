using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Cysharp.Text;
using ServiceStack.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.VersionContrller
{
    /// <summary>
    /// 服务器Asset的Config
    /// </summary>
    public class ServerAssetConfig
    {
        /// <summary>
        /// 平台
        /// </summary>
        public string Platfrom = "";

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version = "0.0.1";

        /// <summary>
        /// 资源子包的版本
        /// 暂未启用,后续考虑子包的自动版本更新
        /// </summary>
        public Dictionary<string, string> AssetsSubPackageVersionMap = new Dictionary<string, string>();
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
    }

    public enum UpdateMode
    {
        CompareVersionConfig, //对比版本文件
        Repair, //修复模式,对比本地文件
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
    static public class AssetsVersionContrller
    {
        /// <summary>
        /// 服务器错误码
        /// </summary>
        public enum VersionControllerStatus
        {
            Error,
            Success
        }

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="serverConfigPath"></param>
        /// <param name="assetPackageName">分包名,如果不填则为下载所有</param>
        /// <param name="onProccess">下载进度</param>
        /// <param name="onTaskEndCallback">结果回调</param>
        static public void Start(UpdateMode mode, string serverConfigPath, string assetPackageName = "", Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess = null, Action<VersionControllerStatus, string> onTaskEndCallback = null)
        {
            //下载资源位置必须为Persistent
            IEnumeratorTool.StartCoroutine(IE_StartVersionControl(mode, serverConfigPath, Application.persistentDataPath, assetPackageName, onDownloadProccess, onTaskEndCallback));
        }

        /// <summary>
        /// 重试次数
        /// </summary>
        private static int RETRY_COUNT = 5;

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="serverUrl">服务器配置根目录</param>
        /// <param name="localAssetsRootPath">本地根目录</param>
        /// <param name="onDownloadProccess"></param>
        /// <param name="onError">返回失败后，只需要重试重新调用该函数即可</param>
        /// 返回码: -1：error  0：success
        static private IEnumerator IE_StartVersionControl(UpdateMode mode, string serverUrl, string localAssetsRootPath, string subPackageName, Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess,
            Action<VersionControllerStatus, string> onTaskEndCallback)
        {
            //目录准备
            var platform = BDApplication.GetPlatformPath(Application.platform);
            var platformPath = ZString.Format("{0}/{1}", localAssetsRootPath, platform);
            if (!Directory.Exists(platformPath))
            {
                Directory.CreateDirectory(platformPath);
            }

            //子包模式判断
            bool isDownloadSubPackageMode = !string.IsNullOrEmpty(subPackageName);
            //开始下载服务器配置
            var serverAssetsVersionConfigUrl = GetServerAssetsVersionConfigPath(serverUrl, Application.platform);
            var localAssetsVersionConfigPath = GetServerAssetsVersionConfigPath(localAssetsRootPath, Application.platform);
            BDebug.Log("server version config:" + serverAssetsVersionConfigUrl);
            /**********************获取差异列表**********************/
            Queue<ServerAssetItem> diffDownloadQueue = null;
            //1.下载服务器version config

            #region ServerAssetsConfig 处理

            var serverAssetsVersionConfigWebReq = UnityWebRequest.Get(serverAssetsVersionConfigUrl);
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

            //读取服务器配置
            var serverconf = LitJson.JsonMapper.ToObject<ServerAssetConfig>(serverAssetsVersionConfigWebReq.downloadHandler.text);
            var localconf = new ServerAssetConfig();
            if (File.Exists(localAssetsVersionConfigPath))
            {
                localconf = LitJson.JsonMapper.ToObject<ServerAssetConfig>(File.ReadAllText(localAssetsVersionConfigPath));
            }

            #endregion


            //2.服务器AssetInfo 本地AssetInfo对比

            #region 对比版本,生成差异列表

            List<ServerAssetItem> serverAssetsInfoList = new List<ServerAssetItem>();
            string serverAssetInfosUrl = "";
            //全量包下载
            if (!isDownloadSubPackageMode)
            {
                BDebug.Log("【版本控制】全量下载模式!");
                //判断版本号
                if (localconf == null || localconf.Version != serverconf.Version)
                {
                    serverAssetInfosUrl = GetServerAssetsVersionInfoPath(serverUrl, Application.platform);
                }
            }
            else
            {
                BDebug.Log("【版本控制】子包模式:" + subPackageName);
                //分包逻辑
                serverAssetInfosUrl = GetServerAssetsSubPackageInfoPath(serverUrl, Application.platform, subPackageName);
            }

            //重试5次下载服务器配置
            var serverAssetsInfoWebReq = UnityWebRequest.Get(serverAssetInfosUrl);
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


            //读取本地AssetInfo，persistent没有 就去streamingAssets中读
            var localAssetInfoPath = GetServerAssetsVersionInfoPath(localAssetsRootPath, Application.platform);
            var localserverAssetsInfoList = new List<ServerAssetItem>();
            string localAssetsInfoFileContent = null;
            //本地存在
            if (File.Exists(localAssetInfoPath))
            {
                localAssetsInfoFileContent = File.ReadAllText(localAssetInfoPath);
            }
            //本地不存在则从streaming中读，streaming也不存在，则为空包，下载服务器版本，最后写到本地
            else
            {
                var steamingAssetsInfoPath = string.Format("{0}/{1}", Application.platform, BResources.SERVER_ASSETS_INFO_PATH);
                if (BetterStreamingAssets.FileExists(steamingAssetsInfoPath))
                {
                    localAssetsInfoFileContent = BetterStreamingAssets.ReadAllText(steamingAssetsInfoPath);
                    FileHelper.WriteAllText(localAssetInfoPath, localAssetsInfoFileContent);
                    BDebug.Log("【版本管理】从Streamming拷贝:" + steamingAssetsInfoPath);
                }
            }

            if (localAssetsInfoFileContent != null)
            {
                localserverAssetsInfoList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(localAssetsInfoFileContent);
            }

            //生成差异列表
            switch (mode)
            {
                case UpdateMode.CompareVersionConfig:
                    diffDownloadQueue = CompareVersionConfig(localserverAssetsInfoList, serverAssetsInfoList);
                    break;
                case UpdateMode.Repair:
                    diffDownloadQueue = Repair(localAssetsRootPath, localserverAssetsInfoList, serverAssetsInfoList);
                    break;
            }

            #endregion

            //3.开始下载
            var downloadCacheList = diffDownloadQueue.ToList();
            var failDownloadList = new List<ServerAssetItem>();
            while (diffDownloadQueue.Count > 0)
            {
                var downloadItem = diffDownloadQueue.Dequeue();
                //本地存在hash文件则不用下载,可能之前任务有部分失败了，本次重新下载
                var localDownloadFile = ZString.Format("{0}/{1}/{2}", localAssetsRootPath, platform, downloadItem.HashName);
                if (File.Exists(localDownloadFile))
                {
                    continue;
                }

                //先进行下载hash文件,所有的完成后再进行rename
                var serverAssetUrl = ZString.Format("{0}/{1}/{2}", serverUrl, platform, downloadItem.HashName);
                //下载具体资源 ,任务会重试5次
                var assetDownloadWebReq = UnityWebRequest.Get(serverAssetUrl);
                for (int i = 0; i < RETRY_COUNT; i++)
                {
                    yield return assetDownloadWebReq.SendWebRequest();
                    if (assetDownloadWebReq.error == null)
                    {
                        BDebug.Log("下载成功：" + serverAssetUrl);
                        break;
                    }
                }

                //成功通知
                if (assetDownloadWebReq.error == null)
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

                assetDownloadWebReq.Dispose();
      
            }

            if (failDownloadList.Count > 0)
            {
                onTaskEndCallback(VersionControllerStatus.Error, "部分资源未下载完毕!");
                yield break;
            }

            //4.重命名资源,并写入配置到本地
            foreach (var assetItem in downloadCacheList)
            {
                var localHashPath = ZString.Format("{0}/{1}/{2}", localAssetsRootPath, platform, assetItem.HashName);
                var localRealPath = ZString.Format("{0}/{1}/{2}", localAssetsRootPath, platform, assetItem.LocalPath);
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

            if (diffDownloadQueue.Count > 0)
            {
                //写入ConfigVersion
                File.WriteAllText(localAssetsVersionConfigPath, serverAssetsVersionConfigWebReq.downloadHandler.text);
                //写入AssetInfo
                File.WriteAllText(localAssetInfoPath, serverAssetsInfoWebReq.downloadHandler.text);
            }
            else
            {
                BDebug.Log("不用更新");
            }

            //the end.
            serverAssetsVersionConfigWebReq.Dispose();
            serverAssetsInfoWebReq.Dispose();
            onTaskEndCallback(VersionControllerStatus.Success, null);
        }


        /// <summary>
        /// 对比版本配置
        /// </summary>
        static public Queue<ServerAssetItem> CompareVersionConfig(List<ServerAssetItem> localAssetsInfo, List<ServerAssetItem> serverAssetsInfo)
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
        /// 修复模式,是要对比本地文件是否存在
        /// </summary>
        static public Queue<ServerAssetItem> Repair(string localRootPath, List<ServerAssetItem> localAssetsInfo, List<ServerAssetItem> serverAssetsInfo)
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
                    var fs = ZString.Format("{0}/{1}/{2}", localRootPath, platform, serverAsset.LocalPath);
                    if (!File.Exists(fs))
                    {
                        diffQueue.Enqueue(serverAsset);
                    }
                }
            }


            return diffQueue;
        }


        /// <summary>
        /// 获取分包资源
        /// 分包资源 + 分包依赖资源
        /// </summary>
        static public Queue<ServerAssetItem> GetMultiplePackageAssetItems()
        {
            return null;
        }

        /// <summary>
        /// 获取版本配置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetServerAssetsVersionConfigPath(string rootPath, RuntimePlatform platform)
        {
            return ZString.Format("{0}/{1}/{2}", rootPath, BDApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_VERSION_CONFIG_PATH);
        }

        static public string GetServerAssetsVersionInfoPath(string rootPath, RuntimePlatform platform)
        {
            return ZString.Format("{0}/{1}/{2}", rootPath, BDApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_INFO_PATH);
        }

        /// <summary>
        /// 获取分包设置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetServerAssetsSubPackageInfoPath(string rootPath, RuntimePlatform platform, string subPackageName)
        {
            var subPackagePath = string.Format(BResources.SERVER_ART_ASSETS_SUB_PACKAGE_INFO_PATH, subPackageName);
            return ZString.Format("{0}/{1}/{2}", rootPath, BDApplication.GetPlatformPath(platform), subPackagePath);
        }
    }
}
