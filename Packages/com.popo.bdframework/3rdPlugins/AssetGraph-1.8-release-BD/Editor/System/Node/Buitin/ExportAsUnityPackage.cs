using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Export/Export As UnityPackage", 101)]
	public class UnityPackageExporter : Node {

		[SerializeField] private SerializableMultiTargetString m_packageName;
		[SerializeField] private SerializableMultiTargetInt m_exportPackageOptions;

		public override string ActiveStyle => "node 0 on";

		public override string InactiveStyle => "node 0";

		public override string Category => "Export";

		public override Model.NodeOutputSemantics NodeInputType =>
			(Model.NodeOutputSemantics) 
			(uint)Model.NodeOutputSemantics.Assets;

		public override Model.NodeOutputSemantics NodeOutputType => Model.NodeOutputSemantics.Assets;

		public override void Initialize(Model.NodeData data)
		{
			m_packageName = new SerializableMultiTargetString();
			m_exportPackageOptions = new SerializableMultiTargetInt();

			data.AddDefaultInputPoint();
			data.AddOutputPoint("UnityPackage");
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new UnityPackageExporter
			{
				m_packageName = new SerializableMultiTargetString(m_packageName),
				m_exportPackageOptions = new SerializableMultiTargetInt(m_exportPackageOptions)
			};

			newData.AddDefaultInputPoint();
			newData.AddOutputPoint("UnityPackage");

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {
			
			var currentEditingGroup = inspector.CurrentEditingGroup;

			EditorGUILayout.HelpBox("Export As UnityPackage: Export given files as UnityPackage.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			inspector.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = inspector.DrawOverrideTargetToggle(node, m_packageName.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Export Settings", node, true)){
						if(enabled) {
							m_packageName[currentEditingGroup] = m_packageName.DefaultValue;
							m_exportPackageOptions[currentEditingGroup] = m_exportPackageOptions.DefaultValue;
						}  else {
							m_packageName.Remove(currentEditingGroup);
							m_exportPackageOptions.Remove(currentEditingGroup);
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
					var name = m_packageName[currentEditingGroup];
					var newName = EditorGUILayout.TextField("Package Name", name);
					if(newName != name) {
						using(new RecordUndoScope("Change Package Name", node, true)){
							m_packageName[currentEditingGroup] = newName;
							onValueChanged();
						}
					}
					EditorGUILayout.HelpBox("You can use {Platform} and {GroupName} for package name variable.", MessageType.Info);
					
					GUILayout.Space(8f);

					var exportOptions = m_exportPackageOptions[currentEditingGroup];
					
					foreach (var option in Model.Settings.ExportPackageOptions) {

						// contains keyword == enabled. if not, disabled.
						var isEnabled = (exportOptions & (int)option.option) != 0;
						var result = EditorGUILayout.ToggleLeft(option.description, isEnabled);
						if (result != isEnabled) {
							using(new RecordUndoScope("Change Bundle Options", node, true)){
								exportOptions = (result) ? 
									((int)option.option | exportOptions) : 
									(((~(int)option.option)) & exportOptions);
								m_exportPackageOptions[inspector.CurrentEditingGroup] = exportOptions;
								onValueChanged();
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
			if(incoming != null && Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				var projectPath = Directory.GetParent(Application.dataPath).ToString();

				foreach(var ag in incoming) {
					var outputDict = new Dictionary<string, List<AssetReference>>();
					foreach (var key in ag.assetGroups.Keys)
					{
						var packageName = GetPackageNameForGroup(target, key);
						outputDict.Add(key, new List<AssetReference>
						{
							AssetReference.CreateUnityPackageReference(FileUtility.PathCombine(projectPath, packageName))
						});
					}
					
					Output(dst, outputDict);
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

			foreach(var ag in incoming)
			{
				foreach (var groupKey in ag.assetGroups.Keys) {
					var assetsList = ag.assetGroups[groupKey].Select(a => a.importFrom);

					if (assetsList.Any())
					{
						AssetDatabase.ExportPackage(assetsList.ToArray(), GetPackageNameForGroup(target, groupKey),
							(ExportPackageOptions) m_exportPackageOptions[target]);
					}
				}
			}
		}

		private string GetPackageNameForGroup(BuildTarget target, string groupKey)
		{
			var exportPackageName = m_packageName[target];
			var packageName = string.IsNullOrEmpty(exportPackageName) ? groupKey :
				exportPackageName.
					Replace("{GroupName}", groupKey).
					Replace("{Platform}", BuildTargetUtility.TargetToAssetBundlePlatformName (target));
			if (!packageName.EndsWith(".unitypackage"))
			{
				packageName += ".unitypackage";
			}
			return packageName;
		}
	}
}