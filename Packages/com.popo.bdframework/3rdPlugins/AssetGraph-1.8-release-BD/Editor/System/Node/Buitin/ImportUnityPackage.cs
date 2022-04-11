using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Import/Import UnityPacakges", 95)]
	public class ImportUnityPackage : Node {

		[SerializeField] private SerializableMultiTargetString m_importDirectoryPath;
		[SerializeField] private SerializableMultiTargetInt m_isInteractive;
		[SerializeField] private SerializableMultiTargetInt m_isRecursive;

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
				return "Import";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType => Model.NodeOutputSemantics.None;

		public override Model.NodeOutputSemantics NodeOutputType => Model.NodeOutputSemantics.None;

		public override void Initialize(Model.NodeData data) {
			//Take care of this with Initialize(NodeData)
			m_importDirectoryPath = new SerializableMultiTargetString();
			m_isInteractive = new SerializableMultiTargetInt();
			m_isRecursive = new SerializableMultiTargetInt();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new ImportUnityPackage
			{
				m_importDirectoryPath = new SerializableMultiTargetString(m_importDirectoryPath),
				m_isInteractive = new SerializableMultiTargetInt(m_isInteractive),
				m_isRecursive = new SerializableMultiTargetInt(m_isRecursive)
			};

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {
			
			if (m_importDirectoryPath == null) {
				return;
			}

			var currentEditingGroup = editor.CurrentEditingGroup;

			EditorGUILayout.HelpBox("Import Unity Packages: Import Unity Packages.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_importDirectoryPath.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Export Settings", node, true)){
						if(enabled) {
							m_importDirectoryPath[currentEditingGroup] = m_importDirectoryPath.DefaultValue;
							m_isInteractive[currentEditingGroup] = m_isInteractive.DefaultValue;
							m_isRecursive[currentEditingGroup] = m_isRecursive.DefaultValue;
						}  else {
							m_importDirectoryPath.Remove(currentEditingGroup);
							m_isInteractive.Remove(currentEditingGroup);
							m_isRecursive.Remove(currentEditingGroup);
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
					EditorGUILayout.LabelField("Import Path:");

					string newImportDir = null;

                    newImportDir = editor.DrawFolderSelector ("", "Select Import Folder", 
                        m_importDirectoryPath[currentEditingGroup],
                        GetImportDirectoryPath(m_importDirectoryPath[currentEditingGroup]),
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
                    if (newImportDir != m_importDirectoryPath[currentEditingGroup]) {
                        using(new RecordUndoScope("Change Import Path", node, true)){
	                        m_importDirectoryPath[currentEditingGroup] = newImportDir;
                            onValueChanged();
                        }
                    }

					int isInteractive = m_isInteractive[currentEditingGroup];
					var newIsInteractive = EditorGUILayout.ToggleLeft("Interactive", isInteractive == 1) ? 1:0;
					if(newIsInteractive != isInteractive) {
						using(new RecordUndoScope("Change Interactive", node, true)){
							m_isInteractive[currentEditingGroup] = newIsInteractive;
							onValueChanged();
						}
					}
					
					int isRecursive = m_isRecursive[currentEditingGroup];
					var newIsRecursive = EditorGUILayout.ToggleLeft("Import packages in subfolders", isRecursive == 1) ? 1:0;
					if(newIsRecursive != isRecursive) {
						using(new RecordUndoScope("Change Recursive", node, true)){
							m_isRecursive[currentEditingGroup] = newIsRecursive;
							onValueChanged();
						}
					}
					

					var importDirectoryPath = GetImportDirectoryPath(newImportDir);
					if (!Directory.Exists(importDirectoryPath))
					{
						using (new EditorGUILayout.HorizontalScope()) {
							EditorGUILayout.LabelField(importDirectoryPath + " does not exist.");
							if(GUILayout.Button("Create directory")) {
								Directory.CreateDirectory(importDirectoryPath);
							}
							onValueChanged();
						}
						EditorGUILayout.Space();

						var parentDir = Path.GetDirectoryName(importDirectoryPath);
						if(Directory.Exists(parentDir)) {
							EditorGUILayout.LabelField("Available Directories:");
							var dirs = Directory.GetDirectories(parentDir);
							foreach(var s in dirs) {
								EditorGUILayout.LabelField(s);
							}
						}
					}
					else
					{
						GUILayout.Space(10f);

						using (new EditorGUILayout.HorizontalScope()) {
							GUILayout.FlexibleSpace();
							if(GUILayout.Button(GUIHelper.RevealInFinderLabel)) {
								EditorUtility.RevealInFinder(importDirectoryPath);
							}
						}
					}
				}
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output)
		{
			var importPath = GetImportDirectoryPath(m_importDirectoryPath[target]);
			if (!Directory.Exists(importPath))
			{
				throw new NodeException("Import Path directory does not exist. Path:" + m_importDirectoryPath[target], 
					"Create directory or set valid import path from inspector.", node);
			}
		}
		
		public override void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			var importPath = GetImportDirectoryPath(m_importDirectoryPath[target]);

			var isRecursive = m_isRecursive[target] != 0;
			var isInteractive = m_isInteractive[target] != 0;

			var files = isRecursive
				? FileUtility.GetAllFilePathsInFolder(importPath)
				: FileUtility.GetFilePathsInFolder(importPath);

			foreach(var packagePath in files.Where(p => p.EndsWith(".unitypackage")))
			{
				AssetDatabase.ImportPackage(packagePath, isInteractive);
			}
		}

		private static string GetImportDirectoryPath(string path) {
			if(string.IsNullOrEmpty(path)) {
				return Directory.GetParent(Application.dataPath).ToString();
			} else if(path[0] == '/') {
				return path;
			} else {
				return FileUtility.GetPathWithProjectPath(path);
			}
		}
	}
}