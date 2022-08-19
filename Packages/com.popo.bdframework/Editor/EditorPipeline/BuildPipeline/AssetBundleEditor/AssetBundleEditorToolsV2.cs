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


        #region  构建资产

        /// <summary>
        /// 执行AssetGraph构建打包
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outPath"></param>
        /// <param name="isUseHash"></param>
        static public void ExcuteAssetGraphBuild(BuildTarget buildTarget, string outPath)
        {
            var (cg, bdenvNode) = GetBDFrameExAssetGraph();
            var bdenv = (bdenvNode.Operation.Object as BDFrameworkAssetsEnv);
            bdenv.SetBuildParams(outPath, true);
            //执行
            AssetGraphUtility.ExecuteGraph(buildTarget, cg);
        }


        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isUseHashName">是否为hash name</param>
        public static bool GenAssetBundle(RuntimePlatform platform, string outputPath)
        {
            var buildTarget = BApplication.GetBuildTarget(platform);
            ExcuteAssetGraphBuild(buildTarget, outputPath);
            return true;
        }



        #endregion



        #region MyRegion 

        

        #endregion
        
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
        static public void MixAssetBundle(string outpath, RuntimePlatform platform)
        {
            var mixAssets = GetMixAssets();
            if (mixAssets.Length == 0)
            {
                Debug.LogError("【AssetBundle混淆】不存在混淆源文件!");
            }

            byte[][] mixSourceBytes = new byte[mixAssets.Length][];
            for (int i = 0; i < mixAssets.Length; i++)
            {
                var path = IPath.Combine(outpath, BApplication.GetPlatformPath(platform), BResources.ART_ASSET_ROOT_PATH, mixAssets[i]);
                var mixBytes = File.ReadAllBytes(path);
                mixSourceBytes[i] = mixBytes;
            }

            //构建ab管理器对象
            AssetBundleMgrV2 abv2 = new AssetBundleMgrV2();
            abv2.Init(outpath);
            //
            var mixAssetbundleItems = abv2.AssetConfigLoder.AssetbundleItemList.Where((i) => mixAssets.Contains(i.AssetBundlePath)).ToArray();

            Debug.Log("<color=green>--------------------开始混淆Assetbundle------------------------</color>");

            //开始混淆AssetBundle
            for (int i = 0; i < abv2.AssetConfigLoder.AssetbundleItemList.Count; i++)
            {
                //源AB
                var sourceItem = abv2.AssetConfigLoder.AssetbundleItemList[i];
                //非混合文件、ab不存在、mix过
                if (mixAssetbundleItems.Contains(sourceItem) || sourceItem.AssetBundlePath == null || sourceItem.Mix > 0)
                {
                    continue;
                }

                var idx = (int) (Random.Range(0, (mixAssetbundleItems.Length - 1) * 10000) / 10000);
                var mixBytes = mixSourceBytes[idx];
                //
                var abpath = IPath.Combine(outpath, BApplication.GetPlatformPath(platform), BResources.ART_ASSET_ROOT_PATH, sourceItem.AssetBundlePath);
                if (!File.Exists(abpath))
                {
                    
                    Debug.LogError($"不存在AB:{sourceItem.AssetBundlePath} - {AssetDatabase.GUIDToAssetPath(sourceItem.AssetBundlePath)}");
                    continue;
                }
                
                var abBytes = File.ReadAllBytes(abpath);
                //拼接
                var outbytes = new byte[mixBytes.Length + abBytes.Length];
                Array.Copy(mixBytes, 0, outbytes, 0, mixBytes.Length);
                Array.Copy(abBytes, 0, outbytes, mixBytes.Length, abBytes.Length);
                //写入
                FileHelper.WriteAllBytes(abpath, outbytes);
                var hash = FileHelper.GetMurmurHash3(abpath);

                //相同ab的都进行赋值，避免下次重新被修改。
                foreach (var item in abv2.AssetConfigLoder.AssetbundleItemList)
                {
                    if (sourceItem.AssetBundlePath.Equals(item.AssetBundlePath))
                    {
                        item.Mix = mixBytes.Length;
                        item.Hash = hash;
                    }
                }

                //sourceItem.Mix = mixBytes.Length;

                //混淆
                Debug.Log("【Assetbundle混淆】" + sourceItem.AssetBundlePath);
            }

            //重新写入配置
            abv2.AssetConfigLoder.OverrideConfig();
            Debug.Log("<color=green>--------------------混淆Assetbundle完毕------------------------</color>");
        }

        #endregion


        /// <summary>
        /// 这里将
        /// </summary>
        /// <returns></returns>
        static public string GetGetAssetbundleSourceHash()
        {


            return "";
        }
    }
}
