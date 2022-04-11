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
	[CustomNode("Addressables/Set Asset Group", 101)]
	public class AddressableAssetGroup : Node {

        [SerializeField] private string m_groupGuid;

        private UnityEditor.AddressableAssets.Settings.AddressableAssetGroup m_group; 

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
            m_groupGuid = string.Empty;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new AddressableAssetGroup();
            newNode.m_groupGuid = m_groupGuid;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Addressable Asset Group: Configure Asset Group for Addressable System.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			if (!AddressableAssetSettingsDefaultObject.SettingsExists)
			{
				EditorGUILayout.HelpBox("To work with this node, create Addressable Asset Settings object from Addressables window first.", MessageType.Warning);
				return;
			}

			var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

			if (!string.IsNullOrEmpty(m_groupGuid))
			{
				m_group = settings.FindGroup(g => g.Guid == m_groupGuid);
			}
			
			var groupNames = settings.groups.Where(g => !g.ReadOnly).Select(g => g.Name).ToArray();
			
			var selectedIndex = Array.IndexOf(groupNames, m_group == null ? string.Empty : m_group.Name);

            var newIndex = EditorGUILayout.Popup("Group", selectedIndex, groupNames);
            if (newIndex != selectedIndex)
            {
	            var newGroup = settings.FindGroup(g => g.Name == groupNames[newIndex]);

	            m_group = newGroup;
	            m_groupGuid = newGroup.Guid;
	            
                onValueChanged ();
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

            if (string.IsNullOrEmpty(m_groupGuid))
            {
	            throw new NodeException ($"No valid Asset Group is selected.", "Configure valid Asset Group from inspector.");
            }
            
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            m_group = settings.FindGroup(g => g.Guid == m_groupGuid);            
            if (m_group == null)
            {
	            throw new NodeException ($"Asset Group '{m_groupGuid}' not found.", "Reselect valid Asset Group from inspector.");
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

        public override void Build (BuildTarget target, 
            Model.NodeData node, 
            IEnumerable<PerformGraph.AssetGroups> incoming, 
            IEnumerable<Model.ConnectionData> connectionsToOutput, 
            PerformGraph.Output Output,
            Action<Model.NodeData, string, float> progressFunc) 
        {
            ConfigureGroup(incoming);
            
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


		private void ConfigureGroup (IEnumerable<PerformGraph.AssetGroups> incoming) 
		{
            var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            
			if(incoming != null) {
				foreach(var ag in incoming) {
                    foreach (var g in ag.assetGroups.Keys) {
                        var assets = ag.assetGroups [g];
						foreach(var a in assets) {

                            var guid = a.assetDatabaseId;

                            var entry = aaSettings.FindAssetEntry(guid);
                            if (entry != null) {
	                            aaSettings.MoveEntry(entry, m_group);
                            }
						}
					}
				}
			}
		}
    }
}

#endif