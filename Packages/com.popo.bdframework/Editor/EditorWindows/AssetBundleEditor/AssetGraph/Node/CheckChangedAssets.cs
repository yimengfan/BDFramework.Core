using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/检查变更资源", 50)]
    public class CheckChangedAssets : UnityEngine.AssetGraph.Node, IBDAssetBundleV2Node
    {
        public BuildInfo BuildInfo { get; private set; }

        public override string ActiveStyle
        {
            get { return "node 3 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 3"; }
        }

        public override string Category
        {
            get { return "检查变更资源"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new CheckChangedAssets();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
            NodeGUIEditor editor,
            Action onValueChanged)
        {
        }

        public override void Prepare(BuildTarget target, NodeData nodeData,
            IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput,
            PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }

            this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;

            var buildInfoPath = IPath.Combine(artOutputPath, "BuildInfo.json");
            //获取改动的数据
            BuildInfo lastBuildInfo = new BuildInfo();
            if (File.Exists(buildInfoPath))
            {
                var content = File.ReadAllText(buildInfoPath);
                lastBuildInfo = JsonMapper.ToObject<BuildInfo>(content);
            }

            var changedBuildInfo = GetChangedAssets(lastBuildInfo, this.BuildInfo);
            // newbuildInfo = null; //防止后面再用
            if (changedBuildInfo.AssetDataMaps.Count == 0)
            {
                Debug.Log("无资源改变,不需要打包!");
                return;
            }
        }


        /// <summary>
        /// 获取改动的Assets
        /// </summary>
        static BuildInfo GetChangedAssets(BuildInfo lastAssetsInfo, BuildInfo newAssetsInfo)
        {
            //根据变动的list 刷出关联
            //1.单ab 单资源，直接重打
            //2.单ab 多资源的 整个ab都要重新打包
            if (lastAssetsInfo.AssetDataMaps.Count != 0)
            {
                Debug.Log("<color=red>开始增量分析...</color>");
                var changedAssetList = new List<KeyValuePair<string, BuildInfo.AssetData>>();
                //找出差异文件
                foreach (var newAssetItem in newAssetsInfo.AssetDataMaps)
                {
                    BuildInfo.AssetData lastAssetData = null;
                    if (lastAssetsInfo.AssetDataMaps.TryGetValue(newAssetItem.Key, out lastAssetData))
                    {
                        if (lastAssetData.Hash == newAssetItem.Value.Hash)
                        {
                            continue;
                        }
                    }

                    changedAssetList.Add(newAssetItem);
                }

                Debug.LogFormat("<color=red>变动文件数:{0}</color>", changedAssetList.Count);
                //rebuild
                List<string> rebuildABNameList = new List<string>();
                foreach (var tempAsset in changedAssetList)
                {
                    //1.添加自身的ab
                    rebuildABNameList.Add(tempAsset.Value.ABName);
                    //2.添加所有依赖的ab
                    foreach (var depend in tempAsset.Value.DependList)
                    {
                        BuildInfo.AssetData dependAssetData = null;
                        if (newAssetsInfo.AssetDataMaps.TryGetValue(depend, out dependAssetData))
                        {
                            rebuildABNameList.Add(dependAssetData.ABName);
                        }
                        else
                        {
                            Debug.LogError("不存在资源:" + depend);
                        }
                    }
                }

                //去重
                rebuildABNameList = rebuildABNameList.Distinct().ToList();
                //搜索依赖的ab，直到没有新ab为止
                int counter = 0;
                while (counter < rebuildABNameList.Count) //防死循环
                {
                    string abName = rebuildABNameList[counter];

                    var findAssets = newAssetsInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == abName);
                    foreach (var asset in findAssets)
                    {
                        //添加本体
                        var assetdata = newAssetsInfo.AssetDataMaps[asset.Key];
                        if (!rebuildABNameList.Contains(assetdata.ABName))
                        {
                            rebuildABNameList.Add(assetdata.ABName);
                        }

                        //添加依赖文件
                        foreach (var depend in assetdata.DependList)
                        {
                            BuildInfo.AssetData dependAssetData = null;
                            if (newAssetsInfo.AssetDataMaps.TryGetValue(depend, out dependAssetData))
                            {
                                if (!rebuildABNameList.Contains(dependAssetData.ABName))
                                {
                                    rebuildABNameList.Add(dependAssetData.ABName);
                                }
                            }
                            else
                            {
                                Debug.LogError("不存在资源:" + depend);
                            }
                        }
                    }

                    counter++;
                }


                var allRebuildAssets = new List<KeyValuePair<string, BuildInfo.AssetData>>();
                //根据影响的ab，寻找出所有文件
                foreach (var abname in rebuildABNameList)
                {
                    var findAssets = newAssetsInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == abname);
                    allRebuildAssets.AddRange(findAssets);
                }


                //去重
                var retBuildInfo = new BuildInfo();
                foreach (var kv in allRebuildAssets)
                {
                    retBuildInfo.AssetDataMaps[kv.Key] = kv.Value;
                }

                Debug.LogFormat("<color=red>影响文件数:{0}</color>", retBuildInfo.AssetDataMaps.Count);

                return retBuildInfo;
            }

            return newAssetsInfo;
        }
    }
}