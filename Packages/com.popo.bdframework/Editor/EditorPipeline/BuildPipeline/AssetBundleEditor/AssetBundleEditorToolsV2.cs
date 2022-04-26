using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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

        #region Assetbundle混淆

        /// <summary>
        /// 获取混淆的资源
        /// </summary>
        /// <returns></returns>
        static public string[] GetMixAssets()
        {
            return AssetDatabase.FindAssets("t:TextAsset", new string[] {BResources.MIX_SOURCE_FOLDER});
        }

        /// <summary>
        /// 检测混淆资源
        /// </summary>
        static public void CheckABObfuscationSource()
        {
            var mixAsset = GetMixAssets();
            if (mixAsset.Length == 0)
            {
                Debug.LogError("【AssetBundle】不存在混淆源文件!");
            }
        }


        /// <summary>
        /// 添加混淆
        /// </summary>
        static public void MixAssetBundle(string path)
        {
            var mixAssets = GetMixAssets();
            //构建ab管理器对象
            AssetBundleMgrV2 abv2 = new AssetBundleMgrV2();
            abv2.Init(path);
            //
            var mixAssetbundleItems = abv2.AssetConfigLoder.AssetbundleItemList.Where((i) => mixAssets.Contains(i.AssetBundlePath)).ToArray();


            //开始混淆AssetBundle
            for (int i = 0; i < abv2.AssetConfigLoder.AssetbundleItemList.Count; i++)
            {
                //源AB
                var sourceItem = abv2.AssetConfigLoder.AssetbundleItemList[i];
                //非混合文件
                if (mixAssetbundleItems.Contains(sourceItem))
                {
                    continue;
                }

                var idx = (int) (Random.Range(0, (mixAssetbundleItems.Length - 1) * 10000) / 10000);
                var mixItem = mixAssetbundleItems[idx];
                //
                var mixBytes = File.ReadAllBytes(IPath.Combine(path, BResources.ASSET_ROOT_PATH, mixItem.AssetBundlePath));
                var abpath = IPath.Combine(path, BResources.ASSET_ROOT_PATH, sourceItem.AssetBundlePath);
                var abBytes = File.ReadAllBytes(abpath);

                //拼接
                var mixLen = mixBytes.Length;
                Array.Resize(ref mixBytes, mixBytes.Length + abBytes.Length);
                Array.Copy(abBytes, 0, mixBytes, mixLen, abBytes.Length);
                
                //写入
                File.WriteAllBytes(abpath,mixBytes);
                sourceItem.Mix = mixLen;
            }
            //
            abv2.AssetConfigLoder.OverrideConfig();
        }

        #endregion
    }
}
