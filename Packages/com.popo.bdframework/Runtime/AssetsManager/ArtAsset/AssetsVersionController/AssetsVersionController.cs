using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
            UniTask.RunOnThreadPool(() =>
            {
                //开始版本控制逻辑
                StartVersionControl(updateMode, serverConfigUrl, BDApplication.persistentDataPath, assetsPackageName, onDownloadProccess, onTaskEndCallback);
            });
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
            var platform = Application.platform;
            //本地、服务器版本信息的路径
            var serverAssetsVersionInfoUrl = GetServerAssetsVersionInfoPath(serverUrl, platform);
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

            var serverVersionInfo = JsonMapper.ToObject<AssetsVersionInfo>(serverAssetsVersionConfigWebReq.downloadHandler.text);
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
        async private Task StartVersionControl(UpdateMode updateMode, string serverUrl, string localSaveAssetsPath, string subPackageName, Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess,
            Action<VersionControllerStatus, string> onTaskEndCallback)
        {
            var platform = Application.platform;
            //目录准备
            var platformStr = BDApplication.GetPlatformPath(platform);
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

            #region AssetVersion.info下载

            {
                var ret = await DownloadAssetVersionInfo(serverUrl, localSaveAssetsPath);
                if (ret.Item1 != null)
                {
                    await UniTask.SwitchToMainThread();
                    onTaskEndCallback?.Invoke(VersionControllerStatus.Error, ret.Item1);
                    return;
                }

                serverVersionInfo = ret.Item2;
                localVersionInfo = ret.Item3;
            }

            #endregion

            //2.对比版本、获取对应数据
            string err = null;
            string suc = null;
            var serverAssetsInfoList = new List<ServerAssetItem>();
            var localAssetsInfoList = new List<ServerAssetItem>();
            var serverAssetsContent = "";
            //
            switch (updateMode)
            {
                case UpdateMode.Compare:
                case UpdateMode.CompareWithRepair://CP模式对比版本与Compare一致
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
                    var serverAssetInfosUrl = GetAssetsInfoPath(serverUrl, platform);
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
                onTaskEndCallback?.Invoke(VersionControllerStatus.Error, err);
                return;
            }

            if (suc != null)
            {
                BDebug.Log(suc);
                await UniTask.SwitchToMainThread();
                onTaskEndCallback?.Invoke(VersionControllerStatus.Success, suc);
                return;
            }


            //3.生成差异列表
            Queue<ServerAssetItem> diffDownloadQueue = null;

            #region 生成差异文件

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

            BDebug.Log($"【版本控制】 配置数量:{serverAssetsInfoList.Count} ,本地存在{serverAssetsInfoList.Count - diffDownloadQueue.Count},下载文件数量{diffDownloadQueue.Count}", "yellow");

            #endregion

            //4.开始下载

            #region 根据差异文件下载

            {
                var failDownloadList = await DownloadAssets(serverUrl, localSaveAssetsPath, diffDownloadQueue, onDownloadProccess);
                if (failDownloadList.Count > 0)
                {
                    onTaskEndCallback(VersionControllerStatus.Error, "部分资源未下载完毕!");
                    return;
                }
            }

            #endregion


            //5.写入配置到本地

            #region 存储配置到本地

            string localAssetInfoPath = "";
            if (isDownloadSubPackageMode)
            {
                localAssetInfoPath = GetAssetsSubPackageInfoPath(BDApplication.persistentDataPath, platform, subPackageName);
            }
            else
            {
                localAssetInfoPath = GetAssetsInfoPath(BDApplication.persistentDataPath, platform);
            }

            //写入Asset.Info
            File.WriteAllText(localAssetInfoPath, serverAssetsContent);
            BDebug.Log($"【版本控制】写入{Path.GetFileName(localAssetInfoPath)}  \n {serverAssetsContent}");

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

            var localAssetsVersionInfoPath = GetServerAssetsVersionInfoPath(localSaveAssetsPath, platform);
            File.WriteAllText(localAssetsVersionInfoPath, JsonMapper.ToJson(localVersionInfo));
            BDebug.Log($"【版本控制】写入{Path.GetFileName(localAssetsVersionInfoPath)}");

            #endregion

            //TODO 6.删除冗余资源
            if (!isDownloadSubPackageMode)
            {
                //  serverAssetsInfoList
            }

            //the end.
            await UniTask.SwitchToMainThread();
            onTaskEndCallback(VersionControllerStatus.Success, null);
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
        public (string, string, List<ServerAssetItem>, List<ServerAssetItem>, string) GetDownloadAssetsData(string serverUrl, RuntimePlatform platform, AssetsVersionInfo serverVersionInfo, AssetsVersionInfo localVersionInfo)
        {
            //返回数据
            string err = null;
            string suc = null;
            var serverAssetsInfoList = new List<ServerAssetItem>();
            var localAssetsInfoList = new List<ServerAssetItem>();
            var serverAssetsContent = "";

            //1.判断版本号
            if (localVersionInfo.Version == serverVersionInfo.Version)
            {
                suc = "【版本控制】全量版本一致,无需下载!";
                BDebug.Log(suc);
                return (err, suc, null, null, null);
            }

            //2.获取Assets.info
            BDebug.Log($"【版本控制】全量下载模式! server:{serverVersionInfo.Version} local:{localVersionInfo.Version} ", "red");
            {
                //服务器路径
                var serverAssetInfosUrl = GetAssetsInfoPath(serverUrl, platform);
                //下载服务器Assets.info
                (err, serverAssetsInfoList, serverAssetsContent) = LoadServerAssetInfo(serverAssetInfosUrl);
            }
            //加载本地SubPackage配置
            localAssetsInfoList = LoadLocalSubPacakgeAssetInfo(platform, localVersionInfo);

            //加载本地asset.info
            var localAssetsInfo = this.LoadLocalAssetInfo(platform);
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
        public (string, string, List<ServerAssetItem>, List<ServerAssetItem>, string) GetDownloadSubPackageData(string serverUrl, string subPackageName, RuntimePlatform platform, AssetsVersionInfo serverVersionInfo,
            AssetsVersionInfo localVersionInfo)
        {
            //返回数据
            string err = null;
            string suc = null;
            var serverAssetsInfoList = new List<ServerAssetItem>();
            var localAssetsInfoList = new List<ServerAssetItem>();
            var serverAssetsContent = "";

            BDebug.Log("【版本控制】分包模式:" + subPackageName);
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
//                BDebug.Log(suc);
                return (err, suc, null, null, null);
            }

            //本地版本记录
            if (!string.IsNullOrEmpty(localVersionInfo.Version) && !string.IsNullOrEmpty(localSubPckVersion))
            {
                if (localVersionInfo.Version != localSubPckVersion)
                {
                    BDebug.Log("【版本控制】分包资源大于 本地整包资源.请注意资源版本有可能不匹配!", "red");
                }
            }

            //2.下载AssetInfo
            BDebug.Log($"【版本控制】分包下载模式! server:{serverSubPckVersion} local:{localSubPckVersion} ", "red");
            {
                //服务器路径
                var serverAssetInfosUrl = GetAssetsSubPackageInfoPath(serverUrl, platform, subPackageName);
                //下载服务器配置
                (err, serverAssetsInfoList, serverAssetsContent) = LoadServerAssetInfo(serverAssetInfosUrl);
            }

            //1.加载本地SubPackage配置
            localAssetsInfoList = LoadLocalSubPacakgeAssetInfo(platform, localVersionInfo);

            //2.加载本地asset.info
            var localAssetsInfo = this.LoadLocalAssetInfo(platform);
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
        async private Task<Tuple<string, AssetsVersionInfo, AssetsVersionInfo>> DownloadAssetVersionInfo(string serverUrl, string localSaveAssetsPath)
        {
            var platform = Application.platform;
            //本地、服务器版本信息的路径
            var serverAssetsVersionInfoUrl = GetServerAssetsVersionInfoPath(serverUrl, platform);
            var localAssetsVersionInfoPath = GetServerAssetsVersionInfoPath(localSaveAssetsPath, platform);
            string err = null;
            var serverVersionInfo = new AssetsVersionInfo();
            var localVersionInfo = new AssetsVersionInfo();
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
                }
            }

            if (File.Exists(localAssetsVersionInfoPath))
            {
                localVersionInfo = JsonMapper.ToObject<AssetsVersionInfo>(File.ReadAllText(localAssetsVersionInfoPath));
                if (localVersionInfo == null)
                {
                    localVersionInfo = new AssetsVersionInfo();
                }
            }

            //返回
            return new Tuple<string, AssetsVersionInfo, AssetsVersionInfo>(err, serverVersionInfo, localVersionInfo);
        }


        /// <summary>
        /// 加载服务器Asset.Info
        /// </summary>
        /// <returns></returns>
        async private Task<Tuple<string, List<ServerAssetItem>, string>> DownloadAssetsInfo(string serverAssetInfosUrl)
        {
            var err = "";
            var serverAssetsInfoList = new List<ServerAssetItem>();
            var serverlAssetsContent = "";
            //开始下载
            for (int i = 0; i < RETRY_COUNT; i++)
            {
                try
                {
                    serverlAssetsContent = await webClient.DownloadStringTaskAsync(serverAssetInfosUrl);
                    serverAssetsInfoList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(serverlAssetsContent);
                    err = null;
                    break;
                }
                catch (Exception e)
                {
                    err = e.Message;
                }
            }

            return new Tuple<string, List<ServerAssetItem>, string>(err, serverAssetsInfoList, serverlAssetsContent);
        }


        /// <summary>
        /// 加载服务器Asset.info
        /// </summary>
        /// <returns></returns>
        private (string, List<ServerAssetItem>, string) LoadServerAssetInfo(string serverAssetInfosUrl)
        {
            //返回数据
            string err = null;
            var serverAssetsInfoList = new List<ServerAssetItem>();
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
        private List<ServerAssetItem> LoadLocalAssetInfo(RuntimePlatform platform)
        {
            var retList = new List<ServerAssetItem>();
            //优先加载persistent的Assets.info
            var persistentAssetInfoPath = GetAssetsInfoPath(BDApplication.persistentDataPath, platform);
            if (File.Exists(persistentAssetInfoPath))
            {
                var content = File.ReadAllText(persistentAssetInfoPath);
                retList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
            }
            //streaming 和其他的Assets.info
            else
            {
                //根据加载模式不同,寻找不同目录下的其他配置
                //打包时，本地会带一份ServerAssets.info以标记当前包携带的资源
                var loadArtRoot = BDLauncher.Inst.GameConfig.ArtRoot;
                switch (loadArtRoot)
                {
                    case AssetLoadPathType.Persistent:
                    case AssetLoadPathType.StreamingAsset:
                    {
                        //TODO ：BSA 读取，不需要Streaming前缀
                        var steamingAssetsInfoPath = IPath.Combine(BDApplication.GetPlatformPath(platform), BResources.ASSETS_INFO_PATH);
                        //var steamingAssetsInfoPath = GetAssetsInfoPath(BDApplication.streamingAssetsPath, platform);
                        if (BetterStreamingAssets.FileExists(steamingAssetsInfoPath))
                        {
                            var content = BetterStreamingAssets.ReadAllText(steamingAssetsInfoPath);
                            retList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
                        }
                    }
                        break;
                    case AssetLoadPathType.DevOpsPublish:
                    {
                        var path = GameConfig.GetLoadPath(loadArtRoot);
                        var devopsAssetInfoPath = GetAssetsInfoPath(path, platform);
                        if (File.Exists(devopsAssetInfoPath))
                        {
                            var content = File.ReadAllText(devopsAssetInfoPath);
                            retList = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
                        }
                    }
                        break;
                }
            }

            return retList;
        }


        /// <summary>
        /// 加载SubPacakge的AssetInfo
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        private List<ServerAssetItem> LoadLocalSubPacakgeAssetInfo(RuntimePlatform platform, AssetsVersionInfo localVersionInfo)
        {
            var retList = new List<ServerAssetItem>();

            foreach (var kv in localVersionInfo.SubPckMap)
            {
                var subPackageInfoPath = GetAssetsSubPackageInfoPath(BDApplication.persistentDataPath, platform, kv.Key);
                if (File.Exists(subPackageInfoPath))
                {
                    var content = File.ReadAllText(subPackageInfoPath);
                    var assetItems = CsvSerializer.DeserializeFromString<List<ServerAssetItem>>(content);
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
        public IEnumerator IE_DownloadAssets(string serverUrl, string localSaveAssetsPath, Queue<ServerAssetItem> downloadQueue, Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess)
        {
            var failDownloadList = new List<ServerAssetItem>();
            //url构建
            var platform = BDApplication.GetPlatformPath(Application.platform);
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
                            BDebug.Log("下载成功：" + serverAssetUrl);
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
        async private Task<List<ServerAssetItem>> DownloadAssets(string serverUrl, string localSaveAssetsPath, Queue<ServerAssetItem> downloadQueue, Action<ServerAssetItem, List<ServerAssetItem>> onDownloadProccess)
        {
            var failDownloadList = new List<ServerAssetItem>();
            //url构建
            var platform = BDApplication.GetPlatformPath(Application.platform);
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
                            BDebug.Log("下载成功：" + serverAssetUrl);
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
        private Queue<ServerAssetItem> Compare(List<ServerAssetItem> localAssetsInfo, List<ServerAssetItem> serverAssetsInfo, RuntimePlatform platform)
        {
            var diffQueue = new Queue<ServerAssetItem>();
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
                    if (!IsExsitAssetName(platform, serverAsset))
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
        private Queue<ServerAssetItem> Repair(List<ServerAssetItem> serverAssetsInfo, RuntimePlatform platform)
        {
            var diffQueue = new Queue<ServerAssetItem>();
            //平台
            //根据服务器配置,遍历本地所有文件判断存在且修复
            foreach (var serverAsset in serverAssetsInfo)
            {
                if (!IsExsitAsset(platform, serverAsset))
                {
                    diffQueue.Enqueue(serverAsset);
                }
            }
            return diffQueue;
        }


        /// <summary>
        /// 是否存在资源、且hash相同
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="serverAsset"></param>
        /// <returns></returns>
        private bool IsExsitAsset(RuntimePlatform platform, ServerAssetItem serverAsset)
        {
            //本地是否下载过hash文件(之前下到一半就中止了)
            var persistentHashPath = IPath.Combine(BDApplication.persistentDataPath, BDApplication.GetPlatformPath(platform), serverAsset.HashName);
            if (File.Exists(persistentHashPath))
            {
                var hash = FileHelper.GetMurmurHash3(persistentHashPath);
                if (serverAsset.HashName.Equals(hash))
                {
                    BDebug.Log($"[版本控制]hash文件存在,无需下载! - {serverAsset.HashName}");
                    return true;
                }
                else
                {
                    File.Delete(persistentHashPath);
                }
            }

            //persistent判断
            var persistentAssetPath = IPath.Combine(BDApplication.persistentDataPath, BDApplication.GetPlatformPath(platform), serverAsset.LocalPath);
            if (File.Exists(persistentAssetPath))
            {
                var hash = FileHelper.GetMurmurHash3(persistentAssetPath);
                if (serverAsset.HashName.Equals(hash))
                {
                    BDebug.Log($"[版本控制]persistent存在,无需下载! - {serverAsset.LocalPath}");
                    return true;
                }
            }

            //Streaming 文件判断,无需Streaming前缀
            var streamingAssetPath = IPath.Combine(BDApplication.GetPlatformPath(platform), serverAsset.LocalPath);
            if (BetterStreamingAssets.FileExists(streamingAssetPath))
            {
                var bytes = BetterStreamingAssets.ReadAllBytes(streamingAssetPath);
                var hash = FileHelper.GetMurmurHash3(bytes);
                if (serverAsset.HashName.Equals(hash))
                {
                    BDebug.Log($"[版本控制]streaming存在,无需下载! - {streamingAssetPath}");
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// 是否存在资源、文件存在即可
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="serverAsset"></param>
        /// <returns></returns>
        private bool IsExsitAssetName(RuntimePlatform platform, ServerAssetItem serverAsset)
        {
            //本地是否下载过hash文件(之前下到一半就中止了)
            var persistentHashPath = IPath.Combine(BDApplication.persistentDataPath, BDApplication.GetPlatformPath(platform), serverAsset.HashName);
            if (File.Exists(persistentHashPath))
            {
                var hash = FileHelper.GetMurmurHash3(persistentHashPath);
                if (serverAsset.HashName.Equals(hash))
                {
                    return true;
                }
                else
                {
                    File.Delete(persistentHashPath);
                }
            }

            //persistent判断
            var persistentAssetPath = IPath.Combine(BDApplication.persistentDataPath, BDApplication.GetPlatformPath(platform), serverAsset.LocalPath);
            if (File.Exists(persistentAssetPath))
            {
                return true;
            }

            //Streaming 文件判断,无需Streaming前缀
            var streamingAssetPath = IPath.Combine(BDApplication.GetPlatformPath(platform), serverAsset.LocalPath);
            if (BetterStreamingAssets.FileExists(streamingAssetPath))
            {
                return true;
            }


            return false;
        }

        #endregion

        #region 路径

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

        #endregion
    }
}
