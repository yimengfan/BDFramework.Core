using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,不修改 只作为连线查看用 避免线到一坨了
    /// </summary>
    [CustomNode("BDFramework/[辅助]预览", 36)]
    public class SetGranularityNull : UnityEngine.AssetGraph.Node
    {
        public AssetBundleBuildingContext BuildingCtx { get; set; }
        public override string ActiveStyle
        {
            get { return "node 6 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 6"; }
        }

        public override string Category
        {
            get { return "[颗粒度]预览"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            return new SetGranularityNull();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
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

            if (incoming == null)
            {
                return;
            }
            
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;
            var outMap = new Dictionary<string, List<AssetReference>>();
            var comingList = AssetGraphTools.GetComingAssets(incoming);

            foreach (var ar in comingList)
            {
                var ai=  this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                if (ai != null)
                {
                    if (!outMap.ContainsKey(ai.ABName))
                    {
                        outMap[ai.ABName] = new List<AssetReference>();
                    }
                    outMap[ai.ABName].Add(ar);
                }
            }
            //
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }
    }
}