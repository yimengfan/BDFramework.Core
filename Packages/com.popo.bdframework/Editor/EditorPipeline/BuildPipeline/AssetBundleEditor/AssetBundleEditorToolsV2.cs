using System;
using System.Collections.Generic;
using BDFramework.Core.Tools;
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
    static public class AssetBundleEditorToolsV2
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
        
                
        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="outputPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isUseHashName">是否为hash name</param>
        public static bool GenAssetBundle(string outputPath, RuntimePlatform platform)
        {
            var buildTarget = BDApplication.GetBuildTarget(platform);
            Build(buildTarget, outputPath);
            return true;
        }


        /// <summary>
        /// 获取主资源类型
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Type GetMainAssetTypeAtPath(string path)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            //图片类型得特殊判断具体的实例类型
            if (type == typeof(Texture2D))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null)
                {
                    return typeof(Sprite);
                }

                var tex2d = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex2d != null)
                {
                    return typeof(Texture2D);
                }

                var tex3d = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
                if (tex3d != null)
                {
                    return typeof(Texture3D);
                }
            }

            return type;
        }
    }
}
