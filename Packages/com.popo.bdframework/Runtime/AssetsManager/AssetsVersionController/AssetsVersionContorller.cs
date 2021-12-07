using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.VersionContrller
{
    /// <summary>
    /// 服务器Asset的Config
    /// </summary>
    public class AssetConfig
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
        /// 所有资源名
        /// </summary>
        public List<AssetItem> Assets = new List<AssetItem>();
    }

    /// <summary>
    /// 服务器Asset的描述
    /// </summary>
    public struct AssetItem
    {
        /// <summary>
        /// hash名 -服务器存储文件名
        /// </summary>
        public string HashName;

        /// <summary>
        /// 本地存储名
        /// </summary>
        public string LocalPath;
    }

    public enum UpdateMode
    {
        CompareVersionConfig, //对比版本文件
        Repair, //修复模式,对比本地文件
    }

    /// <summary>
    /// 资源分包配置
    /// </summary>
    public struct AssetMultiplePackageConfigItem
    {
        /// <summary>
        /// 分包名
        /// </summary>
        public string PackageName;

        /// <summary>
        /// 资源目录名
        /// </summary>
        public List<string> AssetsDirectPathList;
    }

    /// <summary>
    /// 版本控制
    /// </summary>
    static public class AssetsVersionContorller
    {
        /// <summary>
        /// 服务器错误码
        /// </summary>
        public enum ServerErrorCode
        {
            Error,
        }

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="serverConfigPath"></param>
        /// <param name="localConfigRootPath"></param>
        /// <param name="assetPackageName">分包名,如果不填则为下载所有</param>
        /// <param name="onProcess">成功回调</param>
        /// <param name="onError">失败回调</param>
        static public void Start(UpdateMode mode, string serverConfigPath, string localConfigRootPath, string assetPackageName = "", Action<int, int> onProcess = null, Action<string> onError = null)
        {
            IEnumeratorTool.StartCoroutine(IE_StartVersionControl(mode, serverConfigPath, localConfigRootPath, assetPackageName, onProcess, onError));
        }

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="serverUrl">服务器配置根目录</param>
        /// <param name="localConfigRootPath">本地根目录</param>
        /// <param name="onProcess"></param>
        /// <param name="onError"></param>
        /// 返回码: -1：error  0：success
        static private IEnumerator IE_StartVersionControl(UpdateMode mode, string serverUrl, string localConfigRootPath, string assetPackageName, Action<int, int> onProcess, Action<string> onError)
        {
            //开始下载服务器配置
            var platform = BDApplication.GetPlatformPath(Application.platform);
            var serverVersionConfigUrl = GetServerAssetsVersionConfigPath(serverUrl, Application.platform);
            var localVersionConfigPath = GetServerAssetsVersionConfigPath(localConfigRootPath, Application.platform);
            BDebug.Log("server version config:" + serverVersionConfigUrl);
            /**********************获取差异列表**********************/
            Queue<AssetItem> diffDownloadQueue = null;
            //1.下载服务器config
            var serverVersionConfigWebReq = UnityWebRequest.Get(serverVersionConfigUrl);
            yield return serverVersionConfigWebReq.SendWebRequest();
            if (serverVersionConfigWebReq.error == null)
            {
                BDebug.Log("服务器资源配置:" + serverVersionConfigWebReq.downloadHandler.text);
            }
            else
            {
                BDebug.LogError(serverVersionConfigWebReq.error);
                onError(serverVersionConfigWebReq.error);
                yield break;
            }

            //服务器配置
            var serverconf = LitJson.JsonMapper.ToObject<AssetConfig>(serverVersionConfigWebReq.downloadHandler.text);
            //本地配置
            AssetConfig localconf = null;
            if (File.Exists(localVersionConfigPath))
            {
                localconf = LitJson.JsonMapper.ToObject<AssetConfig>(File.ReadAllText(localVersionConfigPath));
            }

            //1.1 判断不同package name需要下载的资源列表
            if (!string.IsNullOrEmpty(assetPackageName))
            {
                var serverMultiplePackageConfigUrl = GetServerAssetsMultipleConfigPath(serverUrl, Application.platform);
            }

            //2.对比差异列表进行下载
            //不同模式生成不同下载列表
            switch (mode)
            {
                case UpdateMode.CompareVersionConfig:
                    diffDownloadQueue = CompareVersionConfig(localconf, serverconf);
                    break;
                case UpdateMode.Repair:

                    diffDownloadQueue = Repair(localConfigRootPath, localconf, serverconf);
                    break;
            }

            if (diffDownloadQueue.Count > 0)
            {
                //预通知要进入热更模式
                onProcess(0, diffDownloadQueue.Count);
            }


            //3.开始下载
            int downloadCounter = 0;
            var totalNum = diffDownloadQueue.Count;
            while (diffDownloadQueue.Count > 0)
            {
                var downloadItem = diffDownloadQueue.Dequeue();
                var serverAssetUrl = ZString.Format("{0}/{1}/{2}", serverUrl, platform, downloadItem.HashName);
                var localPath = ZString.Format("{0}/{1}/{2}", localConfigRootPath, platform, downloadItem.LocalPath);
                //创建目录
                var direct = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(direct))
                {
                    Directory.CreateDirectory(direct);
                }

                //下载具体资源
                var spWebReq = UnityWebRequest.Get(serverAssetUrl);
                yield return spWebReq.SendWebRequest();
                if (spWebReq.error == null)
                {
                    BDebug.Log("下载成功：" + serverAssetUrl);
                    File.WriteAllBytes(localPath, spWebReq.downloadHandler.data);
                    onProcess(totalNum - diffDownloadQueue.Count, totalNum);
                }
                else
                {
                    //低于失败次数 持续下载,高于失败次数 提示失败
                    if (downloadCounter < totalNum * 2)
                    {
                        diffDownloadQueue.Enqueue(downloadItem);
                    }
                    else
                    {
                        BDebug.LogError("下载失败:" + spWebReq.error);
                        onError(spWebReq.error);
                        yield break;
                    }
                }

                spWebReq.Dispose();
                //自增
                downloadCounter++;
            }

            //4.配置写到本地
            if (diffDownloadQueue.Count > 0)
            {
                File.WriteAllText(localVersionConfigPath, serverVersionConfigWebReq.downloadHandler.text);
            }
            else
            {
                BDebug.Log("不用更新");
                onProcess(1, 1);
            }

            //the end.
            serverVersionConfigWebReq.Dispose();
        }


        /// <summary>
        /// 对比版本配置
        /// </summary>
        static public Queue<AssetItem> CompareVersionConfig(AssetConfig localConfig, AssetConfig serverConfig)
        {
            if (localConfig == null)
            {
                return new Queue<AssetItem>(serverConfig.Assets);
            }

            var diffQueue = new Queue<AssetItem>();
            //比对平台
            if (localConfig.Platfrom == serverConfig.Platfrom)
            {
                foreach (var serverAsset in serverConfig.Assets)
                {
                    //比较本地是否有 hash、文件名一致的资源
                    var result = localConfig.Assets.FindIndex((a) => a.HashName == serverAsset.HashName && a.LocalPath == serverAsset.LocalPath);
                    //不存在
                    if (result == -1)
                    {
                        diffQueue.Enqueue(serverAsset);
                    }
                }
            }

            return diffQueue;
        }


        /// <summary>
        /// 修复模式,是要对比本地文件是否存在
        /// </summary>
        static public Queue<AssetItem> Repair(string localRootPath, AssetConfig localConfig, AssetConfig serverConfig)
        {
            if (localConfig == null)
            {
                return new Queue<AssetItem>(serverConfig.Assets);
            }

            var diffQueue = new Queue<AssetItem>();
            //比对平台
            if (localConfig.Platfrom == serverConfig.Platfrom)
            {
                //平台
                var platform = BDApplication.GetPlatformPath(Application.platform);
                //
                foreach (var serverAsset in serverConfig.Assets)
                {
                    //比较本地是否有 hash、文件名一致的资源
                    var result = localConfig.Assets.FindIndex((a) => a.HashName == serverAsset.HashName && a.LocalPath == serverAsset.LocalPath);
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
            }

            return diffQueue;
        }


        /// <summary>
        /// 获取分包资源
        /// 分包资源 + 分包依赖资源
        /// </summary>
        static public Queue<AssetItem> GetMultiplePackageAssetItems()
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
            return ZString.Format("{0}/{1}/{2}", rootPath, BDApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_VERSION_CONFIG);
        }

        /// <summary>
        /// 获取分包设置路径
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetServerAssetsMultipleConfigPath(string rootPath, RuntimePlatform platform)
        {
            return ZString.Format("{0}/{1}/{2}", rootPath, BDApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_MULTIPLE_PACKAGE_CONFIG);
        }
    }
}
