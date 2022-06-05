using System;
using System.IO;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.PublishPipeline;
using BDFramework.Editor.Table;
using BDFramework.ResourceMgr;
using ServiceStack.Text;
using UnityEngine;

namespace BDFramework.Editor.BuildPipeline
{
    /// <summary>
    /// 构建资源的工具
    /// </summary>
    static public class BuildAssetsTools
    {
        /// <summary>
        /// 构建所有资源
        /// </summary>
        /// <param name="platform">平台</param>
        /// <param name="outputPath">输出目录</param>
        /// <param name="setNewVersionNum">新版本号</param>
        static public void BuildAllAssets(RuntimePlatform platform, string outputPath, string setNewVersionNum = null)
        {
            var newVersionNum = "";
            //触发事件
            var lastPackageBuildInfo = GlobalAssetsHelper.GetPackageBuildInfo(outputPath, platform);
            var lastVersionNum = lastPackageBuildInfo.Version;
            //没有指定版本号，则需要触发版本号的实现逻辑
            if (string.IsNullOrEmpty(setNewVersionNum))
            {
                BDFrameworkPipelineHelper.OnBeginBuildAllAssets(platform, outputPath, lastVersionNum, out newVersionNum);
            }

            //项目没有实现提供新的版本号,则内部提供一个版本号
            if (string.IsNullOrEmpty(newVersionNum) || lastVersionNum == newVersionNum)
            {
                //没指定版本号
                if (string.IsNullOrEmpty(setNewVersionNum))
                {
                    newVersionNum = VersionNumHelper.AddVersionNum(lastVersionNum,add:1);
                }
                //指定版本号
                else
                {
                    newVersionNum = VersionNumHelper.AddVersionNum(lastVersionNum, setNewVersionNum);
                }
            }


            //开始构建资源
            var _outputPath = Path.Combine(outputPath, BApplication.GetPlatformPath(platform));
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            //1.编译脚本
            try
            {
                EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, platform, ScriptBuildTools.BuildMode.Release);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            //2.打包表格
            try
            {
                Excel2SQLiteTools.AllExcel2SQLite(outputPath, platform);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            //3.打包资源
            try
            {
                //var config = BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildAssetBundle;
                AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            //4.生成母包资源信息
            GlobalAssetsHelper.GenBasePackageAssetBuildInfo(outputPath, platform, version: newVersionNum);
            
            //5.生成本地Assets.info配置
            //这个必须最后生成！！！！
            //这个必须最后生成！！！！
            //这个必须最后生成！！！！
            var allServerAssetItemList = PublishPipelineTools.GetAssetItemList(outputPath, platform);
            var csv = CsvSerializer.SerializeToString(allServerAssetItemList);
            var assetsInfoPath = BResources.GetAssetsInfoPath(outputPath,platform);
            FileHelper.WriteAllText(assetsInfoPath, csv);
            //
            Debug.Log($"<color=yellow>{ BApplication.GetPlatformPath(platform)} - 旧版本:{lastPackageBuildInfo.Version} 新版本号:{newVersionNum} </color> ");
            //完成回调通知
            BDFrameworkPipelineHelper.OnEndBuildAllAssets(platform, outputPath, newVersionNum);
        }
    }
}
