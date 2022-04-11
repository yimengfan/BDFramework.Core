using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
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

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
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
            Debug.Log("【BuildAssetbundle】执行Prepare");

            if (incoming == null)
            {
                return;
            }
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;
            
            //这里只做临时的输出，预览用，不做实际更改
            var tempBuildAssetsInfo = this.BuildingCtx.BuildAssetsInfo?.Clone();
            if (tempBuildAssetsInfo == null)
            {
                tempBuildAssetsInfo = new BuildAssetsInfo();
            }

            Debug.Log("Buildinfo 数量:" + tempBuildAssetsInfo.AssetDataMaps.Count);
            //预计算输出,不直接修改buildinfo
            // var platform = BDApplication.GetRuntimePlatform(target);
            BDFrameworkAssetsEnv.BuildingCtx.MergeABName(tempBuildAssetsInfo);
            //对比差异文件
            BDFrameworkAssetsEnv.BuildingCtx.GetChangedAssets(tempBuildAssetsInfo, target);

            //搜集所有的 asset reference 
            List<AssetReference> assetReferenceList = new List<AssetReference>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    assetReferenceList.AddRange(ag.Value);
                }
            }

            //----------------验证资源-------------
            if (assetReferenceList.Count == BDFrameworkAssetsEnv.BuildingCtx.BuildAssetsInfo.AssetDataMaps.Count)
            {
                foreach (var ar in assetReferenceList)
                {
                    if (! this.BuildingCtx.BuildAssetsInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                    {
                        Debug.LogError("【资源验证】不存在:" + ar.importFrom);
                    }
                }
            }
            else
            {
                var list = new List<string>();
                if (assetReferenceList.Count > tempBuildAssetsInfo.AssetDataMaps.Count)
                {
                    foreach (var ar in assetReferenceList)
                    {
                        if (!tempBuildAssetsInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                        {
                            list.Add(ar.importFrom);
                        }
                    }

                    Debug.Log("Buildinfo缺少资源:\n " + JsonMapper.ToJson(list));
                }
                else
                {
                    foreach (var key in tempBuildAssetsInfo.AssetDataMaps.Keys)
                    {
                        var ret = assetReferenceList.Find((ar) => ar.importFrom.Equals(key, StringComparison.OrdinalIgnoreCase));
                        if (ret == null)
                        {
                            list.Add(key);
                        }
                    }

                    Debug.Log("Buildinfo多余资源:\n " + JsonMapper.ToJson(list, true));
                }

                Debug.LogErrorFormat("【资源验证】coming资源和Buildinfo资源数量不相等!{0}-{1}", assetReferenceList.Count, tempBuildAssetsInfo.AssetDataMaps.Count);
            }


            //输出节点 预览
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var buildAssetItem in tempBuildAssetsInfo.AssetDataMaps)
            {
                if (!outMap.TryGetValue(buildAssetItem.Value.ABName, out var list))
                {
                    list = new List<AssetReference>();
                    outMap[buildAssetItem.Value.ABName] = list;
                }

                //找到资源的assetref
                var ar = assetReferenceList.Find((a) => a.importFrom.Equals(buildAssetItem.Key, StringComparison.OrdinalIgnoreCase));
                if (ar != null)
                {
                    list.Add(ar);
                }
                else
                {
                    Debug.LogFormat("<color=red>【BuildAssetBundle】资源没有inComing:{0} </color>", buildAssetItem.Key);
                }
            }

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }

        

        /// <summary>
        /// build assetbundle的结果，用以给后续流程使用
        /// </summary>
        public static BuildAssetsInfo BuildAssetsResult { get; private set; } = null;

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

            BuildAssetsResult = BDFrameworkAssetsEnv.BuildingCtx.BuildAssetsInfo.Clone();
            
            BDFrameworkAssetsEnv.BuildingCtx = null;
        }


    }
}
