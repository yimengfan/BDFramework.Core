
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
	[CustomNode("Group Assets/Group By File", 42)]
	public class GroupingByFile : Node {

        [SerializeField] private SerializableMultiTargetString m_groupNameFormat;

        public override string ActiveStyle {
			get {
				return "node 2 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 2";
			}
		}

		public override string Category {
			get {
				return "GroupByFile";
			}
		}

		public override void Initialize(Model.NodeData data) {
            m_groupNameFormat = new SerializableMultiTargetString();

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new GroupingByFile();
            newNode.m_groupNameFormat = new SerializableMultiTargetString(m_groupNameFormat);

            newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			EditorGUILayout.HelpBox("Group By File: Create group per individual asset.", MessageType.Info);
			inspector.UpdateNodeName(node);

            GUILayout.Space(4f);

            //Show target configuration tab
            inspector.DrawPlatformSelector(node);
            using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {
                var disabledScope = inspector.DrawOverrideTargetToggle (node, m_groupNameFormat.ContainsValueOf (inspector.CurrentEditingGroup), (bool enabled) => {
                    using (new RecordUndoScope ("Remove Target Grouping Settings", node, true)) {
                        if (enabled) {
                            m_groupNameFormat [inspector.CurrentEditingGroup] = m_groupNameFormat.DefaultValue;
                        } else {
                            m_groupNameFormat.Remove (inspector.CurrentEditingGroup);
                        }
                        onValueChanged ();
                    }
                });

                using (disabledScope) {
                    var newGroupNameFormat = EditorGUILayout.TextField ("Group Name Format", m_groupNameFormat [inspector.CurrentEditingGroup]);
                    EditorGUILayout.HelpBox (
                        "You can customize group name. You can use variable {OldGroup} for old group name and {NewGroup} for current matching name.You can also use {FileName} and {FileExtension}.", 
                        MessageType.Info);

                    if (newGroupNameFormat != m_groupNameFormat [inspector.CurrentEditingGroup]) {
                        using (new RecordUndoScope ("Change Group Name", node, true)) {
                            m_groupNameFormat [inspector.CurrentEditingGroup] = newGroupNameFormat;
                            onValueChanged ();
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
            if(connectionsToOutput == null || Output == null) {
                return;
            }

            var outputDict = new Dictionary<string, List<AssetReference>>();

            if(incoming != null) {
                int i = 0;
                foreach(var ag in incoming) {
                    foreach (var g in ag.assetGroups.Keys) {
                        var assets = ag.assetGroups [g];
                        foreach(var a in assets) {
                            var key = i.ToString ();

                            if (!string.IsNullOrEmpty (m_groupNameFormat [target])) {
                                key = m_groupNameFormat [target]
                                    .Replace ("{FileName}", a.fileName)
                                    .Replace ("{FileExtension}", a.extension)
                                    .Replace ("{NewGroup}", key)
                                    .Replace ("{OldGroup}", g);

                            }

                            outputDict[key] = new List<AssetReference>();
                            outputDict [key].Add (a);
                            ++i;
                        }
                    }
                }
            }

            var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
                null : connectionsToOutput.First();
            Output(dst, outputDict);
        }
	}
}