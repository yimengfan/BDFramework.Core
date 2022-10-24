
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
	[CustomNode("Modify Assets/Label Assets", 62)]
	public class Label : Node {

		[SerializeField] private string m_label;
		[SerializeField] private bool m_overwriteLabels;

		public override string ActiveStyle {
			get {
				return "node 8 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 8";
			}
		}

		public override string Category {
			get {
				return "Modify";
			}
		}

		public override void Initialize(Model.NodeData data) {
			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Label();
			newNode.m_label = m_label;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			EditorGUILayout.HelpBox("Label Assets: Add Label to incoming assets.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				bool newOverwriteLabels = EditorGUILayout.ToggleLeft ("Overwrite", m_overwriteLabels);
				GUILayout.Space(4f);
				if (newOverwriteLabels != m_overwriteLabels) {
					using(new RecordUndoScope("Change Overwrite Label", node, true)){
						m_overwriteLabels = newOverwriteLabels;
						onValueChanged();
					}
				}

				var newLabel = EditorGUILayout.TextField("Label",m_label);
				EditorGUILayout.HelpBox("You can use \",\" to specify multiple labels. You can also use \"*\" to include group name for label.", MessageType.Info);

				if (newLabel != m_label) {
					using(new RecordUndoScope("Change Label", node, true)){
						m_label = newLabel;
						onValueChanged();
					}
				}
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			// Label does not add, filter or change structure of group, so just pass given group of assets
			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				if(incoming != null) {
					foreach(var ag in incoming) {
						Output(dst, ag.assetGroups);
					}
				} else {
					Output(dst, new Dictionary<string, List<AssetReference>>());
				}
			}
		}

		public override void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming != null) {
				foreach(var ag in incoming) {
					foreach (var groupName in ag.assetGroups.Keys) {
						var labels = m_label.Replace ("*", groupName).Split(',').Select (s => s.Trim ()).Where (s => !string.IsNullOrEmpty (s)).Distinct ();
						if (labels.Any()) {
							var assets = ag.assetGroups [groupName];
							foreach(var a in assets) {
								var o = AssetDatabase.LoadMainAssetAtPath (a.importFrom);

								if (m_overwriteLabels) {
									AssetDatabase.SetLabels (o, labels.ToArray());
								} else {
									var currentLabels = AssetDatabase.GetLabels (o);
									var combined = labels.ToList ();
									combined.AddRange (currentLabels);
									AssetDatabase.SetLabels (o, combined.Distinct().ToArray());
								}
							}
						}
					}
				}
			}
		}
	}
}