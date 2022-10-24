using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Load Assets/Load Last Imported Items", 19)]
	public class LoaderLastImported : Node {

        private List<AssetReference> m_lastImportedAssets;

		public override string ActiveStyle {
			get {
				return "node 0 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 0";
			}
		}

		public override string Category {
			get {
				return "Loader last imported";
			}
		}
			
		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}

		public override void Initialize(Model.NodeData data) {
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new LoaderLastImported();

			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override bool OnAssetsReimported(
			Model.NodeData nodeData,
			AssetReferenceStreamManager streamManager,
			BuildTarget target, 
            AssetPostprocessorContext ctx,
            bool isBuilding)
		{
            if (m_lastImportedAssets == null) {
                m_lastImportedAssets = new List<AssetReference> ();
			}
		
            m_lastImportedAssets.Clear ();
            m_lastImportedAssets.AddRange (ctx.ImportedAssets);
            m_lastImportedAssets.AddRange (ctx.MovedAssets);

			return true;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			EditorGUILayout.HelpBox("Last Imported Items: Load assets just imported.", MessageType.Info);
			inspector.UpdateNodeName(node);

		}


		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if (m_lastImportedAssets != null) {
				m_lastImportedAssets.RemoveAll (a => a == null);
			}

			Load(target, node, connectionsToOutput, Output);
		}
		
		void Load (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if(connectionsToOutput == null || Output == null) {
				return;
			}
			
			if (m_lastImportedAssets == null) {
				m_lastImportedAssets = new List<AssetReference> ();
			}			

			var output = new Dictionary<string, List<AssetReference>> {
                {"0", m_lastImportedAssets}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}
	}
}