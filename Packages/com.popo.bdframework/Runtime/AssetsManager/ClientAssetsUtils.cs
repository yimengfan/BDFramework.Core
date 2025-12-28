using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using BDFramework.Assets.VersionContrller;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.Core.Tools;
using Cysharp.Text;
using DotNetExtension;
using LitJson;
using UnityEngine;

namespace BDFramework.Asset
{
    /// <summary>
    /// 客户端资产管理器
    /// </summary>
    public class ClientPackageBuildInfo
    {
        /// <summary>
        /// 构建时间
        /// </summary>
        public long BuildTime = 0;

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version = "0.0.0";

        /// <summary>
        /// 母包主工程的svn版本号
        /// </summary>
        /// <returns></returns>
        public string BasePckScriptSVCVersion = "none";

        /// <summary>
        /// 热更脚本构建svc版本号
        /// </summary>
        public string HotfixScriptSVCVersion = "none";

        /// <summary>
        /// AB构建svc版本号
        /// </summary>
        public string AssetBundleSVCVersion = "none";

        /// <summary>
        /// 表格构建svc版本号
        /// </summary>
        public string TableSVCVersion = "none";
    }

    /// <summary>
    /// 全局资源管理
    /// 用于管理
    /// 用以统一管理Sql、dll、和ArtConfig资源
    /// </summary>
    static public class ClientAssetsUtils
    {
        /// <summary>
        /// 包体构建信息路径
        /// </summary>
        readonly static public string PACKAGE_BUILD_INFO_PATH = "package_build.info";

        /// <summary>
        /// 第一加载路径
        /// </summary>
        public static string FIRST_LOAD_DIR = null;

        /// <summary>
        /// 第二加载路径
        /// </summary>
        public static string SECOND_LOAD_DIR = null;

        static ClientAssetsUtils()
        {
            BetterStreamingAssets.Initialize();
        }


        /// <summary>
        /// 获取多寻址加载路径
        /// 如: download0/0.0.0/android  和 streaming/android
        /// </summary>
        /// <param name="version">母包版本号 - download0/0.0.0/android </param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public (string, string) GetMultiAssetsLoadPath(RuntimePlatform platform, string version)
        {
            var platformStr = BApplication.GetPlatformLoadPath(platform);
            //download0/0.0.0/android
            //比如1.1.5=> version目录也是1.0.0  兼容小版本。
            var ver = VersionNumHelper.ParseVersion(version);
            ver.smallNum = 0;
            ver.additiveNum = 0;
            version = ver.ToString();
            var firstLoadDir = IPath.Combine(BApplication.persistentDataPath, version, platformStr);

#if UNITY_ANDROID
            var  secondLoadDir = platformStr; //BetterStreaming 加载
#else
            var secondLoadDir = IPath.Combine(BApplication.streamingAssetsPath, platformStr);
#endif
            FIRST_LOAD_DIR = firstLoadDir;
            SECOND_LOAD_DIR = secondLoadDir;
            //
            return (firstLoadDir, secondLoadDir);
        }


        /// <summary>
        /// Persistent的资产路径
        ///  version/platform/fileName
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public string GetPersistentAssetPath(string fileName)
        {
            return IPath.Combine(FIRST_LOAD_DIR, fileName);
        }

        /// <summary>
        /// Streaming的资产路径
        ///  version/platform/fileName
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public string GetStreamingAssetPath(string fileName)
        {
            return IPath.Combine(SECOND_LOAD_DIR, fileName);
        }


        #region 母包资产拷贝

        /// <summary>
        /// 只能在persistent目录读取的问题
        /// db
        /// </summary>
        static string[] PersistentOnlyFiles = new string[]
        {
            SqliteLoder.LOCAL_DB_PATH, //db =>只能在persistent下进行io
            BResources.ART_ASSET_INFO_PATH, BResources.ART_ASSET_TYPES_PATH, //ArtConfig,这两个配置文件是保证 更新资源后逻辑统一.
        };


        /// <summary>
        /// 母包资源检测逻辑
        /// </summary>
        /// <returns></returns>
        static public void CheckBasePackageAssets(string firstPath, string secondPath)
        {
            //
            BDebug.Log("【资源包】执行母包资源检测逻辑！");
            //母包的build.info信息 => streamingassets/android/package_build.info
            var clientPckBuildInfoPath = IPath.Combine(secondPath, PACKAGE_BUILD_INFO_PATH);
            if (BetterStreamingAssets.FileExists(clientPckBuildInfoPath))
            {
                //不存在Streaming配置
                if (!Application.isEditor)
                {
                    throw new Exception($"【母包资源检测】严重错误！母包配置不存在：{clientPckBuildInfoPath}");
                }
                else
                {
                    BDebug.Log($"【母包资源检测】Editor不存在母包配置 不处理", Color.red);
                }

                return;
            }

            if (!Directory.Exists(firstPath))
            {
                BDebug.Log("------------【母包资源检测】第一次创建母包文件夹，复制&清理旧版本资产-----------");
                ClearOldPersistentAssets();
            }

            //开始拷贝逻辑
            BDebug.Log("【母包资源检测】开始拷贝:");
            for (int i = 0; i < PersistentOnlyFiles.Length; i++)
            {
                var copyFile = PersistentOnlyFiles[i];
                //复制新版本的资产
                var persistentPath = IPath.Combine(firstPath, copyFile);
                var basePckAssetPath = IPath.Combine(secondPath, copyFile);
                //开始拷贝
                if (!File.Exists(persistentPath))
                {
                    var bytes = BetterStreamingAssets.ReadAllBytes(basePckAssetPath);
                    FileHelper.WriteAllBytes(persistentPath, bytes);
                    BDebug.Log($"【母包资源检测】streaming->persistent 复制成功! {basePckAssetPath}=>{persistentPath}");
                }
            }
        }

        #endregion


        /// <summary>
        /// 获取资源构建信息
        /// </summary>
        /// <returns></returns>
        static public ClientPackageBuildInfo GetPackageBuildInfo(string ouptputPath, RuntimePlatform platform)
        {
            var path = IPath.Combine(ouptputPath, BApplication.GetPlatformLoadPath(platform), PACKAGE_BUILD_INFO_PATH);
            var buildinfo = new ClientPackageBuildInfo();
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                buildinfo = JsonMapper.ToObject<ClientPackageBuildInfo>(text);
            }

            return buildinfo;
        }

        /// <summary>
        /// 获取母包的资源构建信息
        /// </summary>
        /// <returns></returns>
        static public ClientPackageBuildInfo GetBasePackBuildInfo()
        {
            return GetPackageBuildInfo(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
        }


        /// <summary>
        /// 读取bytes
        /// 多寻址
        /// </summary>
        /// <param name="firstPath"></param>
        /// <param name="secondPath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public byte[] ReadAllBytes(string firstPath, string secondPath, string fileName)
        {
            var path = ZString.Concat(firstPath, "/", fileName);
#if UNITY_ANDROID
            var path2 = ZString.Concat(  BApplication.GetRuntimePlatformPath(), "/", fileName);
#else
            var path2 = ZString.Concat(secondPath, "/", fileName);
#endif


            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
#if UNITY_ANDROID
            else if(BetterStreamingAssets.FileExists(path2))
            {
                return BetterStreamingAssets.ReadAllBytes(path2);
            }
#else
            else if (File.Exists(path2))
            {
                return File.ReadAllBytes(path2);
            }

#endif
            Debug.LogError("不存在:" + fileName);
            return null;
        }

        /// <summary>
        /// 读取文件Text
        /// </summary>
        /// <param name="firstPath"></param>
        /// <param name="secondPath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public string ReadAllText(string firstPath, string secondPath, string fileName)
        {
            var bytes = ReadAllBytes(firstPath, secondPath, fileName);

            if (bytes != null)
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }

            Debug.LogError("读取失败，字节数组为空: " + fileName);
            return null;
        }


        /// <summary>
        /// 清理旧的persistent资源
        /// </summary>
        static private void ClearOldPersistentAssets()
        {
            Regex VersionFolderRegex = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

            var dirs = Directory.GetDirectories(BApplication.persistentDataPath);
            foreach (var dir in dirs)
            {
                //删除 x.x.x 的文件夹
                bool isMatch = VersionFolderRegex.IsMatch(dir);
                if (isMatch)
                {
                    Directory.Delete(dir, true);
                    Debug.Log("======>【persistent清理】:" + dir);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 生成母包资源构建信息
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="platform"></param>
        /// <param name="bundleVersion">bundle版本号</param>
        /// <param name="basePckScriptSVC">母包的svc版本号</param>
        /// <param name="artAssetsSVC">美术资产svc版本号</param>
        /// <param name="hotfixScriptSVC">热更代码svc版本号</param>
        /// <param name="tableSVC">表格svc版本号</param>
        static public void GenBasePackageBuildInfo(string outputPath, RuntimePlatform platform,
            string bundleVersion = "", string basePckScriptSVC = "", string artAssetsSVC = "",
            string hotfixScriptSVC = "", string tableSVC = "")
        {
            //获取旧BuildAssetInfo
            var info = GetPackageBuildInfo(outputPath, platform);

            //写入buildinfo内容
            info.BuildTime = DateTimeEx.GetTotalSeconds();

            //资源版本号默认自增
            if (!string.IsNullOrEmpty(bundleVersion))
            {
                info.Version = bundleVersion;
            }
            else
            {
                info.Version = VersionNumHelper.AddVersionNum(info.Version);
                BDebug.Log("新版本号:" + info.Version, Color.green);
            }

            //母包版本信息
            if (!string.IsNullOrEmpty(basePckScriptSVC))
            {
                info.BasePckScriptSVCVersion = basePckScriptSVC;
                info.HotfixScriptSVCVersion = basePckScriptSVC; //母包的svc版本号，覆盖热更一次
            }

            //美术资产信息
            if (!string.IsNullOrEmpty(artAssetsSVC))
            {
                info.AssetBundleSVCVersion = artAssetsSVC;
            }

            //热更脚本资产
            if (!string.IsNullOrEmpty(hotfixScriptSVC))
            {
                info.HotfixScriptSVCVersion = hotfixScriptSVC;
            }

            //表格数据
            if (!string.IsNullOrEmpty(tableSVC))
            {
                info.TableSVCVersion = tableSVC;
            }

            SaveBasePackageBuildInfo(outputPath, platform, info);
        }

        /// <summary>
        /// 保存母包资源info
        /// </summary>
        /// <param name="ouptputPath"></param>
        /// <param name="platform"></param>
        /// <param name="info"></param>
        static public void SaveBasePackageBuildInfo(string ouptputPath, RuntimePlatform platform,
            ClientPackageBuildInfo info)
        {
            //转json
            var content = JsonMapper.ToJson(info);
            //写入本地
            var path = IPath.Combine(ouptputPath, BApplication.GetPlatformLoadPath(platform), PACKAGE_BUILD_INFO_PATH);
            FileHelper.WriteAllText(path, content);
        }
#endif
    }
}
