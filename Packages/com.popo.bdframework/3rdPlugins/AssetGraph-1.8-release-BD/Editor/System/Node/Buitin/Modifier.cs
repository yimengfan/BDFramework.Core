using UnityEditor;
using UnityEditor.SceneManagement;

using System;
using System.Linq;
using System.Collections.Generic;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Modify Assets/Modify Assets", 61)]
	public class Modifier : Node, Model.NodeDataImporter {

		[SerializeField] private SerializableMultiTargetInstance m_instance;
        [SerializeField] private string m_modifierType;

		public override string ActiveStyle {
			get {
				return "node 8 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 8";
			}
		}

		public override string Category {
			get {
				return "Modify";
			}
		}
		
		public IModifier GetModifier(BuildTarget target)
		{
			return m_instance.Get<IModifier>(target);
		}

		public override void Initialize(Model.NodeData data) {
			m_instance = new SerializableMultiTargetInstance();
            m_modifierType = string.Empty;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_instance = new SerializableMultiTargetInstance(v1.ScriptClassName, v1.InstanceData);
            m_modifierType = string.Empty;
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Modifier();
			newNode.m_instance = new SerializableMultiTargetInstance(m_instance);
            newNode.m_modifierType = m_modifierType;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			EditorGUILayout.HelpBox("Modify Assets Directly: Modify assets.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                if (string.IsNullOrEmpty (m_modifierType)) {
                    EditorGUILayout.HelpBox ("Select asset type to modify with this node.", MessageType.Info);
                    using (new EditorGUILayout.HorizontalScope ()) {
                        EditorGUILayout.LabelField ("Asset Type");
                        if (GUILayout.Button ("", "Popup", GUILayout.MinWidth (150f))) {

                            var menu = new GenericMenu ();

                            var types = ModifierUtility.GetModifyableTypes ().ToArray();

                            for (var i = 0; i < types.Length; i++) {
                                var index = i;
                                menu.AddItem (
                                    new GUIContent (types [i].Name),
                                    false,
                                    () => {
                                        ResetConfig();
                                        m_modifierType = types[index].AssemblyQualifiedName;
                                    }
                                );
                            }
                            menu.ShowAsContext ();
                        }
                    }
                    return;
                }

                var targetType = Type.GetType (m_modifierType);
				var modifier = m_instance.Get<IModifier>(inspector.CurrentEditingGroup);

				Dictionary<string, string> map = null;

                if(targetType != null) {
                    EditorGUILayout.LabelField ("Asset Type", targetType.Name);

                    map = ModifierUtility.GetAttributeAssemblyQualifiedNameMap(targetType);
				}

				if(map != null  && map.Count > 0) {
					using(new GUILayout.HorizontalScope()) {
						GUILayout.Label("Modifier");
                        var guiName = ModifierUtility.GetModifierGUIName(m_instance.ClassName);
						if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
							var builders = map.Keys.ToList();

							if(builders.Count > 0) {
								NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
									{
										using(new RecordUndoScope("Change Modifier class", node, true)) {
                                            modifier = ModifierUtility.CreateModifier(selectedGUIName, targetType);
											m_instance.Set(inspector.CurrentEditingGroup,modifier);
											onValueChanged();
										}
									}  
								);
							}
						}

						MonoScript s = TypeUtility.LoadMonoScript(m_instance.ClassName);

						using(new EditorGUI.DisabledScope(s == null)) {
							if(GUILayout.Button("Edit", GUILayout.Width(50))) {
								AssetDatabase.OpenAsset(s, 0);
							}
						}
					}

				} else {

					string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_MODIFIER.Split('/');

                    if (targetType == null) {
						EditorGUILayout.HelpBox(
							"You need to create at least one Modifier script to select script for Modifier. " +
							$"To start, select {menuNames[1]}>{menuNames[2]}>{menuNames[3]} menu and create a new script.", MessageType.Info);
					} else {
						EditorGUILayout.HelpBox(
							string.Format(
								"No CustomModifier found for {3} type. \n" +
								"You need to create at least one Modifier script to select script for Modifier. " +
								"To start, select {0}>{1}>{2} menu and create a new script.",
                                menuNames[1],menuNames[2], menuNames[3], targetType.FullName
							), MessageType.Info);
					}
				}

				GUILayout.Space(10f);

				inspector.DrawPlatformSelector(node);
				using (new EditorGUILayout.VerticalScope()) {
					var disabledScope = inspector.DrawOverrideTargetToggle(node, m_instance.ContainsValueOf(inspector.CurrentEditingGroup), (bool enabled) => {
						if(enabled) {
							m_instance.CopyDefaultValueTo(inspector.CurrentEditingGroup);
						} else {
							m_instance.Remove(inspector.CurrentEditingGroup);
						}
						onValueChanged();
					});

					using (disabledScope) {
						if (modifier != null) {
							Action onChangedAction = () => {
								using(new RecordUndoScope("Change Modifier Setting", node)) {
									m_instance.Set(inspector.CurrentEditingGroup, modifier);
									onValueChanged();
								}
							};

							modifier.OnInspectorGUI(onChangedAction);
						}
					}
				}

                GUILayout.Space (40f);
                using (new EditorGUILayout.HorizontalScope (GUI.skin.box)) {
                    GUILayout.Space (4f);
                    EditorGUILayout.LabelField ("Reset Modifier Setting");

                    if (GUILayout.Button ("Clear")) {
                        if (EditorUtility.DisplayDialog ("Clear Modifier Setting",
	                        $"Do you want to reset modifier for \"{node.Name}\"?", "OK", "Cancel")) 
                        {
                            using (new RecordUndoScope ("Clear Modifier Setting", node)) {
                                ResetConfig ();
                            }
                        }
                    }
                }
			}
		}
			
		public override void OnContextMenuGUI(GenericMenu menu) {
			MonoScript s = TypeUtility.LoadMonoScript(m_instance.ClassName);
			if(s != null) {
				menu.AddItem(
					new GUIContent("Edit Script"),
					false, 
					() => {
						AssetDatabase.OpenAsset(s, 0);
					}
				);
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            var modifier = m_instance.Get<IModifier> (target);
            if (modifier != null && string.IsNullOrEmpty (m_modifierType)) {
                m_modifierType = ModifierUtility.GetModifierTargetType (m_instance.ClassName).AssemblyQualifiedName;
            }

            ValidateModifier (node, target, incoming);

			if(incoming != null && Output != null) {
				// Modifier does not add, filter or change structure of group, so just pass given group of assets
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				foreach(var ag in incoming) {
					Output(dst, ag.assetGroups);
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
			if(incoming == null) {
				return;
			}
			var modifier = m_instance.Get<IModifier>(target);
			UnityEngine.Assertions.Assert.IsNotNull(modifier);
            Type targetType = ModifierUtility.GetModifierTargetType (m_instance.ClassName);
			bool isAnyAssetModified = false;

            var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
            foreach(var ag in incoming) {
                foreach(var name in ag.assetGroups.Keys) {
                    if(!aggregatedGroups.ContainsKey(name)) {
                        aggregatedGroups[name] = new List<AssetReference>();
                    }
                    aggregatedGroups[name].AddRange(ag.assetGroups[name].AsEnumerable());
                }
            }

            foreach(var assets in aggregatedGroups.Values) {
				foreach(var asset in assets) {
                    if (asset.assetType == targetType) {
                        if(modifier.IsModified(asset.allData, assets)) {
                            modifier.Modify(asset.allData, assets);
                            asset.SetDirty();
                            AssetProcessEventRecord.GetRecord ().LogModify (asset);

                            isAnyAssetModified = true;

                            // apply asset setting changes to AssetDatabase.
                            if (asset.isSceneAsset) {
                                if (!EditorSceneManager.SaveScene (asset.scene)) {
                                    throw new NodeException ("Failed to save modified scene:" + asset.importFrom, "See console for details.", node);
                                }
                            } else {
                                AssetDatabase.SaveAssets ();
                            }
                        }
                        asset.ReleaseData ();
                    }
				}
			}

			if(isAnyAssetModified) {
				AssetDatabase.Refresh();
			}

			if(incoming != null && Output != null) {
				// Modifier does not add, filter or change structure of group, so just pass given group of assets
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

                Output(dst, aggregatedGroups);
			}
		}

        private void ResetConfig() {
            m_modifierType = string.Empty;
            m_instance = new SerializableMultiTargetInstance ();
        }
			
		public void ValidateModifier (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming) 
        {
            if (string.IsNullOrEmpty (m_modifierType)) {
                throw new NodeException("Modifier asset type not set.", "Select asset type to modify from inspector.", node);
            }
            var modifier = m_instance.Get<IModifier> (target);
            if(modifier == null) {
                throw new NodeException("Failed to create Modifier.", "Select modifier from inspector.", node);
			}
            modifier.OnValidate ();

            Type expected = Type.GetType (m_modifierType);
            Type modifierFor = ModifierUtility.GetModifierTargetType (m_instance.ClassName);

            if (expected != modifierFor) {
                throw new NodeException("Modifier type does not match.", "Reset setting or fix Modifier code.", node);
            }
		}			
	}
}
