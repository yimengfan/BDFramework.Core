using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.PublishPipeline;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    public class BDFrameworkPublishPipelineBehaviourTest : ABDFrameworkPublishPipelineBehaviour
    {
        #region DLL

        public override void OnBeginBuildDLL()
        {
            Debug.Log("实现回调测试:"+ nameof(OnBeginBuildDLL));
        }

        public override void OnEndBuildDLL(string outputPath)
        {
            Debug.Log("实现回调测试:"+ nameof(OnEndBuildDLL));
        }

        #endregion

        #region SQLite

        public override void OnBeginBuildSqlite()
        {
            Debug.Log("实现回调测试:"+ nameof(OnBeginBuildSqlite));
        }

        public override void OnEndBuildSqlite(string outputPath)
        {
            Debug.Log("实现回调测试:"+ nameof(OnEndBuildSqlite));
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
            Debug.Log("实现回调测试:"+ nameof(OnBeginBuildAssetBundle));
        }

        public override void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            Debug.Log("实现回调测试:"+ nameof(OnEndBuildAssetBundle));
        }

        #endregion

        #region 一键构建资源

        /// <summary>
        /// 一键构建资源前
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="lastVersionNum"></param>
        /// <param name="newVersionNum"></param>
        public override void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath, string lastVersionNum, out string newVersionNum)
        {
            Debug.Log("实现回调测试:"+ nameof(OnBeginBuildAllAssets));
            newVersionNum = VersionNumHelper.AddVersionNum(lastVersionNum, add: 1);
        }

        /// <summary>
        /// 一键构建资源后
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="newVersionNum"></param>
        public override void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath, string newVersionNum)
        {
            Debug.Log("实现回调测试:"+ nameof(OnEndBuildAllAssets));
         
        }

        #endregion
        

        #region 发布资源

        /// <summary>
        /// 发布资源处理前
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="versionNum"></param>
        public override void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            
            Debug.Log("实现回调测试:"+ nameof(OnBeginPublishAssets));
        }

        /// <summary>
        /// 发布资源处理后
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="versionNum"></param>
        public override void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
        
            Debug.Log("实现回调测试:"+ nameof(OnEndPublishAssets));
        }

        #endregion
    }
}
