using UnityEditor;

using System;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [CustomNode("Assert/Error", 80)]
    public class Error : Node {

        [SerializeField] private string m_description;
        [SerializeField] private string m_howtoFix;

    	public override string ActiveStyle {
    		get {
    			return "node 7 on";
    		}
    	}

    	public override string InactiveStyle {
    		get {
    			return "node 7";
    		}
    	}

    	public override string Category {
    		get {
    			return "Assert";
    		}
    	}

    	public override Model.NodeOutputSemantics NodeInputType {
    		get {
                return Model.NodeOutputSemantics.Any;
    		}
    	}

    	public override Model.NodeOutputSemantics NodeOutputType {
    		get {
                return Model.NodeOutputSemantics.None;
    		}
    	}

    	public override void Initialize(Model.NodeData data) {
            m_description = "Error occured.";
    		data.AddDefaultInputPoint();
    	}

    	public override Node Clone(Model.NodeData newData) {
            var newNode = new Error();
            newNode.m_description = this.m_description;
    		newData.AddDefaultInputPoint();
    		return newNode;
    	}

    	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

    		EditorGUILayout.HelpBox("Error: Raise error if there is any input asset.", MessageType.Info);
    		inspector.UpdateNodeName(node);

    		GUILayout.Space(10f);

            EditorGUILayout.LabelField ("Description");

            GUIStyle textAreaStyle = new GUIStyle (EditorStyles.textArea);
            textAreaStyle.wordWrap = true;

            var newDesc = EditorGUILayout.TextArea(m_description, textAreaStyle, GUILayout.MaxHeight(100f));
            if(newDesc != m_description) {
    			using(new RecordUndoScope("Change Description", node, true)) {
                    m_description = newDesc;
    				onValueChanged();
    			}
    		}

            GUILayout.Space (4);

            EditorGUILayout.LabelField ("How to fix this error");
            var newHowtoFix = EditorGUILayout.TextArea(m_howtoFix, textAreaStyle, GUILayout.MaxHeight(100f));
            if(newHowtoFix != m_howtoFix) {
                using(new RecordUndoScope("Change HowtoFix", node, true)) {
                    m_howtoFix = newHowtoFix;
                    onValueChanged();
                }
            }
    	}

    	/**
    	 * Prepare is called whenever graph needs update. 
    	 */ 
        public override void Prepare (
            BuildTarget target, 
    		Model.NodeData node, 
    		IEnumerable<PerformGraph.AssetGroups> incoming, 
    		IEnumerable<Model.ConnectionData> connectionsToOutput, 
    		PerformGraph.Output Output) 
    	{
            if(string.IsNullOrEmpty(m_description)) {
                throw new NodeException(node.Name + ":Description is empty.", "Write description for this error.", node);
            }

            if(string.IsNullOrEmpty(m_howtoFix)) {
                throw new NodeException(node.Name + ":HowToFix is empty.", "Write How To Fix this error.", node);
            }

            if(incoming != null) {
                foreach(var ag in incoming) {
                    foreach (var assets in ag.assetGroups.Values) {
                        if (assets.Count > 0) {
                            throw new NodeException(m_description, m_howtoFix, node, assets[0]);
                        }
                    }
                }
            }
    	}
    }
}