using System;
using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.Editor.Inspector.Config;
using Game.Editor.PublishPipeline;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// BDFrame的扩展生命周期
    /// </summary>
    abstract public class ABDFrameworkPublishPipelineBehaviour
    {
        #region 编译DLL

        /// <summary>
        ///一键打包时， 开始build dll
        /// </summary>
        virtual public void OnBeginBuildDLL()
        {
        }

        /// <summary>
        /// 一键打包时，结束build dll
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        virtual public void OnEndBuildDLL(string outputPath)
        {
        }

        #endregion

        #region 打包Sqlite

        /// <summary>
        /// 一键打包时，开始导出sqlite
        /// </summary>
        virtual public void OnBeginBuildSqlite()
        {
        }

        /// <summary>
        ///  一键打包时，完成导出sqlite
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        virtual public void OnEndBuildSqlite(string outputPath)
        {
        }

        #endregion

        #region 导表

        /// <summary>
        /// 当excel表格导出
        /// </summary>
        /// <param name="type"></param>
        virtual public void OnExportExcel(Type type)
        {
        }

        #endregion

        #region 开始打包AssetBundle

        /// <summary>
        /// 一键打包时，开始导出AssetBundle
        /// </summary>
        virtual public void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
        }

        
        /// <summary>
        ///  一键打包时，完成导出AssetBundle
        /// </summary>
        virtual public void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            
        }

        #endregion


        #region 构建所有Assets

        /// <summary>
        ///  发布资源处理前，需要提供一个版本号
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="versionNum"></param>
        virtual public void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath,string lastVersionNum,out string newVersionNum)
        {
            newVersionNum = lastVersionNum;
        }

        /// <summary>
        ///  发布资源处理后
        /// </summary>
        virtual public void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath,string newVersionNum)
        {
            
        }

        #endregion

        #region SVC版本号

        /// <summary>
        ///  获取美术资源版本号(git\svn\p4...)
        /// </summary>
        virtual public string GetArtSVCNum(RuntimePlatform platform, string outputPath)
        {
            return "0";
        }

        /// <summary>
        ///  获取表格资源版本号(git\svn\p4...)
        /// </summary>
        virtual public string GetTableSVCNum(RuntimePlatform platform, string outputPath)
        {
            return "0";
        }
        
        /// <summary>
        ///  获取表格资源版本号(git\svn\p4...)
        /// </summary>
        virtual public string GetScriptSVCNum(RuntimePlatform platform, string outputPath)
        {
            return "0";
        }
        
        #endregion
        
        #region 构建移动端包体
       
        /// <summary>
        /// 构建母包开始
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outputpath"></param>
        virtual public void OnBeginBuildPackage(BuildTarget buildTarget, string outputpath )
        {
            //设置母包脚本版本号=>publish assets
            var githash = GitProcessor.GetVersion(6);
            ClientAssetsHelper.GenBasePackageBuildInfo(BApplication.DevOpsPublishAssetsPath,BApplication.GetRuntimePlatform(buildTarget),basePckScriptSVC:githash);
            //
            var config =  ConfigEditorUtil.GetEditorConfig<GameBaseConfigProcessor.Config>();
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    PlayerSettings.Android.bundleVersionCode++;
                    //设置版本号
                    PlayerSettings.bundleVersion = config.ClientVersionNum;
                    BDebug.Log($"APP版本号：Version:{ PlayerSettings.bundleVersion} / BundleVersion:{PlayerSettings.Android.bundleVersionCode}", Color.yellow);
                }
                    break;
                case BuildTarget.iOS:
                {
                    int buildNumber = 0;
                    int.TryParse(PlayerSettings.iOS.buildNumber, out buildNumber);
                    buildNumber++;
                    //设置build number
                    PlayerSettings.iOS.buildNumber = buildNumber.ToString();
                    //设置版本号
                    PlayerSettings.bundleVersion = config.GetClientVersionNumForIOS();
                    
                    BDebug.Log($"APP版本号：Version:{ PlayerSettings.bundleVersion} / BundleVersion:{PlayerSettings.iOS.buildNumber}", Color.yellow);

                }
                    break;
            }
        }

        /// <summary>
        /// 构建母包结束
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outputpath"></param>
        virtual public void OnEndBuildPackage(BuildTarget buildTarget, string outputpath)
        {
            
        }
        
        #endregion
        
        #region 资源转hash,预备上传服务器

        /// <summary>
        ///  发布资源处理前,该资源提交到服务器
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="versionNum"></param>
        virtual public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            
        }

        /// <summary>
        ///  发布资源处理后,该资源提交到服务器
        /// </summary>
        virtual public void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            
        }

        #endregion
    }
}
