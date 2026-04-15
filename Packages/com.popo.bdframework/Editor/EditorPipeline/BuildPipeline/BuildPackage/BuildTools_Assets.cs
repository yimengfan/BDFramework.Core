using System;
using System.IO;
using BDFramework.Asset;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.Editor.Environment;
using BDFramework.Editor.HotfixScript;
using BDFramework.Editor.PublishPipeline;
using BDFramework.Editor.Table;
using BDFramework.ResourceMgr;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.BuildPipeline
{
    /// <summary>
    /// 构建资源的工具
    /// </summary>
    static public class BuildTools_Assets
    {
        internal const string CIOutputRootBatchArgName = "-ciOutputRoot";

        /// <summary>
        /// 构建包体操作
        /// </summary>
        [System.Flags]
        public enum BuildPackageOption
        {
            None = 0,
           
            /// <summary>
            /// 构建热更代码
            /// </summary>
            BuildHotfixCode = 1 << 1,

            /// <summary>
            /// 构建sqlite表格
            /// </summary>
            BuildSqlite = 1 << 2,

            /// <summary>
            /// 构建美术资产
            /// </summary>
            BuildArtAssets = 1 << 3,
            
            BuildAll = BuildHotfixCode | BuildSqlite | BuildArtAssets ,
        }

        /// <summary>
        /// 解析本次 CI 构建的产物根目录。
        /// 优先读取命令行里的 <c>-ciOutputRoot</c>，否则回退到框架默认的发布目录。
        /// </summary>
        static internal string GetCIOutputRootForBatchMode(string defaultOutputRoot)
        {
            var outputRoot = BatchModeCommandLine.GetArg(CIOutputRootBatchArgName);
            if (!string.IsNullOrWhiteSpace(outputRoot))
            {
                var resolvedOutputRoot = Path.GetFullPath(outputRoot.Trim());
                Directory.CreateDirectory(resolvedOutputRoot);
                return resolvedOutputRoot;
            }

            Directory.CreateDirectory(defaultOutputRoot);
            return defaultOutputRoot;
        }

        /// <summary>
        /// 执行 ClientRes BatchMode 构建入口。
        /// 这里负责收敛版本号、输出目录、目标平台和 Android External Tools，真正的构建仍复用 BuildAll。
        /// </summary>
        static public void BuildClientResForBatchMode(BuildTarget buildTarget, BuildPackageOption buildOption)
        {
            var clientVersion = BuildTools_ClientPackage.GetClientVersionForBatchMode();
            var outputRoot = GetCIOutputRootForBatchMode(BApplication.DevOpsPublishAssetsPath);
            var platform = BApplication.GetRuntimePlatform(buildTarget);
            Debug.Log($"【CI】BuildClientRes Target:{buildTarget} Platform:{platform} Option:{buildOption} ClientVersion:{clientVersion} OutputRoot:{outputRoot}");

            if (buildTarget == BuildTarget.Android)
            {
                AndroidExternalToolsBatchResolver.EnsureAndroidExternalToolsForBatchMode();
            }

            BuildAll(
                platform,
                outputRoot,
                setNewVersionNum: clientVersion,
                opa: buildOption);
        }

        /// <summary>
        /// 构建所有游戏资产
        /// Dll、table、assetbundle
        /// </summary>
        /// <param name="platform">平台</param>
        /// <param name="outputPath">输出目录</param>
        /// <param name="setNewVersionNum">新版本号</param>
        static public void BuildAll(RuntimePlatform platform, string outputPath, string setNewVersionNum = null, BuildPackageOption opa = BuildPackageOption.BuildAll)
        {
            Debug.Log("BuildAssetOpt:" + opa);
            
            var bundleVersion = "";
            //触发事件
            var lastPackageBuildInfo = ClientAssetsUtils.GetPackageBuildInfo(outputPath, platform);
            var lastVersionNum = lastPackageBuildInfo.Version;
            //没有指定版本号，则需要触发版本号的实现逻辑
            if (string.IsNullOrEmpty(setNewVersionNum))
            {
                BDFrameworkPipelineHelper.OnBeginBuildAllAssets(platform, outputPath, lastVersionNum, out bundleVersion);
            }

            //项目没有实现提供新的版本号,则内部提供一个版本号
            if (string.IsNullOrEmpty(bundleVersion) || lastVersionNum == bundleVersion)
            {
                //没指定版本号
                if (string.IsNullOrEmpty(setNewVersionNum))
                {
                    bundleVersion = VersionNumHelper.AddVersionNum(lastVersionNum, add: 1);
                }
                //指定版本号
                else
                {
                    bundleVersion = VersionNumHelper.AddVersionNum(lastVersionNum, setNewVersionNum);
                }
            }


            //开始构建资源
            var _outputPath = Path.Combine(outputPath, BApplication.GetPlatformLoadPath(platform));
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            //1.编译脚本
            try
            {
                if (opa.HasFlag(BuildPackageOption.BuildHotfixCode) || opa == BuildPackageOption.BuildAll)
                {
                    Debug.Log("<color=yellow>=====>打包热更代码</color>");
                    BuildTools_HotfixScript.BuildDLL(outputPath, platform);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw e;
            }

            //2.打包表格
            try
            {
                if (opa.HasFlag(BuildPackageOption.BuildSqlite) || opa == BuildPackageOption.BuildAll)
                {
                    Debug.Log("<color=yellow>=====>打包Sqlite</color>");
                    var ret = BuildTools_Excel2SQLite.BuildSqlite(outputPath, platform);

                    if (!ret)
                    {
                        throw new Exception("打包表格失败!");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw e;
            }

            //3.打包资源
            try
            {
                if (opa.HasFlag(BuildPackageOption.BuildArtAssets) || opa == BuildPackageOption.BuildAll)
                {
                    Debug.Log("<color=yellow>=====>打包AssetBundle</color>");
                    BuildTools_AssetBundleV2.BuildAssetBundles(platform,outputPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw e;
            }

            //4.生成母包资源信息
            ClientAssetsUtils.GenBasePackageBuildInfo(outputPath, platform, bundleVersion: bundleVersion);

            //5.生成本地Assets.info配置
            //这个必须最后生成！！！！
            //这个必须最后生成！！！！
            //这个必须最后生成！！！！
            var allServerAssetItemList = PublishPipelineTools.GetGameAssetItemList(outputPath, platform);
            var csv = CsvSerializer.SerializeToString(allServerAssetItemList);
            var assetsInfoPath = BResources.GetAssetsInfoPath(outputPath, platform);
            FileHelper.WriteAllText(assetsInfoPath, csv);
            //
            Debug.Log($"<color=yellow>{BApplication.GetPlatformLoadPath(platform)} - 旧版本:{lastPackageBuildInfo.Version} 新版本号:{bundleVersion} </color> ");
            //完成回调通知
            BDFrameworkPipelineHelper.OnEndBuildAllAssets(platform, outputPath, bundleVersion);
        }
    }
}
