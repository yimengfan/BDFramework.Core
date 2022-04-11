using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("File/Copy or Move Files", 111)]
	public class FileOperation : Node {

		public enum DestinationDirectoryOption : int {
			ErrorIfNoExportDirectoryFound,
			AutomaticallyCreateIfNoExportDirectoryFound,
			DeleteAndRecreateExportDirectory
		}
		
		public enum FileOperationType : int {
			Copy,
			Move
		}

		[SerializeField] private SerializableMultiTargetString m_destinationPath;
		[SerializeField] private SerializableMultiTargetInt m_destinationOption;
		[SerializeField] private FileOperationType m_operationType;
		[SerializeField] private int m_removingDirectoryDepth;

		public override string ActiveStyle => "node 0 on";

		public override string InactiveStyle => "node 0";

		public override string Category => "File";

		public override Model.NodeOutputSemantics NodeInputType =>
			(Model.NodeOutputSemantics) 
			((uint)Model.NodeOutputSemantics.Assets | 
			 (uint)Model.NodeOutputSemantics.AssetBundles);

		public override Model.NodeOutputSemantics NodeOutputType => Model.NodeOutputSemantics.None;

		public override void Initialize(Model.NodeData data) {
			//Take care of this with Initialize(NodeData)
			m_destinationPath = new SerializableMultiTargetString();
			m_destinationOption = new SerializableMultiTargetInt();
			m_operationType = FileOperationType.Copy;
			m_removingDirectoryDepth = 1;

			data.AddDefaultInputPoint();
		}
		
		public override Node Clone(Model.NodeData newData) {
			var newNode = new FileOperation
			{
				m_destinationPath = new SerializableMultiTargetString(m_destinationPath),
				m_destinationOption = new SerializableMultiTargetInt(m_destinationOption),
				m_operationType = m_operationType,
				m_removingDirectoryDepth = m_removingDirectoryDepth
			};

			newData.AddDefaultInputPoint();

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {
			
			if (m_destinationPath == null) {
				return;
			}

			var currentEditingGroup = editor.CurrentEditingGroup;

			EditorGUILayout.HelpBox("File Operation: Copy or Move Files.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);
			
			var newOp = (FileOperationType)EditorGUILayout.EnumPopup("Operation", m_operationType);
			if(newOp != m_operationType) {
				using(new RecordUndoScope("Change Copy/Move Operation", node, true))
				{
					m_operationType = newOp;
					onValueChanged();
				}
			}
			
			var newDepth = EditorGUILayout.IntField("Removing Directory Depth", m_removingDirectoryDepth);
			if(newDepth != m_removingDirectoryDepth) {
				using(new RecordUndoScope("Change Directory Depth", node, true))
				{
					m_removingDirectoryDepth = newDepth;
					onValueChanged();
				}
			}

			GUILayout.Space(8f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_destinationPath.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Directory Settings", node, true)){
						if(enabled) {
							m_destinationPath[currentEditingGroup] = m_destinationPath.DefaultValue;
							m_destinationOption[currentEditingGroup] = m_destinationOption.DefaultValue;
						}  else {
							m_destinationPath.Remove(currentEditingGroup);
							m_destinationOption.Remove(currentEditingGroup);
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
					DestinationDirectoryOption opt = (DestinationDirectoryOption)m_destinationOption[currentEditingGroup];
					var newOption = (DestinationDirectoryOption)EditorGUILayout.EnumPopup("Directory Option", opt);
					if(newOption != opt) {
						using(new RecordUndoScope("Change Directory Option", node, true)){
							m_destinationOption[currentEditingGroup] = (int)newOption;
							onValueChanged();
						}
					}

					EditorGUILayout.LabelField("Destination Path:");

					string newDstPath = null;

                    newDstPath = editor.DrawFolderSelector ("", "Select Destination Folder", 
                        m_destinationPath[currentEditingGroup],
                        GetDestinationPath(m_destinationPath[currentEditingGroup]),
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
                    if (newDstPath != m_destinationPath[currentEditingGroup]) {
                        using(new RecordUndoScope("Change Destination Path", node, true)){
                            m_destinationPath[currentEditingGroup] = newDstPath;
                            onValueChanged();
                        }
                    }

					var exporterNodePath = GetDestinationPath(newDstPath);
					if(ValidateExportPath(
						newDstPath,
						exporterNodePath,
						() => {
						},
						() => {
							using (new EditorGUILayout.HorizontalScope()) {
								EditorGUILayout.LabelField(exporterNodePath + " does not exist.");
								if(GUILayout.Button("Create directory")) {
									Directory.CreateDirectory(exporterNodePath);
								}
								onValueChanged();
							}
							EditorGUILayout.Space();

							string parentDir = Path.GetDirectoryName(exporterNodePath);
							if(Directory.Exists(parentDir)) {
								EditorGUILayout.LabelField("Available Directories:");
								string[] dirs = Directory.GetDirectories(parentDir);
								foreach(string s in dirs) {
									EditorGUILayout.LabelField(s);
								}
							}
						}
					)) {
						GUILayout.Space(10f);

						using (new EditorGUILayout.HorizontalScope()) {
							GUILayout.FlexibleSpace();
							if(GUILayout.Button(GUIHelper.RevealInFinderLabel)) {
								EditorUtility.RevealInFinder(exporterNodePath);
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
			ValidateExportPath(
				m_destinationPath[target],
				GetDestinationPath(m_destinationPath[target]),
				() => { },
				() => {
					if( m_destinationOption[target] == (int)DestinationDirectoryOption.ErrorIfNoExportDirectoryFound ) {
						throw new NodeException("Destination directory does not exist. Path:" + m_destinationPath[target], 
                            "Create directory or set valid export path from inspector.", node);
					}
				}
			);
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

			var dstPath = GetDestinationPath(m_destinationPath[target]);

			if(m_destinationOption[target] == (int)DestinationDirectoryOption.DeleteAndRecreateExportDirectory) {
				if (Directory.Exists(dstPath)) {
                    FileUtility.DeleteDirectory(dstPath, true);
				}
			}

			if(m_destinationOption[target] != (int)DestinationDirectoryOption.ErrorIfNoExportDirectoryFound) {
				if (!Directory.Exists(dstPath)) {
					Directory.CreateDirectory(dstPath);
				}
			}
			var opTypeName = m_operationType == FileOperationType.Copy ? "Copy" : "Move";
			var isDestinationWithinProject = dstPath.StartsWith(Application.dataPath);
			var projectPathLength = Directory.GetParent(Application.dataPath).ToString().Length + 1;
			
			foreach(var ag in incoming) {
				foreach (var groupKey in ag.assetGroups.Keys) {
					var inputSources = ag.assetGroups[groupKey];

					foreach (var source in inputSources) {					
						var destination = FileUtility.PathCombine(dstPath, GetReducedPath(source));

						var parentDir = Directory.GetParent(destination).ToString();

						if (!Directory.Exists(parentDir)) {
							Directory.CreateDirectory(parentDir);
						}
						if (File.Exists(destination)) {
							File.Delete(destination);
						}
						if (string.IsNullOrEmpty(source.importFrom)) {
							continue;
						}
						try
						{
							progressFunc?.Invoke(node, $"{opTypeName} {source.fileNameAndExtension}", 0.5f);

							if (m_operationType == FileOperationType.Copy)
							{
								if (source.isProjectAsset && isDestinationWithinProject)
								{
									var relativePath = destination.Substring(projectPathLength);
									AssetDatabase.CopyAsset(source.importFrom, relativePath);
								}
								else
								{
									File.Copy(source.importFrom, destination);
								}
							}
							else
							{
								if (source.isProjectAsset &&  isDestinationWithinProject)
								{
									var relativePath = destination.Substring(projectPathLength);
									AssetDatabase.MoveAsset(source.importFrom, relativePath);
								}
								else
								{
									File.Move(source.importFrom, destination);
								} 
							}
						}
						catch (Exception )
						{
							// ignored
						}
					}
				}
			}
		}

		private string GetReducedPath(AssetReference asset)
		{
			var path = asset.importFrom;
			var separatorPos = 0;
			for (var c = 0; c < m_removingDirectoryDepth; c++)
			{
				separatorPos = path.IndexOf('/', separatorPos + 1);
				if (separatorPos < 0)
				{
					break;
				}
			}

			return separatorPos < 0 ? asset.fileNameAndExtension : path.Substring(separatorPos + 1);
		}

		private static string GetDestinationPath(string path) {
			if(string.IsNullOrEmpty(path)) {
				return Directory.GetParent(Application.dataPath).ToString();
			} else if(path[0] == '/') {
				return path;
			} else {
				return FileUtility.GetPathWithProjectPath(path);
			}
		}

		private static bool ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action DoesNotExist) {
			if (string.IsNullOrEmpty(currentExportFilePath)) {
				NullOrEmpty();
				return false;
			}
			if (!Directory.Exists(combinedPath)) {
				DoesNotExist();
				return false;
			}
			return true;
		}
	}
}