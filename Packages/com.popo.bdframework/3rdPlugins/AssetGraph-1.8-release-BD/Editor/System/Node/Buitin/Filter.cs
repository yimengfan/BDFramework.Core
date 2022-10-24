using UnityEditor;
using UnityEditorInternal;

using System;
using System.Linq;
using System.Collections.Generic;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Split Assets/Split By Filter", 20)]
	public class Filter : Node, Model.NodeDataImporter {

		[System.Serializable]
		public class FilterInstance : SerializedInstance<IFilter> {

			public FilterInstance() : base() {}
			public FilterInstance(FilterInstance instance): base(instance) {}
			public FilterInstance(IFilter obj) : base(obj) {}
		}

		[Serializable]
		public class FilterEntry {
			[SerializeField] private FilterInstance m_instance;
			[SerializeField] private string m_pointId;

			public FilterEntry(IFilter filter, Model.ConnectionPointData point) {
				m_instance = new FilterInstance(filter);
				m_pointId = point.Id;
			}

			public FilterEntry(FilterEntry e) {
				m_instance = new FilterInstance(e.m_instance);
				m_pointId = e.m_pointId;
			}

			public string ConnectionPointId {
				get {
					return m_pointId; 
				}
			}

			public FilterInstance Instance {
				get {
					return m_instance;
				}
				set {
					m_instance = value;
				}
			}

			public string Hash {
				get {
					return m_instance.Data;
				}
			}
		}

		[SerializeField] private List<FilterEntry> m_filter;
        ReorderableList m_filterList;

        NodeGUI m_node;
        Action m_OnValueChanged;

		public override string ActiveStyle {
			get {
				return "node 1 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 1";
			}
		}

		public override string Category {
			get {
				return "Filter";
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_filter = new List<FilterEntry>();

			data.AddDefaultInputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {

			foreach(var f in v1.FilterConditions) {
				m_filter.Add(new FilterEntry(new FilterByNameAndType(f.FilterKeyword, f.FilterKeytype), v2.FindOutputPoint(f.ConnectionPointId)));
			}
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Filter();
			newNode.m_filter = new List<FilterEntry>(m_filter.Count);

			newData.AddDefaultInputPoint();

			foreach(var f in m_filter) {
				newNode.AddFilterCondition(newData, f.Instance.Object);
			}

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

            if (m_filterList == null) {
                m_filterList = new ReorderableList(m_filter, typeof(FilterEntry), true, false, true, true);
                m_filterList.onReorderCallback = ReorderFilterEntryList;
                m_filterList.onAddCallback = AddToFilterEntryList;
                m_filterList.onRemoveCallback = RemoveFromFilterEntryList;
                m_filterList.onCanRemoveCallback = CanRemoveFilterEntry;
                m_filterList.drawElementCallback = DrawFilterEntryListElement;
                m_filterList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
                m_filterList.headerHeight = 3;
            }

            m_node = node;
            m_OnValueChanged = onValueChanged;

			EditorGUILayout.HelpBox("Split By Filter: Split incoming assets by filter conditions.", MessageType.Info);
			inspector.UpdateNodeName(node);

            EditorGUILayout.LabelField ("Filter Conditions");
            m_filterList.DoLayoutList();
            EditorGUILayout.HelpBox("Assets are filtered by filter conditions applied from the top of the list to bottom. You may want to reorder conditions to get desired result.", MessageType.Info);
		}

        void AddToFilterEntryList(ReorderableList list) {
            var map = FilterUtility.GetAttributeAssemblyQualifiedNameMap();
            if(map.Keys.Count > 1) {
                GenericMenu menu = new GenericMenu();
                foreach(var name in map.Keys) {
                    var guiName = name;
                    menu.AddItem(new GUIContent(guiName), false, () => {
                        using(new RecordUndoScope("Add Filter Condition", m_node)){
                            var filter = FilterUtility.CreateFilter(guiName);
                            AddFilterCondition(m_node.Data, filter);
                            m_OnValueChanged();
                        }
                        list.index = m_filter.Count - 1;
                    });
                }
                menu.ShowAsContext();
            } else {
                using(new RecordUndoScope("Add Filter Condition", m_node)){
                    AddFilterCondition(m_node.Data, new FilterByNameAndType());
                    m_OnValueChanged();
                    list.index = m_filter.Count - 1;
                }
            }
        }

        public void ReorderFilterEntryList(ReorderableList list) {
            m_node.Data.OutputPoints.Sort ((Model.ConnectionPointData x, Model.ConnectionPointData y) => {
                int xIndex = m_filter.FindIndex(f => f.ConnectionPointId == x.Id );
                int yIndex = m_filter.FindIndex(f => f.ConnectionPointId == y.Id );

                return xIndex - yIndex;
            });
            m_OnValueChanged ();
            // redo node output due to filter condition change
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_UPDATED, m_node));
        }

        private void RemoveFromFilterEntryList(ReorderableList list) {

            // how is removing taking effect?
            // -> list.index

            var removingItem = m_filter [list.index];

            using(new RecordUndoScope("Remove Filter Condition", m_node, true)){
                // event must raise to remove connection associated with point
                NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, m_node, Vector2.zero, GetConnectionPoint(m_node.Data, removingItem)));
                RemoveFilterCondition(m_node.Data, removingItem);
                m_OnValueChanged();
            }
        }

        private bool CanEditFilterEntry(int index) {
            if (index < 0 || index >= m_filter.Count) {
                return false;
            }
            return true;
        }

        private bool CanRemoveFilterEntry(ReorderableList list)
        {
            return CanEditFilterEntry(list.index);
        }

        private void DrawFilterEntryListElement(Rect rect, int index, bool selected, bool focused)
        {
            bool oldEnabled = GUI.enabled;
            GUI.enabled = CanEditFilterEntry(index);

            var cond = m_filter[index];
            IFilter filter = cond.Instance.Object;
            if(filter == null) {
                using (new GUILayout.VerticalScope()) {
                    EditorGUILayout.HelpBox(
	                    $"Failed to deserialize assigned filter({cond.Instance.ClassName}). Select a valid class.", MessageType.Error);
                    if (GUILayout.Button(cond.Instance.ClassName, "Popup", GUILayout.MinWidth(150f))) {
                        var map = FilterUtility.GetAttributeAssemblyQualifiedNameMap();
                        NodeGUI.ShowTypeNamesMenu(cond.Instance.ClassName, map.Keys.ToList(), (string selectedGUIName) => 
                            {
                                using(new RecordUndoScope("Change Filter Setting", m_node)) {
                                    var newFilter = FilterUtility.CreateFilter(selectedGUIName);
                                    cond.Instance = new FilterInstance(newFilter);
                                    m_OnValueChanged();
                                }
                            }  
                        );
                    }
                }
            } else {
                cond.Instance.Object.OnInspectorGUI(rect, () => {
                    using(new RecordUndoScope("Change Filter Setting", m_node)) {
                        cond.Instance.Save();
                        UpdateFilterEntry(m_node.Data, cond);
                        // event must raise to propagate change to connection associated with point
                        NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, m_node, Vector2.zero, GetConnectionPoint(m_node.Data, cond)));
                        m_OnValueChanged();
                    }
                });
            }


            GUI.enabled = oldEnabled;
        }

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateFilters(node);
			ValidateOverlappingFilterCondition(node, true);
			FilterAssets(node, incoming, connectionsToOutput, Output);
		}

		private void FilterAssets (Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if(connectionsToOutput == null || Output == null) {
				return;
			}

			var allOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();

			foreach(var outPoints in node.OutputPoints) {
				allOutput[outPoints.Id] = new Dictionary<string, List<AssetReference>>();
			}
			if(incoming != null) {
				foreach(var ag in incoming) {
					foreach(var groupKey in ag.assetGroups.Keys) {

						foreach(var a in ag.assetGroups[groupKey]) {
							foreach(var filter in m_filter) {

								if(filter.Instance.Object.FilterAsset(a)) {
									var output = allOutput[filter.ConnectionPointId];
									if(!output.ContainsKey(groupKey)) {
										output[groupKey] = new List<AssetReference>();
									}
									output[groupKey].Add(a);
									// consume this asset with this output
									break;
								}
							}
						}
					}
				}
			}

			foreach(var dst in connectionsToOutput) {
				if(allOutput.ContainsKey(dst.FromNodeConnectionPointId)) {
					Output(dst, allOutput[dst.FromNodeConnectionPointId]);
				}
			}
		}

		public void AddFilterCondition(Model.NodeData n, IFilter filter) {
			var point = n.AddOutputPoint(filter.Label);
			var newEntry = new FilterEntry(filter, point);
			m_filter.Add(newEntry);
			UpdateFilterEntry(n, newEntry);
		}

		public void RemoveFilterCondition(Model.NodeData n, FilterEntry f) {
			m_filter.Remove(f);
			n.OutputPoints.Remove(GetConnectionPoint(n, f));
		}

		public Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, FilterEntry f) {
			Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		public void UpdateFilterEntry(Model.NodeData n, FilterEntry f) {

			Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			p.Label = f.Instance.Object.Label;
		}

		public void ValidateFilters(Model.NodeData n) {

			foreach(var f in m_filter) {
				if(f.Instance.Object == null) {
                    throw new NodeException($"Could not deserialize filter with class {f.Instance.ClassName}.","Fix filter setting from inspector.", n);
				}
			}
		}

		public bool ValidateOverlappingFilterCondition(Model.NodeData n, bool throwException) {

			var conditionGroup = m_filter.Select(v => v).GroupBy(v => v.Hash).ToList();
			var overlap = conditionGroup.Find(v => v.Count() > 1);

			if( overlap != null && throwException ) {
				var element = overlap.First();
				throw new NodeException($"Duplicated filter condition found [Label:{element.Instance.Object.Label}]","Change filter condition and avoid collision.", n);
			}
			return overlap != null;
		}
	}
}