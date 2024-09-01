using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

	[Serializable] 
    public class SerializableGroups {

		[Serializable]
		public class Group {
			[SerializeField] public string name;
            [SerializeField] public List<string> assets;

            public Group(string n, List<string> a) {
                name = n;
                assets = new List<string>(a);
            }
		}

        [SerializeField] private List<Group> m_groups;

        public SerializableGroups() {
            m_groups = new List<Group>();
		}

        public SerializableGroups(SerializableGroups rhs) {
            m_groups = new List<Group>();
			foreach(var v in rhs.m_groups) {
                m_groups.Add(new Group(v.name, v.assets));
			}
		}

        public SerializableGroups(Dictionary<string, List<AssetReference>> groups) {
            m_groups = new List<Group>();
            foreach (var groupName in groups.Keys) {
				try {
                    m_groups.Add(new Group(groupName, groups[groupName].Select(x => x.importFrom).ToList()));
				} catch(Exception e) {
                    LogUtility.Logger.Log(LogType.Warning, "Failed to retrieve SerializableGroups. error:" + e);
				}
			}
		}

        public List<Group> Groups {
			get {
				return m_groups;
			}
		}

        public List<string> this[string groupName] {
			get {
                if (groupName == null) {
                    throw new ArgumentNullException ();
                }

                int i = m_groups.FindIndex(v => v.name == groupName);
				if(i >= 0) {
                    return m_groups[i].assets;
				} else {
                    throw new KeyNotFoundException (groupName + " key not found.");
				}
			}
			set {
                int i = m_groups.FindIndex(v => v.name == groupName);
				if(i >= 0) {
                    m_groups[i].assets = value;
				} else {
                    m_groups.Add(new Group(groupName, value));
				}
			}
		}

        public bool ContainsKey (string groupName) {
            return m_groups.FindIndex(v => v.name == groupName) >= 0;
		}

        public string FindGroupOfAsset (string path) {
            foreach (var g in m_groups) {
                if (g.assets.Contains (path)) {
                    return g.name;
                }
            }

            return null;
        }

        public void Remove (string groupName) {
            int index = m_groups.FindIndex(v => v.name == groupName);
			if(index >= 0) {
				m_groups.RemoveAt(index);
			}
		}

        public Dictionary<string, List<AssetReference>> ToGroupDictionary() {
            Dictionary<string, List<AssetReference>> dic = new Dictionary<string, List<AssetReference>>();
			foreach(var g in m_groups) {
                dic [g.name] = g.assets.Select(AssetReferenceDatabase.GetReference).ToList();
			}
			return dic;
		}

		public override bool Equals(object rhs)
		{
            SerializableGroups other = rhs as SerializableGroups; 
			if (other == null) {
				return false;
			} else {
				return other == this;
			}
		}

		public override int GetHashCode()
		{
			return this.m_groups.GetHashCode(); 
		}

        public static bool operator == (SerializableGroups lhs, SerializableGroups rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

			if( lhs.m_groups.Count != rhs.m_groups.Count ) {
				return false;
			}

			foreach(var l in lhs.m_groups) {
                if (!rhs.ContainsKey (l.name)) {
                    return false;
                }
                if(rhs[l.name] != l.assets) {
					return false;
				}
			}

			return true;
		}

        public static bool operator != (SerializableGroups lhs, SerializableGroups rhs) {
			return !(lhs == rhs);
		}
	}
}