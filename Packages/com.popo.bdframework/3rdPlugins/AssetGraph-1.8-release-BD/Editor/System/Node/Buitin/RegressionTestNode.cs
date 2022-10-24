using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomNode("Test/Test Regression with Input", 8000)]
public class RegressionTestNode : Node {

	[SerializeField] private SerializableMultiTargetString m_result;

	private AssetGroupStructures m_current;

	public override string ActiveStyle {
		get {
			return "node 8 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 8";
		}
	}

	public override string Category {
		get {
			return "Test";
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

	[Serializable]
	private class AssetGroupStructures
	{
		public List<Structure> incoming;

		public AssetGroupStructures()
		{
			incoming = new List<Structure>();
		}
		
		[Serializable]
		public class Structure
		{
			public string connectionId;
			public List<Group> groups;

			public Structure(string cid)
			{
				connectionId = cid;
				groups = new List<Group>();
			}
		}
		
		[Serializable]
		public class Entry
		{
			public string importFrom;
			public string assetType;
			public string importerType;

			public Entry(string i, string a, string t)
			{
				importFrom = i;
				assetType = a;
				importerType = t;
			}
		}

		[Serializable]
		public class Group
		{
			public string name;
			public List<Entry> entires;

			public Group(string n)
			{
				name = n;
				entires = new List<Entry>();
			}
		}
	}

	public override void Initialize(Model.NodeData data) {
		m_result = new SerializableMultiTargetString();
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new RegressionTestNode();
		newNode.m_result = new SerializableMultiTargetString(m_result);
		newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged) {

		EditorGUILayout.HelpBox("My Custom Node: Implement your own Inspector.", MessageType.Info);
		inspector.UpdateNodeName(node);

		GUILayout.Space(10f);

		//Show target configuration tab
		inspector.DrawPlatformSelector(node);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
			// Draw Platform selector tab. 
			var disabledScope = inspector.DrawOverrideTargetToggle(node, m_result.ContainsValueOf(inspector.CurrentEditingGroup), (bool b) => {
				using(new RecordUndoScope("Remove Target Platform Settings", node, true)) {
					if(b) {
						m_result[inspector.CurrentEditingGroup] = m_result.DefaultValue;
					} else {
						m_result.Remove(inspector.CurrentEditingGroup);
					}
					onValueChanged();
				}
			});

			// Draw tab contents
			using (disabledScope) {
				var val = m_result[inspector.CurrentEditingGroup];

				using (new GUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Capture reference data"))
					{
						using(new RecordUndoScope("My Value Changed", node, true)){
							val = JsonUtility.ToJson(m_current);
							m_result[inspector.CurrentEditingGroup] = val;
							onValueChanged();
						}
					}
					if (GUILayout.Button("Clear", GUILayout.Width(50f)))
					{
						m_result.Remove(inspector.CurrentEditingGroup);
						val = string.Empty;
						onValueChanged();
					}
				}

				
				GUILayout.Space(10f);
				if (!string.IsNullOrEmpty(val))
				{
					GUILayout.Label("Reference Data","BoldLabel");
//					var width = GUILayoutUtility.GetLastRect().width;
//					GUI.skin.label.CalcHeight(new GUIContent(val), 500f);
					GUILayout.Label(val, GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
					
					GUILayout.Space(4f);
					using (new GUILayout.HorizontalScope())
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Copy to Clipboard", GUILayout.Width(150f)))
						{
							EditorGUIUtility.systemCopyBuffer = val;
						}
					}
				}
			}
		}
	}

	private static AssetGroupStructures CreateASG(IEnumerable<PerformGraph.AssetGroups> incoming)
	{
		var asg = new AssetGroupStructures();

		foreach (var g in incoming)
		{
			var s  = new AssetGroupStructures.Structure(g.connection.Id);
			asg.incoming.Add(s);
			foreach (var key in g.assetGroups.Keys)
			{
				var group = new AssetGroupStructures.Group(key);
				s.groups.Add(group);
				foreach (var a in g.assetGroups[key])
				{					
					group.entires.Add(new AssetGroupStructures.Entry(a.importFrom, 
						a.assetType == null ? string.Empty: a.assetType.FullName, 
						a.importerType == null ? string.Empty: a.importerType.FullName));
				}				
				group.entires.Sort((x, y) => EditorUtility.NaturalCompare(x.importFrom,y.importFrom));
			}
			s.groups.Sort((x, y) => EditorUtility.NaturalCompare(x.name,y.name));
		}
		asg.incoming.Sort((x, y) => EditorUtility.NaturalCompare(x.connectionId,y.connectionId));

		return asg;
	}

	private static void ValidateASG(Model.NodeData node, AssetGroupStructures expected, AssetGroupStructures actual)
	{
		AssertNode(node, expected.incoming.Count, actual.incoming.Count, "Incoming data count is changed.");
		
		for (int i = 0; i < actual.incoming.Count; ++i)
		{
			var sExpected = expected.incoming[i];
			var sActual = actual.incoming[i];
			
			AssertNode(node, sExpected.groups.Count, sActual.groups.Count, "Group count is changed.");
			AssertNode(node, sExpected.connectionId, sActual.connectionId, "ConnectionId is changed.");


			for (int j = 0; j < sExpected.groups.Count; ++j)
			{
				var gExpected = sExpected.groups[j];
				var gActual = sActual.groups[j];
				
				AssertNode(node, gExpected.name, gActual.name, "Group name is changed.");
				AssertNode(node, gExpected.entires.Count, gActual.entires.Count, "Group entry count is changed for " + gExpected.name);

				for (int k = 0; k < gExpected.entires.Count; ++k)
				{
					var eExpected = gExpected.entires[k];
					var eActual = gActual.entires[k];
					
					AssertNode(node, eExpected.importFrom, eActual.importFrom, "Asset path is changed.");
					AssertNode(node, eExpected.assetType, eActual.assetType, "Asset type is changed for " + eExpected.importFrom);
					AssertNode(node, eExpected.importerType, eActual.importerType, "Importer type is changed for " + eExpected.importFrom);
				}
			}
		}
	}

	private static void AssertNode<T> (Model.NodeData node, T expected, T actual, string issue) where T: IComparable
	{
		const string howtofix = "Check data and fix problem. Otherwise, reset reference data for regression.";
		if (expected.CompareTo(actual) != 0)
		{
			throw new NodeException($"{issue} - Expected:{expected}, Actual:{actual}", howtofix, node);
		}
	}
	

	/**
	 * Prepare is called whenever graph needs update. 
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output)
	{
		if (incoming != null)
		{
			m_current = CreateASG(incoming);
		}

		// Pass incoming assets straight to Output
		if(Output != null) {
			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();

			if(incoming != null) {
				foreach(var ag in incoming) {
					Output(destination, ag.assetGroups);
				}
			} else {
				// Overwrite output with empty Dictionary when there is no incoming asset
				Output(destination, new Dictionary<string, List<AssetReference>>());
			}
		}
	}

	/**
	 * Build is called when Unity builds assets with AssetBundle Graph. 
	 */ 
	public override void Build (BuildTarget target, 
		Model.NodeData nodeData, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output outputFunc,
		Action<Model.NodeData, string, float> progressFunc)
	{
		if (incoming != null)
		{
			AssetGroupStructures expected = null;
			if (!string.IsNullOrEmpty(m_result[target]))
			{
				expected = JsonUtility.FromJson<AssetGroupStructures>(m_result[target]);
			}
			
			if (expected == null)
			{
				throw new NodeException("Retrieving reference data failed.", "Please recreate regression reference data from Inspector.", nodeData);
			}
			
			m_current = CreateASG(incoming);
			ValidateASG(nodeData, expected, m_current);
		}
	}
}
