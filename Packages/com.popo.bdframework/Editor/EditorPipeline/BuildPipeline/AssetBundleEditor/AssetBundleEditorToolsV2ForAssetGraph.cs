using System;
using System.Collections.Generic;
using BDFramework.Editor.AssetGraph.Node;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// AssetGraph构建AssetBundle
    /// </summary>
    static public class AssetBundleEditorToolsV2ForAssetGraph
    {
        /// <summary>
        /// 获取所有bd拓展的AssetGraph配置
        /// </summary>
        static public (ConfigGraph, NodeData) GetBDFrameExAssetGraph()
        {
            List<ConfigGraph> retList = new List<ConfigGraph>();

            var assets = AssetDatabase.FindAssets("t: UnityEngine.AssetGraph.DataModel.Version2.ConfigGraph", new string[1] {"Assets"});
            string envclsName = typeof(BDFrameworkAssetsEnv).FullName;
            foreach (var assetGuid in assets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var configGraph = AssetDatabase.LoadAssetAtPath<ConfigGraph>(assetPath);


                foreach (var node in configGraph.Nodes)
                {
                    if (node.Operation.Object is BDFrameworkAssetsEnv)
                    {
                        //含有bdenv节点的加入

                        return (configGraph, node);
                    }
                }
            }

            return (null, null);
        }


        /// <summary>
        /// build
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outPath"></param>
        /// <param name="isUseHash"></param>
        static public void Build(BuildTarget buildTarget, string outPath)
        {
            var (cg, bdenvNode) = GetBDFrameExAssetGraph();
            var bdenv = (bdenvNode.Operation.Object as BDFrameworkAssetsEnv);
            bdenv.SetBuildParams(outPath);
         
            //执行
            AssetGraphUtility.ExecuteGraph(buildTarget, cg);
        }
    }
}
