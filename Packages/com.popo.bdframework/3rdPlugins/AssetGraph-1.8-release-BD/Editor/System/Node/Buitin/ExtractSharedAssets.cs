using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [CustomNode("Configure Bundle/Extract Shared Assets", 71)]
    public class ExtractSharedAssets : Node {

        enum GroupingType : int {
            ByFileSize,
            ByRuntimeMemorySize
        };

        [SerializeField] private string m_bundleNameTemplate;
        [SerializeField] private SerializableMultiTargetInt m_groupExtractedAssets;
        [SerializeField] private SerializableMultiTargetInt m_groupSizeByte;
        [SerializeField] private SerializableMultiTargetInt m_groupingType;

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
    			return "Configure";
    		}
    	}

    	public override Model.NodeOutputSemantics NodeInputType {
    		get {
    			return Model.NodeOutputSemantics.AssetBundleConfigurations;
    		}
    	}

    	public override Model.NodeOutputSemantics NodeOutputType {
    		get {
    			return Model.NodeOutputSemantics.AssetBundleConfigurations;
    		}
    	}

    	public override void Initialize(Model.NodeData data) {
    		m_bundleNameTemplate = "shared_*";
            m_groupExtractedAssets = new SerializableMultiTargetInt();
            m_groupSizeByte = new SerializableMultiTargetInt();
            m_groupingType = new SerializableMultiTargetInt();
    		data.AddDefaultInputPoint();
    		data.AddDefaultOutputPoint();
    	}

    	public override Node Clone(Model.NodeData newData) {
    		var newNode = new ExtractSharedAssets();
            newNode.m_groupExtractedAssets = new SerializableMultiTargetInt(m_groupExtractedAssets);
            newNode.m_groupSizeByte = new SerializableMultiTargetInt(m_groupSizeByte);
            newNode.m_groupingType = new SerializableMultiTargetInt(m_groupingType);
    		newNode.m_bundleNameTemplate = m_bundleNameTemplate;
    		newData.AddDefaultInputPoint();
    		newData.AddDefaultOutputPoint();
    		return newNode;
    	}

    	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

    		EditorGUILayout.HelpBox("Extract Shared Assets: Extract shared assets between asset bundles and add bundle configurations.", MessageType.Info);
    		inspector.UpdateNodeName(node);

    		GUILayout.Space(10f);

    		var newValue = EditorGUILayout.TextField("Bundle Name Template", m_bundleNameTemplate);
    		if(newValue != m_bundleNameTemplate) {
    			using(new RecordUndoScope("Bundle Name Template Change", node, true)) {
    				m_bundleNameTemplate = newValue;
    				onValueChanged();
    			}
    		}

            GUILayout.Space(10f);

            //Show target configuration tab
            inspector.DrawPlatformSelector(node);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                var disabledScope = inspector.DrawOverrideTargetToggle(node, m_groupSizeByte.ContainsValueOf(inspector.CurrentEditingGroup), (bool enabled) => {
                    using(new RecordUndoScope("Remove Target Grouping Size Settings", node, true)){
                        if(enabled) {
                            m_groupExtractedAssets[inspector.CurrentEditingGroup] = m_groupExtractedAssets.DefaultValue;
                            m_groupSizeByte[inspector.CurrentEditingGroup] = m_groupSizeByte.DefaultValue;
                            m_groupingType[inspector.CurrentEditingGroup] = m_groupingType.DefaultValue;
                        } else {
                            m_groupExtractedAssets.Remove(inspector.CurrentEditingGroup);
                            m_groupSizeByte.Remove(inspector.CurrentEditingGroup);
                            m_groupingType.Remove(inspector.CurrentEditingGroup);
                        }
                        onValueChanged();
                    }
                });

                using (disabledScope) {
                    var useGroup = EditorGUILayout.ToggleLeft ("Subgroup shared assets by size", m_groupExtractedAssets [inspector.CurrentEditingGroup] != 0);
                    if (useGroup != (m_groupExtractedAssets [inspector.CurrentEditingGroup] != 0)) {
                        using(new RecordUndoScope("Change Grouping Type", node, true)){
                            m_groupExtractedAssets[inspector.CurrentEditingGroup] = (useGroup)? 1:0;
                            onValueChanged();
                        }
                    }

                    using (new EditorGUI.DisabledScope (!useGroup)) {
                        var newType = (GroupingType)EditorGUILayout.EnumPopup("Grouping Type",(GroupingType)m_groupingType[inspector.CurrentEditingGroup]);
                        if (newType != (GroupingType)m_groupingType[inspector.CurrentEditingGroup]) {
                            using(new RecordUndoScope("Change Grouping Type", node, true)){
                                m_groupingType[inspector.CurrentEditingGroup] = (int)newType;
                                onValueChanged();
                            }
                        }

                        var newSizeText = EditorGUILayout.TextField("Size(KB)",m_groupSizeByte[inspector.CurrentEditingGroup].ToString());
                        int newSize = 0;
                        Int32.TryParse (newSizeText, out newSize);

                        if (newSize != m_groupSizeByte[inspector.CurrentEditingGroup]) {
                            using(new RecordUndoScope("Change Grouping Size", node, true)){
                                m_groupSizeByte[inspector.CurrentEditingGroup] = newSize;
                                onValueChanged();
                            }
                        }
                    }
                }
            }

    		EditorGUILayout.HelpBox("Bundle Name Template replaces \'*\' with number.", MessageType.Info);
    	}

    	/**
    	 * Prepare is called whenever graph needs update. 
    	 */ 
    	public override void Prepare (BuildTarget target, 
    		Model.NodeData node, 
    		IEnumerable<PerformGraph.AssetGroups> incoming, 
    		IEnumerable<Model.ConnectionData> connectionsToOutput, 
    		PerformGraph.Output Output) 
    	{
    		if(string.IsNullOrEmpty(m_bundleNameTemplate)) {
    			throw new NodeException("Bundle Name Template is empty.", "Set valid bundle name template.",node);
    		}
            if (m_groupExtractedAssets [target] != 0) {
                if(m_groupSizeByte[target] < 0) {
                    throw new NodeException("Invalid size. Size property must be a positive number.", "Set valid size.", node);
                }
            }

    		// Pass incoming assets straight to Output
    		if(Output != null) {
    			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())? 
    				null : connectionsToOutput.First();

    			if(incoming != null) {

                    var buildMap = AssetBundleBuildMap.GetBuildMap ();
                    buildMap.ClearFromId (node.Id);

                    var dependencyCollector = new Dictionary<string, List<string>>(); // [asset path:group name]
    				var sharedDependency = new Dictionary<string, List<AssetReference>>();
    				var groupNameMap = new Dictionary<string, string>();

    				// build dependency map
    				foreach(var ag in incoming) {
    					foreach (var key in ag.assetGroups.Keys) {
    						var assets = ag.assetGroups[key];

    						foreach(var a in assets) {
    							CollectDependencies(key, a, ref dependencyCollector);
    						}
    					}
    				}

    				foreach(var entry in dependencyCollector) {
    					if(entry.Value != null && entry.Value.Count > 1) {
    						var joinedName = string.Join("-", entry.Value.ToArray());
    						if(!groupNameMap.ContainsKey(joinedName)) {
    							var count = groupNameMap.Count;
    							var newName = m_bundleNameTemplate.Replace("*", count.ToString());
    							if(newName == m_bundleNameTemplate) {
    								newName = m_bundleNameTemplate + count.ToString();
    							}
    							groupNameMap.Add(joinedName, newName);
    						}
    						var groupName = groupNameMap[joinedName];

    						if(!sharedDependency.ContainsKey(groupName)) {
    							sharedDependency[groupName] = new List<AssetReference>();
    						}
    						sharedDependency[groupName].Add( AssetReference.CreateReference(entry.Key) );
    					}
    				}

    				if(sharedDependency.Keys.Count > 0) {
                        // subgroup shared dependency bundles by size
                        if (m_groupExtractedAssets [target] != 0) {
                            List<string> devidingBundleNames = new List<string> (sharedDependency.Keys);
                            long szGroup = m_groupSizeByte[target] * 1000;

                            foreach(var bundleName in devidingBundleNames) {
                                var assets = sharedDependency[bundleName];
                                int groupCount = 0;
                                long szGroupCount = 0;
                                foreach(var a in assets) {
                                    var subGroupName = $"{bundleName}_{groupCount}";
                                    if (!sharedDependency.ContainsKey(subGroupName)) {
                                        sharedDependency[subGroupName] = new List<AssetReference>();
                                    }
                                    sharedDependency[subGroupName].Add(a);

                                    szGroupCount += GetSizeOfAsset(a, (GroupingType)m_groupingType[target]);
                                    if(szGroupCount >= szGroup) {
                                        szGroupCount = 0;
                                        ++groupCount;
                                    }
                                }
                                sharedDependency.Remove (bundleName);
                            }
                        }

                        foreach(var bundleName in sharedDependency.Keys) {
                            var bundleConfig = buildMap.GetAssetBundleWithNameAndVariant (node.Id, bundleName, string.Empty);
                            bundleConfig.AddAssets (node.Id, sharedDependency[bundleName].Select(a => a.importFrom));
                        }

    					foreach(var ag in incoming) {
    						Output(destination, new Dictionary<string, List<AssetReference>>(ag.assetGroups));
    					}
    					Output(destination, sharedDependency);
    				} else {
    					foreach(var ag in incoming) {
    						Output(destination, ag.assetGroups);
    					}
    				}

    			} else {
    				// Overwrite output with empty Dictionary when no there is incoming asset
    				Output(destination, new Dictionary<string, List<AssetReference>>());
    			}
    		}
    	}

    	private void CollectDependencies(string groupKey, AssetReference asset, ref Dictionary<string, List<string>> collector) {
    		var dependencies = AssetDatabase.GetDependencies(new []{ asset.importFrom });

            var keyName = String.IsNullOrEmpty(asset.variantName)
	            ? groupKey
	            : $"{groupKey}.{asset.variantName}";
            
    		foreach(var d in dependencies) {
                // AssetBundle must not include script asset
                if (TypeUtility.GetMainAssetTypeAtPath (d) == typeof(MonoScript)) {
                    continue;
                }

    			if(!collector.ContainsKey(d)) {
    				collector[d] = new List<string>();
    			}
    			if(!collector[d].Contains(keyName)) {
    				collector[d].Add(keyName);
    				collector[d].Sort();
    			}
    		}
    	}

        private long GetSizeOfAsset(AssetReference a, GroupingType t) {

            long size = 0;

            // You can not read scene and do estimate
			if (a.isSceneAsset) {
                t = GroupingType.ByFileSize;
            }

            if (t == GroupingType.ByRuntimeMemorySize) {
                var objects = a.allData;
                foreach (var o in objects) {
                    size += Profiler.GetRuntimeMemorySizeLong (o);
                }

                a.ReleaseData ();
            } else if (t == GroupingType.ByFileSize) {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(a.absolutePath);
                if (fileInfo.Exists) {
                    size = fileInfo.Length;
                }
            }

            return size;
        }
    }
}