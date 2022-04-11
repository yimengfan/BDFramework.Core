using UnityEditor;
using UnityEditorInternal;

using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class ExecutionOrderSettingsTab {

        static readonly int kOrderPriorityLevel = 100;

        internal class Styles
        {
            public static readonly GUIContent defaultExecutionOrder = EditorGUIUtility.TrTextContent("Default Execution Order");
            public static readonly GUIContent executionOrderLabel = EditorGUIUtility.TrTextContent("AssetPostprocessor Graph Execution Order");
        }
        
        private class GraphExecOrder {

            private string m_graphGuid;
            private string m_graphName;
            private Model.ConfigGraph m_graph;

            public GraphExecOrder() {
                m_graphGuid = null;
            }

            public GraphExecOrder(string guid, string name, Model.ConfigGraph graph) {
                m_graphGuid = guid;
                m_graphName = name;
                m_graph = graph;
            }

            public string Name {
                get {
                    if (IsDefault) {
                        return Styles.defaultExecutionOrder.text;
                    }
                    return m_graphName;
                }
            }

            public string Guid {
                get {
                    return m_graphGuid;
                }
            }

            public int ExecuteOrderPriority {
                get {
                    if (IsDefault) {
                        return Model.Settings.GRAPHEXECPRIORITY_DEFAULT;
                    }
                    return m_graph.ExecuteOrderPriority;
                }

                set {
                    if (!IsDefault) {
                        if (m_graph.ExecuteOrderPriority != value) {
                            m_graph.ExecuteOrderPriority = value;
                            EditorUtility.SetDirty (m_graph);
                        }
                    }
                }
            }

            public bool IsDefault {
                get {
                    return string.IsNullOrEmpty (m_graphGuid);
                }
            }
        }

        List<GraphExecOrder> m_orderData;
        ReorderableList m_execOrderList;
        GraphExecOrder m_defaultOrder;

        public ExecutionOrderSettingsTab() {
            m_defaultOrder = new GraphExecOrder ();
            Refresh();
        }

        private void ReloadExecOrderData() {

            var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            m_orderData = new List<GraphExecOrder> ();
            m_orderData.Add (m_defaultOrder); // Default Order

            foreach(var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);

                if (graph.ExecuteOrderPriority != Model.Settings.GRAPHEXECPRIORITY_DEFAULT) {
                    m_orderData.Add (new GraphExecOrder (guid, name, graph));
                }
            }

            m_orderData.Sort ((l, r) => l.ExecuteOrderPriority - r.ExecuteOrderPriority);
        }

        public void Refresh() {

            ReloadExecOrderData ();

            m_execOrderList = new ReorderableList(m_orderData, typeof(GraphExecOrder), true, false, true, true);
            m_execOrderList.onReorderCallback = ReorderExecOrderPriority;
            m_execOrderList.onAddCallback = AddToExecOrderPriorityList;
            m_execOrderList.onRemoveCallback = RemoveFromExecOrderPriorityList;
            m_execOrderList.onCanRemoveCallback = CanRemoveExecOrderPriority;
            m_execOrderList.drawElementCallback = DrawExecOrderPriorityElement;
            m_execOrderList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
            m_execOrderList.headerHeight = 3;
        }

		public void OnGUI () {
            EditorGUILayout.LabelField (Styles.executionOrderLabel);
            m_execOrderList.DoLayoutList();
		}

        void AddToExecOrderPriorityList(ReorderableList list) {

            var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            GenericMenu menu = new GenericMenu();
            foreach(var guid in guids) {
                var thisGuid = guid;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);

                menu.AddItem(new GUIContent(name), false, () => {
                    var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
                    graph.ExecuteOrderPriority = 
                        m_orderData.OrderByDescending(o => o.ExecuteOrderPriority).First().ExecuteOrderPriority + kOrderPriorityLevel;
                    m_orderData.Add(new GraphExecOrder(thisGuid, name, graph));
                });
            }
            menu.ShowAsContext();
        }

        public void ReorderExecOrderPriority(ReorderableList list) {

            var defaultIndex = m_orderData.IndexOf (m_defaultOrder);

            for (int i = 0; i < m_orderData.Count; ++i) {
                m_orderData [i].ExecuteOrderPriority = (i - defaultIndex) * kOrderPriorityLevel;
            }
        }

        private void RemoveFromExecOrderPriorityList(ReorderableList list) {

            var order = m_orderData[list.index];
            order.ExecuteOrderPriority = Model.Settings.GRAPHEXECPRIORITY_DEFAULT;

            m_orderData.RemoveAt (list.index);
        }

        private bool CanEditGraphExecOrder(int index) {
            if (index < 0 || index >= m_orderData.Count) {
                return false;
            }
            if (m_orderData [index].IsDefault) {
                return false;
            }
            return true;
        }

        private bool CanRemoveExecOrderPriority(ReorderableList list)
        {
            return CanEditGraphExecOrder(list.index);
        }

        private void DrawExecOrderPriorityElement(Rect rect, int index, bool selected, bool focused)
        {
            bool oldEnabled = GUI.enabled;
            GUI.enabled = CanEditGraphExecOrder(index);

            var nameRect = new Rect (rect.x, rect.y, rect.width - 100, rect.height);
            var orderField = new Rect (nameRect.xMax, rect.y, 100, rect.height);

            var orderObj = m_orderData [index];

            EditorGUI.LabelField (nameRect, orderObj.Name);
            EditorGUI.LabelField (orderField, orderObj.ExecuteOrderPriority.ToString());

            GUI.enabled = oldEnabled;
        }
	}
}
