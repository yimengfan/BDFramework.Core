using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Assertions;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Create Assets/Create Prefab From Group", 50)]
	public class PrefabBuilder : Node, Model.NodeDataImporter {

        public enum OutputOption : int {
            CreateInCacheDirectory,
            CreateInSelectedDirectory
        }

		[SerializeField] private SerializableMultiTargetInstance m_instance;
        [SerializeField] private SerializableMultiTargetString m_outputDir;
        [SerializeField] private SerializableMultiTargetInt m_outputOption;
        [SerializeField] private bool m_loadPreviousPrefab;

        private PrefabCreateDescription m_createDescription;
        
        public static readonly string kCacheDirName = "Prefabs";

		public SerializableMultiTargetInstance Builder {
			get {
				return m_instance;
			}
		}

		public override string ActiveStyle {
			get {
				return "node 4 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 4";
			}
		}

		public override string Category {
			get {
				return "Create";
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_instance = new SerializableMultiTargetInstance();
            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.CreateInCacheDirectory);
            m_loadPreviousPrefab = false;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_instance = new SerializableMultiTargetInstance(v1.ScriptClassName, v1.InstanceData);
            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.CreateInCacheDirectory);
            m_loadPreviousPrefab = false;
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new PrefabBuilder();
			newNode.m_instance = new SerializableMultiTargetInstance(m_instance);
            newNode.m_outputDir = new SerializableMultiTargetString(m_outputDir);
            newNode.m_outputOption = new SerializableMultiTargetInt(m_outputOption);
            newNode.m_loadPreviousPrefab = m_loadPreviousPrefab;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public IPrefabBuilder GetPrefabBuilder(BuildTarget target)
		{
			return m_instance.Get<IPrefabBuilder>(target);
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Create Prefab From Group: Create prefab from incoming group of assets, using assigned script.", MessageType.Info);
			editor.UpdateNodeName(node);

			var builder = m_instance.Get<IPrefabBuilder>(editor.CurrentEditingGroup);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				var map = PrefabBuilderUtility.GetAttributeAssemblyQualifiedNameMap();
				if(map.Count > 0) {
					using(new GUILayout.HorizontalScope()) {
						GUILayout.Label("PrefabBuilder");
						var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(m_instance.ClassName);

						if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
							var builders = map.Keys.ToList();

							if(builders.Count > 0) {
								NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
									{
										using(new RecordUndoScope("Change PrefabBuilder class", node, true)) {
											builder = PrefabBuilderUtility.CreatePrefabBuilder(selectedGUIName);
											m_instance.Set(editor.CurrentEditingGroup, builder);
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
					if(!string.IsNullOrEmpty(m_instance.ClassName)) {
						EditorGUILayout.HelpBox(
							$"Your PrefabBuilder script {m_instance.ClassName} is missing from assembly. Did you delete script?", MessageType.Info);
					} else {
						string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_PREFABBUILDER.Split('/');
						EditorGUILayout.HelpBox(
							$"You need to create at least one PrefabBuilder script to use this node. To start, select {menuNames[1]}>{menuNames[2]}>{menuNames[3]} menu and create new script from template.", MessageType.Info);
					}
				}

				GUILayout.Space(10f);

				editor.DrawPlatformSelector(node);
				using (new EditorGUILayout.VerticalScope()) {
					var disabledScope = editor.DrawOverrideTargetToggle(node, m_instance.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
						if(enabled) {
							m_instance.CopyDefaultValueTo(editor.CurrentEditingGroup);
                            m_outputDir[editor.CurrentEditingGroup] = m_outputDir.DefaultValue;
                            m_outputOption[editor.CurrentEditingGroup] = m_outputOption.DefaultValue;
						} else {
							m_instance.Remove(editor.CurrentEditingGroup);
                            m_outputDir.Remove(editor.CurrentEditingGroup);
                            m_outputOption.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					});

					using (disabledScope)
					{
						var newLoadPrevPrefab = EditorGUILayout.ToggleLeft("Load previously created prefab", m_loadPreviousPrefab);
						if (newLoadPrevPrefab != m_loadPreviousPrefab)
						{
							m_loadPreviousPrefab = newLoadPrevPrefab;
							onValueChanged();
						}
						
                        OutputOption opt = (OutputOption)m_outputOption[editor.CurrentEditingGroup];
                        var newOption = (OutputOption)EditorGUILayout.EnumPopup("Output Option", opt);
                        if(newOption != opt) {
                            using(new RecordUndoScope("Change Output Option", node, true)){
                                m_outputOption[editor.CurrentEditingGroup] = (int)newOption;
                                onValueChanged();
                            }
                            opt = newOption;
                        }

                        using (new EditorGUI.DisabledScope (opt == OutputOption.CreateInCacheDirectory)) {
                            var newDirPath = editor.DrawFolderSelector ("Output Directory", "Select Output Folder", 
                                m_outputDir[editor.CurrentEditingGroup],
                                Application.dataPath,
                                (string folderSelected) => {
                                    string basePath = Application.dataPath;

                                    if(basePath == folderSelected) {
                                        folderSelected = string.Empty;
                                    } else {
                                        var index = folderSelected.IndexOf(basePath);
                                        if(index >= 0 ) {
                                            folderSelected = folderSelected.Substring(basePath.Length + index);
                                            if(folderSelected.IndexOf('/') == 0) {
                                                folderSelected = folderSelected.Substring(1);
                                            }
                                        }
                                    }
                                    return folderSelected;
                                }
                            );
                            if (newDirPath != m_outputDir[editor.CurrentEditingGroup]) {
                                using(new RecordUndoScope("Change Output Directory", node, true)){
                                    m_outputDir[editor.CurrentEditingGroup] = newDirPath;
                                    onValueChanged();
                                }
                            }

                            var dirPath = Path.Combine (Application.dataPath, m_outputDir [editor.CurrentEditingGroup]);

                            if (opt == OutputOption.CreateInSelectedDirectory && 
                                !string.IsNullOrEmpty(m_outputDir [editor.CurrentEditingGroup]) &&
                                !Directory.Exists (dirPath)) 
                            {
                                using (new EditorGUILayout.HorizontalScope()) {
                                    EditorGUILayout.LabelField(m_outputDir[editor.CurrentEditingGroup] + " does not exist.");
                                    if(GUILayout.Button("Create directory")) {
                                        Directory.CreateDirectory(dirPath);
                                        AssetDatabase.Refresh ();
                                    }
                                }
                                EditorGUILayout.Space();

                                string parentDir = Path.GetDirectoryName(m_outputDir[editor.CurrentEditingGroup]);
                                if(Directory.Exists(parentDir)) {
                                    EditorGUILayout.LabelField("Available Directories:");
                                    string[] dirs = Directory.GetDirectories(parentDir);
                                    foreach(string s in dirs) {
                                        EditorGUILayout.LabelField(s);
                                    }
                                }
                                EditorGUILayout.Space();
                            }

                            var outputDir = PrepareOutputDirectory (BuildTargetUtility.GroupToTarget(editor.CurrentEditingGroup), node.Data);

                            using (new EditorGUI.DisabledScope (!Directory.Exists (outputDir))) 
                            {
                                using (new EditorGUILayout.HorizontalScope ()) {
                                    GUILayout.FlexibleSpace ();
                                    if (GUILayout.Button ("Highlight in Project Window", GUILayout.Width (180f))) {
                                        var folder = AssetDatabase.LoadMainAssetAtPath (outputDir);
                                        EditorGUIUtility.PingObject (folder);
                                    }
                                }
                            }
                        }

                        GUILayout.Space (8f);

						if (builder != null) {
							Action onChangedAction = () => {
								using(new RecordUndoScope("Change PrefabBuilder Setting", node)) {
									m_instance.Set(editor.CurrentEditingGroup, builder);
									onValueChanged();
								}
							};

							try
							{
								builder.OnInspectorGUI(onChangedAction);
							}
							catch (Exception e)
							{
								throw new NodeException(e.Message, "See reason for detail.", node.Data);
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
			ValidatePrefabBuilder(node, target, incoming,
                () => throw new NodeException ("Output directory not found.", "Create output directory or set a valid directory path.", node),
				() => throw new NodeException("PrefabBuilder is not configured.", "Configure PrefabBuilder from inspector.", node),
				() => throw new NodeException("Failed to create PrefabBuilder from settings.", "Fix settings from inspector.", node),
				(string groupKey) => throw new NodeException(
					$"Can not create prefab with incoming assets for group {groupKey}.", "Fix group input assets for selected PrefabBuilder.",node),
				(AssetReference badAsset) => throw new NodeException(
					$"Can not import incoming asset {badAsset.fileNameAndExtension}.", "", node));

			if(incoming == null) {
				return;
			}

            var prefabOutputDir = PrepareOutputDirectory (target, node);

			var builder = m_instance.Get<IPrefabBuilder>(target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			foreach(var ag in incoming) {
				foreach(var key in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(key)){
						aggregatedGroups[key] = new List<AssetReference>();
					}
					aggregatedGroups[key].AddRange(ag.assetGroups[key].AsEnumerable());
				}
			}

			foreach(var key in aggregatedGroups.Keys) {

				var assets = aggregatedGroups[key];
                var threshold = PrefabBuilderUtility.GetPrefabBuilderAssetThreshold(m_instance.ClassName);
				if( threshold < assets.Count ) {
					var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(m_instance.ClassName);
					throw new NodeException(
                        string.Format("Too many assets passed to {0} for group:{1}. {2}'s threshold is set to {4}", guiName, key, guiName,threshold),
                        string.Format("Limit number of assets in a group to {4}", threshold), node);
				}

				List<UnityEngine.Object> allAssets = LoadAllAssets(assets);
				bool canCreatePrefab;

				if (m_createDescription == null)
				{
					m_createDescription = new PrefabCreateDescription();
				}
				
				try
				{
					m_createDescription.Reset();
					canCreatePrefab = builder.CanCreatePrefab(key, allAssets, ref m_createDescription);
				}
				catch (Exception e)
				{
					throw new NodeException(e.Message, "See reason for detail.", node);
				}
				
				if(output != null && canCreatePrefab) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(FileUtility.PathCombine(prefabOutputDir, m_createDescription.prefabName + ".prefab"))
					};
				}
				UnloadAllAssets(assets);
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		private static List<UnityEngine.Object> LoadAllAssets(List<AssetReference> assets) {
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

			foreach(var a in assets) {
				objects.AddRange(a.allData.AsEnumerable());
			}
			return objects;
		}

		private static void UnloadAllAssets(List<AssetReference> assets) {
			assets.ForEach(a => a.ReleaseData());
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

			var builder = m_instance.Get<IPrefabBuilder>(target);
			Assert.IsNotNull(builder);

            var prefabOutputDir = PrepareOutputDirectory(target, node);
			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			foreach(var ag in incoming) {
				foreach(var key in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(key)){
						aggregatedGroups[key] = new List<AssetReference>();
					}
					aggregatedGroups[key].AddRange(ag.assetGroups[key].AsEnumerable());
				}
			}

            var anyPrefabCreated = false;

			foreach(var key in aggregatedGroups.Keys) {

				var assets = aggregatedGroups[key];

				var allAssets = LoadAllAssets(assets);

				try
				{
					m_createDescription.Reset();
					var canCreatePrefab = builder.CanCreatePrefab(key, allAssets, ref m_createDescription);					
					Assert.IsTrue(canCreatePrefab, "CanCreatePrefab() should not fail at Build phase.");
				}
				catch (Exception e)
				{
					throw new NodeException(e.Message, "See reason for detail.", node);
				}

				var prefabSavePath = FileUtility.PathCombine(prefabOutputDir, m_createDescription.prefabName + ".prefab");

				if (!Directory.Exists(Path.GetDirectoryName(prefabSavePath))) {
					Directory.CreateDirectory(Path.GetDirectoryName(prefabSavePath));
				}

                if(!File.Exists(prefabSavePath) || PrefabBuildInfo.DoesPrefabNeedRebuilding(prefabOutputDir, this, node, target, key, assets, m_createDescription))
                {
	                GameObject obj;
	                GameObject previous = null;
	                
	                try
	                {
		                if (m_loadPreviousPrefab && File.Exists(prefabSavePath))
		                {
			                previous = PrefabUtility.LoadPrefabContents(prefabSavePath);
		                }
		                
		                obj = builder.CreatePrefab(key, allAssets, previous);
	                }
	                catch (Exception e)
	                {
		                throw new NodeException(e.Message, "See reason for detail.", node);
	                }
	                
					if(obj == null) {
						throw new AssetGraphException(
							$"{node.Name} :PrefabBuilder {builder.GetType().FullName} returned null in CreatePrefab() [groupKey:{key}]");
					}

					LogUtility.Logger.LogFormat(LogType.Log, "{0} is (re)creating Prefab:{1} with {2}({3})", node.Name, m_createDescription.prefabName,
						PrefabBuilderUtility.GetPrefabBuilderGUIName(m_instance.ClassName),
						PrefabBuilderUtility.GetPrefabBuilderVersion(m_instance.ClassName));

					progressFunc?.Invoke(node, $"Creating {m_createDescription.prefabName}", 0.5f);

					var isPartOfAsset = PrefabUtility.IsPartOfPrefabAsset(obj);
					
					PrefabUtility.SaveAsPrefabAsset(obj, prefabSavePath);
					
					if (previous != obj && isPartOfAsset)
					{
						PrefabUtility.UnloadPrefabContents(obj);
					}
					else
					{
						Object.DestroyImmediate(obj);
					}
					
					if (previous)
					{
						PrefabUtility.UnloadPrefabContents(previous);
					}                    
                    
                    PrefabBuildInfo.SavePrefabBuildInfo(prefabOutputDir, this, node, target, key, assets, m_createDescription);
                    anyPrefabCreated = true;
                    AssetProcessEventRecord.GetRecord ().LogModify (AssetDatabase.AssetPathToGUID(prefabSavePath));
				}
				UnloadAllAssets(assets);

                if (anyPrefabCreated) {
                    AssetDatabase.SaveAssets ();
                }

				if(output != null) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(prefabSavePath)
					};
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		private void ValidatePrefabBuilder (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
            Action folderDoesntExist,
			Action noBuilderData,
			Action failedToCreateBuilder,
			Action<string> canNotCreatePrefab,
			Action<AssetReference> canNotImportAsset
		) {
            var outputDir = PrepareOutputDirectory (target, node);

            if (!Directory.Exists (outputDir)) {
                folderDoesntExist ();
            }

			var builder = m_instance.Get<IPrefabBuilder>(target);

			if(null == builder ) {
				failedToCreateBuilder();
			}

			if (m_createDescription == null)
			{
				m_createDescription = new PrefabCreateDescription();
			}
			
			try
			{
				builder.OnValidate ();
			}
			catch (Exception e)
			{
				throw new NodeException(e.Message, "See reason for detail.", node);
			}

			if(null != builder && null != incoming) {
				foreach(var ag in incoming) {
					foreach(var key in ag.assetGroups.Keys) {
						var assets = ag.assetGroups[key];
						if(assets.Any()) {
							bool isAllGoodAssets = true;
							foreach(var a in assets) {
								if(string.IsNullOrEmpty(a.importFrom)) {
									canNotImportAsset(a);
									isAllGoodAssets = false;
								}
							}
							if(isAllGoodAssets) {
								// do not call LoadAllAssets() unless all assets have importFrom
								var al = ag.assetGroups[key];
								var allAssets = LoadAllAssets(al);			
								
								try
								{
									m_createDescription.Reset();
									if(!builder.CanCreatePrefab(key, allAssets, ref m_createDescription)) {
										canNotCreatePrefab(key);
									}
								}
								catch (Exception e)
								{
									throw new NodeException(e.Message, "See reason for detail.", node);
								}
								
								UnloadAllAssets(al);
							}
						}
					}
				}
			}
		}	

        private string PrepareOutputDirectory(BuildTarget target, Model.NodeData node) {

            var outputOption = (OutputOption)m_outputOption [target];

            if(outputOption == OutputOption.CreateInCacheDirectory) {
                return FileUtility.EnsureCacheDirExists (target, node, kCacheDirName);
            }

            return Path.Combine("Assets", m_outputDir [target]);
        }
	}
}