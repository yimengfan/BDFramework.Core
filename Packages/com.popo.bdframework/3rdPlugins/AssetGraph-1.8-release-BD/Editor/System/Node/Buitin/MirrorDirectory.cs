using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("File/Mirror Directory", 110)]
	public class MirrorDirectory : Node {

        public enum MirrorOption : int {
            KeepAlreadyCopiedFiles,
            AlwaysRecreateDestination
        }

        [SerializeField] private SerializableMultiTargetString m_srcPath;
        [SerializeField] private SerializableMultiTargetString m_dstPath;
        [SerializeField] private SerializableMultiTargetInt m_mirrorOption;

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
				return "File";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.Any;
			}
		}

		public override Model.NodeOutputSemantics NodeOutputType {
			get {
                return Model.NodeOutputSemantics.Any;
			}
		}
			
		public override void Initialize(Model.NodeData data) {
            m_srcPath = new SerializableMultiTargetString();
            m_dstPath = new SerializableMultiTargetString();
            m_mirrorOption = new SerializableMultiTargetInt ();

            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new MirrorDirectory();
            newNode.m_srcPath = new SerializableMultiTargetString(m_srcPath);
            newNode.m_dstPath = new SerializableMultiTargetString(m_dstPath);
            newNode.m_mirrorOption = new SerializableMultiTargetInt (m_mirrorOption);

			newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {
			
            if (m_srcPath == null) {
				return;
			}

			var currentEditingGroup = editor.CurrentEditingGroup;

			EditorGUILayout.HelpBox("Mirror Directory: Mirror source directory to destination. This node does not use assets passed by.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                var disabledScope = editor.DrawOverrideTargetToggle(node, m_srcPath.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Mirror Directory Settings", node, true)){
						if(enabled) {
                            m_srcPath[currentEditingGroup] = m_srcPath.DefaultValue;
                            m_dstPath[currentEditingGroup] = m_dstPath.DefaultValue;
                            m_mirrorOption[currentEditingGroup] = m_mirrorOption.DefaultValue;
						}  else {
                            m_srcPath.Remove(currentEditingGroup);
                            m_dstPath.Remove(currentEditingGroup);
                            m_mirrorOption[currentEditingGroup] = m_mirrorOption.DefaultValue;
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
                    MirrorOption opt = (MirrorOption)m_mirrorOption[currentEditingGroup];
                    var newOption = (MirrorOption)EditorGUILayout.EnumPopup("Mirror Option", opt);
                    if(newOption != opt) {
                        using(new RecordUndoScope("Change Mirror Option", node, true)){
                            m_mirrorOption[currentEditingGroup] = (int)newOption;
                            onValueChanged();
                        }
                    }

					EditorGUILayout.LabelField("Source Directory Path:");

                    string newSrcPath = null;
                    string newDstPath = null;

                    newSrcPath = editor.DrawFolderSelector ("", "Select Source Folder", 
                        m_srcPath[currentEditingGroup],
                        Directory.GetParent(Application.dataPath).ToString(),
                        (string folderSelected) => {
                            var projectPath = Directory.GetParent(Application.dataPath).ToString();

                            if(projectPath == folderSelected) {
                                folderSelected = string.Empty;
                            } else {
                                var index = folderSelected.IndexOf(projectPath);
                                if(index >= 0 ) {
                                    folderSelected = folderSelected.Substring(projectPath.Length + index);
                                    if(folderSelected.IndexOf('/') == 0) {
                                        folderSelected = folderSelected.Substring(1);
                                    }
                                }
                            }
                            return folderSelected;
                        }
                    );
                    if (newSrcPath != m_srcPath[currentEditingGroup]) {
                        using(new RecordUndoScope("Change Source Folder", node, true)){
                            m_srcPath[currentEditingGroup] = newSrcPath;
                            onValueChanged();
                        }
                    }
                    DrawDirectorySuggestion (GetNormalizedPath (m_srcPath[currentEditingGroup]), currentEditingGroup, onValueChanged);

                    GUILayout.Space (10f);

                    EditorGUILayout.LabelField("Destination Directory Path:");
                    newDstPath = editor.DrawFolderSelector ("", "Select Destination Folder", 
                        m_dstPath[currentEditingGroup],
                        Directory.GetParent(Application.dataPath).ToString(),
                        (string folderSelected) => {
                            var projectPath = Directory.GetParent(Application.dataPath).ToString();

                            if(projectPath == folderSelected) {
                                folderSelected = string.Empty;
                            } else {
                                var index = folderSelected.IndexOf(projectPath);
                                if(index >= 0 ) {
                                    folderSelected = folderSelected.Substring(projectPath.Length + index);
                                    if(folderSelected.IndexOf('/') == 0) {
                                        folderSelected = folderSelected.Substring(1);
                                    }
                                }
                            }
                            return folderSelected;
                        }
                    );

                    if (newDstPath != m_dstPath[currentEditingGroup]) {
                        using(new RecordUndoScope("Change Destination Folder", node, true)){
                            m_dstPath[currentEditingGroup] = newDstPath;
                            onValueChanged();
                        }
                    }

                    DrawDirectorySuggestion (GetNormalizedPath (m_dstPath[currentEditingGroup]), currentEditingGroup, onValueChanged);
				}
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            if (string.IsNullOrEmpty(m_srcPath [target])) {
                throw new NodeException("Mirror Directory can not set project directory as source.", 
                    "Set valid source directory from inspector.",node);
            }
            if (string.IsNullOrEmpty(m_dstPath [target])) {
                throw new NodeException("Mirror Directory can not set project directory as destination.", 
                    "Set valid destination directory from inspector.",node);
            }
            if (!Directory.Exists (GetNormalizedPath (m_srcPath [target]))) {
                throw new NodeException("Source Directory does not exist. Path:" + m_srcPath[target], 
                    "Create source diretory or set valid source directory path from inspector.", node);
            }

            // MirrorDirectory does not add, filter or change structure of group, so just pass given group of assets
            if(Output != null) {
                var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
                    null : connectionsToOutput.First();

                if(incoming != null) {
                    foreach(var ag in incoming) {
                        Output(dst, ag.assetGroups);
                    }
                } else {
                    Output(dst, new Dictionary<string, List<AssetReference>>());
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
			Mirror(target, node, incoming, connectionsToOutput, progressFunc);
		}

        private void Mirror (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			Action<Model.NodeData, string, float> progressFunc) 
		{
            try {
                var srcDir = GetNormalizedPath(m_srcPath[target]);
                var dstDir = GetNormalizedPath(m_dstPath[target]);

                if (Directory.Exists (dstDir)) {
                    if(m_mirrorOption[target] == (int)MirrorOption.AlwaysRecreateDestination) {
                        FileUtility.DeleteDirectory(dstDir, true);
                    }
                    else if(m_mirrorOption[target] == (int)MirrorOption.KeepAlreadyCopiedFiles)
                    {
                        var dstFilePaths = FileUtility.GetAllFilePathsInFolder (dstDir);
                        // checking destination files - remove files if not exist in source
                        foreach (var dstPath in dstFilePaths) {
                            var srcPath = dstPath.Replace (dstDir, srcDir);
                            if (!File.Exists (srcPath)) {
                                File.Delete (dstPath);
                            }
                        }
                    }
                }

                var targetFilePaths = FileUtility.GetAllFilePathsInFolder(srcDir);
                int i = 0;

                foreach (var srcPath in targetFilePaths) {

                    var dstPath = srcPath.Replace (srcDir, dstDir);

                    var dstParentDir = Directory.GetParent (dstPath);
                    if (!dstParentDir.Exists) {
                        var dirPath = dstParentDir.ToString();
                        Directory.CreateDirectory (dirPath);
                    }

                    var srcInfo = new FileInfo (srcPath);
                    var dstInfo = new FileInfo (dstPath);

                    if (!dstInfo.Exists ||
                        srcInfo.LastWriteTimeUtc > dstInfo.LastWriteTimeUtc) 
                    {
                        File.Copy (srcPath, dstPath, true);
                        progressFunc (node, $"Copying {Path.GetFileName(srcPath)}", (float)(i++) / (float)targetFilePaths.Count);
                    }
                }

            } catch(Exception e) {
                throw new NodeException(e.Message, "See description for detail.", node);
            }
		}

		private string GetNormalizedPath(string path) {
			if(string.IsNullOrEmpty(path)) {
				return Directory.GetParent(Application.dataPath).ToString();
			} else if(path[0] == '/') {
				return path;
			} else {
				return FileUtility.GetPathWithProjectPath(path);
			}
		}

        private void DrawDirectorySuggestion(string targetPath, BuildTargetGroup currentEditingGroup, Action onValueChanged) {
            if (!Directory.Exists (targetPath)) {
                using (new EditorGUILayout.HorizontalScope ()) {
                    EditorGUILayout.LabelField (targetPath + " does not exist.");
                    if (GUILayout.Button ("Create directory")) {
                        Directory.CreateDirectory (targetPath);
                    }
                    onValueChanged ();
                }
                EditorGUILayout.Space ();

                string parentDir = Path.GetDirectoryName (targetPath);
                if (Directory.Exists (parentDir)) {
                    EditorGUILayout.LabelField ("Available Directories:");
                    string[] dirs = Directory.GetDirectories (parentDir);
                    foreach (string s in dirs) {
                        EditorGUILayout.LabelField (s);
                    }
                }
            } else {
                GUILayout.Space(10f);

                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button(GUIHelper.RevealInFinderLabel)) {
                        EditorUtility.RevealInFinder(targetPath);
                    }
                }
            }
        }

		public static bool ValidateDirectory (string combinedPath, Action DoesNotExist) {
			if (!Directory.Exists(combinedPath)) {
				DoesNotExist();
				return false;
			}
			return true;
		}
	}
}