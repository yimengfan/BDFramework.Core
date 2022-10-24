using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Create Assets/Generate Asset", 51)]
	public class AssetGenerator : Node {

        [System.Serializable]
        public class GeneratorEntry
        {
            public string m_name;
            public string m_id;
            public SerializableMultiTargetInstance m_instance;

            public GeneratorEntry(string name, Model.ConnectionPointData point) {
                m_name = name;
                m_id = point.Id;
                m_instance = new SerializableMultiTargetInstance();
            }

            public GeneratorEntry(string name, SerializableMultiTargetInstance i, Model.ConnectionPointData point) {
                m_name = name;
                m_id = point.Id;
                m_instance = new SerializableMultiTargetInstance(i);
            }
            
            public IAssetGenerator GetGenerator(BuildTarget target)
            {
                return m_instance.Get<IAssetGenerator>(target);
            }
        }
            
        public enum OutputOption : int {
            CreateInCacheDirectory,
            CreateInSelectedDirectory,
            RelativeToSourceAsset
        }

        [SerializeField] private List<GeneratorEntry> m_entries;
        [SerializeField] private string m_defaultOutputPointId;
        [SerializeField] private SerializableMultiTargetString m_outputDir;
        [SerializeField] private SerializableMultiTargetInt m_outputOption;

        private GeneratorEntry m_removingEntry;
	    private Texture2D m_popupIcon;

        public static readonly string kCacheDirName = "GeneratedAssets";

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

	    public IEnumerable<GeneratorEntry> Generators
	    {
	        get { return m_entries; }
	    }

		public override void Initialize(Model.NodeData data) {
            m_entries = new List<GeneratorEntry>();

            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.CreateInCacheDirectory);

            data.AddDefaultInputPoint();
            var point = data.AddDefaultOutputPoint();
            m_defaultOutputPointId = point.Id;
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new AssetGenerator();
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            var point = newData.AddDefaultOutputPoint();
            newNode.m_defaultOutputPointId = point.Id;

            newNode.m_outputDir = new SerializableMultiTargetString(m_outputDir);
            newNode.m_outputOption = new SerializableMultiTargetInt(m_outputOption);

            newNode.m_entries = new List<GeneratorEntry>();
            foreach(var s in m_entries) {
                newNode.AddEntryForClone (newData, s);
            }

			return newNode;
		}

        private void DrawGeneratorSetting(
            GeneratorEntry entry, 
            NodeGUI node, 
            AssetReferenceStreamManager streamManager, 
            NodeGUIInspector inspector, 
            Action onValueChanged) 
        {
            var generator = entry.m_instance.Get<IAssetGenerator>(inspector.CurrentEditingGroup);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                using (new GUILayout.HorizontalScope())
                {
                    var newName = EditorGUILayout.TextField ("Name", entry.m_name);
                    if (newName != entry.m_name) {
                        using(new RecordUndoScope("Change Name", node, true)) {
                            entry.m_name = newName;
                            UpdateGeneratorEntry (node, entry);
                            onValueChanged();
                        }
                    }

                    if (GUILayout.Button(m_popupIcon, GUI.skin.label, GUILayout.Width(20f)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Remove Generator"), false, () =>
                        {
                            m_removingEntry = entry;
                        } );
                        menu.ShowAsContext();
                    }
                }

                var map = AssetGeneratorUtility.GetAttributeAssemblyQualifiedNameMap();
                if(map.Count > 0) {
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label("AssetGenerator");
                        var guiName = AssetGeneratorUtility.GetGUIName(entry.m_instance.ClassName);

                        if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
                            var builders = map.Keys.ToList();

                            if(builders.Count > 0) {
                                NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
                                    {
                                        using(new RecordUndoScope("Change AssetGenerator class", node, true)) {
                                            generator = AssetGeneratorUtility.CreateGenerator(selectedGUIName);
                                            entry.m_instance.Set(inspector.CurrentEditingGroup, generator);
                                            onValueChanged();
                                        }
                                    } 
                                );
                            }
                        }

                        MonoScript s = TypeUtility.LoadMonoScript(entry.m_instance.ClassName);

                        using(new EditorGUI.DisabledScope(s == null)) {
                            if(GUILayout.Button("Edit", GUILayout.Width(50))) {
                                AssetDatabase.OpenAsset(s, 0);
                            }
                        }
                    }
                } else {
                    if(!string.IsNullOrEmpty(entry.m_instance.ClassName)) {
                        EditorGUILayout.HelpBox(
                            $"Your AssetGenerator script {entry.m_instance.ClassName} is missing from assembly. Did you delete script?", MessageType.Info);
                    } else {
                        string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_ASSETGENERATOR.Split('/');
                        EditorGUILayout.HelpBox(
                            $"You need to create at least one AssetGenerator script to use this node. To start, select {menuNames[1]}>{menuNames[2]}>{menuNames[3]} menu and create new script from template.", MessageType.Info);
                    }
                }

                GUILayout.Space(10f);

                inspector.DrawPlatformSelector(node);
                using (new EditorGUILayout.VerticalScope()) {
                    var disabledScope = inspector.DrawOverrideTargetToggle(node, entry.m_instance.ContainsValueOf(inspector.CurrentEditingGroup), (bool enabled) => {
                        if(enabled) {
                            entry.m_instance.CopyDefaultValueTo(inspector.CurrentEditingGroup);
                        } else {
                            entry.m_instance.Remove(inspector.CurrentEditingGroup);
                        }
                        onValueChanged();
                    });

                    using (disabledScope) {
                        if (generator != null) {
                            Action onChangedAction = () => {
                                using(new RecordUndoScope("Change AssetGenerator Setting", node)) {
                                    entry.m_instance.Set(inspector.CurrentEditingGroup, generator);
                                    onValueChanged();
                                }
                            };

                            generator.OnInspectorGUI(onChangedAction);
                        }
                    }
                }
            }
        }

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			EditorGUILayout.HelpBox("Generate Asset: Generate new asset from incoming asset.", MessageType.Info);
			inspector.UpdateNodeName(node);

            GUILayout.Space(8f);

		    if (m_popupIcon == null)
		    {
		        m_popupIcon = EditorGUIUtility.Load (EditorGUIUtility.isProSkin ? "icons/d__Popup.png" : "icons/_Popup.png") as Texture2D;
		    }		    

            inspector.DrawPlatformSelector(node);
            using (new EditorGUILayout.VerticalScope()) {
                var disabledScope = inspector.DrawOverrideTargetToggle(node, m_outputOption.ContainsValueOf(inspector.CurrentEditingGroup), (bool enabled) => {
                    if(enabled) {
                        m_outputOption[inspector.CurrentEditingGroup] = m_outputOption.DefaultValue;
                        m_outputDir[inspector.CurrentEditingGroup] = m_outputDir.DefaultValue;
                    } else {
                        m_outputOption.Remove(inspector.CurrentEditingGroup);
                        m_outputDir.Remove(inspector.CurrentEditingGroup);
                    }
                    onValueChanged();
                });

                using (disabledScope) {
                    OutputOption opt = (OutputOption)m_outputOption[inspector.CurrentEditingGroup];
                    var newOption = (OutputOption)EditorGUILayout.EnumPopup("Output Option", opt);
                    if(newOption != opt) {
                        using(new RecordUndoScope("Change Output Option", node, true)){
                            m_outputOption[inspector.CurrentEditingGroup] = (int)newOption;
                            onValueChanged();
                        }
                        opt = newOption;
                    }
                    if (opt != OutputOption.CreateInCacheDirectory) {
                        EditorGUILayout.HelpBox ("When you are not creating assets under cache directory, make sure your generators are not overwriting assets each other.", MessageType.Info);
                    }

                    using (new EditorGUI.DisabledScope (opt == OutputOption.CreateInCacheDirectory)) {
                        var newDirPath = m_outputDir[inspector.CurrentEditingGroup];

                        if (opt == OutputOption.CreateInSelectedDirectory) {
                            newDirPath = inspector.DrawFolderSelector ("Output Directory", "Select Output Folder", 
                                m_outputDir [inspector.CurrentEditingGroup],
                                Application.dataPath,
                                (string folderSelected) => {
                                    string basePath = Application.dataPath;

                                    if (basePath == folderSelected) {
                                        folderSelected = string.Empty;
                                    } else {
                                        var index = folderSelected.IndexOf (basePath);
                                        if (index >= 0) {
                                            folderSelected = folderSelected.Substring (basePath.Length + index);
                                            if (folderSelected.IndexOf ('/') == 0) {
                                                folderSelected = folderSelected.Substring (1);
                                            }
                                        }
                                    }
                                    return folderSelected;
                                }
                            );
                        } else if (opt == OutputOption.RelativeToSourceAsset) {
                            newDirPath = EditorGUILayout.TextField("Relative Path", m_outputDir[inspector.CurrentEditingGroup]);
                        }

                        if (newDirPath != m_outputDir[inspector.CurrentEditingGroup]) {
                            using(new RecordUndoScope("Change Output Directory", node, true)){
                                m_outputDir[inspector.CurrentEditingGroup] = newDirPath;
                                onValueChanged();
                            }
                        }

                        var dirPath = Path.Combine (Application.dataPath, m_outputDir [inspector.CurrentEditingGroup]);

                        if (opt == OutputOption.CreateInSelectedDirectory && 
                            !string.IsNullOrEmpty(m_outputDir [inspector.CurrentEditingGroup]) &&
                            !Directory.Exists (dirPath)) 
                        {
                            using (new EditorGUILayout.HorizontalScope()) {
                                EditorGUILayout.LabelField(m_outputDir[inspector.CurrentEditingGroup] + " does not exist.");
                                if(GUILayout.Button("Create directory")) {
                                    Directory.CreateDirectory(dirPath);
                                    AssetDatabase.Refresh ();
                                }
                            }
                            EditorGUILayout.Space();

                            string parentDir = Path.GetDirectoryName(m_outputDir[inspector.CurrentEditingGroup]);
                            if(Directory.Exists(parentDir)) {
                                EditorGUILayout.LabelField("Available Directories:");
                                string[] dirs = Directory.GetDirectories(parentDir);
                                foreach(string s in dirs) {
                                    EditorGUILayout.LabelField(s);
                                }
                            }
                            EditorGUILayout.Space();
                        }

                        if (opt == OutputOption.CreateInSelectedDirectory || opt == OutputOption.CreateInCacheDirectory) {
                            var outputDir = PrepareOutputDirectory (BuildTargetUtility.GroupToTarget(inspector.CurrentEditingGroup), node.Data, null);

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
                    }
                }
            }

            GUILayout.Space(8f);

            foreach (var s in m_entries) {
                DrawGeneratorSetting (s, node, streamManager, inspector, onValueChanged);
                GUILayout.Space (10f);
            }

            if (m_removingEntry != null) {
                using (new RecordUndoScope ("Remove Generator", node)) {
                    RemoveGeneratorEntry (node, m_removingEntry);
                    m_removingEntry = null;
                    onValueChanged ();
                }
            }

            GUILayout.Space (8);

            if (GUILayout.Button ("Add Generator")) {
                using (new RecordUndoScope ("Add Generator", node)) {
                    AddEntry (node);
                    onValueChanged ();
                }
            }
		}

		public override void OnContextMenuGUI(GenericMenu menu) {
            foreach (var s in m_entries) {
                MonoScript script = TypeUtility.LoadMonoScript(s.m_instance.ClassName);
                if(script != null) {
                    menu.AddItem(
                        new GUIContent($"Edit Script({script.name})"),
                        false, 
                        () => {
                            AssetDatabase.OpenAsset(script, 0);
                        }
                    );
                }
            }
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{

            ValidateAssetGenerator(node, target, incoming,
                () => {
                    throw new NodeException("AssetGenerator is not specified.", "Select generator from inspector.", node);
                },
                () => {
                    throw new NodeException("Failed to create AssetGenerator from settings.", "Fix AssetGenerator settings from inspector", node);
                },
                (AssetReference badAsset) => {
                    throw new NodeException($"Generator not create asset from source : Source: {badAsset.importFrom}", 
                        "Remove source asset from node input.", node);
                },
                (AssetReference badAsset) => {
                    throw new NodeException($"Can not import incoming asset {badAsset.fileNameAndExtension}.", 
                        "Remove source asset from node input.", node);
                }
            );

			if(incoming == null) {
				return;
			}

            if(connectionsToOutput == null || Output == null) {
                return;
            }

            var allOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();

            foreach(var outPoints in node.OutputPoints) {
                allOutput[outPoints.Id] = new Dictionary<string, List<AssetReference>>();
            }

            var defaultOutputCond = connectionsToOutput.Where (c => c.FromNodeConnectionPointId == m_defaultOutputPointId);
            Model.ConnectionData defaultOutput = null;
            if (defaultOutputCond.Any ()) {
                defaultOutput = defaultOutputCond.First ();
            }

			foreach(var ag in incoming) {
                if (defaultOutput != null) {
                    Output(defaultOutput, ag.assetGroups);
                }
                foreach(var groupKey in ag.assetGroups.Keys) {
                    foreach(var a in ag.assetGroups [groupKey]) {
                        foreach (var entry in m_entries) {
                            var assetOutputDir = PrepareOutputDirectory(target, node, a);
                            var generator = entry.m_instance.Get<IAssetGenerator>(target);
                            UnityEngine.Assertions.Assert.IsNotNull(generator);

                            var newItem = FileUtility.PathCombine (assetOutputDir, GetGeneratorIdForSubPath(target, entry), a.fileName + generator.GetAssetExtension (a));
                            var output = allOutput[entry.m_id];
                            if(!output.ContainsKey(groupKey)) {
                                output[groupKey] = new List<AssetReference>();
                            }
                            output[groupKey].Add(AssetReferenceDatabase.GetReferenceWithType (newItem, generator.GetAssetType(a)));
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

            bool isAnyAssetGenerated = false;

            foreach (var entry in m_entries) {
                var generator = entry.m_instance.Get<IAssetGenerator>(target);
                UnityEngine.Assertions.Assert.IsNotNull(generator);

                foreach(var ag in incoming) {
                    foreach(var assets in ag.assetGroups.Values) {
                        foreach (var a in assets) {
                            var assetOutputDir = PrepareOutputDirectory (target, node, a);
                            var assetSaveDir  = FileUtility.PathCombine (assetOutputDir, GetGeneratorIdForSubPath(target, entry));
                            var assetSavePath = FileUtility.PathCombine (assetSaveDir, a.fileName + generator.GetAssetExtension(a));

                            if(!File.Exists(assetSavePath) || AssetGenerateInfo.DoesAssetNeedRegenerate(entry, node, target, a)) 
                            {
                                if (!Directory.Exists (assetSaveDir)) {
                                    Directory.CreateDirectory (assetSaveDir);
                                }

                                if (!generator.GenerateAsset (a, assetSavePath)) {
                                    throw new AssetGraphException(
                                        $"{node.Name} :Failed to generate asset for {entry.m_name}");
                                }
                                if (!File.Exists (assetSavePath)) {
                                    throw new AssetGraphException(
                                        $"{node.Name} :{entry.m_name} returned success, but generated asset not found.");
                                }

                                AssetProcessEventRecord.GetRecord ().LogModify (AssetDatabase.AssetPathToGUID(assetSavePath));

                                isAnyAssetGenerated = true;

                                LogUtility.Logger.LogFormat(LogType.Log, "{0} is (re)generating Asset:{1} with {2}({3})", node.Name, assetSavePath,
                                    AssetGeneratorUtility.GetGUIName(entry.m_instance.ClassName),
                                    AssetGeneratorUtility.GetVersion(entry.m_instance.ClassName));

                                if(progressFunc != null) progressFunc(node, $"Creating {assetSavePath}", 0.5f);

                                AssetGenerateInfo.SaveAssetGenerateInfo(entry, node, target, a);
                            }
                        }
                    }
                }
            }

            if (isAnyAssetGenerated) {
                AssetDatabase.Refresh ();
            }
		}

        public void AddEntry(NodeGUI node) {
            var point = node.Data.AddOutputPoint("");
            var newEntry = new GeneratorEntry("", point);
            m_entries.Add(newEntry);
            UpdateGeneratorEntry(node, newEntry);
        }

        // For Clone
        public void AddEntryForClone(Model.NodeData data, GeneratorEntry src) {
            var point = data.AddOutputPoint(src.m_name);
            var newEntry = new GeneratorEntry(src.m_name, src.m_instance, point);
            m_entries.Add(newEntry);
            UpdateGeneratorEntry(null, data, newEntry);
        }

        public void RemoveGeneratorEntry(NodeGUI node, GeneratorEntry e) {
            m_entries.Remove(e);
            var point = GetConnectionPoint (node.Data, e);
            node.Data.OutputPoints.Remove(point);
            // event must raise to remove connection associated with point
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, point));
        }

        public Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, GeneratorEntry e) {
            Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == e.m_id);
            UnityEngine.Assertions.Assert.IsNotNull(p);
            return p;
        }

        public void UpdateGeneratorEntry(NodeGUI node, GeneratorEntry e) {
            UpdateGeneratorEntry (node, node.Data, e);
        }

        public void UpdateGeneratorEntry(NodeGUI node, Model.NodeData data, GeneratorEntry e) {
            Model.ConnectionPointData p = node.Data.OutputPoints.Find(v => v.Id == e.m_id);
            UnityEngine.Assertions.Assert.IsNotNull(p);
            p.Label = e.m_name;

            if (node != null) {
                // event must raise to propagate change to connection associated with point
                NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, node, Vector2.zero, GetConnectionPoint(node.Data, e)));
            }
        }

        private string GetGeneratorIdForSubPath(BuildTarget target, GeneratorEntry e) {
            var outputOption = (OutputOption)m_outputOption [target];
            if(outputOption == OutputOption.CreateInCacheDirectory) {
                return e.m_id;
            }
            return string.Empty;
        }

        private string PrepareOutputDirectory(BuildTarget target, Model.NodeData node, AssetReference a) {

            var outputOption = (OutputOption)m_outputOption [target];

            if (outputOption == OutputOption.CreateInSelectedDirectory) {
                return Path.Combine("Assets", m_outputDir [target]);
            }

            if(outputOption == OutputOption.CreateInCacheDirectory) {
                return FileUtility.EnsureCacheDirExists (target, node, kCacheDirName);
            }

            var sourceDir = Path.GetDirectoryName (a.importFrom);

            return FileUtility.PathCombine (sourceDir, m_outputDir [target]);
        }

		public void ValidateAssetGenerator (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
            Action noGeneratorData,
			Action failedToCreateGenerator,
            Action<AssetReference> canNotGenerateAsset,
			Action<AssetReference> canNotImportAsset
		) {
            foreach (var entry in m_entries) {

                var generator = entry.m_instance.Get<IAssetGenerator>(target);

                if(null == generator ) {
                    failedToCreateGenerator();
                }

                generator.OnValidate ();

                if(null != generator && null != incoming) {
                    foreach(var ag in incoming) {
                        foreach(var assets in ag.assetGroups.Values) {
                            foreach (var a in assets) {
                                if(string.IsNullOrEmpty(a.importFrom)) {
                                    canNotImportAsset(a);
                                    continue;
                                }
                                if(!generator.CanGenerateAsset(a)) {
                                    canNotGenerateAsset(a);
                                }
                            }
                        }
                    }
                }
            }
		}			
	}
}