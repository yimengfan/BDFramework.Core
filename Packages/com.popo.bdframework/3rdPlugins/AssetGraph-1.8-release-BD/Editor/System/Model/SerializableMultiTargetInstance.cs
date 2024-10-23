using UnityEditor;
using System;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[Serializable] 
	public class SerializableMultiTargetInstance : ISerializationCallbackReceiver {

		[Serializable]
		public class Entry {
			[SerializeField] public BuildTargetGroup targetGroup;
			[SerializeField] public string value;
            [NonSerialized]  public object instance;

            public Entry(BuildTargetGroup g, string v, object i) {
				targetGroup = g;
				value = v;
                instance = i;
			}

            public T Get<T> () {
                if (instance == null) {
                    instance = JsonUtility.FromJson (CustomScriptUtility.DecodeString (value), typeof(T));
                }

                return (T) instance;
            }
		}
		[SerializeField] private string m_className;
		[SerializeField] private List<Entry> m_values;

		public SerializableMultiTargetInstance(SerializableMultiTargetInstance rhs) {
			m_className = rhs.m_className;
			m_values = new List<Entry>(rhs.m_values.Count);
			foreach(var v in rhs.m_values) {
                m_values.Add(new Entry(v.targetGroup, v.value, null));
			}
		}

		public SerializableMultiTargetInstance(object value) {
            m_className = value.GetType().AssemblyQualifiedName;
			m_values = new List<Entry>();
			Set(BuildTargetUtility.DefaultTarget, value);
		}

        public SerializableMultiTargetInstance(string assemblyQualifiedName, SerializableMultiTargetString instanceData) {
            m_className = assemblyQualifiedName;
			m_values = new List<Entry>(instanceData.Values.Count);
			foreach(var v in instanceData.Values) {
				m_values.Add(new Entry(v.targetGroup, CustomScriptUtility.EncodeString(v.value), null));
			}
		}
		
		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			m_className = VersionCompatibilityUtility.UpdateClassName(m_className);
		}

		public SerializableMultiTargetInstance() {
			m_className = string.Empty;
			m_values = new List<Entry>();
		}

		public string ClassName {
			get {
				return m_className;
			}
		}

		public List<Entry> Values {
			get {
				return m_values;
			}
		}

		public string this[BuildTargetGroup g] {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					return DefaultValue;
				}
			}
		}

		public string this[BuildTarget index] {
			get {
				return this[BuildTargetUtility.TargetToGroup(index)];
			}
		}

		public string DefaultValue {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == BuildTargetUtility.DefaultTarget);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					string defaultValue = string.Empty;
					m_values.Add(new Entry(BuildTargetUtility.DefaultTarget, defaultValue, null));
					return defaultValue;
				}
			}
		}

		public string CurrentPlatformValue {
			get {
				return this[EditorUserBuildSettings.selectedBuildTargetGroup];
			}
		}

		public T Get<T>(BuildTargetGroup g) {
			if(m_className == null) {
				return default(T);
			}
			int i = m_values.FindIndex(v => v.targetGroup == g);
			if(i >= 0) {
				Type t = Type.GetType(m_className);

				if(t == null) {
					LogUtility.Logger.LogFormat(LogType.Warning, "Could not retrieve Type info from classname:{0}", m_className);
					return default(T);
				}
				UnityEngine.Assertions.Assert.IsTrue( typeof(T).IsAssignableFrom(t) );

                if (m_values [i].instance == null) {
                    m_values [i].instance = JsonUtility.FromJson (CustomScriptUtility.DecodeString (m_values [i].value), t);
                }

                return (T) m_values [i].instance;
			} else {
				return GetDefaultValue<T>();
			}
		}

		public void Set(BuildTargetGroup g, object value) {

			if(value == null) {
				Remove(g);
				return;
			}

			bool defaultNeedsUpdate = false;

            if(m_className != value.GetType().AssemblyQualifiedName) {
				m_values.Clear();
                m_className = value.GetType().AssemblyQualifiedName;
				defaultNeedsUpdate = true;
			}

			int i = m_values.FindIndex(v => v.targetGroup == g);
			var json = CustomScriptUtility.EncodeString(JsonUtility.ToJson(value));
			if(i >= 0) {
				m_values [i].value = json;
                m_values [i].instance = value;
			} else {
				m_values.Add(new Entry(g, json, value));
				if(defaultNeedsUpdate && g != BuildTargetUtility.DefaultTarget) {
					m_values.Add(new Entry(BuildTargetUtility.DefaultTarget, json, value));
				}
			}
		}

		public T Get<T>(BuildTarget t) {
			return Get<T>(BuildTargetUtility.TargetToGroup(t));
		}
		public void Set(BuildTarget t, object value) {
			Set(BuildTargetUtility.TargetToGroup(t),value);
		}

		public void CopyDefaultValueTo(BuildTargetGroup g) {
			int i = m_values.FindIndex(v => v.targetGroup == BuildTargetUtility.DefaultTarget);
			int iTarget = m_values.FindIndex(v => v.targetGroup == g);
			if(i >= 0) {
				if(iTarget >= 0 && i != iTarget) {
					m_values[iTarget].value = m_values[i].value;
				}
				if(iTarget < 0) {
					m_values.Add(new Entry(g, m_values[i].value, null));
				}
			}
		}

		public void CopyDefaultValueTo(BuildTarget t) {
			CopyDefaultValueTo(BuildTargetUtility.TargetToGroup(t));
		}

			
		public T GetDefaultValue<T>() {
			int i = m_values.FindIndex(v => v.targetGroup == BuildTargetUtility.DefaultTarget);
			if(i >= 0) {
				Type t = Type.GetType(m_className);

				if(t == null) {
					LogUtility.Logger.LogFormat(LogType.Warning, "Could not retrieve Type info from classname:{0}", m_className);
					return default(T);
				}
				UnityEngine.Assertions.Assert.IsTrue( typeof(T).IsAssignableFrom(t) );
				return (T) JsonUtility.FromJson(CustomScriptUtility.DecodeString(m_values[i].value), t);
			} else {
				return default(T);
			}
		}

		public void SetDefaultValue(object value) {
			Set(BuildTargetUtility.DefaultTarget, value);
		}

		public T GetCurrentPlatformValue<T>() {
			return Get<T>(EditorUserBuildSettings.selectedBuildTargetGroup);
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

		public override bool Equals(object rhs)
		{
			SerializableMultiTargetInstance other = rhs as SerializableMultiTargetInstance; 
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

		public static bool operator == (SerializableMultiTargetInstance lhs, SerializableMultiTargetInstance rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

			if( lhs.m_className != rhs.m_className ) {
				return false;
			}

			if( lhs.m_values.Count != rhs.m_values.Count ) {
				return false;
			}

			foreach(var l in lhs.m_values) {
				var r = rhs.m_values.Find(v => v.targetGroup == l.targetGroup);
				if(r == null || r.value != l.value) {
					return false;
				}
			}

			return true;
		}

		public static bool operator != (SerializableMultiTargetInstance lhs, SerializableMultiTargetInstance rhs) {
			return !(lhs == rhs);
		}
	}
}

