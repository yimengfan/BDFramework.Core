using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,不修改 只作为连线查看用 避免线到一坨了
    /// </summary>
    [CustomNode("BDFramework/[辅助]模块", 35)]
    public class SetGranularityMoudule : UnityEngine.AssetGraph.Node
    {
        public AssetBundleBuildingContext BuildingCtx { get; set; }

        public override string ActiveStyle
        {
            get { return "node 2 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 2"; }
        }

        public override string Category
        {
            get { return "模块预览"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();

            return new SetGranularityMoudule();
        }

        private NodeGUI selfNodeGUI;

        public override void OnDrawNodeGUIContent(NodeGUI node)
        {
            this.selfNodeGUI = node;
            base.OnDrawNodeGUIContent(node);
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            inspector.UpdateNodeName(node);
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


            List<string> allLabelList = new List<string>();
            foreach (var ags in incoming)
            {
                var label = ags.connection.Label;
                allLabelList.Add(label);

                //准备输出数据
                var outMap = new Dictionary<string, List<AssetReference>>();
                foreach (var item in ags.assetGroups)
                {
                    outMap[item.Key] = new List<AssetReference>(item.Value);
                }

                //添加outpoint
                var outputPoint = nodeData.OutputPoints.FirstOrDefault((p) => p.IsOutput && p.Label == label);
                if (outputPoint == null)
                {
                    outputPoint = nodeData.AddOutputPoint(label);
                }

                //找到输出线
                if (connectionsToOutput != null)
                {
                    var outputLine = connectionsToOutput.FirstOrDefault((c) => c.FromNodeConnectionPointId == outputPoint.Id);
                    if (outputLine != null)
                    {
                        outputFunc(outputLine, outMap);
                    }
                }
            }

            //删除没有输入的output
            bool isRemoved = false;

            for (int i = nodeData.OutputPoints.Count - 1; i >= 0; i--)
            {
                var outputPoint = nodeData.OutputPoints[i];
                if (!allLabelList.Contains(outputPoint.Label))
                {
                    //刷新
                    isRemoved = true;
                    AssetGraphTools.RemoveOutputNode(this.selfNodeGUI, outputPoint);
                }
            }

            if (isRemoved)
            {
                AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
            }
        }


        private void AddOutPutNode()
        {
        }
    }
}
