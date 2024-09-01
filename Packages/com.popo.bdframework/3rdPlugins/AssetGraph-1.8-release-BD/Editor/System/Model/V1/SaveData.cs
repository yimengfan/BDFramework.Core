using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

namespace AssetBundleGraph {

	internal static class SaveDataConstants {
		/*
			data key for AssetBundleGraph.json
		*/

		public const string GROUPING_KEYWORD_DEFAULT = "/Group_*/";
		public const string BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT = "bundle_*";

		// by default, AssetBundleGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string DEFAULT_FILTER_KEYTYPE = "Any";

		public static NodeKind NodeKindFromString (string val) {
			return (NodeKind)Enum.Parse(typeof(NodeKind), val);
		}
	}

	/*
	 * Old data which holds all AssetBundleGraph settings and configurations until AssetGraph 1.1.x
	 */ 
	internal class SaveData : ScriptableObject {

		private const string LASTMODIFIED 	= "lastModified";
		private const string NODES 			= "nodes";
		private const string CONNECTIONS 	= "connections";
		private const string VERSION 		= "version";

		/*
		 * Important: 
		 * ABG_FILE_VERSION must be increased by one when any structure change(s) happen
		 */ 
		private const int ABG_FILE_VERSION = 1;

		[SerializeField] private List<NodeData> m_allNodes;
		[SerializeField] private List<ConnectionData> m_allConnections;
		[SerializeField] private string m_lastModified;
		[SerializeField] private int m_version;

		private string GetFileTimeUtcString() {
			return DateTime.UtcNow.ToFileTimeUtc().ToString();
		}

		private void InitializeFromJson(Dictionary<string, object> jsonData) {
			m_allNodes = new List<NodeData>();
			m_allConnections = new List<ConnectionData>();
			m_lastModified = jsonData[LASTMODIFIED] as string;
			m_version = ABG_FILE_VERSION;

			var nodeList = jsonData[NODES] as List<object>;
			var connList = jsonData[CONNECTIONS] as List<object>;

			foreach(var n in nodeList) {
				m_allNodes.Add(new NodeData(n as Dictionary<string, object>));
			}

			foreach(var c in connList) {
				m_allConnections.Add(new ConnectionData(c as Dictionary<string, object>));
			}
			EditorUtility.SetDirty(this);
		}

		public string LastModified
		{
			get { return m_lastModified; }
		}

		public int Version
		{
			get { return m_version; }
		}

		public List<NodeData> Nodes {
			get{ 
				return m_allNodes;
			}
		}

		public List<ConnectionData> Connections {
			get{ 
				return m_allConnections;
			}
		}

		//
		// Load from disk
		//
		public static SaveData LoadSaveData(string saveDataFilePath)
		{
			return saveDataFilePath.EndsWith(".json") ? LoadJsonData(saveDataFilePath) : AssetDatabase.LoadAssetAtPath<SaveData>(saveDataFilePath);
		}

		private static SaveData LoadJsonData(string saveDataFilePath) {
			var dataStr = string.Empty;
			using (var sr = new StreamReader(saveDataFilePath)) {
				dataStr = sr.ReadToEnd();
			}
			var deserialized = Json.Deserialize(dataStr) as Dictionary<string, object>;
			var data = ScriptableObject.CreateInstance<SaveData>();
			data.InitializeFromJson(deserialized);

			data.Validate();

			return data;
		}

		/*
		 * Checks deserialized SaveData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		private bool Validate () {
			var changed = false;

			List<NodeData> removingNodes = new List<NodeData>();
			List<ConnectionData> removingConnections = new List<ConnectionData>();

			/*
				delete undetectable node.
			*/
			foreach (var n in m_allNodes) {
				if(!n.Validate(m_allNodes, m_allConnections)) {
					removingNodes.Add(n);
					changed = true;
				}
			}

			foreach (var c in m_allConnections) {
				if(!c.Validate(m_allNodes, m_allConnections)) {
					removingConnections.Add(c);
					changed = true;
				}
			}

			if(changed) {
				Nodes.RemoveAll(n => removingNodes.Contains(n));
				Connections.RemoveAll(c => removingConnections.Contains(c));
				m_lastModified = GetFileTimeUtcString();
			}

			return !changed;
		}
	}
}