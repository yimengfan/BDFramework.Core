using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,不修改 只作为连线查看用 避免线到一坨了
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]空", 30)]
    public class SetGranularityNull : UnityEngine.AssetGraph.Node
    {
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
            get { return "[颗粒度]空"; }
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

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
        }

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            var outMap = new Dictionary<string, List<AssetReference>>();


            foreach (var ags in incoming)
            {
                foreach (var assetGroup in ags.assetGroups)
                {
                    outMap[assetGroup.Key] = assetGroup.Value.ToList();
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