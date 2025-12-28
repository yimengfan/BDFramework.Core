using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BDFramework.Asset;
using BDFramework.Assets.VersionContrller;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using LitJson;
using MurmurHash.Net;
using ServiceStack.Text;
using Telepathy;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.VersionController
{
    /// <summary>
    /// 服务器Asset的Config
    /// </summary>
    public class AssetsVersionInfo
    {
        /// <summary>
        /// 平台
        /// </summary>
        public string Platfrom { get; set; } = "";

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = "";

        /// <summary>
        /// 资源子包的版本
        /// packname - verion
        /// </summary>
        public Dictionary<string, string> SubPckMap { get; private set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 服务器Asset的描述
    /// </summary>
    public class AssetItem
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
            if (obj is AssetItem _SAI)
            {
                return (this.HashName.Equals(_SAI.HashName) && this.LocalPath.Equals(_SAI.LocalPath));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ZString.Concat(this.LocalPath, "|", this.HashName).GetHashCode();
        }
    }

    public enum UpdateMode
    {
        /// <summary>
        /// 1.对比版本: 本地、服务器版本信息,版本号相同跳过
        /// 2.获取差异: 对比本地与服务器差异(只对比配置和判断文件是否存在)
        /// 3.下载资源: 根据差异信息下载
        /// </summary>
        Compare,

        /// <summary>
        /// 1.对比版本: 不对比
        /// 2.获取差异: 遍历服务器版本,判断本地文件和hash值
        /// 3.下载资源: 根据差异信息下载
        /// </summary>
        Repair,

        /// <summary>
        /// 1.对比版本: 跟Compare模式一致
        /// 2.获取差异: 跟Repair模式一致
        /// 3.下载资源: 根据差异信息下载
        /// 本质上时:每次版本变化做一次修复
        /// 建议使用该模式,平时开启Remote资源加载防止资源丢失
        /// </summary>
        CompareWithRepair,
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
        private static string LogTag = "版本控制";

        /// <summary>
        /// 操作状态码
        /// </summary>
        public enum RetStatus
        {
            Checkassets = 0,
            DeleteOldAssets,
            Error,
            Success
        }


        public AssetsVersionController()
        {
            //BSA初始化
            BetterStreamingAssets.Initialize();
        }

        /// <summary>
        /// 重试次数
        /// </summary>
        private static uint RETRY_COUNT = 5;

        /// <summary>
        /// 设置重试次数
        /// </summary>
        /// <param name="count"></param>
        public static void SetRetryCount(uint count)
        {
            RETRY_COUNT = count;
        }

        /// <summary>
        /// 开始版本控制
        /// </summary>
        /// <param name="updateMode"></param>
        /// <param name="serverConfigUrl"></param>
        /// <param name="assetsPackageName">分包名,如果不填则为下载所有</param>
        /// <param name="onProccess">下载进度</param>
        /// <param name="onTaskEndCallback">结果回调</param>
        public void UpdateAssets(UpdateMode updateMode, string serverConfigUrl, string assetsPackageName = "", Action<AssetItem, List<AssetItem>> onDownloadProccess = null, Action<RetStatus, string> onTaskEndCallback = null)
        {
            BDebug.EnableLog(LogTag);
            //下载资源位置必须为Persistent
            UniTask.RunOnThreadPool(() =>
            {
                //开始版本控制逻辑
                StartVersionControl(updateMode, serverConfigUrl, BApplication.persistentDataPath, assetsPackageName, onDownloadProccess, onTaskEndCallback);
            });
        }


        /// <summary>
        /// 获取服务器子包信息
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="callback"></param>
        public void GetServerSubPackageInfos(string serverUrl, Action<Dictionary<string, string>> callback)
        {
            //下载资源位置必须为Persistent
            UniTask.RunOnThreadPool(() =>
            {
                //
                GetServerVersionInfo(serverUrl, callback);
            });
            // Debug.Log("test:------------------------");
        }


        /// <summary>
        ///获取服务器版本信息
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        async private Task GetServerVersionInfo(string serverUrl, Action<Dictionary<string, string>> callback, Action<string> onError = null)
        {
            var ret = await DownloadAssetVersionInfo(serverUrl, null);
            if (ret.Item1 != null)
            {
                await UniTask.SwitchToMainThread();
                //返回err信息
                onError?.Invoke(ret.Item1);
                return;
            }

            var serverVersionInfo = ret.Item2;
            await UniTask.SwitchToMainThread();
            callback?.Invoke(serverVersionInfo.SubPckMap);
        }

        /// <summary>
        /// 开始版本控制逻辑
        /// </summary>
        /// <param name="serverUrl">服务器配置根目录</param>
        /// <param name="localSaveAssetsPath">本地根目录</param>
        /// <param name="onDownloadProccess">任务进度通知（下载完不等于任务完成!）</param>
        /// <param name="onTaskEndCallback">任务成功\失败通知!</param>
        /// 返回码: -1：error  0：success
        async private Task StartVersionControl(UpdateMode updateMode, string serverUrl, string localSaveAssetsPath, string subPackageName, Action<AssetItem, List<AssetItem>> onDownloadProccess, Action<RetStatus, string> onTaskEndCallback)
        {
            var platform = BApplication.RuntimePlatform;
            //目录准备
            var platformStr = BApplication.GetRuntimePlatformPath();
            var localSavePlatformPath = IPath.Combine(localSaveAssetsPath, platformStr);
            if (!Directory.Exists(localSavePlatformPath))
            {
                Directory.CreateDirectory(localSavePlatformPath);
            }

            //子包模式判断
            bool isDownloadSubPackageMode = !string.IsNullOrEmpty(subPackageName);


            //1.下载服务器version config
            var serverVersionInfo = new AssetsVersionInfo();
            var localVersionInfo = new AssetsVersionInfo();

            #region AssetVersion.info对比，简单对比信息

            BDebug.Log(LogTag, "1.获取server版本信息~", Color.red);
            {
                var ret = await DownloadAssetVersionInfo(serverUrl, localSaveAssetsPath);
                if (ret.Item1 != null)
                {
                    await UniTask.SwitchToMainThread();
                    onTaskEndCallback?.Invoke(RetStatus.Error, ret.Item1);
                    return;
                }

                serverVersionInfo = ret.Item2;
                localVersionInfo = ret.Item3;
            }

            #endregion

            //2.对比版本、获取对应数据
            BDebug.Log(LogTag, $"2.对比版本信息,模式:{updateMode}", Color.red);
            string err = null;
            string suc = null;
            var serverAssetsInfoList = new List<AssetItem>();
            var localAssetsInfoList = new List<AssetItem>();
            var serverAssetsContent = "";
            //
            switch (updateMode)
            {
                case UpdateMode.Compare:
                case UpdateMode.CompareWithRepair: //CP模式对比版本与Compare一致
                {
                    if (isDownloadSubPackageMode)
                    {
                        //分包模式
                        (err, suc, serverAssetsInfoList, localAssetsInfoList, serverAssetsContent) = GetDownloadSubPackageData(serverUrl, subPackageName, platform, serverVersionInfo, localVersionInfo);
                    }
                    else
                    {
                        //全量下载
                        (err, suc, serverAssetsInfoList, localAssetsInfoList, serverAssetsContent) = GetDownloadAssetsData(serverUrl, platform, serverVersionInfo, localVersionInfo);
                    }
                }
                    break;
                case UpdateMode.Repair:
                {
                    //服务器路径
                    var serverAssetInfosUrl = BResources.GetAssetsInfoPath(serverUrl, platform);
                    //下载服务器Assets.info
                    (err, serverAssetsInfoList, serverAssetsContent) = LoadServerAssetInfo(serverAssetInfosUrl);
                }
                    break;
            }

            //返回返回结果，是否继续下载
            if (err != null)
            {
                BDebug.LogError(err);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Error, err);
                return;
            }

            if (suc != null)
            {
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Success, suc);
                return;
            }


            //3.生成差异列表
            BDebug.Log(LogTag, "3.获取差异列表~", Color.red);

            Queue<AssetItem> diffDownloadQueue = null;

            #region 生成差异文件

            BDebug.LogWatchBegin("差异列表");
            switch (updateMode)
            {
                case UpdateMode.Compare:
                {
                    diffDownloadQueue = Compare(localAssetsInfoList, serverAssetsInfoList, platform);
                }
                    break;
                case UpdateMode.Repair:
                case UpdateMode.CompareWithRepair: //CP 获取差异模式与Repair一致
                {
                    diffDownloadQueue = Repair(serverAssetsInfoList, platform);
                }
                    break;
            }

            BDebug.LogWatchEnd("差异列表");
            BDebug.Log(LogTag, $" 配置数量:{serverAssetsInfoList.Count} ,本地存在{serverAssetsInfoList.Count - diffDownloadQueue.Count},下载文件数量{diffDownloadQueue.Count}", Color.yellow);

            #endregion

            //4.开始下载

            #region 根据差异文件下载

            BDebug.Log(LogTag, "4.下载资源:", Color.red);
            if (diffDownloadQueue.Count > 0)
            {
                var failDownloadList = await DownloadAssets(serverUrl, localSaveAssetsPath, diffDownloadQueue, onDownloadProccess);
                if (failDownloadList.Count > 0)
                {
                    onTaskEndCallback(RetStatus.Error, "部分资源未下载完毕!");
                    return;
                }
            }

            #endregion


            //5.写入配置到本地

            #region 存储配置到本地

            BDebug.Log(LogTag, "5.写入配置~", Color.red);
            string localAssetInfoPath = "";
            if (isDownloadSubPackageMode)
            {
                localAssetInfoPath =  ClientAssetsUtils.GetPersistentAssetPath(subPackageName);
                //BResources.GetAssetsSubPackageInfoPath(BApplication.persistentDataPath, platform, subPackageName);
            }
            else
            {
                localAssetInfoPath = ClientAssetsUtils.GetPersistentAssetPath(BResources.ASSETS_INFO_PATH);//BResources.GetAssetsInfoPath();
            }

            //写入Asset.Info
            File.WriteAllText(localAssetInfoPath, serverAssetsContent);
            BDebug.Log(LogTag, $"写入{Path.GetFileName(localAssetInfoPath)}  \n {serverAssetsContent}");

            //写入Version.Info
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

            var localAssetsVersionInfoPath = BResources.GetServerAssetsVersionInfoPath(localSaveAssetsPath, platform);
            File.WriteAllText(localAssetsVersionInfoPath, JsonMapper.ToJson(localVersionInfo));
            BDebug.Log(LogTag, $"写入{Path.GetFileName(localAssetsVersionInfoPath)}");

            #endregion

            // 6.删除过期资源
            BDebug.Log(LogTag, "【版本控制】6.过期资源检查~", Color.red);
            if (!isDownloadSubPackageMode)
            {
                var artAssetsPath = IPath.Combine(localSavePlatformPath, BResources.ART_ASSET_ROOT_PATH);
                if (Directory.Exists(artAssetsPath))
                {
                    var persistentArtAssets = Directory.GetFiles(artAssetsPath, "*", SearchOption.AllDirectories);
                    var replacePath = localSavePlatformPath + "/";
                    foreach (var assetPath in persistentArtAssets)
                    {
                        var localPath = assetPath.Replace(replacePath, "").Replace("\\", "/");
                        var ret = serverAssetsInfoList.FirstOrDefault((info) => info.LocalPath.Equals(localPath));
                        if (ret == null)
                        {
                            await UniTask.SwitchToMainThread();
                            onTaskEndCallback?.Invoke(RetStatus.DeleteOldAssets, localPath);
                            BDebug.Log(LogTag, "删除过期资源:" + localPath);
                            File.Delete(assetPath);
                        }
                    }
                }
            }

            // 7.资源校验文件
            BDebug.Log(LogTag, "7.差异资源校验~", Color.red);
            err = null;
            foreach (var serverAssetItem in serverAssetsInfoList)
            {
                var ret = BResources.IsExsitAssetWithCheckHash(platform, serverAssetItem.LocalPath, serverAssetItem.HashName);

                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(RetStatus.Checkassets, serverAssetItem.HashName);
                if (!ret)
                {
                    if (string.IsNullOrEmpty(err))
                    {
                        err = "资源不存在:";
                    }

                    err += $"\n {serverAssetItem.LocalPath}";
                }
            }

            //the end.
            BDebug.Log(LogTag, "end.资源下载完成~", Color.red);
            await UniTask.SwitchToMainThread();
            if (err == null)
            {
                onTaskEndCallback?.Invoke(RetStatus.Success, null);
            }
            else
            {
                onTaskEndCallback?.Invoke(RetStatus.Error, err);
            }
        }

        #region 不同模式逻辑

        /// <summary>
        /// 获取下载资源数据
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="localSaveAssetsPath"></param>
        /// <param name="platform"></param>
        /// <param name="serverVersionInfo"></param>
        /// <param name="localVersionInfo"></param>
        /// <returns>err, suc, 服务器数据, 本地数据,服务器数据内容 </returns>
        public (string, string, List<AssetItem>, List<AssetItem>, string) GetDownloadAssetsData(string serverUrl, RuntimePlatform platform, AssetsVersionInfo serverVersionInfo, AssetsVersionInfo localVersionInfo)
        {
            //返回数据
            string err = null;
            string suc = null;
            var serverAssetsInfoList = new List<AssetItem>();
            var localAssetsInfoList = new List<AssetItem>();
            var serverAssetsContent = "";

            //1.判断版本号
            if (VersionNumHelper.GT(localVersionInfo.Version, serverVersionInfo.Version))
            {
                suc = $"本地版本相同或更新,无需下载! serVer:{serverVersionInfo.Version}  localVer:{localVersionInfo.Version}";
                BDebug.Log(LogTag, suc, Color.red);
                return (err, suc, null, null, null);
            }

            //2.获取Assets.info
            BDebug.Log(LogTag, $"全量下载模式! serVer:{serverVersionInfo.Version} localVer:{localVersionInfo.Version} ", Color.red);
            {
                //服务器路径
                var serverAssetInfosUrl = BResources.GetAssetsInfoPath(serverUrl, platform);
                //下载服务器Assets.info
                (err, serverAssetsInfoList, serverAssetsContent) = LoadServerAssetInfo(serverAssetInfosUrl);
            }
            //加载本地SubPackage配置
            localAssetsInfoList = LoadLocalSubPacakgeAssetInfo(platform, localVersionInfo);

            //加载本地asset.info
            var localAssetsInfo = this.LoadLocalAssetInfo();
            localAssetsInfoList.AddRange(localAssetsInfo);
            //去重
            localAssetsInfoList = localAssetsInfoList.Distinct().ToList();
            localAssetsInfoList.Sort((a, b) =>
            {
                if (a.Id < b.Id)
                {
                    return -1;
                }
                else if (a.Id == b.Id)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            });
            //返回
            return (err, suc, serverAssetsInfoList, localAssetsInfoList, serverAssetsContent);
        }

        /// <summary>
        /// 获取下载子包的数据
        /// </summary>
        /// <returns>err, suc, server.info, local.info, </returns>
        public (string, string, List<AssetItem>, List<AssetItem>, string) GetDownloadSubPackageData(string serverUrl, string subPackageName, RuntimePlatform platform, AssetsVersionInfo serverVersionInfo, AssetsVersionInfo localVersionInfo)
        {
            //返回数据
            string err = null;
            string suc = null;
            var serverAssetsInfoList = new List<AssetItem>();
            var localAssetsInfoList = new List<AssetItem>();
            var serverAssetsContent = "";

            BDebug.Log(LogTag, "分包模式:" + subPackageName);
            //AssetInfo路径
            //1.判断版本号
            var ret = serverVersionInfo.SubPckMap.TryGetValue(subPackageName, out var serverSubPckVersion);
            if (!ret)
            {
                err = "【版本控制】服务器不存在子包:" + subPackageName;
                return (err, suc, null, null, null);
            }


            localVersionInfo.SubPckMap.TryGetValue(subPackageName, out var localSubPckVersion);
            if (serverSubPckVersion == localSubPckVersion)
            {
                suc = "【版本控制】分包版本一致,无需下载!";
                BDebug.Log(LogTag, suc);
                return (err, suc, null, null, null);
            }

            //本地版本记录
            if (!string.IsNullOrEmpty(localVersionInfo.Version) && !string.IsNullOrEmpty(localSubPckVersion))
            {
                if (localVersionInfo.Version != localSubPckVersion)
                {
                    BDebug.Log(LogTag, "分包资源大于 本地整包资源.请注意资源版本有可能不匹配!", Color.red);
                }
            }

            //2.下载AssetInfo
            BDebug.Log(LogTag, $"分包下载模式! server:{serverSubPckVersion} local:{localSubPckVersion} ", Color.red);
            {
                //服务器路径
                var serverAssetInfosUrl = BResources.GetAssetsSubPackageInfoPath(serverUrl, platform, subPackageName);
                //下载服务器配置
                (err, serverAssetsInfoList, serverAssetsContent) = LoadServerAssetInfo(serverAssetInfosUrl);
            }

            //1.加载本地SubPackage配置
            localAssetsInfoList = LoadLocalSubPacakgeAssetInfo(platform, localVersionInfo);

            //2.加载本地asset.info
            var localAssetsInfo = this.LoadLocalAssetInfo();
            localAssetsInfoList.AddRange(localAssetsInfo);
            //去重、排序
            localAssetsInfoList = localAssetsInfoList.Distinct().ToList();
            localAssetsInfoList.Sort((a, b) =>
            {
                if (a.Id < b.Id)
                {
                    return -1;
                }
                else if (a.Id == b.Id)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            });
            //返回
            return (err, suc, serverAssetsInfoList, localAssetsInfoList, serverAssetsContent);
        }

        #endregion


        #region 执行主逻辑

        private WebClient webClient = new WebClient();

        /// <summary>
        /// 下载Version.info
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="localSaveAssetsPath"></param>
        /// <returns></returns>
        async private Task<Tuple<string, AssetsVersionInfo, AssetsVersionInfo>> DownloadAssetVersionInfo(string serverUrl, string localSaveAssetsPath = null)
        {
            var platform = BApplication.RuntimePlatform;
            //本地、服务器版本信息的路径
            var serverAssetsVersionInfoUrl = BResources.GetServerAssetsVersionInfoPath(serverUrl, platform);

            string err = null;
            AssetsVersionInfo serverVersionInfo = null;
            AssetsVersionInfo localVersionInfo = null;
            //开始下载服务器配置
            for (int i = 0; i < RETRY_COUNT; i++)
            {
                try
                {
                    var task = await webClient.DownloadStringTaskAsync(serverAssetsVersionInfoUrl);
                    serverVersionInfo = JsonMapper.ToObject<AssetsVersionInfo>(task);
                    err = null;
                    break;
                }
                catch (Exception e)
                {
                    err = e.Message;
                    BDebug.LogError(serverAssetsVersionInfoUrl + " 下载失败! " + err);
                }
            }

            //判断本地路径
            if (!string.IsNullOrEmpty(localSaveAssetsPath))
            {
                var localAssetsVersionInfoPath = BResources.GetServerAssetsVersionInfoPath(localSaveAssetsPath, platform);
                if (File.Exists(localAssetsVersionInfoPath))
                {
                    localVersionInfo = JsonMapper.ToObject<AssetsVersionInfo>(File.ReadAllText(localAssetsVersionInfoPath));
                }
                else
                {
                    await UniTask.SwitchToMainThread();
                    BDebug.Log("Persistent不存在server_assets_version.info, 使用母包的package_build.info! ", Color.yellow);
                    var basePackBuildInfo = ClientAssetsUtils.GetBasePackBuildInfo();
                    localVersionInfo = new AssetsVersionInfo();
                    if (basePackBuildInfo != null)
                    {
                        localVersionInfo.Version = basePackBuildInfo.Version;
                    }
                }
            }

            //返回
            return new Tuple<string, AssetsVersionInfo, AssetsVersionInfo>(err, serverVersionInfo, localVersionInfo);
        }


        /// <summary>
        /// 加载服务器Asset.Info
        /// </summary>
        /// <returns></returns>
        async private Task<Tuple<string, List<AssetItem>, string>> DownloadAssetsInfo(string serverAssetInfosUrl)
        {
            var err = "";
            var serverAssetsInfoList = new List<AssetItem>();
            var serverlAssetsContent = "";
            //开始下载
            for (int i = 0; i < RETRY_COUNT; i++)
            {
                try
                {
                    serverlAssetsContent = await webClient.DownloadStringTaskAsync(serverAssetInfosUrl);
                    serverAssetsInfoList = CsvSerializer.DeserializeFromString<List<AssetItem>>(serverlAssetsContent);
                    err = null;
                    break;
                }
                catch (Exception e)
                {
                    err = e.Message;
                    BDebug.LogError(err);
                }
            }

            return new Tuple<string, List<AssetItem>, string>(err, serverAssetsInfoList, serverlAssetsContent);
        }


        /// <summary>
        /// 加载服务器Asset.info
        /// </summary>
        /// <returns></returns>
        private (string, List<AssetItem>, string) LoadServerAssetInfo(string serverAssetInfosUrl)
        {
            //返回数据
            string err = null;
            var serverAssetsInfoList = new List<AssetItem>();
            var serverAssetsContent = "";
            //下载
            var task = DownloadAssetsInfo(serverAssetInfosUrl);
            task.Wait();
            if (task.Result.Item1 != null)
            {
                err = task.Result.Item1;
            }

            serverAssetsInfoList = task.Result.Item2;
            serverAssetsContent = task.Result.Item3;

            return (err, serverAssetsInfoList, serverAssetsContent);
        }

        /// <summary>
        /// 加载本地的Asset.info
        /// </summary>
        private List<AssetItem> LoadLocalAssetInfo()
        {
            var retList = new List<AssetItem>();
            //优先加载persistent的Assets.info
            var persistentAssetInfoPath = ClientAssetsUtils.GetPersistentAssetPath(BResources.ASSETS_INFO_PATH);
            if (File.Exists(persistentAssetInfoPath))
            {
                var content = File.ReadAllText(persistentAssetInfoPath);
                retList = CsvSerializer.DeserializeFromString<List<AssetItem>>(content);
            }
            //streaming 和其他的Assets.info
            else
            {
                var steamingAssetsInfoPath = ClientAssetsUtils.GetStreamingAssetPath(BResources.ASSETS_INFO_PATH);
#if UNITY_ANDROID
                if (BetterStreamingAssets.FileExists(steamingAssetsInfoPath))
                {
                    var content = BetterStreamingAssets.ReadAllText(steamingAssetsInfoPath);
                    retList = CsvSerializer.DeserializeFromString<List<AssetItem>>(content);
                }
#else

                if (File.Exists(steamingAssetsInfoPath))
                {
                    var content = File.ReadAllText(steamingAssetsInfoPath);
                    retList = CsvSerializer.DeserializeFromString<List<AssetItem>>(content);
                }
#endif
            }

            return retList;
        }


        /// <summary>
        /// 加载SubPacakge的AssetInfo
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        private List<AssetItem> LoadLocalSubPacakgeAssetInfo(RuntimePlatform platform, AssetsVersionInfo localVersionInfo)
        {
            var retList = new List<AssetItem>();

            foreach (var kv in localVersionInfo.SubPckMap)
            {
                var subPackageInfoPath = BResources.GetAssetsSubPackageInfoPath(BApplication.persistentDataPath, platform, kv.Key);
                if (File.Exists(subPackageInfoPath))
                {
                    var content = File.ReadAllText(subPackageInfoPath);
                    var assetItems = CsvSerializer.DeserializeFromString<List<AssetItem>>(content);
                    retList.AddRange(assetItems);
                }
            }


            return retList;
        }

        /// <summary>
        /// 下载Assets
        /// 用于加载资源时,本地不存在则向服务器请求
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="localSaveAssetsPath"></param>
        /// <param name="downloadQueue"></param>
        /// <param name="onDownloadProccess"></param>
        /// <param name="failDownloadList"></param>
        /// <returns></returns>
        public IEnumerator IE_DownloadAssets(string serverUrl, string localSaveAssetsPath, Queue<AssetItem> downloadQueue, Action<AssetItem, List<AssetItem>> onDownloadProccess)
        {
            var failDownloadList = new List<AssetItem>();
            //url构建
            var platform = BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform);
            serverUrl = IPath.Combine(serverUrl, platform);
            localSaveAssetsPath = IPath.Combine(localSaveAssetsPath, platform);
            //1.任务缓存
            var downloadCacheList = downloadQueue.ToList();

            //2.开始任务
            while (downloadQueue.Count > 0)
            {
                var downloadItem = downloadQueue.Dequeue();

                //本地存在hash文件
                var localDownloadFile = IPath.Combine(localSaveAssetsPath, downloadItem.HashName);
                //下载
                UnityWebRequest uwq = null;

                //先进行下载hash文件,所有的完成后再进行rename成资源
                var serverAssetUrl = IPath.Combine(serverUrl, downloadItem.HashName);
                //下载具体资源 ,任务会重试5次
                uwq = UnityWebRequest.Get(serverAssetUrl);

                for (int i = 0; i < RETRY_COUNT; i++)
                {
                    yield return uwq.SendWebRequest();

                    if (uwq.isHttpError || uwq.isNetworkError)
                    {
                        //对比hash
                        var downloadFileHash = FileHelper.GetMurmurHash3(uwq.downloadHandler.data);
                        if (downloadFileHash == downloadItem.HashName)
                        {
                            BDebug.Log(LogTag, "下载成功：" + serverAssetUrl);
                            break;
                        }
                        else
                        {
                            BDebug.LogError("【版本控制】重下, hash校验失败! server-" + downloadItem.HashName + " local-" + downloadFileHash);
                        }
                    }
                }

                //
                if (!uwq.isHttpError && !uwq.isNetworkError)
                {
                    onDownloadProccess(downloadItem, downloadCacheList);
                    FileHelper.WriteAllBytes(localDownloadFile, uwq.downloadHandler.data);
                }
                else
                {
                    //这边需要继续下载,最后统计失败文件
                    failDownloadList.Add(downloadItem);
                    BDebug.LogError("下载失败:" + uwq.error);
                }

                uwq?.Dispose();
            }

            //3.写入本地
            foreach (var assetItem in downloadCacheList)
            {
                var localHashPath = IPath.Combine(localSaveAssetsPath, assetItem.HashName);
                var localRealPath = IPath.Combine(localSaveAssetsPath, assetItem.LocalPath);
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
        }

        /// <summary>
        /// 下载Assets
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="localSaveAssetsPath"></param>
        /// <param name="downloadQueue"></param>
        /// <param name="onDownloadProccess"></param>
        /// <returns></returns>
        async private Task<List<AssetItem>> DownloadAssets(string serverUrl, string localSaveAssetsPath, Queue<AssetItem> downloadQueue, Action<AssetItem, List<AssetItem>> onDownloadProccess)
        {
            var failDownloadList = new List<AssetItem>();
            //url构建
            var platform = BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform);
            serverUrl = IPath.Combine(serverUrl, platform);
            localSaveAssetsPath = IPath.Combine(localSaveAssetsPath, platform);
            //1.任务缓存
            var downloadCacheList = downloadQueue.ToList();
            //2.开始任务
            while (downloadQueue.Count > 0)
            {
                var downloadItem = downloadQueue.Dequeue();
                //本地存在hash文件
                var localHashFile = IPath.Combine(localSaveAssetsPath, downloadItem.HashName);
                var serverAssetUrl = IPath.Combine(serverUrl, downloadItem.HashName);
                var err = "";
                //开始下载
                byte[] taskByte = null;
                for (int i = 0; i < RETRY_COUNT; i++)
                {
                    try
                    {
                        var taskData = await webClient.DownloadDataTaskAsync(serverAssetUrl);
                        var hash = FileHelper.GetMurmurHash3(taskData);
                        if (hash == downloadItem.HashName)
                        {
                            taskByte = taskData;
                            BDebug.Log(LogTag, $"下载成功：{serverAssetUrl} local:{downloadItem.LocalPath}");
                            err = null;
                            break;
                        }
                        else
                        {
                            err = "【版本控制】重下, hash校验失败! server-" + downloadItem.HashName + " local-" + hash;
                        }
                    }
                    catch (Exception e)
                    {
                        err = e.Message;
                        BDebug.LogError(err);
                    }
                }

                if (string.IsNullOrEmpty(err))
                {
                    //切回主线程，防止回调触发主线程安全模型
                    await UniTask.SwitchToMainThread();
                    {
                        onDownloadProccess(downloadItem, downloadCacheList);
                    }
                    await UniTask.SwitchToThreadPool();

                    //写入本地
                    FileHelper.WriteAllBytes(localHashFile, taskByte);
                }
                else
                {
                    //这边需要继续下载,最后统计失败文件
                    failDownloadList.Add(downloadItem);
                    BDebug.LogError($"下载失败:{err} - {serverUrl}");
                }
            }

            //3.写入本地
            foreach (var assetItem in downloadCacheList)
            {
                var localHashPath = IPath.Combine(localSaveAssetsPath, assetItem.HashName);
                var localRealPath = IPath.Combine(localSaveAssetsPath, assetItem.LocalPath);
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

            return failDownloadList;
        }

        #endregion

        #region 更新模式

        /// <summary>
        /// 对比
        /// 原则上认为StreamingAsset资源为母包携带,且完整
        /// </summary>
        private Queue<AssetItem> Compare(List<AssetItem> localAssetsInfo, List<AssetItem> serverAssetsInfo, RuntimePlatform platform)
        {
            var diffQueue = new Queue<AssetItem>();
            //比对平台
            foreach (var serverAsset in serverAssetsInfo)
            {
                //比较本地配置是否有 hash、文件名一致的资源
                var result = localAssetsInfo.FirstOrDefault((info) => serverAsset.Equals(info));
                //不存在
                if (result == null)
                {
                    diffQueue.Enqueue(serverAsset);
                }
                else
                {
                    if (!BResources.IsExsitAsset(platform, serverAsset.LocalPath, serverAsset.HashName))
                    {
                        diffQueue.Enqueue(serverAsset);
                    }
                }
            }


            return diffQueue;
        }


        /// <summary>
        /// 修复模式
        /// Persistent资源和Streaming资源全量进行对比：文件名和hash
        /// </summary>
        private Queue<AssetItem> Repair(List<AssetItem> serverAssetsInfo, RuntimePlatform platform)
        {
            var diffQueue = new Queue<AssetItem>();
            //平台
            //根据服务器配置,遍历本地所有文件判断存在且修复
            foreach (var serverAsset in serverAssetsInfo)
            {
                if (!BResources.IsExsitAssetWithCheckHash(platform, serverAsset.LocalPath, serverAsset.HashName))
                {
                    diffQueue.Enqueue(serverAsset);
                }
            }

            return diffQueue;
        }

        #endregion
    }
}
