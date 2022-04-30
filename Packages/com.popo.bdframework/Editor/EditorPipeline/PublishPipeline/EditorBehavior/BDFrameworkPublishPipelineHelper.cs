using System;
using System.Collections.Generic;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.Editor.Environment;
using UnityEditor;
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
            var types = BDFrameworkEditorEnvironment.Types;
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
        /// <param name="assetbundleBuildingCtx"></param>
        static public void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginBuildAssetBundle(assetbundleBuildingCtx);
            }
        }

        static public void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndBuildAssetBundle(assetbundleBuildingCtx);
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
        static public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string lastVersionNum, out string versionNum)
        {
            versionNum = "0.0.0";
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginPublishAssets(outputPath,platform, lastVersionNum, out versionNum);
            }
        }
        
        /// <summary>
        /// 发布资源处理后
        /// </summary>
        static public void OnEndPublishAssets(RuntimePlatform platform,string outputPath)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndPublishAssets(platform, outputPath);
            }
        }
        
        
        
        /// <summary>
        /// 构建母包开始
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outputpath"></param>
        static public void OnBeginBuildPackage(BuildTarget buildTarget, string outputpath)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginBuildPackage(buildTarget, outputpath);
            }
        }

        /// <summary>
        /// 构建母包结束
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outputpath"></param>
        static public void OnEndBuildPackage( BuildTarget buildTarget, string outputpath)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndBuildPackage(buildTarget, outputpath);
            }
        }
    }
}