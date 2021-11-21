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
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
	[CustomNode("Addressables/Set Asset Address", 100)]
	public class AddressableAssetAddress : Node {

		[SerializeField] private bool m_isAddressable;
		[SerializeField] private bool m_isAllLowerCase;
        [SerializeField] private string m_matchPattern;
        [SerializeField] private string m_addressPattern;

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
            m_isAddressable = true;
            m_isAllLowerCase = false;
            m_matchPattern = string.Empty;
            m_addressPattern = string.Empty;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new AddressableAssetAddress();
            newNode.m_isAddressable = m_isAddressable;
            newNode.m_isAllLowerCase = m_isAllLowerCase;
            newNode.m_matchPattern = m_matchPattern;
            newNode.m_addressPattern = m_addressPattern;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Asset Address: Configure Asset Address with pattern.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

            var newAddressable = EditorGUILayout.ToggleLeft("Addressable", m_isAddressable);
            if (newAddressable != m_isAddressable) {
                m_isAddressable = newAddressable;
                onValueChanged ();
            }

            var newMatchPattern = EditorGUILayout.TextField("Path Match Pattern", m_matchPattern);
            if (newMatchPattern != m_matchPattern) {
                m_matchPattern = newMatchPattern;
                onValueChanged ();
            }

            using (new EditorGUI.DisabledScope (string.IsNullOrEmpty (newMatchPattern))) {
                var newAddressPattern = EditorGUILayout.TextField("Address Pattern", m_addressPattern);
                if (newAddressPattern != m_addressPattern) {
                    m_addressPattern = newAddressPattern;
                    onValueChanged ();
                }
                
                var newAllLowerCase = EditorGUILayout.ToggleLeft("Set Address to Lower Case", m_isAllLowerCase);
                if (newAllLowerCase != m_isAllLowerCase) {
	                m_isAllLowerCase = newAllLowerCase;
	                onValueChanged ();
                }
            }
            

            EditorGUILayout.HelpBox(
                "You can configure address with regular expression. Leave Path Match Pattern blank if you just want to use asset path as address.", 
                MessageType.Info);
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
            ConfigureAddress(incoming);
            
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


		private void ConfigureAddress (IEnumerable<PerformGraph.AssetGroups> incoming) 
		{
            var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            Regex match = null;

            if (!string.IsNullOrEmpty (m_matchPattern)) {
                match = new Regex (m_matchPattern);
            }

			if(incoming != null) {
				foreach(var ag in incoming) {
                    foreach (var g in ag.assetGroups.Keys) {
                        var assets = ag.assetGroups [g];
						foreach(var a in assets) {

                            var guid = a.assetDatabaseId;

                            if (m_isAddressable) {
                                var entry = aaSettings.FindAssetEntry(guid);
                                if (entry == null) {
                                    entry = aaSettings.CreateOrMoveEntry (guid, aaSettings.DefaultGroup);
                                }

                                if (match != null) {
                                    if (match.IsMatch (a.importFrom)) {
                                        entry.address = match.Replace (a.importFrom, m_addressPattern);
                                    }
                                } else {
                                    entry.address = entry.AssetPath;
                                }

                                if (m_isAllLowerCase)
                                {
	                                entry.address = entry.address.ToLower();
                                }
                            } else {
                                aaSettings.RemoveAssetEntry (guid);
                            }
						}
					}
				}
			}
		}
    }
}

#endif