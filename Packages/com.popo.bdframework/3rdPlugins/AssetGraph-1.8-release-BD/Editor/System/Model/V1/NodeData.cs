using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AssetGraph;

namespace AssetBundleGraph {

	public enum NodeKind : int {
		LOADER_GUI,
		FILTER_GUI,
		IMPORTSETTING_GUI,
		MODIFIER_GUI,

		GROUPING_GUI,
		PREFABBUILDER_GUI,
		BUNDLECONFIG_GUI,
		BUNDLEBUILDER_GUI,

		EXPORTER_GUI
	}

	public enum ExporterExportOption : int {
		ErrorIfNoExportDirectoryFound,
		AutomaticallyCreateIfNoExportDirectoryFound,
		DeleteAndRecreateExportDirectory
	}

	[Serializable]
	public class FilterEntry {
		[SerializeField] private string m_filterKeyword;
		[SerializeField] private string m_filterKeytype;
		[SerializeField] private ConnectionPointData m_point; // deprecated. it is here for compatibility
		[SerializeField] private string m_pointId;

		public FilterEntry(string keyword, string keytype, ConnectionPointData point) {
			m_filterKeyword = keyword;
			m_filterKeytype = keytype;
			m_point = null;
			m_pointId = point.Id;
		}

		public string FilterKeyword {
			get {
				return m_filterKeyword;
			}
			set {
				m_filterKeyword = value;
			}
		}
		public string FilterKeytype {
			get {
				return m_filterKeytype; 
			}
			set {
				m_filterKeytype = value;
			}
		}
		public string ConnectionPointId {
			get {
				if(m_pointId == null && m_point != null) {
					m_pointId = m_point.Id;
				}
				return m_pointId; 
			}
		}
		public string Hash {
			get {
				return m_filterKeyword+m_filterKeytype;
			}
		}
	}

	[Serializable]
	public class Variant {
		[SerializeField] private string m_name;
		[SerializeField] private ConnectionPointData m_point; // deprecated. it is here for compatibility
		[SerializeField] private string m_pointId;

		public Variant(string name, ConnectionPointData point) {
			m_name = name;
			m_point = null;
			m_pointId = point.Id;
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
				if(m_pointId == null && m_point != null) {
					m_pointId = m_point.Id;
				}
				return m_pointId; 
			}
		}
	}

	/*
	 * node data saved in/to Json
	 */
	[Serializable]
	public class NodeData {

		private const string NODE_NAME = "name";
		private const string NODE_ID = "id";
		private const string NODE_KIND = "kind";
		private const string NODE_POS = "pos";
		private const string NODE_POS_X = "x";
		private const string NODE_POS_Y = "y";

		private const string NODE_INPUTPOINTS = "inputPoints";
		private const string NODE_OUTPUTPOINTS = "outputPoints";

		//loader settings
		private const string NODE_LOADER_LOAD_PATH = "loadPath";

		//exporter settings
		private const string NODE_EXPORTER_EXPORT_PATH = "exportTo";
		private const string NODE_EXPORTER_EXPORT_OPTION = "exportOption";

		//filter settings
		private const string NODE_FILTER = "filter";
		private const string NODE_FILTER_KEYWORD = "keyword";
		private const string NODE_FILTER_KEYTYPE = "keytype";
		private const string NODE_FILTER_POINTID = "pointId";

		//group settings
		private const string NODE_GROUPING_KEYWORD = "groupingKeyword";

		//mofidier/prefabBuilder settings
		private const string NODE_SCRIPT_CLASSNAME 		= "scriptClassName";
		private const string NODE_SCRIPT_INSTANCE_DATA  = "scriptInstanceData";

		//bundleconfig settings
		private const string NODE_BUNDLECONFIG_BUNDLENAME_TEMPLATE = "bundleNameTemplate";
		private const string NODE_BUNDLECONFIG_VARIANTS 		 = "variants";
		private const string NODE_BUNDLECONFIG_VARIANTS_NAME 	 = "name";
		private const string NODE_BUNDLECONFIG_VARIANTS_POINTID = "pointId";
		private const string NODE_BUNDLECONFIG_USE_GROUPASVARIANTS = "useGroupAsVariants";

		//bundlebuilder settings
		private const string NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS = "enabledBundleOptions";

		//prefabbuilder settings
		private const string NODE_PREFABBUILDER_REPLACEPREFABOPTIONS = "replacePrefabOptions";

		[SerializeField] private string m_name;
		[SerializeField] private string m_id;
		[SerializeField] private NodeKind m_kind;
		[SerializeField] private float m_x;
		[SerializeField] private float m_y;
		[SerializeField] private string m_scriptClassName;
		[SerializeField] private List<FilterEntry> m_filter;
		[SerializeField] private List<ConnectionPointData> 	m_inputPoints; 
		[SerializeField] private List<ConnectionPointData> 	m_outputPoints;
		[SerializeField] private SerializableMultiTargetString m_loaderLoadPath;
		[SerializeField] private SerializableMultiTargetString m_exporterExportPath;
		[SerializeField] private SerializableMultiTargetString m_groupingKeyword;
		[SerializeField] private SerializableMultiTargetString m_bundleConfigBundleNameTemplate;
		[SerializeField] private SerializableMultiTargetString m_scriptInstanceData;
		[SerializeField] private List<Variant> m_variants;
		[SerializeField] private bool m_bundleConfigUseGroupAsVariants;
		[SerializeField] private SerializableMultiTargetInt m_bundleBuilderEnabledBundleOptions;
		[SerializeField] private SerializableMultiTargetInt m_exporterExportOption;
		[SerializeField] private int m_prefabBuilderReplacePrefabOptions;

		private bool m_nodeNeedsRevisit;

		/*
		 * Properties
		 */ 

		public bool NeedsRevisit {
			get {
				return m_nodeNeedsRevisit;
			}
			set {
				m_nodeNeedsRevisit = value;
			}
		}

		public string Name {
			get {
				return m_name;
			}
			set {
				m_name = value;
			}
		}
		public string Id {
			get {
				return m_id;
			}
		}
		public NodeKind Kind {
			get {
				return m_kind;
			}
		}
		public string ScriptClassName {
			get {
				return m_scriptClassName;
			}
			set {
				m_scriptClassName = value;
			}
		}

		public float X {
			get {
				return m_x;
			}
			set {
				m_x = value;
			}
		}

		public float Y {
			get {
				return m_y;
			}
			set {
				m_y = value;
			}
		}

		public List<ConnectionPointData> InputPoints {
			get {
				return m_inputPoints;
			}
		}

		public List<ConnectionPointData> OutputPoints {
			get {
				return m_outputPoints;
			}
		}

		public SerializableMultiTargetString LoaderLoadPath {
			get {
				ValidateAccess(
					NodeKind.LOADER_GUI 
				);
				return m_loaderLoadPath;
			}
		}

		public SerializableMultiTargetString ExporterExportPath {
			get {
				ValidateAccess(
					NodeKind.EXPORTER_GUI 
				);
				return m_exporterExportPath;
			}
		}

		public SerializableMultiTargetString GroupingKeywords {
			get {
				ValidateAccess(
					NodeKind.GROUPING_GUI 
				);
				return m_groupingKeyword;
			}
		}

		public SerializableMultiTargetString BundleNameTemplate {
			get {
				ValidateAccess(
					NodeKind.BUNDLECONFIG_GUI 
				);
				return m_bundleConfigBundleNameTemplate;
			}
		}

		public bool BundleConfigUseGroupAsVariants {
			get {
				ValidateAccess(
					NodeKind.BUNDLECONFIG_GUI 
				);
				return m_bundleConfigUseGroupAsVariants;
			}
			set {
				ValidateAccess(
					NodeKind.BUNDLECONFIG_GUI 
				);
				m_bundleConfigUseGroupAsVariants = value;
			}
		}

		public SerializableMultiTargetString InstanceData {
			get {
				ValidateAccess(
					NodeKind.PREFABBUILDER_GUI,
					NodeKind.MODIFIER_GUI
				);
				return m_scriptInstanceData;
			}
		}

		public List<Variant> Variants {
			get {
				ValidateAccess(
					NodeKind.BUNDLECONFIG_GUI 
				);
				return m_variants;
			}
		}

		public SerializableMultiTargetInt BundleBuilderBundleOptions {
			get {
				ValidateAccess(
					NodeKind.BUNDLEBUILDER_GUI 
				);
				return m_bundleBuilderEnabledBundleOptions;
			}
		}

		public SerializableMultiTargetInt ExporterExportOption {
			get {
				ValidateAccess(
					NodeKind.EXPORTER_GUI 
				);
				return m_exporterExportOption;
			}
		}

		public int ReplacePrefabOptions {
			get {
				ValidateAccess(
					NodeKind.PREFABBUILDER_GUI 
				);
				return m_prefabBuilderReplacePrefabOptions;
			}
			set {
				ValidateAccess(
					NodeKind.PREFABBUILDER_GUI 
				);
				m_prefabBuilderReplacePrefabOptions = value;
			}
		}

		public List<FilterEntry> FilterConditions {
			get {
				ValidateAccess(
					NodeKind.FILTER_GUI
				);
				return m_filter;
			}
		}

		private Dictionary<string, object> _SafeGet(Dictionary<string, object> jsonData, string key) {
			if(jsonData.ContainsKey(key)) {
				return jsonData[key] as Dictionary<string, object>;
			} else {
				return new Dictionary<string, object>();
			}
		}

		/*
		 *  Create NodeData from JSON
		 */ 
		public NodeData(Dictionary<string, object> jsonData) {
			FromJsonDictionary(jsonData);
		}

		public void FromJsonDictionary(Dictionary<string, object> jsonData) {
			m_name = jsonData[NODE_NAME] as string;
			m_id = jsonData[NODE_ID]as string;
			m_kind = SaveDataConstants.NodeKindFromString(jsonData[NODE_KIND] as string);
			m_scriptClassName = string.Empty;
			m_nodeNeedsRevisit = false;

			if(jsonData.ContainsKey(NODE_SCRIPT_CLASSNAME)) {
				m_scriptClassName = jsonData[NODE_SCRIPT_CLASSNAME] as string;
			}

			var pos = jsonData[NODE_POS] as Dictionary<string, object>;
			m_x = (float)Convert.ToDouble(pos[NODE_POS_X]);
			m_y = (float)Convert.ToDouble(pos[NODE_POS_Y]);

			var inputs  = jsonData[NODE_INPUTPOINTS] as List<object>;
			var outputs = jsonData[NODE_OUTPUTPOINTS] as List<object>;
			m_inputPoints  = new List<ConnectionPointData>();
			m_outputPoints = new List<ConnectionPointData>();

			foreach(var obj in inputs) {
				var pDic = obj as Dictionary<string, object>;
				m_inputPoints.Add(new ConnectionPointData(pDic, this, true));
			}

			foreach(var obj in outputs) {
				var pDic = obj as Dictionary<string, object>;
				m_outputPoints.Add(new ConnectionPointData(pDic, this, false));
			}

			switch (m_kind) {
			case NodeKind.IMPORTSETTING_GUI:
				// nothing to do
				break;
			case NodeKind.PREFABBUILDER_GUI:
				{
					if(jsonData.ContainsKey(NODE_PREFABBUILDER_REPLACEPREFABOPTIONS)) {
						m_prefabBuilderReplacePrefabOptions = Convert.ToInt32(jsonData[NODE_PREFABBUILDER_REPLACEPREFABOPTIONS]);
					}
					if(jsonData.ContainsKey(NODE_SCRIPT_INSTANCE_DATA)) {
						m_scriptInstanceData = new SerializableMultiTargetString(_SafeGet(jsonData, NODE_SCRIPT_INSTANCE_DATA));
					}
				}
				break;
			case NodeKind.MODIFIER_GUI:
				{
					if(jsonData.ContainsKey(NODE_SCRIPT_INSTANCE_DATA)) {
						m_scriptInstanceData = new SerializableMultiTargetString(_SafeGet(jsonData, NODE_SCRIPT_INSTANCE_DATA));
					}
				}
				break;
			case NodeKind.LOADER_GUI:
				{
					m_loaderLoadPath = new SerializableMultiTargetString(_SafeGet(jsonData, NODE_LOADER_LOAD_PATH));
				}
				break;
			case NodeKind.FILTER_GUI:
				{
					var filters = jsonData[NODE_FILTER] as List<object>;

					m_filter = new List<FilterEntry>();

					for(int i=0; i<filters.Count; ++i) {
						var f = filters[i] as Dictionary<string, object>;

						var keyword = f[NODE_FILTER_KEYWORD] as string;
						var keytype = f[NODE_FILTER_KEYTYPE] as string;
						var pointId = f[NODE_FILTER_POINTID] as string;

						var point = m_outputPoints.Find(p => p.Id == pointId);
						UnityEngine.Assertions.Assert.IsNotNull(point, "Output point not found for " + keyword);
						var newEntry = new FilterEntry(keyword, keytype, point);
						m_filter.Add(newEntry);
						UpdateFilterEntry(newEntry);
					}
				}
				break;
			case NodeKind.GROUPING_GUI:
				{
					m_groupingKeyword = new SerializableMultiTargetString(_SafeGet(jsonData, NODE_GROUPING_KEYWORD));
				}
				break;
			case NodeKind.BUNDLECONFIG_GUI:
				{
					m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(_SafeGet(jsonData, NODE_BUNDLECONFIG_BUNDLENAME_TEMPLATE));
					if(jsonData.ContainsKey(NODE_BUNDLECONFIG_USE_GROUPASVARIANTS)) {
						m_bundleConfigUseGroupAsVariants = Convert.ToBoolean(jsonData[NODE_BUNDLECONFIG_USE_GROUPASVARIANTS]);
					}
					m_variants = new List<Variant>();
					if(jsonData.ContainsKey(NODE_BUNDLECONFIG_VARIANTS)){
						var variants = jsonData[NODE_BUNDLECONFIG_VARIANTS] as List<object>;

						for(int i=0; i<variants.Count; ++i) {
							var v = variants[i] as Dictionary<string, object>;

							var name    = v[NODE_BUNDLECONFIG_VARIANTS_NAME] as string;
							var pointId = v[NODE_BUNDLECONFIG_VARIANTS_POINTID] as string;

							var point = m_inputPoints.Find(p => p.Id == pointId);
							UnityEngine.Assertions.Assert.IsNotNull(point, "Input point not found for " + name);
							var newVariant = new Variant(name, point);
							m_variants.Add(newVariant);
							UpdateVariant(newVariant);
						}
					}
				}
				break;
			case NodeKind.BUNDLEBUILDER_GUI:
				{
					m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt(_SafeGet(jsonData, NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS));
				}
				break;
			case NodeKind.EXPORTER_GUI:
				{
					m_exporterExportPath = new SerializableMultiTargetString(_SafeGet(jsonData, NODE_EXPORTER_EXPORT_PATH));
					m_exporterExportOption = new SerializableMultiTargetInt(_SafeGet(jsonData, NODE_EXPORTER_EXPORT_OPTION));
				}
				break;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		/*
		 * Constructor used to create new node from GUI
		 */ 
		public NodeData(string name, NodeKind kind, float x, float y) {

			m_id = Guid.NewGuid().ToString();
			m_name = name;
			m_x = x;
			m_y = y;
			m_kind = kind;
			m_nodeNeedsRevisit = false;
			m_scriptClassName = String.Empty;

			m_inputPoints  = new List<ConnectionPointData>();
			m_outputPoints = new List<ConnectionPointData>();

			// adding defalut input point.
			// Loader does not take input
			if(kind != NodeKind.LOADER_GUI) {
				m_inputPoints.Add(new ConnectionPointData(SaveDataConstants.DEFAULT_INPUTPOINT_LABEL, this, true));
			}

			// adding default output point.
			// Filter and Exporter does not have output.
			if(kind != NodeKind.FILTER_GUI && kind != NodeKind.EXPORTER_GUI) {
				m_outputPoints.Add(new ConnectionPointData(SaveDataConstants.DEFAULT_OUTPUTPOINT_LABEL, this, false));
			}

			switch(m_kind) {
			case NodeKind.PREFABBUILDER_GUI:
				m_prefabBuilderReplacePrefabOptions = 0;
				m_scriptInstanceData = new SerializableMultiTargetString();
				break;

			case NodeKind.MODIFIER_GUI:
				m_scriptInstanceData = new SerializableMultiTargetString();
				break;
			
			case NodeKind.IMPORTSETTING_GUI:
				break;

			case NodeKind.FILTER_GUI:
				m_filter = new List<FilterEntry>();
				break;

			case NodeKind.LOADER_GUI:
				m_loaderLoadPath = new SerializableMultiTargetString();
				break;

			case NodeKind.GROUPING_GUI:
				m_groupingKeyword = new SerializableMultiTargetString(SaveDataConstants.GROUPING_KEYWORD_DEFAULT);
				break;

			case NodeKind.BUNDLECONFIG_GUI:
				m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(SaveDataConstants.BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT);
				m_bundleConfigUseGroupAsVariants = false;
				m_variants = new List<Variant>();
				break;

			case NodeKind.BUNDLEBUILDER_GUI:
				m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt();
				break;

			case NodeKind.EXPORTER_GUI:
				m_exporterExportPath = new SerializableMultiTargetString();
				m_exporterExportOption = new SerializableMultiTargetInt();
				break;

			default:
				throw new AssetGraphException("[FATAL]Unhandled nodekind. unimplmented:"+ m_kind);
			}
		}

		/**
		 * Duplicate this node with new guid.
		 */ 
		public NodeData Duplicate (bool keepGuid = false) {

			if(keepGuid) {
				return new NodeData( this.ToJsonDictionary() );
			}

			var newData = new NodeData(m_name, m_kind, m_x, m_y);
			newData.m_nodeNeedsRevisit = false;
			newData.m_scriptClassName = m_scriptClassName;

			switch(m_kind) {
			case NodeKind.IMPORTSETTING_GUI:
				break;
			case NodeKind.PREFABBUILDER_GUI:
				newData.m_prefabBuilderReplacePrefabOptions = m_prefabBuilderReplacePrefabOptions;
				newData.m_scriptInstanceData = new SerializableMultiTargetString(m_scriptInstanceData);
				break;

			case NodeKind.MODIFIER_GUI:
				newData.m_scriptInstanceData = new SerializableMultiTargetString(m_scriptInstanceData);
				break;

			case NodeKind.FILTER_GUI:
				foreach(var f in m_filter) {
					newData.AddFilterCondition(f.FilterKeyword, f.FilterKeytype);
				}
				break;

			case NodeKind.LOADER_GUI:
				newData.m_loaderLoadPath = new SerializableMultiTargetString(m_loaderLoadPath);
				break;

			case NodeKind.GROUPING_GUI:
				newData.m_groupingKeyword = new SerializableMultiTargetString(m_groupingKeyword);
				break;

			case NodeKind.BUNDLECONFIG_GUI:
				newData.m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(m_bundleConfigBundleNameTemplate);
				newData.m_bundleConfigUseGroupAsVariants = m_bundleConfigUseGroupAsVariants;
				foreach(var v in m_variants) {
					newData.AddVariant(v.Name);
				}
				break;

			case NodeKind.BUNDLEBUILDER_GUI:
				newData.m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt(m_bundleBuilderEnabledBundleOptions);
				break;

			case NodeKind.EXPORTER_GUI:
				newData.m_exporterExportPath = new SerializableMultiTargetString(m_exporterExportPath);
				newData.m_exporterExportOption = new SerializableMultiTargetInt(m_exporterExportOption);
				break;

			default:
				throw new AssetGraphException("[FATAL]Unhandled nodekind. unimplmented:"+ m_kind);
			}

			return newData;
		}

		public ConnectionPointData AddInputPoint(string label) {
			var p = new ConnectionPointData(label, this, true);
			m_inputPoints.Add(p);
			return p;
		}

		public ConnectionPointData AddOutputPoint(string label) {
			var p = new ConnectionPointData(label, this, false);
			m_outputPoints.Add(p);
			return p;
		}

		public ConnectionPointData FindInputPoint(string id) {
			return m_inputPoints.Find(p => p.Id == id);
		}

		public ConnectionPointData FindOutputPoint(string id) {
			return m_outputPoints.Find(p => p.Id == id);
		}

		public ConnectionPointData FindConnectionPoint(string id) {
			var v = FindInputPoint(id);
			if(v != null) {
				return v;
			}
			return FindOutputPoint(id);
		}

		public string GetLoaderFullLoadPath(BuildTarget g) {
			return FileUtility.PathCombine(Application.dataPath, LoaderLoadPath[g]);
		}

		public bool ValidateOverlappingFilterCondition(bool throwException) {
			ValidateAccess(NodeKind.FILTER_GUI);

			var conditionGroup = FilterConditions.Select(v => v).GroupBy(v => v.Hash).ToList();
			var overlap = conditionGroup.Find(v => v.Count() > 1);

			if( overlap != null && throwException ) {
				var element = overlap.First();
                throw new AssetGraphException(
	                $"Duplicated filter condition found for [Keyword:{element.FilterKeyword} Type:{element.FilterKeytype}]");
			}
			return overlap != null;
		}

		public void AddFilterCondition(string keyword, string keytype) {
			ValidateAccess(
				NodeKind.FILTER_GUI
			);

			var point = new ConnectionPointData(keyword, this, false);
			m_outputPoints.Add(point);
			var newEntry = new FilterEntry(keyword, keytype, point);
			m_filter.Add(newEntry);
			UpdateFilterEntry(newEntry);
		}

		public void RemoveFilterCondition(FilterEntry f) {
			ValidateAccess(
				NodeKind.FILTER_GUI
			);

			m_filter.Remove(f);
			m_outputPoints.Remove(GetConnectionPoint(f));
		}

		public ConnectionPointData GetConnectionPoint(FilterEntry f) {
			ConnectionPointData p = m_outputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		public void UpdateFilterEntry(FilterEntry f) {

			ConnectionPointData p = m_outputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			if(f.FilterKeytype == SaveDataConstants.DEFAULT_FILTER_KEYTYPE) {
				p.Label = f.FilterKeyword;
			} else {
				var pointIndex = f.FilterKeytype.LastIndexOf('.');
				var keytypeName = (pointIndex > 0)? f.FilterKeytype.Substring(pointIndex+1):f.FilterKeytype;
				p.Label = $"{f.FilterKeyword}[{keytypeName}]";
			}
		}

		public void AddVariant(string name) {
			ValidateAccess(
				NodeKind.BUNDLECONFIG_GUI
			);

			name = name.ToLower();

			var point = new ConnectionPointData(name, this, true);
			m_inputPoints.Add(point);
			var newEntry = new Variant(name, point);
			m_variants.Add(newEntry);
			UpdateVariant(newEntry);
		}

		public void RemoveVariant(Variant v) {
			ValidateAccess(
				NodeKind.BUNDLECONFIG_GUI
			);

			m_variants.Remove(v);
			m_inputPoints.Remove(GetConnectionPoint(v));
		}

		public ConnectionPointData GetConnectionPoint(Variant v) {
			ConnectionPointData p = m_inputPoints.Find(point => point.Id == v.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		public void UpdateVariant(Variant variant) {

			ConnectionPointData p = m_inputPoints.Find(v => v.Id == variant.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			p.Label = variant.Name;
		}

		private void ValidateAccess(params NodeKind[] allowedKind) {
			foreach(var k in allowedKind) {
				if (k == m_kind) {
					return;
				}
			}
			throw new AssetGraphException(m_name + ": Tried to access invalid method or property.");
		}

		public bool Validate (List<NodeData> allNodes, List<ConnectionData> allConnections) {

			switch(m_kind) {
			case NodeKind.BUNDLEBUILDER_GUI:
				{
					foreach(var v in m_bundleBuilderEnabledBundleOptions.Values) {
						bool isDisableWriteTypeTreeEnabled  = 0 < (v.value & (int)BuildAssetBundleOptions.DisableWriteTypeTree);
						bool isIgnoreTypeTreeChangesEnabled = 0 < (v.value & (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges);

						// If both are marked something is wrong. Clear both flag and save.
						if(isDisableWriteTypeTreeEnabled && isIgnoreTypeTreeChangesEnabled) {
							int flag = ~((int)BuildAssetBundleOptions.DisableWriteTypeTree + (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges);
							v.value = v.value & flag;
							LogUtility.Logger.LogWarning(LogUtility.kTag, m_name + ": DisableWriteTypeTree and IgnoreTypeTreeChanges can not be used together. Settings overwritten.");
						}
					}
				}
				break;
			}

			return true;
		}

		public bool CompareIgnoreGUIChanges (NodeData rhs) {

			if(this.m_kind != rhs.m_kind) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Kind");
				return false;
			}

			if(m_scriptClassName != rhs.m_scriptClassName) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Script classname different");
				return false;
			}

			if(m_inputPoints.Count != rhs.m_inputPoints.Count) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Input Count");
				return false;
			}

			if(m_outputPoints.Count != rhs.m_outputPoints.Count) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Output Count");
				return false;
			}

			foreach(var pin in m_inputPoints) {
				if(rhs.m_inputPoints.Find(x => pin.Id == x.Id) == null) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Input point not found");
					return false;
				}
			}

			foreach(var pout in m_outputPoints) {
				if(rhs.m_outputPoints.Find(x => pout.Id == x.Id) == null) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Output point not found");
					return false;
				}
			}

			switch (m_kind) {
			case NodeKind.PREFABBUILDER_GUI:
				if(m_prefabBuilderReplacePrefabOptions != rhs.m_prefabBuilderReplacePrefabOptions) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "ReplacePrefabOptions different");
					return false;
				}
				if(m_scriptInstanceData != rhs.m_scriptInstanceData) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Script instance data different");
					return false;
				}
				break;

			case NodeKind.MODIFIER_GUI:
				if(m_scriptInstanceData != rhs.m_scriptInstanceData) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Script instance data different");
					return false;
				}
				break;

			case NodeKind.LOADER_GUI:
				if(m_loaderLoadPath != rhs.m_loaderLoadPath) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Loader load path different");
					return false;
				}
				break;

			case NodeKind.FILTER_GUI:
				foreach(var f in m_filter) {
					if(null == rhs.m_filter.Find(x => x.FilterKeytype == f.FilterKeytype && x.FilterKeyword == f.FilterKeyword)) {
						LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Filter entry not found");
						return false;
					}
				}
				break;

			case NodeKind.GROUPING_GUI:
				if(m_groupingKeyword != rhs.m_groupingKeyword) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Grouping keyword different");
					return false;
				}
				break;

			case NodeKind.BUNDLECONFIG_GUI:
				if(m_bundleConfigBundleNameTemplate != rhs.m_bundleConfigBundleNameTemplate) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "BundleNameTemplate different");
					return false;
				}
				if(m_bundleConfigUseGroupAsVariants != rhs.m_bundleConfigUseGroupAsVariants) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "UseGroupAsVariants different");
					return false;
				}
				foreach(var v in m_variants) {
					if(null == rhs.m_variants.Find(x => x.Name == v.Name && x.ConnectionPointId == v.ConnectionPointId)) {
						LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Variants not found");
						return false;
					}
				}
				break;

			case NodeKind.BUNDLEBUILDER_GUI:
				if(m_bundleBuilderEnabledBundleOptions != rhs.m_bundleBuilderEnabledBundleOptions) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "EnabledBundleOptions different");
					return false;
				}
				break;

			case NodeKind.EXPORTER_GUI:
				if(m_exporterExportPath != rhs.m_exporterExportPath) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "ExporterPath different");
					return false;
				}
				if(m_exporterExportOption != rhs.m_exporterExportOption) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "ExporterOption different");
					return false;
				}
				break;

			case NodeKind.IMPORTSETTING_GUI:
				// nothing to do
				break;

			default:
				throw new ArgumentOutOfRangeException ();
			}

			return true;
		}


		/**
		 * Serialize to JSON dictionary
		 */ 
		public Dictionary<string, object> ToJsonDictionary() {
			var nodeDict = new Dictionary<string, object>();

			nodeDict[NODE_NAME] = m_name;
			nodeDict[NODE_ID] 	= m_id;
			nodeDict[NODE_KIND] = m_kind.ToString();

			if(!string.IsNullOrEmpty(m_scriptClassName)) {
				nodeDict[NODE_SCRIPT_CLASSNAME] = m_scriptClassName;
			}

			var inputs  = new List<object>();
			var outputs = new List<object>();

			foreach(var p in m_inputPoints) {
				inputs.Add( p.ToJsonDictionary() );
			}

			foreach(var p in m_outputPoints) {
				outputs.Add( p.ToJsonDictionary() );
			}

			nodeDict[NODE_INPUTPOINTS]  = inputs;
			nodeDict[NODE_OUTPUTPOINTS] = outputs;

			nodeDict[NODE_POS] = new Dictionary<string, object>() {
				{NODE_POS_X, m_x},
				{NODE_POS_Y, m_y}
			};
				
			switch (m_kind) {
			case NodeKind.PREFABBUILDER_GUI:
				nodeDict[NODE_PREFABBUILDER_REPLACEPREFABOPTIONS] = m_prefabBuilderReplacePrefabOptions;
				nodeDict[NODE_SCRIPT_INSTANCE_DATA] = m_scriptInstanceData.ToJsonDictionary();
				break;

			case NodeKind.MODIFIER_GUI:
				nodeDict[NODE_SCRIPT_INSTANCE_DATA] = m_scriptInstanceData.ToJsonDictionary();
				break;

			case NodeKind.LOADER_GUI:
				nodeDict[NODE_LOADER_LOAD_PATH] = m_loaderLoadPath.ToJsonDictionary();
				break;

			case NodeKind.FILTER_GUI:
				var filterDict = new List<object>();
				foreach(var f in m_filter) {
					var df = new Dictionary<string, object>();
					df[NODE_FILTER_KEYWORD] = f.FilterKeyword;
					df[NODE_FILTER_KEYTYPE] = f.FilterKeytype;
					df[NODE_FILTER_POINTID] = f.ConnectionPointId;
					filterDict.Add(df);
				}
				nodeDict[NODE_FILTER] = filterDict;
				break;

			case NodeKind.GROUPING_GUI:
				nodeDict[NODE_GROUPING_KEYWORD] = m_groupingKeyword.ToJsonDictionary();
				break;

			case NodeKind.BUNDLECONFIG_GUI:
				nodeDict[NODE_BUNDLECONFIG_BUNDLENAME_TEMPLATE] = m_bundleConfigBundleNameTemplate.ToJsonDictionary();
				nodeDict[NODE_BUNDLECONFIG_USE_GROUPASVARIANTS] = m_bundleConfigUseGroupAsVariants;
				var variantsDict = new List<object>();
				foreach(var v in m_variants) {
					var dv = new Dictionary<string, object>();
					dv[NODE_BUNDLECONFIG_VARIANTS_NAME] 	= v.Name;
					dv[NODE_BUNDLECONFIG_VARIANTS_POINTID] = v.ConnectionPointId;
					variantsDict.Add(dv);
				}
				nodeDict[NODE_BUNDLECONFIG_VARIANTS] = variantsDict;
				break;

			case NodeKind.BUNDLEBUILDER_GUI:
				nodeDict[NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] = m_bundleBuilderEnabledBundleOptions.ToJsonDictionary();
				break;

			case NodeKind.EXPORTER_GUI:
				nodeDict[NODE_EXPORTER_EXPORT_PATH] = m_exporterExportPath.ToJsonDictionary();
				nodeDict[NODE_EXPORTER_EXPORT_OPTION] = m_exporterExportOption.ToJsonDictionary();
				break;

			case NodeKind.IMPORTSETTING_GUI:
				// nothing to do
				break;

			default:
				throw new ArgumentOutOfRangeException ();
			}

			return nodeDict;
		}

		/**
		 * Serialize to JSON string
		 */ 
		public string ToJsonString() {
			return AssetBundleGraph.Json.Serialize(ToJsonDictionary());
		}
	}
}
