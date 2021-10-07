using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.ResourceMgr;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[逻辑]检查变更资源", 60)]
    public class CollectChangedAssets : UnityEngine.AssetGraph.Node, IBDFrameowrkAssetEnvParams
    {
        public BuildInfo BuildInfo { get;  set; }
        public BuildAssetBundleParams BuildParams { get; set; }

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
            return new CollectChangedAssets();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
            NodeGUIEditor editor,
            Action onValueChanged)
        {
        }

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput,
            PerformGraph.Output outputFunc)
        {

            if (this.BuildInfo == null)
            {
                this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;
            }
            if (this.BuildParams == null)
            {
                this.BuildParams = BDFrameworkAssetsEnv.BuildParams;
            }
            //加载上一次缓存的资源
            var lastbuildInfoPath = string.Format("{0}/{1}/{2}",this.BuildParams.OutputPath, BDApplication.GetPlatformPath(target),BResources.ASSET_BUILD_INFO_PATH);
            
            BuildInfo lastBuildInfo = new BuildInfo();
            if (File.Exists(lastbuildInfoPath))
            {
                var content = File.ReadAllText(lastbuildInfoPath);
                lastBuildInfo = JsonMapper.ToObject<BuildInfo>(content);
            }
            //获取变动资源
            var changedBuildInfo = GetChangedAssets(lastBuildInfo, this.BuildInfo);
            if (changedBuildInfo.AssetDataMaps.Count == 0)
            {
                Debug.Log("无资源改变,不需要打包!");
            }
            //输出
            if (incoming == null)
            {
                return;
            }

            Dictionary<string, List<AssetReference>> outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    var assetList = group.Value.ToList();
                    for (int i = assetList.Count-1; i >= 0; i--)
                    {
                        var ar = assetList[i];
                        if (!changedBuildInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                        {
                            assetList.RemoveAt(i);
                        }
                    }

                    outMap[group.Key] = assetList;

                }
            }


            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
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