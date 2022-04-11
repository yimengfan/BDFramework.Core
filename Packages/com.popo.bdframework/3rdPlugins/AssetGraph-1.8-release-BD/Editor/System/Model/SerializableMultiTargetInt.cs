using UnityEditor;

using System;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

	[Serializable] 
	public class SerializableMultiTargetInt {

		[Serializable]
		public class Entry {
			[SerializeField] public BuildTargetGroup targetGroup;
			[SerializeField] public int value;

			public Entry(BuildTargetGroup g, int v) {
				targetGroup = g;
				value = v;
			}
		}

		[SerializeField] private List<Entry> m_values;

		public SerializableMultiTargetInt(int value) {
			m_values = new List<Entry>();
			this[BuildTargetUtility.DefaultTarget] = value;
		}

		public SerializableMultiTargetInt() {
			m_values = new List<Entry>();
		}

		public SerializableMultiTargetInt(SerializableMultiTargetInt rhs) {
			m_values = new List<Entry>();
			foreach(var v in rhs.m_values) {
				m_values.Add(new Entry(v.targetGroup, v.value));
			}
		}

		public SerializableMultiTargetInt(Dictionary<string, object> json) {
			m_values = new List<Entry>();
			foreach (var buildTargetName in json.Keys) {
				try {
					BuildTargetGroup g =  (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), buildTargetName, true);
					int val = Convert.ToInt32(json[buildTargetName]);
					m_values.Add(new Entry(g, val));
				} catch(Exception e) {
					LogUtility.Logger.Log(LogType.Warning, "Failed to retrieve SerializableMultiTargetString. skipping entry - " + buildTargetName + ":" + json[buildTargetName] + " error:" + e);
				}
			}
		}

		public List<Entry> Values {
			get {
				return m_values;
			}
		}

		public int this[BuildTargetGroup g] {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					return DefaultValue;
				}
			}
			set {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					m_values[i].value = value;
				} else {
					m_values.Add(new Entry(g, value));
				}
			}
		}

		public int this[BuildTarget index] {
			get {
				return this[BuildTargetUtility.TargetToGroup(index)];
			}
			set {
				this[BuildTargetUtility.TargetToGroup(index)] = value;
			}
		}

		public int DefaultValue {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == BuildTargetUtility.DefaultTarget);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					var defaultValue = 0;
					m_values.Add(new Entry(BuildTargetUtility.DefaultTarget, defaultValue));
					return defaultValue;
				}
			}
			set {
				this[BuildTargetUtility.DefaultTarget] = value;
			}
		}

		public int CurrentPlatformValue {
			get {
				return this[EditorUserBuildSettings.selectedBuildTargetGroup];
			}
		}

		public bool ContainsValueOf (BuildTargetGroup group) {
			return m_values.FindIndex(v => v.targetGroup == group) >= 0;
		}

		public void Remove (BuildTargetGroup group) {
			int index = m_values.FindIndex(v => v.targetGroup == group);
			if(index >= 0) {
				m_values.RemoveAt(index);
			}
		}

		public Dictionary<string, object> ToJsonDictionary() {
			Dictionary<string, object> dic = new Dictionary<string, object>();
			foreach(Entry e in m_values) {
				dic.Add(e.targetGroup.ToString(), e.value);
			}
			return dic;
		}

		public override bool Equals(object rhs)
		{
			SerializableMultiTargetInt other = rhs as SerializableMultiTargetInt; 
			if (other == null) {
				return false;
			} else {
				return other == this;
			}
		}

		public override int GetHashCode()
		{
			return this.m_values.GetHashCode(); 
		}

		public static bool operator == (SerializableMultiTargetInt lhs, SerializableMultiTargetInt rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

			if( lhs.m_values.Count != rhs.m_values.Count ) {
				return false;
			}

			foreach(var l in lhs.m_values) {
				if(rhs[l.targetGroup] != l.value) {
					return false;
				}
			}

			return true;
		}

		public static bool operator != (SerializableMultiTargetInt lhs, SerializableMultiTargetInt rhs) {
			return !(lhs == rhs);
		}
	}
}