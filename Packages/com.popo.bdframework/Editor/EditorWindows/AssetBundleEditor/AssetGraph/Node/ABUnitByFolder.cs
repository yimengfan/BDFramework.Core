using System;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[颗粒度]文件夹", 50)]
    public class ABUnitByFolder : UnityEngine.AssetGraph.Node
    {
        public override string ActiveStyle
        {
            get { return "node 1 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 1"; }
        }

        public override string Category
        {
            get { return "[颗粒度]文件夹"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new ABUnitByFolder();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
            NodeGUIEditor editor,
            Action onValueChanged)
        {
        }
    }
}