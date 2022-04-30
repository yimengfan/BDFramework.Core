using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using DotNetExtension;
using Editor.EditorPipeline.PublishPipeline;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    public class BDFrameworkPublishPipelineBehaviourTest : ABDFrameworkPublishPipelineBehaviour
    {
        #region DLL

        public override void OnBeginBuildDLL()
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包DLL前回调!");
        }

        public override void OnEndBuildDLL(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包DLL后回调!");
        }

        #endregion

        #region SQLite

        public override void OnBeginBuildSqlite()
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包Sqlite前回调!");
        }

        public override void OnEndBuildSqlite(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包Sqlite后回调!");
        }

        /// <summary>
        /// 导出一张excel
        /// </summary>
        /// <param name="type"></param>
        public override void OnExportExcel(Type type)
        {
            Debug.Log("【OnExportExcel生命周期测试】导出单张表格时,回调测试:" + type.FullName);
        }

        #endregion

        #region Assetbundle

        public override void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包Asset时回调!");
        }

        public override void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包Asset后回调!");
        }

        #endregion

        #region 发布资源

        /// <summary>
        /// 发布资源处理前
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="platform"></param>
        /// <param name="lastVersionNum"></param>
        /// <param name="newVersionNum"></param>
        public override void OnBeginPublishAssets(string outputPath, RuntimePlatform platform, string lastVersionNum, out string newVersionNum)
        {
            newVersionNum = VersionNumHelper.AddVersionNum(lastVersionNum, add: 1);
            Debug.Log("【OnPublishAssetsProccessBegin生命周期测试】发布资源处理前,请处理版本信息!  ->" + platform.ToString());
            Debug.Log($"旧版本:{lastVersionNum}  新版本号:{newVersionNum}");
        }

        /// <summary>
        /// 发布资源处理后
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        public override void OnEndPublishAssets(RuntimePlatform platform, string outputPath)
        {
            Debug.Log("【OnPublishAssetsProccessEnd生命周期测试】发布资源已完成,请编写脚本提交以下目录! \n" + outputPath);
            Debug.Log("---------------------------------------------------------------------------------------------------");
        }

        #endregion
    }
}
