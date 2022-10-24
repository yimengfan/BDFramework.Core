using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using LitJson;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
using Debug = UnityEngine.Debug;
using String = System.String;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[Build]打包AssetBundle", 100)]
    public class BuildAssetBundle : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 打包AB的上下文工具
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }

        public override string ActiveStyle
        {
            get { return "node 7 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 7"; }
        }

        public override string Category
        {
            get { return "打包AssetBundle"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddOutputPoint("预览打包结果");
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new BuildAssetBundle();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            //
            if (GUILayout.Button("重新生成-" + BResources.ART_ASSET_INFO_PATH))
            {
                BDFrameworkAssetsEnv.BuildingCtx.StartBuildAssetBundle(AssetGraphEditorWindow.Window.BuildTarget, buildMode: AssetBundleBuildingContext.BuildMode.GenArtAssetInfo);
            }
        }

        /// <summary>
        /// 预览结果 编辑器连线数据，但是build模式也会执行
        /// 这里只建议设置BuildingCtx的ab颗粒度
        /// </summary>
        /// <param name="target"></param>
        /// <param name="nodeData"></param>
        /// <param name="incoming"></param>
        /// <param name="connectionsToOutput"></param>
        /// <param name="outputFunc"></param>
        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            Debug.Log("【BuildAssetbundle】执行:" + this.Category);

            if (incoming == null || BDFrameworkAssetsEnv.BuildingCtx == null)
            {
                return;
            }

            var comingList = AssetGraphTools.GetComingAssets(incoming);
            if (comingList.Count == 0)
            {
                return;
            }

            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;
            //这里只做临时的输出，预览用，不做实际更改
            var tempBuildAssetsInfo = this.BuildingCtx.BuildAssetInfos?.Clone();
            if (tempBuildAssetsInfo == null)
            {
                tempBuildAssetsInfo = new BuildAssetInfos();
            }

            Debug.Log("Buildinfo 数量:" + tempBuildAssetsInfo.GetValidAssetsCount());
            //预计算输出,不直接修改buildinfo
            //重整assetbundle颗粒度
            tempBuildAssetsInfo.ReorganizeAssetBundleUnit();
            //获取ab列表
            var newAssetBundleItemList = tempBuildAssetsInfo.GetAssetBundleItems();
            //对比差异文件
           var changedAssetList = AssetBundleToolsV2.GetChangedAssetsByFileHash(this.BuildingCtx.BuildParams.OutputPath, target, tempBuildAssetsInfo);
        //    var changedAssetList2 = AssetBundleToolsV2.GetChangedAssetsByCompareAB(this.BuildingCtx.BuildParams.OutputPath, target, newAssetBundleItemList);
            //搜集所有的 asset reference 
            var comingAssetReferenceList = AssetGraphTools.GetComingAssets(incoming);

            //----------------验证资源-------------
            if (comingAssetReferenceList.Count == BDFrameworkAssetsEnv.BuildingCtx.BuildAssetInfos.GetValidAssetsCount())
            {
                foreach (var ar in comingAssetReferenceList)
                {
                    if (!this.BuildingCtx.BuildAssetInfos.AssetInfoMap.ContainsKey(ar.importFrom))
                    {
                        Debug.LogError("【资源验证】不存在:" + ar.importFrom);
                    }
                }
            }
            else
            {
                var list = new List<string>();
                if (comingAssetReferenceList.Count > tempBuildAssetsInfo.AssetInfoMap.Count)
                {
                    foreach (var ar in comingAssetReferenceList)
                    {
                        if (!tempBuildAssetsInfo.AssetInfoMap.ContainsKey(ar.importFrom))
                        {
                            list.Add(ar.importFrom);
                        }
                    }

                    Debug.Log("Buildinfo缺少资源:\n " + JsonMapper.ToJson(list));
                }
                else
                {
                    foreach (var key in tempBuildAssetsInfo.AssetInfoMap.Keys)
                    {
                        var ret = comingAssetReferenceList.Find((ar) => ar.importFrom.Equals(key, StringComparison.OrdinalIgnoreCase));
                        if (ret == null)
                        {
                            list.Add(key);
                        }
                    }

                    Debug.Log("Buildinfo多余资源:\n " + JsonMapper.ToJson(list, true));
                }

                Debug.LogErrorFormat("【资源验证】coming资源和Buildinfo资源数量不相等 {0}-{1}，请注意~", comingAssetReferenceList.Count, tempBuildAssetsInfo.GetValidAssetsCount());
            }


            //输出节点 预览
            var outMap = new Dictionary<string, List<AssetReference>>();
            if (comingAssetReferenceList.Count > 0)
            {
                foreach (var buildAssetItem in tempBuildAssetsInfo.AssetInfoMap)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(buildAssetItem.Value.ABName);
                    if (!outMap.TryGetValue(assetPath, out var list))
                    {
                        list = new List<AssetReference>();
                        outMap[assetPath] = list;
                    }

                    //找到资源的assetref
                    var ar = comingAssetReferenceList.Find((a) => a.importFrom.Equals(buildAssetItem.Key, StringComparison.OrdinalIgnoreCase));
                    if (ar != null)
                    {
                        list.Add(ar);
                    }
                    else if(!Directory.Exists(buildAssetItem.Key)) //非文件夹，则报错
                    {
                        Debug.LogFormat("<color=red>【BuildAssetBundle】资源没有inComing:{0} </color>", buildAssetItem.Key);
                    }
                }
            }

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }




        /// <summary>
        /// 构建时候触发
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="nodeData"></param>
        /// <param name="incoming"></param>
        /// <param name="connectionsToOutput"></param>
        /// <param name="outputFunc"></param>
        /// <param name="progressFunc"></param>
        public override void Build(BuildTarget buildTarget, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc,
            Action<NodeData, string, float> progressFunc)
        {
            //构建ab包
            BDFrameworkAssetsEnv.BuildingCtx.StartBuildAssetBundle(buildTarget);

        }
    }
}
