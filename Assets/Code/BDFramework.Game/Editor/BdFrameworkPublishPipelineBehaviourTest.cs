using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    public class BdFrameworkPublishPipelineBehaviourTest : ABDFrameworkPublishPipelineBehaviour
    {
        public override void OnBeginBuildDLL()
        {
            Debug.Log("【BDFrameEditorBehavior】打包DLL前,回调测试!");
        }

        public override void OnEndBuildDLL(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包DLL后,回调测试!");
        }

        public override void OnBeginBuildSqlite()
        {
            Debug.Log("【BDFrameEditorBehavior】打包Sqlite前,回调测试!");
        }

        public override void OnEndBuildSqlite(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Sqlite后,回调测试!");
        }

        public override void OnBeginBuildAssetBundle(BuildAssetBundleParams @params, BuildInfo buildInfo)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Asset时,回调测试!");
        }

        public override void OnEndBuildAssetBundle(BuildAssetBundleParams @params, BuildInfo buildInfo)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Asset之后测试!");
        }

        /// <summary>
        /// 导出一张excel
        /// </summary>
        /// <param name="type"></param>
        public override void OnExportExcel(Type type)
        {
            Debug.Log("导出表格回调测试:" + type.FullName);
        }

        /// <summary>
        /// 准备提交到发布服务器
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        public override void ReadyPublishAssetsToServer(RuntimePlatform platform, string outputPath)
        {
            Debug.Log("资源转hash完毕,等待提交!  目录:" + outputPath);
        }
    }
}
