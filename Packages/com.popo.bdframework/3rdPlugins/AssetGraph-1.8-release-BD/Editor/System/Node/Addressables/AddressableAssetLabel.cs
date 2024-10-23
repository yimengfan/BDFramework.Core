/**
 * Addressable integration
 * 
 * This code will add features for Addressables System.
 * 
 * Addressables System is available from Unity Package Manager.
 */

#if ADDRESSABLES_1_6_OR_NEWER

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
	[CustomNode("Addressables/Set Asset Label", 102)]
	public class AddressableAssetLabel : Node {

		[SerializeField] private string m_label;
		[SerializeField] private bool m_overwriteLabels;

		public override string ActiveStyle {
			get {
				return "node 3 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 3";
			}
		}

		public override string Category {
			get {
				return "Addressable System";
			}
		}

		public override void Initialize(Model.NodeData data) {
			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new AddressableAssetLabel();
			newNode.m_label = m_label;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Addressable Label: Set Addressable Label to incoming assets.", MessageType.Info);
			editor.UpdateNodeName(node);

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
			if (!AddressableAssetSettingsDefaultObject.SettingsExists) {
				throw new NodeException ("Addressable Asset Settings not found.", "Create Addressable Asset Settings object from Addressables window.");
			}

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
			var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

			var registeredLabels = new List<string>();
			
			if(incoming != null) {
				foreach(var ag in incoming) {
					foreach (var groupName in ag.assetGroups.Keys) {
						var labels = m_label.Replace ("*", groupName).Split(',').Select (s => s.Trim ()).Where (s => !string.IsNullOrEmpty (s)).Distinct ();
						if (labels.Any() || m_overwriteLabels) {
							var assets = ag.assetGroups [groupName];
							
							foreach(var a in assets) {
								
								var entry = aaSettings.FindAssetEntry(a.assetDatabaseId);
								if (entry == null) {
									continue;
								}
																
								if (m_overwriteLabels)
								{
									var removingLabelEnum = entry.labels.Except(labels);
									if (removingLabelEnum.Any())
									{
										var removingLabels = removingLabelEnum.ToList();
										foreach (var removingLabel in removingLabels)
										{
											entry.SetLabel(removingLabel, false);
										}
									}
								}

								var addingLabelEnum = labels.Except(entry.labels);
								if (addingLabelEnum.Any())
								{
									var addingLabels = addingLabelEnum.ToList();
									foreach (var addingLabel in addingLabels)
									{
										if (!registeredLabels.Contains(addingLabel))
										{
											aaSettings.AddLabel(addingLabel);
											registeredLabels.Add(addingLabel);
										}
										entry.SetLabel(addingLabel, true);
									}								
								}
							}
						}
					}
				}
			}
			
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
    }
}

#endif