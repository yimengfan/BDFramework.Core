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
    static public class BDFrameworkPipelineHelper
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

        
        #region 一键构建资源

        /// <summary>
        /// 【构建所有资源】处理前
        /// </summary>
        static public void  OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath,string lastVersionNum,out string newVersionNum)
        {
            Debug.Log("【OnBeginBuildAllAssets生命周期测试】构建资源,请生成版本号信息!!!  ->" + platform.ToString());
            newVersionNum = lastVersionNum;
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginBuildAllAssets(platform, outputPath, lastVersionNum,out newVersionNum);
            }
            Debug.Log($"<color=red> 新版本号:{newVersionNum} </color>");
        }
        
        /// <summary>
        /// 【构建所有资源】 处理后
        /// </summary>
        static public void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath,string newVersionNum)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndBuildAllAssets(platform, outputPath, newVersionNum);
            }
            Debug.Log("【OnEndBuildAllAssets】构建资源已完成!" );
        }

        #endregion

        #region 发布资源
        /// <summary>
        /// 【发布资源】处理前,该资源提交到服务器
        /// </summary>
        static public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            Debug.Log("【OnBeginPublishAssets】发布资源处理前.  ->" + platform.ToString());
            
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnBeginPublishAssets(platform, outputPath, versionNum);
            }
        }
        
        /// <summary>
        /// 【发布资源】 处理后,该资源提交到服务器
        /// </summary>
        static public void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            foreach (var behavior in BDFrameworkPipelineBehaviourInstanceList)
            {
                behavior.OnEndPublishAssets(platform, outputPath, versionNum);
            }
            
            Debug.Log($"<color=red> 新版本号:{versionNum} </color>");
            Debug.Log("【OnEndPublishAssets】发布资源已生成,请编写脚本提交以下目录到Server!" );
            Debug.Log(outputPath);
            Debug.Log("------------------------------------------------end---------------------------------------------------");
        }
        #endregion

        #region 构建母包
        /// <summary>
        /// 【构建母包】开始
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
        /// 【构建母包】结束
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
        #endregion
    }
}