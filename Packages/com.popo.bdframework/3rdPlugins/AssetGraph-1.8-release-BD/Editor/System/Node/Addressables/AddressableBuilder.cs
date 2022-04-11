/**
 * Addressable integration
 * 
 * This code will add features for Addressables System.
 * 
 * Addressables System is available from Unity Package Manager.
 */

#if ADDRESSABLES_1_6_OR_NEWER

using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.AddressableAssets;
using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Build/Build Addressable Assets", 102)]
	public class AddressableBuilder : Node
	{
		[SerializeField] private string m_contentStateFilePath;
		[SerializeField] private string m_builderGuid;
		[SerializeField] private string m_profileName;
		[SerializeField] private bool m_preferUpdate;

		private const string kCurrentProfile = "<current>";
		
		public override string ActiveStyle => "node 5 on";
		public override string InactiveStyle => "node 5";
		public override string Category => "Build";
		public override Model.NodeOutputSemantics NodeInputType => Model.NodeOutputSemantics.Assets;
		public override Model.NodeOutputSemantics NodeOutputType => Model.NodeOutputSemantics.Assets;

		private AddressableAssetSettings m_aaSettings;
		private ScriptableObject m_currentDataBuilder;

		private AddressableAssetSettings Settings
		{
			get
			{
				if (m_aaSettings == null)
				{
					m_aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
				}

				return m_aaSettings;
			}
		}
		
		public override void Initialize(Model.NodeData data)
		{
			m_builderGuid = string.Empty;
			m_profileName = string.Empty;
			m_preferUpdate = false;
			
			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData)
		{
			var newNode = new AddressableBuilder
			{
				m_contentStateFilePath = m_contentStateFilePath,
				m_builderGuid = m_builderGuid,
				m_preferUpdate = m_preferUpdate,
				m_profileName = m_profileName,
			};

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
			NodeGUIEditor editor, Action onValueChanged)
		{
			EditorGUILayout.HelpBox("Build Addressable Assets: Build Addressable Assets.",
				MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(4f);
			
			EditorGUILayout.HelpBox("Build Addressable Assets does not respect Platform selection in Graph Editor." +
			                        "Instead, Addressable Profile will be used for platform targeting.",
				MessageType.Info);
			
			if (!AddressableAssetSettingsDefaultObject.SettingsExists)
			{
				return;
			}
			
			var profileNames = Settings.profileSettings.GetAllProfileNames();
			profileNames.Insert(0, kCurrentProfile);
			var profileIndex = string.IsNullOrEmpty(m_profileName) ? 0 : profileNames.IndexOf(m_profileName);
			var newProfileIndex = EditorGUILayout.Popup("Profile", profileIndex, profileNames.ToArray());
			if (newProfileIndex != profileIndex)
			{
				using (new RecordUndoScope("Change Profile", node, true))
				{
					m_profileName = profileNames[newProfileIndex];
					onValueChanged();
				}
			}

			if (m_currentDataBuilder == null && !string.IsNullOrEmpty(m_builderGuid))
			{
				m_currentDataBuilder = Settings.DataBuilders.FirstOrDefault(obj => 
					AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)) == m_builderGuid);
			}

			var dataBuilders = Settings.DataBuilders.Where(builder => (builder as IDataBuilder).CanBuildData<AddressablesPlayerBuildResult>()).ToList();

			var builderIndex = m_currentDataBuilder == null ? -1 : dataBuilders.IndexOf(m_currentDataBuilder);
			var builderNames = dataBuilders
				.Select(builder => ((IDataBuilder) builder).Name).ToArray();
			
			var newIndex = EditorGUILayout.Popup("Builder Script", builderIndex, builderNames);
			if (newIndex != builderIndex)
			{
				using (new RecordUndoScope("Change Builder", node, true))
				{
					m_currentDataBuilder = dataBuilders[newIndex];
					m_builderGuid =
						AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_currentDataBuilder));
					onValueChanged();
				}
			}

			var newPreferUpdate = EditorGUILayout.ToggleLeft("Update", m_preferUpdate);
			if (newPreferUpdate != m_preferUpdate)
			{
				using (new RecordUndoScope("Update Toggle", node, true))
				{
					m_preferUpdate = newPreferUpdate;
					onValueChanged();
				}
			}

			using (new EditorGUI.DisabledScope(!m_preferUpdate))
			{
				GUILayout.Label("Content State File", "BoldLabel");
				using(new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.SelectableLabel(m_contentStateFilePath, EditorStyles.textField, 
						GUILayout.Height(EditorGUIUtility.singleLineHeight));
					if(GUILayout.Button("Select", GUILayout.Width(50f))) {
						var newStateData = ContentUpdateScript.GetContentStateDataPath(true);
						if (newStateData != null && newStateData != m_contentStateFilePath)
						{
							using (new RecordUndoScope("Content State Data Path", node, true))
							{
								m_contentStateFilePath = newStateData;
								onValueChanged();
							}
						}
					}
				}
			}

			GUILayout.Space(10f);
			
			if(GUILayout.Button("Clean Build Cache", GUILayout.Width(250f)))
			{
				if (EditorUtility.DisplayDialog("Clean Build Cache", 
					"Do you really want to clean build cache?", 
					"OK", "Cancel"))
				{
					AddressableAssetSettings.CleanPlayerContent(null);
					BuildCache.PurgeCache(false);
				}
			}
		}

		public override void Prepare(BuildTarget target,
			Model.NodeData node,
			IEnumerable<PerformGraph.AssetGroups> incoming,
			IEnumerable<Model.ConnectionData> connectionsToOutput,
			PerformGraph.Output Output)
		{
			if (!AddressableAssetSettingsDefaultObject.SettingsExists)
			{
				throw new NodeException("Addressable Asset Settings not found.",
					"Create Addressable Asset Settings object from Addressables window.");
			}

			if (string.IsNullOrEmpty(m_builderGuid))
			{
				throw new NodeException("Builder Script not selected.",
					"Please select Builder Script from Inspector.");
			}
		
			m_currentDataBuilder = Settings.DataBuilders.FirstOrDefault(obj => 
				AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)) == m_builderGuid);

			if (m_currentDataBuilder == null)
			{
				throw new NodeException("Selected Builder Script is missing.",
					"Please select a valid Builder Script from Inspector.");
			}
			

			// AddressableBuilder does not add, filter or change structure of group, so just pass given group of assets
			if (Output != null)
			{
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())
					? null
					: connectionsToOutput.First();

				if (incoming != null)
				{
					foreach (var ag in incoming)
					{
						Output(dst, ag.assetGroups);
					}
				}
				else
				{
					Output(dst, new Dictionary<string, List<AssetReference>>());
				}
			}
		}

		public override void Build(BuildTarget target,
			Model.NodeData node,
			IEnumerable<PerformGraph.AssetGroups> incoming,
			IEnumerable<Model.ConnectionData> connectionsToOutput,
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc)
		{
			var updatePerformed = false;

			if (!string.IsNullOrEmpty(m_profileName) && m_profileName != kCurrentProfile)
			{
				Settings.activeProfileId = AddressableAssetSettingsDefaultObject.Settings.profileSettings.GetProfileId(m_profileName);
				//AddressableAssetSettingsDefaultObject.Settings.activeProfileId = AddressableAssetSettingsDefaultObject.Settings.profileSettings.GetProfileId(m_profileName);
			}
			
			if (m_preferUpdate)
			{
				if (string.IsNullOrEmpty(m_contentStateFilePath) || !File.Exists(m_contentStateFilePath))
				{
					m_contentStateFilePath = ContentUpdateScript.GetContentStateDataPath(false);
				}
				
				if (!string.IsNullOrEmpty(m_contentStateFilePath) && File.Exists(m_contentStateFilePath))
				{
					ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, m_contentStateFilePath);
					updatePerformed = true;
				}
			}

			if (!updatePerformed)
			{
				var index = Settings.DataBuilders.IndexOf(m_currentDataBuilder);
			
				AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilderIndex = index;
				AddressableAssetSettings.BuildPlayerContent();
			}
			
			// AddressableBuilder does not add, filter or change structure of group, so just pass given group of assets
			if (Output != null)
			{
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())
					? null
					: connectionsToOutput.First();

				if (incoming != null)
				{
					foreach (var ag in incoming)
					{
						Output(dst, ag.assetGroups);
					}
				}
				else
				{
					Output(dst, new Dictionary<string, List<AssetReference>>());
				}
			}
		}
	}
}

#endif
