using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Load Assets/Load By Search Filter", 11)]
	public class LoaderBySearch : Node {

		[SerializeField] private SerializableMultiTargetString m_searchFilter;
        [SerializeField] private bool m_respondToAssetChange;

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
				return "LoadBySearch";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_searchFilter = new SerializableMultiTargetString();
            m_respondToAssetChange = false;

			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new LoaderBySearch();
			newNode.m_searchFilter = new SerializableMultiTargetString(m_searchFilter);
            newNode.m_respondToAssetChange = m_respondToAssetChange;

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
            if (isBuilding && !m_respondToAssetChange) {
                return false;
            }

            if (m_searchFilter == null) {
                return false;
            }

            var cond = m_searchFilter[target];
            var guids = AssetDatabase.FindAssets(cond);
            if (guids.Length == 0) {
                return false;
            }

            List<string> reimportedAssetGuids = new List<string> ();

            foreach (var a in ctx.ImportedAssets) {
                reimportedAssetGuids.Add (a.assetDatabaseId);
            }
            foreach (var a in ctx.MovedAssets) {
                reimportedAssetGuids.Add (a.assetDatabaseId);
            }

            foreach (var guid in guids) {
                if (reimportedAssetGuids.Contains (guid)) {
                    return true;
                }
            }

            return false;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			if (m_searchFilter == null) {
				return;
			}

			EditorGUILayout.HelpBox("Load By Search Filter: Load assets match given search filter condition.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);

            bool bRespondAP = EditorGUILayout.ToggleLeft ("Respond To Asset Change", m_respondToAssetChange);
            if (bRespondAP != m_respondToAssetChange) {
                using (new RecordUndoScope ("Remove Target Load Path Settings", node, true)) {
                    m_respondToAssetChange = bRespondAP;
                }
            }

            GUILayout.Space(4f);

			//Show target configuration tab
			inspector.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = inspector.DrawOverrideTargetToggle(node, m_searchFilter.ContainsValueOf(inspector.CurrentEditingGroup), (bool b) => {
					using(new RecordUndoScope("Remove Target Search Filter Settings", node, true)) {
						if(b) {
							m_searchFilter[inspector.CurrentEditingGroup] = m_searchFilter.DefaultValue;
						} else {
							m_searchFilter.Remove(inspector.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
					var condition = m_searchFilter[inspector.CurrentEditingGroup];
					EditorGUILayout.LabelField("Search Filter");

					string newCondition = null;

					using(new EditorGUILayout.HorizontalScope()) {
						newCondition = EditorGUILayout.TextField(condition);
					}

					if (newCondition != condition) {
						using(new RecordUndoScope("Modify Search Filter", node, true)){
							m_searchFilter[inspector.CurrentEditingGroup] = newCondition;
							onValueChanged();
						}
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
			ValidateSearchCondition(
				m_searchFilter[target],
				() => {
					throw new NodeException(
                        "Serach filter is empty",
                        "Input search condition from inspector.", node);
				}
			);

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

			var cond = m_searchFilter[target];
			var outputSource = new List<AssetReference>();

			var guids = AssetDatabase.FindAssets(cond);

			foreach (var guid in guids) {

				var targetFilePath = AssetDatabase.GUIDToAssetPath(guid);

                if(!TypeUtility.IsLoadingAsset(targetFilePath)) {
                    continue;
                }


                var r = AssetReferenceDatabase.GetReference(targetFilePath);

				if(r != null) {
                    outputSource.Add(AssetReferenceDatabase.GetReference(targetFilePath));
				}
			}

			var output = new Dictionary<string, List<AssetReference>> {
				{"0", outputSource}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}

		public static void ValidateSearchCondition (string currentCondition, Action NullOrEmpty) {
			if (string.IsNullOrEmpty(currentCondition)) NullOrEmpty();
		}

		private string GetLoaderFullLoadPath(BuildTarget g) {
			return FileUtility.PathCombine(Application.dataPath, m_searchFilter[g]);
		}
	}
}