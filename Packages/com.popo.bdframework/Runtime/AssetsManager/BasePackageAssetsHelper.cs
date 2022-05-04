using System;
using System.Collections;
using System.IO;
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
    /// 母包资源构建信息
    /// </summary>
    public class BasePackageAssetsBuildInfo
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
        /// 母包svc版本号
        /// </summary>
        /// <returns></returns>
        public string BasePacakgeSVCVersion = "";

        /// <summary>
        /// AB构建svc版本号
        /// </summary>
        public string AssetBundleSVCVersion = "";

        /// <summary>
        /// 脚本构建svc版本号
        /// </summary>
        public string ScriptSVCVersion = "";

        /// <summary>
        /// 表格构建svc版本号
        /// </summary>
        public string TableSVCVersion = "";
    }

    /// <summary>
    /// 母包资源帮助
    /// 用于管理
    /// 用以统一管理Sql、dll、和ArtConfig资源
    /// </summary>
    static public class BasePackageAssetsHelper
    {
        static BasePackageAssetsHelper()
        {
            BetterStreamingAssets.Initialize();
        }

        /// <summary>
        /// 只能在persistent目录读取的问题
        /// </summary>
        static string[] PersistentOnlyFiles = new string[]
        {
            ScriptLoder.DLL_PATH, //DLL,
            ScriptLoder.DLL_PATH + ".pdb", //PBD文件
            SqliteLoder.LOCAL_DB_PATH, //db
            BResources.ART_ASSET_CONFIG_PATH, BResources.ART_ASSET_TYPES_PATH, //ArtConfig,这两个配置文件是保证 更新资源后逻辑统一.
        };



        /// <summary>
        /// 获取母包资源构建信息
        /// </summary>
        /// <returns></returns>
        static public BasePackageAssetsBuildInfo GetPacakgeBuildInfo(string ouptputPath, RuntimePlatform platform)
        {
            var path = IPath.Combine(ouptputPath, BApplication.GetPlatformPath(platform), BResources.PACKAGE_BUILD_INFO_PATH);
            var buildinfo = new BasePackageAssetsBuildInfo();
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                buildinfo = JsonMapper.ToObject<BasePackageAssetsBuildInfo>(text);
            }
            return buildinfo;
        }

        /// <summary>
        /// 生成母包资源构建信息
        /// </summary>
        static public void GenBasePackageAssetBuildInfo(string outputPath, RuntimePlatform platform, string version = "", string basePacakgeSVC = "", string artSVC = "", string scriptSVC = "", string tableSVC = "")
        {
            //获取旧BuildAssetInfo
            var info = GetPacakgeBuildInfo(outputPath, platform);
            
            //写入buildinfo内容
            info.BuildTime = DateTimeEx.GetTotalSeconds();

            //资源版本
            
            if (!string.IsNullOrEmpty(version))
            {
                info.Version = version;
            }

            
            //母包版本信息
            if (!string.IsNullOrEmpty(basePacakgeSVC))
            {
                info.BasePacakgeSVCVersion = basePacakgeSVC;
            }

            //美术资产信息
            if (!string.IsNullOrEmpty(artSVC))
            {
                info.AssetBundleSVCVersion = artSVC;
            }

            //热更脚本资产
            if (!string.IsNullOrEmpty(scriptSVC))
            {
                info.ScriptSVCVersion = scriptSVC;
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
        static public void SaveBasePackageBuildInfo(string ouptputPath, RuntimePlatform platform, BasePackageAssetsBuildInfo info)
        {
            //转json
            var content = JsonMapper.ToJson(info);
            //写入本地
            var path = IPath.Combine(ouptputPath, BApplication.GetPlatformPath(platform), BResources.PACKAGE_BUILD_INFO_PATH);
            FileHelper.WriteAllText(path, content);
        }
        
        
        static bool isUseSysIO = false;

        /// <summary>
        /// 母包资源检测逻辑
        /// </summary>
        /// <returns></returns>
        static public void CheckBasePackageVersion(RuntimePlatform platform, Action callback)
        {
            BDebug.Log("【资源包】执行母包资源检测逻辑！");
            //路径初始化
            var persistentPlatformPath = IPath.Combine(Application.persistentDataPath, BApplication.GetPlatformPath(platform));
            //母包路径
            string basePckPath = "";

            //母包路径不同情况不一样
            switch (BDLauncher.Inst.GameConfig.ArtRoot)
            {
                case AssetLoadPathType.Editor:
                case AssetLoadPathType.Persistent:
                case AssetLoadPathType.StreamingAsset:
                {
                    isUseSysIO = false;
                    basePckPath = BApplication.streamingAssetsPath;
                }
                    break;
                case AssetLoadPathType.DevOpsPublish:
                {
                    isUseSysIO = true;
                    basePckPath = BApplication.DevOpsPath;
                }
                    break;
            }

            //源地址
            string basePckPlatformPath = "";
            if (isUseSysIO)
            {
                basePckPlatformPath = IPath.Combine(basePckPath, BApplication.GetPlatformPath(platform));
            }

            //packageinfo
            var persistentPackageBuildInfoPath = IPath.Combine(persistentPlatformPath, BResources.PACKAGE_BUILD_INFO_PATH);
            var basePckBuildInfoPath = IPath.Combine(basePckPlatformPath, BResources.PACKAGE_BUILD_INFO_PATH);

            if (!IsExsitAsset(basePckBuildInfoPath))
            {
                //不存在Streaming配置
                BDebug.LogError("【母包资源检测】拷贝失败,不存在：" + basePckBuildInfoPath);
                callback?.Invoke();
                return;
            }
            else
            {
                var basePckBuildInfoContent = ReadAssetAllText(basePckBuildInfoPath);
                //persitent存在，判断版本
                if (!IsExsitAsset(persistentPackageBuildInfoPath))
                {
                    var content = ReadAssetAllText(persistentPackageBuildInfoPath);
                    //解析
                    var persistentPackageInfo = JsonMapper.ToObject<BasePackageAssetsBuildInfo>(content);
                    var basePackageInfo = JsonMapper.ToObject<BasePackageAssetsBuildInfo>(basePckBuildInfoContent);
                    if (persistentPackageInfo.BuildTime >= basePackageInfo.BuildTime)
                    {
                        //跳出，检测结束
                        BDebug.Log("【母包资源检测】不复制，母包 无新资源");
                        callback?.Invoke();
                        return;
                    }
                    else
                    {
                        BDebug.Log("【母包资源检测】复制，母包 有新资源,即将清理persistent旧资源!!!!", "yellow");
                        ClearOldPersistentAssets();
                        //Streaming版本比较新
                        //复制Stream的packageinfo 到persistent
                        FileHelper.WriteAllText(persistentPackageBuildInfoPath, basePckBuildInfoContent);
                    }
                }
                else
                {
                    BDebug.Log("【母包资源检测】第一次创建资源包info到persistent目录");
                    //persistent版本不存在
                    //复制Stream的packageinfo 到persistent
                    FileHelper.WriteAllText(persistentPackageBuildInfoPath, basePckBuildInfoContent);
                }
            }

            //开始拷贝逻辑
            for (int i = 0; i < PersistentOnlyFiles.Length; i++)
            {
                var copytoFile = PersistentOnlyFiles[i];
                //复制新版本的DLL 
                var persistentPath = IPath.Combine(persistentPlatformPath, copytoFile);
                var basePckAssetPath = IPath.Combine(basePckPlatformPath, copytoFile);

                //开始拷贝
                if (IsExsitAsset(basePckAssetPath))
                {
                    BDebug.Log("【母包资源检测】复制成功:" + copytoFile);
                    var bytes = ReadFileAllBytes(basePckAssetPath, isUseSysIO);
                    FileHelper.WriteAllBytes(persistentPath, bytes);
                }
                else
                {
                    BDebug.LogError("【母包资源检测】复制失败,本地不存在:" + copytoFile);
                }
            }

            //结束
            callback?.Invoke();
        }


        /// <summary>
        /// 是否存在文件
        /// </summary>
        /// <returns></returns>
        static private bool IsExsitAsset(string filePath)
        {
            if (isUseSysIO)
            {
                return File.Exists(filePath);
            }
            else
            {
                return BetterStreamingAssets.FileExists(filePath);
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isUseSysIO"></param>
        /// <returns></returns>
        static private string ReadAssetAllText(string filePath)
        {
            if (isUseSysIO)
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                return BetterStreamingAssets.ReadAllText(filePath);
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isUseSysIO"></param>
        /// <returns></returns>
        static private byte[] ReadFileAllBytes(string filePath, bool isUseSysIO)
        {
            if (isUseSysIO)
            {
                return File.ReadAllBytes(filePath);
            }
            else
            {
                return BetterStreamingAssets.ReadAllBytes(filePath);
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
                var path = IPath.Combine(Application.persistentDataPath, BApplication.GetPlatformPath(runtime));
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }
    }
}
