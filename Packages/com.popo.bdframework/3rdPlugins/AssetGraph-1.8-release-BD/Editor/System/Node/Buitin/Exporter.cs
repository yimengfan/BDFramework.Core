using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Export/Export To Directory", 100)]
	public class Exporter : Node, Model.NodeDataImporter {

		public enum ExportOption : int {
			ErrorIfNoExportDirectoryFound,
			AutomaticallyCreateIfNoExportDirectoryFound,
			DeleteAndRecreateExportDirectory
		}

		[SerializeField] private SerializableMultiTargetString m_exportPath;
		[SerializeField] private SerializableMultiTargetInt m_exportOption;
		[SerializeField] private SerializableMultiTargetInt m_flattenDir;

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
				return "Export";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return 
					(Model.NodeOutputSemantics) 
					((uint)Model.NodeOutputSemantics.Assets | 
					 (uint)Model.NodeOutputSemantics.AssetBundles);
			}
		}

		public override Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}
			
		public override void Initialize(Model.NodeData data) {
			//Take care of this with Initialize(NodeData)
			m_exportPath = new SerializableMultiTargetString();
			m_exportOption = new SerializableMultiTargetInt();
			m_flattenDir = new SerializableMultiTargetInt();

			data.AddDefaultInputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_exportPath = new SerializableMultiTargetString (v1.ExporterExportPath);
			m_exportOption = new SerializableMultiTargetInt(v1.ExporterExportOption);
			m_flattenDir = new SerializableMultiTargetInt();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Exporter();
			newNode.m_exportPath = new SerializableMultiTargetString(m_exportPath);
			newNode.m_exportOption = new SerializableMultiTargetInt(m_exportOption);
			newNode.m_flattenDir = new SerializableMultiTargetInt(m_flattenDir);

			newData.AddDefaultInputPoint();

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {
			
			if (m_exportPath == null) {
				return;
			}

			var currentEditingGroup = inspector.CurrentEditingGroup;

			EditorGUILayout.HelpBox("Export To Directory: Export given files to output directory.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			inspector.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = inspector.DrawOverrideTargetToggle(node, m_exportPath.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Export Settings", node, true)){
						if(enabled) {
							m_exportPath[currentEditingGroup] = m_exportPath.DefaultValue;
							m_exportOption[currentEditingGroup] = m_exportOption.DefaultValue;
							m_flattenDir[currentEditingGroup] = m_flattenDir.DefaultValue;
						}  else {
							m_exportPath.Remove(currentEditingGroup);
							m_exportOption.Remove(currentEditingGroup);
							m_flattenDir.Remove(currentEditingGroup);
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
					ExportOption opt = (ExportOption)m_exportOption[currentEditingGroup];
					var newOption = (ExportOption)EditorGUILayout.EnumPopup("Export Option", opt);
					if(newOption != opt) {
						using(new RecordUndoScope("Change Export Option", node, true)){
							m_exportOption[currentEditingGroup] = (int)newOption;
							onValueChanged();
						}
					}

					EditorGUILayout.LabelField("Export Path:");

					string newExportPath = null;

                    newExportPath = inspector.DrawFolderSelector ("", "Select Export Folder", 
                        m_exportPath[currentEditingGroup],
                        GetExportPath(m_exportPath[currentEditingGroup]),
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
                    if (newExportPath != m_exportPath[currentEditingGroup]) {
                        using(new RecordUndoScope("Change Export Path", node, true)){
                            m_exportPath[currentEditingGroup] = newExportPath;
                            onValueChanged();
                        }
                    }

					int flat = m_flattenDir[currentEditingGroup];
					var newFlat = EditorGUILayout.ToggleLeft("Flatten Directory", flat == 1) ? 1:0;
					if(newFlat != flat) {
						using(new RecordUndoScope("Change Flatten Directory", node, true)){
							m_flattenDir[currentEditingGroup] = newFlat;
							onValueChanged();
						}
					}

					var exporterNodePath = GetExportPath(newExportPath);
					if(ValidateExportPath(
						newExportPath,
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
				m_exportPath[target],
				GetExportPath(m_exportPath[target]),
				() => {
                    throw new NodeException("Export Path is empty.", "Set valid export path from inspector.",node);
				},
				() => {
					if( m_exportOption[target] == (int)ExportOption.ErrorIfNoExportDirectoryFound ) {
						throw new NodeException("Directory set to Export Path does not exist. Path:" + m_exportPath[target], 
                            "Create exporting directory or set valid export path from inspector.", node);
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
			Export(target, node, incoming, connectionsToOutput, progressFunc);
		}

		private void Export (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var exportPath = GetExportPath(m_exportPath[target]);

			if(m_exportOption[target] == (int)ExportOption.DeleteAndRecreateExportDirectory) {
				if (Directory.Exists(exportPath)) {
                    FileUtility.DeleteDirectory(exportPath, true);
				}
			}

			if(m_exportOption[target] != (int)ExportOption.ErrorIfNoExportDirectoryFound) {
				if (!Directory.Exists(exportPath)) {
					Directory.CreateDirectory(exportPath);
				}
			}

			var report = new ExportReport(node);
            var cacheFolderDepth = Model.Settings.Path.BundleBuilderCachePath.Split(Model.Settings.UNITY_FOLDER_SEPARATOR).Length;

			foreach(var ag in incoming) {
				foreach (var groupKey in ag.assetGroups.Keys) {
					var inputSources = ag.assetGroups[groupKey];

					foreach (var source in inputSources) {					
						var destinationSourcePath = source.importFrom;

						string destination = null;

						if(m_flattenDir[target] == 0) {
							// in bundleBulider, use platform-package folder for export destination.
                            if (destinationSourcePath.StartsWith(Model.Settings.Path.BundleBuilderCachePath)) {

								var splitted = destinationSourcePath.Split(Model.Settings.UNITY_FOLDER_SEPARATOR);
								var reducedArray = new string[splitted.Length - cacheFolderDepth];

								Array.Copy(splitted, cacheFolderDepth, reducedArray, 0, reducedArray.Length);
								var fromDepthToEnd = string.Join(Model.Settings.UNITY_FOLDER_SEPARATOR.ToString(), reducedArray);

								destinationSourcePath = fromDepthToEnd;
							}
							destination = FileUtility.PathCombine(exportPath, destinationSourcePath);
						} else {
							destination = FileUtility.PathCombine(exportPath, source.fileNameAndExtension);
						}

						var parentDir = Directory.GetParent(destination).ToString();

						if (!Directory.Exists(parentDir)) {
							Directory.CreateDirectory(parentDir);
						}
						if (File.Exists(destination)) {
							File.Delete(destination);
						}
						if (string.IsNullOrEmpty(source.importFrom)) {
							report.AddErrorEntry(source.absolutePath, destination, "Source Asset import path is empty; given asset is not imported by Unity.");
							continue;
						}
						try {
							if(progressFunc != null) progressFunc(node, $"Copying {source.fileNameAndExtension}", 0.5f);
							File.Copy(source.importFrom, destination);
							report.AddExportedEntry(source.importFrom, destination);
						} catch(Exception e) {
							report.AddErrorEntry(source.importFrom, destination, e.Message);
						}

						source.exportTo = destination;
					}
				}
			}

			AssetBundleBuildReport.AddExportReport(report);
		}

		private string GetExportPath(string path) {
			if(string.IsNullOrEmpty(path)) {
				return Directory.GetParent(Application.dataPath).ToString();
			} else if(path[0] == '/') {
				return path;
			} else {
				return FileUtility.GetPathWithProjectPath(path);
			}
		}

		public static bool ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action DoesNotExist) {
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