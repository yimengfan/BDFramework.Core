using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Configure Bundle/Configure Bundle From Group", 70)]
	public class BundleConfigurator : Node, Model.NodeDataImporter {

		[Serializable]
		public class Variant {
			[SerializeField] private string m_name;
			[SerializeField] private string m_pointId;

			public Variant(string name, Model.ConnectionPointData point) {
				m_name = name;
				m_pointId = point.Id;
			}
			public Variant(Variant v) {
				m_name = v.m_name;
				m_pointId = v.m_pointId;
			}

			public string Name {
				get {
					return m_name;
				}
				set {
					m_name = value;
				}
			}
			public string ConnectionPointId {
				get {
					return m_pointId; 
				}
			}
		}

		[SerializeField] private SerializableMultiTargetString m_bundleNameTemplate;
		[SerializeField] private List<Variant> m_variants;
		[SerializeField] private bool m_useGroupAsVariants;

		public override string ActiveStyle {
			get {
				return "node 3 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 3";
			}
		}

		public override string Category {
			get {
				return "Configure";
			}
		}

		public override Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.AssetBundleConfigurations;
			}
		}

		public override void Initialize(Model.NodeData data) {
			
			m_bundleNameTemplate = new SerializableMultiTargetString(Model.Settings.BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT);
			m_useGroupAsVariants = false;
			m_variants = new List<Variant>();

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {

			m_bundleNameTemplate = new SerializableMultiTargetString(v1.BundleNameTemplate);
			m_useGroupAsVariants = v1.BundleConfigUseGroupAsVariants;

			foreach(var v in v1.Variants) {
				m_variants.Add(new Variant(v.Name, v2.FindInputPoint(v.ConnectionPointId)));
			}
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new BundleConfigurator();
			newNode.m_bundleNameTemplate = new SerializableMultiTargetString(m_bundleNameTemplate);
			newNode.m_variants = new List<Variant>(m_variants.Count);
			newNode.m_useGroupAsVariants = m_useGroupAsVariants;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();

			foreach(var v in m_variants) {
				newNode.AddVariant(newData, v.Name);
			}

			return newNode;
		}

		public override bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			if(!m_useGroupAsVariants) {
				if(m_variants.Count > 0 && m_variants.Find(v => v.ConnectionPointId == point.Id) == null) 
				{
					return false;
				}
			}
			return true;
		}

		private void AddVariant(Model.NodeData n, string name) {
			var p = n.AddInputPoint(name);
			var newEntry = new Variant(name, p);
			m_variants.Add(newEntry);
			UpdateVariant(n, newEntry);
		}

		private void RemoveVariant(Model.NodeData n, Variant v) {
			m_variants.Remove(v);
			n.InputPoints.Remove(GetConnectionPoint(n, v));
		}

		private Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, Variant v) {
			Model.ConnectionPointData p = n.InputPoints.Find(point => point.Id == v.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		private void UpdateVariant(Model.NodeData n,Variant variant) {

			Model.ConnectionPointData p = n.InputPoints.Find(v => v.Id == variant.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			p.Label = variant.Name;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

			if (m_bundleNameTemplate == null) return;

			EditorGUILayout.HelpBox("Configure Bundle From Group: Create asset bundle settings from incoming group of assets.", MessageType.Info);
			inspector.UpdateNodeName(node);

			GUILayout.Space(10f);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				var newUseGroupAsVariantValue = GUILayout.Toggle(m_useGroupAsVariants, "Use input group as variants");
				if(newUseGroupAsVariantValue != m_useGroupAsVariants) {
					using(new RecordUndoScope("Change Bundle Config", node, true)){
						m_useGroupAsVariants = newUseGroupAsVariantValue;

						List<Variant> rv = new List<Variant>(m_variants);
						foreach(var v in rv) {
							NodeGUIUtility.NodeEventHandler(
								new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, GetConnectionPoint(node.Data, v)));
							RemoveVariant(node.Data, v);
						}
						onValueChanged();
					}
				}

				using (new EditorGUI.DisabledScope(newUseGroupAsVariantValue)) {
					GUILayout.Label("Variants:");
					var variantNames = m_variants.Select(v => v.Name).ToList();
					Variant removing = null;
					foreach (var v in m_variants) {
						using (new GUILayout.HorizontalScope()) {
							if (GUILayout.Button("-", GUILayout.Width(30))) {
								removing = v;
							}
							else {
								GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");
								Action makeStyleBold = () => {
									s.fontStyle = FontStyle.Bold;
									s.fontSize = 12;
								};

								ValidateVariantName(v.Name, variantNames, 
									makeStyleBold,
									makeStyleBold,
									makeStyleBold);

								var variantName = EditorGUILayout.TextField(v.Name, s);

								if (variantName != v.Name) {
									using(new RecordUndoScope("Change Variant Name", node, true)){
										v.Name = variantName;
										UpdateVariant(node.Data, v);
										onValueChanged();
									}
								}
							}
						}
					}
					if (GUILayout.Button("+")) {
						using(new RecordUndoScope("Add Variant", node, true)){
							if(m_variants.Count == 0) {
								NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_DELETE_ALL_CONNECTIONS_TO_POINT, node, Vector2.zero, node.Data.InputPoints[0]));
							}
							AddVariant(node.Data, Model.Settings.BUNDLECONFIG_VARIANTNAME_DEFAULT);
							onValueChanged();
						}
					}
					if(removing != null) {
						using(new RecordUndoScope("Remove Variant", node, true)){
							// event must raise to remove connection associated with point
							NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, GetConnectionPoint(node.Data, removing)));
							RemoveVariant(node.Data, removing);
							onValueChanged();
						}
					}
				}
			}

			//Show target configuration tab
			inspector.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = inspector.DrawOverrideTargetToggle(node, m_bundleNameTemplate.ContainsValueOf(inspector.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Bundle Name Template Setting", node, true)){
						if(enabled) {
							m_bundleNameTemplate[inspector.CurrentEditingGroup] = m_bundleNameTemplate.DefaultValue;
						} else {
							m_bundleNameTemplate.Remove(inspector.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
					var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", m_bundleNameTemplate[inspector.CurrentEditingGroup]).ToLower();

					if (bundleNameTemplate != m_bundleNameTemplate[inspector.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Bundle Name Template", node, true)){
							m_bundleNameTemplate[inspector.CurrentEditingGroup] = bundleNameTemplate;
							onValueChanged();
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
			int groupCount = 0;

			if(incoming != null) {
				var groupNames = new List<string>();
				foreach(var ag in incoming) {
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(!groupNames.Contains(groupKey)) {
							groupNames.Add(groupKey);
						}
					}
				}
				groupCount = groupNames.Count;
			}

			ValidateBundleNameTemplate(
				m_bundleNameTemplate[target],
				m_useGroupAsVariants,
				groupCount,
				() => {
					throw new NodeException("Bundle Name Template is empty.", 
                        "Set valid bundle name template from inspector.",node);
				},
				() => {
					throw new NodeException("Bundle Name Template can not contain '" + Model.Settings.KEYWORD_WILDCARD.ToString() 
                        + "' when group name is used for variants.", 
                        "Set valid bundle name without '" + Model.Settings.KEYWORD_WILDCARD.ToString() + "' from inspector.", node);
				},
				() => {
					throw new NodeException("Bundle Name Template must contain '" + Model.Settings.KEYWORD_WILDCARD.ToString() 
                        + "' when group name is not used for variants and expecting multiple incoming groups.", 
                        "Set valid bundle name without '" + Model.Settings.KEYWORD_WILDCARD.ToString() + "' from inspector.", node);
				}
			);

			var variantNames = m_variants.Select(v=>v.Name).ToList();
			foreach(var variant in m_variants) {
				ValidateVariantName(variant.Name, variantNames, 
					() => {
                        throw new NodeException("Variant name is empty.", "Set valid variant name from inspector.", node);
					},
					() => {
						throw new NodeException("Variant name cannot contain whitespace \"" + variant.Name + "\".", "Remove whitespace from variant name.", node);
					},
					() => {
						throw new NodeException("Variant name already exists \"" + variant.Name + "\".", "Avoid variant name collision.", node);
					});
			}


			if(incoming != null) {
				/**
				 * Check if incoming asset has valid import path
				 */
				var invalids = new List<AssetReference>();
				foreach(var ag in incoming) {
					foreach (var groupKey in ag.assetGroups.Keys) {
						ag.assetGroups[groupKey].ForEach( a => { if (string.IsNullOrEmpty(a.importFrom)) invalids.Add(a); } );
					}
				}
				if (invalids.Any()) {
					throw new NodeException(
						"Invalid files are found. Following files need to be imported to put into asset bundle: " + 
						string.Join(", ", invalids.Select(a =>a.absolutePath).ToArray()), "Import these files before building asset bundles.", node );
				}
			}

			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			if(incoming != null) {
				Dictionary<string, List<string>> variantsInfo = new Dictionary<string, List<string>> ();

				var buildMap = AssetBundleBuildMap.GetBuildMap ();
                buildMap.ClearFromId (node.Id);

				foreach(var ag in incoming) {
					string variantName = null;
					if(!m_useGroupAsVariants) {
						var currentVariant = m_variants.Find( v => v.ConnectionPointId == ag.connection.ToNodeConnectionPointId );
						variantName = (currentVariant == null) ? null : currentVariant.Name;
					}

					// set configured assets in bundle name
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(m_useGroupAsVariants) {
							variantName = groupKey;
						}
						var bundleName = GetBundleName(target, node, groupKey);
						var assets = ag.assetGroups[groupKey];

						ConfigureAssetBundleSettings(variantName, assets);

						if (!string.IsNullOrEmpty (variantName)) {
							if (!variantsInfo.ContainsKey (bundleName)) {
								variantsInfo [bundleName] = new List<string> ();
							}
							variantsInfo [bundleName].Add (variantName.ToLower());
						}

						if(output != null) {
							if(!output.ContainsKey(bundleName)) {
								output[bundleName] = new List<AssetReference>();
							} 
							output[bundleName].AddRange(assets);
						}

                        var bundleConfig = buildMap.GetAssetBundleWithNameAndVariant (node.Id, bundleName, variantName);
                        bundleConfig.AddAssets (node.Id, assets.Select(a => a.importFrom));
					}
				}

				if (output != null) {
					ValidateVariantsProperlyConfiguired (node, output, variantsInfo);
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		public void ConfigureAssetBundleSettings (string variantName, List<AssetReference> assets) {		

			foreach(var a in assets) {
				a.variantName = (string.IsNullOrEmpty(variantName))? null : variantName.ToLower();
			}
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, bool useGroupAsVariants, int groupCount,
			Action NullOrEmpty, 
			Action InvalidBundleNameTemplateForVariants, 
			Action InvalidBundleNameTemplateForNotVariants
		) {
			if (string.IsNullOrEmpty(bundleNameTemplate)){
				NullOrEmpty();
			}
			if(useGroupAsVariants && bundleNameTemplate.IndexOf(Model.Settings.KEYWORD_WILDCARD) >= 0) {
				InvalidBundleNameTemplateForVariants();
			}
			if(!useGroupAsVariants && bundleNameTemplate.IndexOf(Model.Settings.KEYWORD_WILDCARD) < 0 &&
				groupCount > 1) {
				InvalidBundleNameTemplateForNotVariants();
			}
		}

		public static void ValidateVariantName (string variantName, List<string> names, Action NullOrEmpty, Action ContainsSpace, Action NameAlreadyExists) {
			if (string.IsNullOrEmpty(variantName)) {
				NullOrEmpty();
			}
			if(Regex.IsMatch(variantName, "\\s")) {
				ContainsSpace();
			}
			var overlappings = names.GroupBy(x => x)
				.Where(group => 1 < group.Count())
				.Select(group => group.Key)
				.ToList();

			if (overlappings.Any()) {
				NameAlreadyExists();
			}
		}

		private void ValidateVariantsProperlyConfiguired(
			Model.NodeData node, 
			Dictionary<string, List<AssetReference>> output, Dictionary<string, List<string>> variantsInfo) 
		{
			foreach (var bundleName in output.Keys) {
				if (!variantsInfo.ContainsKey (bundleName)) {
					continue;
				}
				List<string> variants = variantsInfo [bundleName];

				if (variants.Count < 2) {
					throw new NodeException (bundleName + " is not configured to create more than 2 variants.", "Add another variant or remove variant to this bundle.", node);
				}

				List<AssetReference> assets = output [bundleName];

				List<AssetReference> variant0Assets = assets.Where (a => a.variantName == variants [0]).ToList ();

				for(int i = 1; i< variants.Count; ++i) {
					List<AssetReference> variantAssets = assets.Where (a => a.variantName == variants [i]).ToList ();
					if(variant0Assets.Count != variantAssets.Count) {
						throw new NodeException ("Variant mismatch found." + bundleName + " variant " + variants [0] + " and " + variants [i] + " do not match containing assets.", 
                            "Configure variants' content and make sure they all match."
                            ,node);
					}

					foreach (var a0 in variant0Assets) {
						if(!variantAssets.Any( a => a.fileName == a0.fileName)) {
							throw new NodeException ("Variant mismatch found." + bundleName + " does not contain " + a0.fileNameAndExtension + " in variant " + variants [i],
                                "Configure variants' content and make sure they all match.", node);
						}
					}
				}
			}
		}


		public string GetBundleName(BuildTarget target, Model.NodeData node, string groupKey) {
			var bundleName = m_bundleNameTemplate[target];

			if(m_useGroupAsVariants) {
				return bundleName.ToLower();
			} else {
				return bundleName.Replace(Model.Settings.KEYWORD_WILDCARD.ToString(), groupKey).ToLower();
			}
		}
	}
}