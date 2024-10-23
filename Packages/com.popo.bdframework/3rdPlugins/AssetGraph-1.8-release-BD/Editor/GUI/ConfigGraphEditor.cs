using UnityEditor;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomEditor(typeof(Model.ConfigGraph))]
	public class ConfigGraphEditor : Editor {

		private class Styles {
			public const string kEDITBUTTON_LABEL = "Open in Graph Editor";
			public const string kEDITBUTTON_DESCRIPTION = "Opens graph in editor to modify the graph.";
			public static readonly GUIContent kEDITBUTTON = new GUIContent(kEDITBUTTON_LABEL, kEDITBUTTON_DESCRIPTION);
		}

		public override void OnInspectorGUI()
		{
			Model.ConfigGraph graph = target as Model.ConfigGraph;

			using(new EditorGUILayout.HorizontalScope()) {
				GUILayout.Label(graph.name, "BoldLabel");
				if (GUILayout.Button(Styles.kEDITBUTTON, GUILayout.Width(150f), GUILayout.ExpandWidth(false)))
				{
					// Get the target we are inspecting and open the graph
					var window = EditorWindow.GetWindow<AssetGraphEditorWindow>();
					window.OpenGraph(graph);
				}
			}

			using(new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Version", graph.Version.ToString());
				EditorGUILayout.LabelField("Last Modified", graph.LastModified.ToString());
                GUILayout.Space (10f);

                var newOrder = EditorGUILayout.IntField ("Execution Order", graph.ExecuteOrderPriority);
                if (newOrder != graph.ExecuteOrderPriority) {
                    graph.ExecuteOrderPriority = newOrder;
                }

				using(new EditorGUILayout.HorizontalScope()) {
                    GUIStyle wrapText = new GUIStyle (EditorStyles.textArea);
                    wrapText.wordWrap = true;

					GUILayout.Label("Description", GUILayout.Width(100f));
                    string newdesc = EditorGUILayout.TextArea(graph.Descrption, wrapText);
					if(newdesc != graph.Descrption) {
						graph.Descrption = newdesc;
					}
				}
				GUILayout.Space(2f);
			}
		}
	}
}
	