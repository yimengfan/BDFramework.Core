using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using DotNetExtension;
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
        public override void OnBeginBuildAssetBundle(BuildAssetBundleParams @params, BuildAssetsInfo buildAssetsInfo)
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包Asset时回调!");
        }
        
        public override void OnEndBuildAssetBundle(BuildAssetBundleParams @params, BuildAssetsInfo buildAssetsInfo)
        {
            Debug.Log("【BDFrameEditorBehavior生命周期测试】打包Asset后回调!");
        }
        #endregion


        #region 发布资源

        /// <summary>
        /// 发布资源处理前
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="versionNum"></param>
        public override void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, out string versionNum)
        {
            versionNum = DateTimeEx.GetTotalSeconds().ToString();
            Debug.Log("【OnPublishAssetsProccessBegin生命周期测试】发布资源处理前,处理版本信息!");
        }

        /// <summary>
        /// 发布资源处理后
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        public override void OnEndPublishAssets(RuntimePlatform platform, string outputPath)
        {
            Debug.Log("【OnPublishAssetsProccessEnd生命周期测试】发布资源处理后,等待编写提交脚本! \n 目录:" + outputPath);
        }

        #endregion
    }
}