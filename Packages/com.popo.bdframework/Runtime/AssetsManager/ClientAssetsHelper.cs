using System;
using System.Collections;
using System.IO;
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
    static public class ClientAssetsHelper
    {
        /// <summary>
        /// 包体构建信息路径
        /// </summary>
        readonly static public string PACKAGE_BUILD_INFO_PATH = "package_build.info";

        static ClientAssetsHelper()
        {
            BetterStreamingAssets.Initialize();
        }

        /// <summary>
        /// 只能在persistent目录读取的问题
        /// </summary>
        static string[] PersistentOnlyFiles = new string[]
        {
            ScriptLoder.DLL_PATH, //DLL,
            ScriptLoder.PDB_PATH, //PBD文件
            SqliteLoder.LOCAL_DB_PATH, //db
            BResources.ART_ASSET_INFO_PATH, BResources.ART_ASSET_TYPES_PATH, //ArtConfig,这两个配置文件是保证 更新资源后逻辑统一.
        };
        
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
        static public void GenBasePackageBuildInfo(string outputPath, RuntimePlatform platform, string bundleVersion = "", string basePckScriptSVC = "", string artAssetsSVC = "", string hotfixScriptSVC = "", string tableSVC = "")
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
        static public void SaveBasePackageBuildInfo(string ouptputPath, RuntimePlatform platform, ClientPackageBuildInfo info)
        {
            //转json
            var content = JsonMapper.ToJson(info);
            //写入本地
            var path = IPath.Combine(ouptputPath, BApplication.GetPlatformPath(platform), PACKAGE_BUILD_INFO_PATH);
            FileHelper.WriteAllText(path, content);
        }
#endif
        /// <summary>
        /// 母包资源检测逻辑
        /// </summary>
        /// <returns></returns>
        static public void CheckBasePackageVersion(RuntimePlatform platform, Action callback)
        {
            bool isUseBetterStreaming = false;
            //persistent路径
            var persistentPlatformPath = IPath.Combine(Application.persistentDataPath, BApplication.GetPlatformPath(platform));
            //母包路径
            string basePckPath = "";

            //母包路径不同情况不一样
            switch (BDLauncher.Inst.Config.ArtRoot)
            {
                case AssetLoadPathType.Editor:
                {
                    //editor不进行母包资源管理
                    BDebug.Log("【资源包】Editor加载不执行:母包资源检测逻辑！");
                    callback?.Invoke();
                    return;
                }
                case AssetLoadPathType.Persistent:
                case AssetLoadPathType.StreamingAsset:
                {
                    isUseBetterStreaming = true;
                    basePckPath = BApplication.streamingAssetsPath;
                }
                    break;
                case AssetLoadPathType.DevOpsPublish:
                {
                    if (Application.isEditor)
                    {
                        isUseBetterStreaming = false;
                        basePckPath = BApplication.DevOpsPublishAssetsPath;
                    }
                    else
                    {
                        isUseBetterStreaming = true;
                        basePckPath = BApplication.streamingAssetsPath;
                    }
                }
                    break;
            }

            BDebug.Log("【资源包】执行母包资源检测逻辑！");
            //源地址
            string basePckPlatformPath = "";
            if (isUseBetterStreaming)
            {
                basePckPlatformPath = BApplication.GetPlatformPath(platform);
            }
            else
            {
                basePckPlatformPath = IPath.Combine(basePckPath, BApplication.GetPlatformPath(platform));
            }

            //packageinfo
            var persistentPckBuildInfoPath = IPath.Combine(persistentPlatformPath, PACKAGE_BUILD_INFO_PATH);
            //母包的build.info信息
            var basePckBuildInfoPath = IPath.Combine(basePckPlatformPath, PACKAGE_BUILD_INFO_PATH);

            if (!IsExsitAsset(basePckBuildInfoPath, isUseBetterStreaming))
            {
                //不存在Streaming配置
                if (!Application.isEditor)
                {
                    BDebug.LogError($"【母包资源检测】严重错误！母包配置不存在：{basePckBuildInfoPath}" );
                }
                else
                {
                    BDLauncher.Inst.BasePckBuildInfo = new ClientPackageBuildInfo();
                    BDLauncher.Inst.HotfixAssetsBuildInfo = new ClientPackageBuildInfo();
                    BDebug.Log($"【母包资源检测】Editor不存在母包配置,直接构造：{basePckBuildInfoPath}" ,Color.red);
                }
                callback?.Invoke();
                return;
            }
            else
            {
                BDebug.Log("【母包资源检测】读取母包配置：" + basePckBuildInfoPath);
                var basePckBuildInfoContent = ReadAssetAllText(basePckBuildInfoPath, isUseBetterStreaming);
                //persitent存在，判断版本
                if (IsExsitAsset(persistentPckBuildInfoPath))
                {
                    BDebug.Log("【母包资源检测】读取persistent配置：" + persistentPckBuildInfoPath);
                    var content = ReadAssetAllText(persistentPckBuildInfoPath);
                    //解析
                    var persistentPackageInfo = JsonMapper.ToObject<ClientPackageBuildInfo>(content);
                    var basePackageInfo = JsonMapper.ToObject<ClientPackageBuildInfo>(basePckBuildInfoContent);
                    if (VersionNumHelper.GT( persistentPackageInfo.Version ,basePackageInfo.Version))
                    {
                        //跳出，检测结束
                        BDebug.Log($"【母包资源检测】不复制，persistent更新!母包无新资源! persistent:{persistentPackageInfo.Version },streaming:{basePackageInfo.Version}!",Color.yellow);
                        BDLauncher.Inst.BasePckBuildInfo = basePackageInfo;
                        BDLauncher.Inst.HotfixAssetsBuildInfo = persistentPackageInfo;
                        callback?.Invoke();
                        return;
                    }
                    else
                    {
                        BDebug.Log("【母包资源检测】母包有新资源,即将清理旧资源!!!!", Color.yellow);

                        ClearOldPersistentAssets();
                        //Streaming版本比较新，说明更新了母包
                        //复制Stream的packageinfo 到persistent
                        FileHelper.WriteAllText(persistentPckBuildInfoPath, basePckBuildInfoContent);
                        BDLauncher.Inst.BasePckBuildInfo = basePackageInfo;
                        BDLauncher.Inst.HotfixAssetsBuildInfo = basePackageInfo;
                    }
                }
                else
                {
                    BDebug.Log("【母包资源检测】第一次创建package_build.info到persistent目录");
                    var basePackageInfo = JsonMapper.ToObject<ClientPackageBuildInfo>(basePckBuildInfoContent);
                    BDLauncher.Inst.BasePckBuildInfo = basePackageInfo;
                    BDLauncher.Inst.HotfixAssetsBuildInfo = basePackageInfo;
                    //persistent版本不存在
                    //复制Stream的packageinfo 到persistent
                    FileHelper.WriteAllText(persistentPckBuildInfoPath, basePckBuildInfoContent);
                }
            }

            //开始拷贝逻辑
            for (int i = 0; i < PersistentOnlyFiles.Length; i++)
            {
                var copytoFile = PersistentOnlyFiles[i];
                //复制新版本的资产
                var persistentPath = IPath.Combine(persistentPlatformPath, copytoFile);
                var basePckAssetPath = IPath.Combine(basePckPlatformPath, copytoFile);

                //开始拷贝
                if (IsExsitAsset(basePckAssetPath, isUseBetterStreaming))
                {
                    BDebug.Log("【母包资源检测】复制成功:" + copytoFile);
                    var bytes = ReadFileAllBytes(basePckAssetPath, isUseBetterStreaming);
                    FileHelper.WriteAllBytes(persistentPath, bytes);
                }
                else
                {
                    BDebug.LogError($"母包资源检测】错误！母包配置不存在:{copytoFile}" );
                }
            }

            //结束
            callback?.Invoke();
        }


        /// <summary>
        /// 获取资源构建信息
        /// </summary>
        /// <returns></returns>
        static public ClientPackageBuildInfo GetPackageBuildInfo(string ouptputPath, RuntimePlatform platform)
        {
            var path = IPath.Combine(ouptputPath, BApplication.GetPlatformPath(platform), PACKAGE_BUILD_INFO_PATH);
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
        /// 是否存在文件
        /// </summary>
        /// <returns></returns>
        static private bool IsExsitAsset(string filePath, bool isUseBetterStreaming = false)
        {
            if (isUseBetterStreaming)
            {
                return BetterStreamingAssets.FileExists(filePath);
            }
            else
            {
                return File.Exists(filePath);
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isUseSysIO"></param>
        /// <returns></returns>
        static private string ReadAssetAllText(string filePath, bool isUseBetterStreaming = false)
        {
            if (isUseBetterStreaming)
            {
                return BetterStreamingAssets.ReadAllText(filePath);
            }
            else
            {
                return File.ReadAllText(filePath);
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isUseBetterStreaming"></param>
        /// <returns></returns>
        static private byte[] ReadFileAllBytes(string filePath, bool isUseBetterStreaming = false)
        {
            if (isUseBetterStreaming)
            {
                return BetterStreamingAssets.ReadAllBytes(filePath);
            }
            else
            {
                return File.ReadAllBytes(filePath);
            }
        }

        /// <summary>
        /// 清理旧的persistent资源
        /// </summary>
        static private void ClearOldPersistentAssets()
        {
            var runtimes = BApplication.SupportPlatform;
            foreach (var runtime in runtimes)
            {
                var path = IPath.Combine(Application.persistentDataPath, BApplication.GetPlatformPath(runtime),BResources.ART_ASSET_ROOT_PATH);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }
    }
}