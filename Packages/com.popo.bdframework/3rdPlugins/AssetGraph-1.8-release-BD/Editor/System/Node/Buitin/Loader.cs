using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Load Assets/Load From Directory", 10)]
	public class Loader : Node, Model.NodeDataImporter {

        [Serializable]
        private class IgnorePattern
        {
            public enum FileTypeMask : int {
                File = 1,
                Directory = 2
            }
            [SerializeField] FileTypeMask m_fileTypeMask;
            [SerializeField] string m_ignorePattern;

            private Regex m_match;

            public IgnorePattern(FileTypeMask typeMask, string pattern) {
                m_fileTypeMask = typeMask;
                m_ignorePattern = pattern;
            }

            public IgnorePattern(IgnorePattern p) {
                m_fileTypeMask = p.m_fileTypeMask;
                m_ignorePattern = p.m_ignorePattern;
            }

            public FileTypeMask MatchingFileTypes {
                get {
                    return m_fileTypeMask;
                }
                set{
                    m_fileTypeMask = value;
                }
            }

            public string Pattern {
                get {
                    return m_ignorePattern;
                }
                set{
                    m_ignorePattern = value;
                    m_match = null;
                }
            }

            public bool IsMatch(string path) {
                if (string.IsNullOrEmpty (m_ignorePattern)) {
                    return false;
                }

                if (m_match == null) {
                    m_match = new Regex(m_ignorePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }

                if (((int)m_fileTypeMask & (int)FileTypeMask.File) > 0) {
                    var fileName = Path.GetFileName(path);
                    return m_match.IsMatch (fileName);
                }

                if (((int)m_fileTypeMask & (int)FileTypeMask.Directory) > 0) {
                    var dirName = Path.GetDirectoryName(path);
                    return m_match.IsMatch (dirName);
                }

                return false;
            }

            public void OnGUI(NodeGUI node) {
                m_fileTypeMask = (FileTypeMask)EditorGUILayout.EnumFlagsField (m_fileTypeMask, GUILayout.Width(85f));
                var newPattern = GUILayout.TextField(m_ignorePattern);
                if (newPattern != m_ignorePattern) {
                    m_ignorePattern = newPattern;
                    m_match = null;
                }
            }
        }

        [SerializeField] private SerializableMultiTargetString m_loadPath;
        [SerializeField] private SerializableMultiTargetString m_loadPathGuid;
        [SerializeField] private List<IgnorePattern> m_ignorePatterns;

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
				return "Load";
			}
		}
			
		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}

		public string LoadPath
		{
			get { return m_loadPath[EditorUserBuildSettings.activeBuildTarget]; }
			set
			{
				m_loadPath[EditorUserBuildSettings.activeBuildTarget] = value;
				m_loadPathGuid[EditorUserBuildSettings.activeBuildTarget] = AssetDatabase.AssetPathToGUID(value);
			}
		}

		public Loader() {}
        public Loader(string path) {
            var normalizedPath = NormalizeLoadPath (path);
            var loadPath = FileUtility.PathCombine (Model.Settings.Path.ASSETS_PATH, normalizedPath);
            var guid = AssetDatabase.AssetPathToGUID (loadPath);

            m_loadPath = new SerializableMultiTargetString(normalizedPath);
            m_loadPathGuid = new SerializableMultiTargetString (guid);

            m_respondToAssetChange = false;
        }

		public override void Initialize(Model.NodeData data) {
            if (m_loadPath == null) {
                m_loadPath = new SerializableMultiTargetString();
            }
            if (m_loadPathGuid == null) {
                m_loadPathGuid = new SerializableMultiTargetString();
            }

            m_respondToAssetChange = false;

			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_loadPath = new SerializableMultiTargetString(v1.LoaderLoadPath);
            m_loadPathGuid = new SerializableMultiTargetString();
            foreach (var v in m_loadPath.Values) {
                var loadPath = FileUtility.PathCombine (Model.Settings.Path.ASSETS_PATH, v.value);
                m_loadPathGuid [v.targetGroup] = AssetDatabase.AssetPathToGUID (loadPath);
            }
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Loader();
            newNode.m_loadPath = new SerializableMultiTargetString(m_loadPath);
            newNode.m_loadPathGuid = new SerializableMultiTargetString(m_loadPathGuid);
            newNode.m_respondToAssetChange = m_respondToAssetChange;

            newNode.m_ignorePatterns = new List<IgnorePattern> ();
            foreach (var p in m_ignorePatterns) {
                newNode.m_ignorePatterns.Add (new IgnorePattern (p));
            }

			newData.AddDefaultOutputPoint();
			return newNode;
		}

        private void CheckAndCorrectPath(BuildTarget target) {
            var loadPath = GetLoadPath (target);
            var pathFromGuid = AssetDatabase.GUIDToAssetPath (m_loadPathGuid[target]);

            // fix load path from guid (adopting folder rename)
            if (!AssetDatabase.IsValidFolder (loadPath)) {
                if (!string.IsNullOrEmpty (pathFromGuid)) {
                    if (m_loadPath.ContainsValueOf (target)) {
                        m_loadPath [target] = NormalizeLoadPath (pathFromGuid);
                    } else {
                        m_loadPath.DefaultValue = NormalizeLoadPath (pathFromGuid);
                    }
                }
            } 
            // if folder is valid and guid is invalid, reflect folder to guid
            else {
                if (string.IsNullOrEmpty (pathFromGuid)) {
                    if (m_loadPath.ContainsValueOf (target)) {
                        m_loadPathGuid [target] = AssetDatabase.AssetPathToGUID (loadPath);
                    } else {
                        m_loadPathGuid.DefaultValue = AssetDatabase.AssetPathToGUID (loadPath);
                    }
                }
            }
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

            CheckAndCorrectPath (target);
            var loadPath = GetLoadPath (target);

            foreach(var asset in ctx.ImportedAssets) {
                if (asset.importFrom.StartsWith (loadPath)) {
                    if (!IsIgnored(asset.importFrom)) {
                        return true;
                    }
                }
            }

            foreach(var asset in ctx.MovedAssets) {
                if (asset.importFrom.StartsWith (loadPath)) {
                    if (!IsIgnored(asset.importFrom)) {
                        return true;
                    }
                }
            }

            foreach (var path in ctx.MovedFromAssetPaths) {
                if (path.StartsWith (loadPath)) {
                    if (!IsIgnored(path)) {
                        return true;
                    }
                }
            }

            foreach (var path in ctx.DeletedAssetPaths) {
                if (path.StartsWith (loadPath)) {
                    if (!IsIgnored(path)) {
                        return true;
                    }
                }
            }

			return false;
		}

        public static string NormalizeLoadPath(string path) {
            if(!string.IsNullOrEmpty(path)) {
                var dataPath = Application.dataPath;
                if(dataPath == path) {
                    path = string.Empty;
                } else {
                    var index = path.IndexOf(dataPath);
                    if (index >= 0) {
                        path = path.Substring (dataPath.Length + index);
                        if (path.IndexOf ('/') == 0) {
                            path = path.Substring (1);
                        }
                    } else if(path.StartsWith (Model.Settings.Path.ASSETS_PATH)) {
                        path = path.Substring (Model.Settings.Path.ASSETS_PATH.Length);
                    }
                }
            }
            return path;
        }

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			if (m_loadPath == null) {
				return;
			}

			EditorGUILayout.HelpBox("Load From Directory: Load assets from given directory path.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);

            var bRespondAP = EditorGUILayout.ToggleLeft ("Respond To Asset Change", m_respondToAssetChange);
            if (bRespondAP != m_respondToAssetChange) {
                using (new RecordUndoScope ("Remove Target Load Path Settings", node, true)) {
                    m_respondToAssetChange = bRespondAP;
                    onValueChanged();
                }
            }

            GUILayout.Space(4f);

            //Show target configuration tab
			inspector.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = inspector.DrawOverrideTargetToggle(node, m_loadPath.ContainsValueOf(inspector.CurrentEditingGroup), (bool b) => {
					using(new RecordUndoScope("Remove Target Load Path Settings", node, true)) {
						if(b) {
                            m_loadPath[inspector.CurrentEditingGroup] = m_loadPath.DefaultValue;
                            m_loadPathGuid[inspector.CurrentEditingGroup] = m_loadPathGuid.DefaultValue;
						} else {
							m_loadPath.Remove(inspector.CurrentEditingGroup);
                            m_loadPathGuid.Remove(inspector.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
					var path = m_loadPath[inspector.CurrentEditingGroup];
					EditorGUILayout.LabelField("Load Path:");

					string newLoadPath = null;

                    newLoadPath = inspector.DrawFolderSelector (Model.Settings.Path.ASSETS_PATH, "Select Asset Folder", 
                        path,
                        FileUtility.PathCombine(Model.Settings.Path.ASSETS_PATH, path),
                        (string folderSelected) => { return NormalizeLoadPath(folderSelected); }
                    );

                    var dirPath = Path.Combine(Model.Settings.Path.ASSETS_PATH,newLoadPath);

					if (newLoadPath != path) {
						using(new RecordUndoScope("Load Path Changed", node, true)){
							m_loadPath[inspector.CurrentEditingGroup] = newLoadPath;
                            m_loadPathGuid [inspector.CurrentEditingGroup] = AssetDatabase.AssetPathToGUID (dirPath);
							onValueChanged();
						}
					}

					bool dirExists = Directory.Exists(dirPath);

					GUILayout.Space(10f);

					using (new EditorGUILayout.HorizontalScope()) {
						using(new EditorGUI.DisabledScope(string.IsNullOrEmpty(newLoadPath)||!dirExists)) 
						{
							GUILayout.FlexibleSpace();
							if(GUILayout.Button("Highlight in Project Window", GUILayout.Width(180f))) {
								// trailing is "/" not good for LoadMainAssetAtPath
								if(dirPath[dirPath.Length-1] == '/') {
									dirPath = dirPath.Substring(0, dirPath.Length-1);
								}
								var obj = AssetDatabase.LoadMainAssetAtPath(dirPath);
								EditorGUIUtility.PingObject(obj);
							}
						}
					}

					if(!dirExists) {
						var parentDirPath = Path.GetDirectoryName(dirPath);
						bool parentDirExists = Directory.Exists(parentDirPath);
						if(parentDirExists) {
							EditorGUILayout.LabelField("Available Directories:");
							string[] dirs = Directory.GetDirectories(parentDirPath);
							foreach(string s in dirs) {
								EditorGUILayout.LabelField(s);
							}
						}
					}
				}
			}

            GUILayout.Space(8f);
            DrawIgnoredPatterns(node, onValueChanged);
		}

		private void DrawIgnoredPatterns(NodeGUI node, Action onValueChanged)
		{
			var changed = false;
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Files & Directory Ignore Settings");
                IgnorePattern removingItem = null;
                foreach (var p in m_ignorePatterns) {
                    using(new GUILayout.HorizontalScope() ) {
                        if (GUILayout.Button ("-", GUILayout.Width (30))) {
                            removingItem = p;
                        }
                        EditorGUI.BeginChangeCheck();
                        p.OnGUI(node);
                        if (EditorGUI.EndChangeCheck()) {
                            changed = true;
                        }
                    }
                }
                if (removingItem != null) {
                    m_ignorePatterns.Remove (removingItem);
                    changed = true;
                }
				if (GUILayout.Button("+")) {
                    m_ignorePatterns.Add(new IgnorePattern(IgnorePattern.FileTypeMask.File, string.Empty));
                    changed = true;
				}
			}
			if (changed && onValueChanged != null) {
				using (new RecordUndoScope("Ignored Patterns Changed", node, true)) {
					onValueChanged();
				}
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            CheckAndCorrectPath (target);

			ValidateLoadPath(
				m_loadPath[target],
				GetLoaderFullLoadPath(target),
				() => {
					//can be empty
					//throw new NodeException(node.Name + ": Load Path is empty.", node);
				}, 
				() => {
					throw new NodeException(
                        "Directory not found: " + GetLoaderFullLoadPath(target),
                        "Select a valid directory.", node);
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

			var outputSource = new List<AssetReference>();

            var loadPath = GetLoadPath (target);
            var targetFiles = AssetDatabase.FindAssets("",  new string[] { loadPath });

            foreach (var assetGuid in targetFiles) {
                var targetFilePath = AssetDatabase.GUIDToAssetPath (assetGuid);

                if (!TypeUtility.IsLoadingAsset (targetFilePath)) {
                    continue;
                }

                // Skip folders
                var type = TypeUtility.GetMainAssetTypeAtPath (targetFilePath);
                if (type == typeof(UnityEditor.DefaultAsset) && AssetDatabase.IsValidFolder(targetFilePath) ) {
                    continue;
                }

                var r = AssetReferenceDatabase.GetReference(targetFilePath);

                if (r == null) {
                    continue;
                }

                if (outputSource.Contains (r)) {
                    continue;
                }

                if (IsIgnored(targetFilePath)) {
                    continue;
                }

                outputSource.Add(r);
			}

			var output = new Dictionary<string, List<AssetReference>> {
				{"0", outputSource}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}

        private string GetLoadPath(BuildTarget g) {
            var path = m_loadPath [g];
            if (string.IsNullOrEmpty (path)) {
                return "Assets";
            } else {
                return FileUtility.PathCombine (Model.Settings.Path.ASSETS_PATH, path);
            }
        }

		private string GetLoaderFullLoadPath(BuildTarget g) {
			return FileUtility.PathCombine(Application.dataPath, m_loadPath[g]);
		}

		private bool IsIgnored(string filePath) {
            if (m_ignorePatterns != null) {
                foreach (var p in m_ignorePatterns) {
                    if (p.IsMatch (filePath)) {
                        return true;
                    }
                }
            }
            return false;
		}
	}
}