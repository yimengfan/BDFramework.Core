using System;
using System.Collections.Generic;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// BDFramework publish pipeline各种事件
    /// </summary>
    static public class BDFrameworkPublishPipelineHelper
    {
        static private List<ABDFrameworkPublishPipelineBehaviour> BDFrameworkPipelineBehaviourInstanceList = new List<ABDFrameworkPublishPipelineBehaviour>();
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            var type = typeof(ABDFrameworkPublishPipelineBehaviour);
            var types = BDFrameEditorLife.Types;
            foreach (var t in types)
            {
                if (t.IsSubclassOf(type))
                {
                    var buildPipelineInst = Activator.CreateInstance(t) as ABDFrameworkPublishPipelineBehaviour;
                    BDFrameworkPipelineBehaviourInstanceList.Add(buildPipelineInst);
                }
            }
        }

        /// <summary>
        /// 开始打包热更dll
        /// </summary>
        static public void OnBeginBuildHotfixDLL()
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginBuildDLL();
            }
        }

        /// <summary>
        /// 结束打包热更dll
        /// </summary>
        static public void OnEndBuildDLL(string outputPath)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndBuildDLL(outputPath);
            }
        }

        /// <summary>
        /// 开始导出sqlite
        /// </summary>
        static public void OnBeginBuildSqlite()
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginBuildSqlite();
            }
        }

        /// <summary>
        /// 导出sqlite结束
        /// </summary>
        /// <param name="outputPath"></param>
        static public void OnEndBuildSqlite(string outputPath)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndBuildSqlite(outputPath);
            }
        }

        /// <summary>
        /// 开始打包assetbundle
        /// </summary>
        /// <param name="params"></param>
        /// <param name="buildInfo"></param>
        static public void OnBeginBuildAssetBundle( BuildAssetBundleParams @params, BuildInfo buildInfo)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBuildAssetBundleBegin(@params,buildInfo);
            }
        }

        static public void OnEndBuildAssetBundle( BuildAssetBundleParams @params, BuildInfo buildInfo)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBuildAssetBundleEnd(@params,buildInfo);
            }
        }
        
        /// <summary>
        /// 正在导出excel
        /// </summary>
        /// <param name="type"></param>
        static public void OnExportExcel(Type type)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnExportExcel(type);
            }
        }
        
        
        /// <summary>
        /// 发布资源处理前
        /// </summary>
        static public void OnPublishAssetsProccessBegin(RuntimePlatform platform, string outputPath, out string versionNum)
        {
            versionNum = "version-null";
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnPublishAssetsProccessBegin(platform, outputPath,out versionNum);
            }
        }
        
        /// <summary>
        /// 发布资源处理后
        /// </summary>
        static public void OnPublishAssetsProccessEnd(RuntimePlatform platform,string outputPath)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnPublishAssetsProccessEnd(platform, outputPath);
            }
        }
    }
}