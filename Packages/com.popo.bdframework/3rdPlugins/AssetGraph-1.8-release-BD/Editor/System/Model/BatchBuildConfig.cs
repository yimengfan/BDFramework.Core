using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class BatchBuildConfig : ScriptableObject {

		private const int VERSION = 1;

		[Serializable]
		public class GraphCollection {
			[SerializeField] private string m_name;
            [SerializeField] private List<string> m_graphGuids;
            [SerializeField] private string m_guid;

            static public GraphCollection CreateNewGraphCollection(string suggestedName) {

                string nameCandidate = suggestedName;

                var collection = BatchBuildConfig.GetConfig ().GraphCollections;
                int i = 0;

                while (true) {
                    if (collection.Find (c => c.Name.ToLower () == nameCandidate.ToLower ()) == null) {
                        var newCollection = new GraphCollection (nameCandidate);
                        collection.Add (newCollection);
                        BatchBuildConfig.SetConfigDirty ();
                        return newCollection;
                    }
                    nameCandidate = $"{suggestedName} {++i}";
                }
            }

			private GraphCollection(string name) {
				m_name = name;
				m_graphGuids = new List<string>();
                m_guid = GUID.Generate().ToString();
            }

			public string Name {
				get {
					return m_name;
				}
			}

            public string Guid {
                get {
                    if (string.IsNullOrEmpty (m_guid)) {
                        m_guid = GUID.Generate().ToString();
                    }
                    return m_guid;
                }
            }

			public List<string> GraphGUIDs {
				get {
					return m_graphGuids;
				}
			}

            public bool Validate() {
                bool changed = false;
                List<string> removingItems = null;
                foreach (var guid in m_graphGuids) {
                    var path = AssetDatabase.GUIDToAssetPath (guid);
                    if (string.IsNullOrEmpty (path) || !File.Exists (path) || TypeUtility.GetMainAssetTypeAtPath (path) != typeof(Model.ConfigGraph)) {
                        if (removingItems == null) {
                            removingItems = new List<string> ();
                        }
                        removingItems.Add (guid);
                    }
                }

                if (removingItems != null) {
                    RemoveGraphRange (removingItems);
                    changed = true;
                }
                return changed;
            }

            public void AddGraph(string guid) {
                if (!m_graphGuids.Contains (guid)) {
                    m_graphGuids.Add (guid);
                }
            }

            public void AddGraphRange(IList<string> guids) {
                foreach (var g in guids) {
                    AddGraph (g);
                }
            }

            public void RemoveGraph(string guid) {
                m_graphGuids.Remove (guid);
            }

            public void RemoveGraphRange(IList<string> guids) {
                foreach (var g in guids) {
                    m_graphGuids.Remove (g);
                }
            }

            public void InsertGraph(int index, string guid) {
                if (!m_graphGuids.Contains (guid) && index >= 0 && index < m_graphGuids.Count) {
                    m_graphGuids.Insert (index, guid);
                }
            }

            public void InsertGraphRange(int index, IList<string> guids) {
                if (index < 0 || index >= m_graphGuids.Count) {
                    return;
                }

                var notIncludedList = guids.Where(g => !m_graphGuids.Contains(g)).AsEnumerable();

                m_graphGuids.InsertRange (index, notIncludedList);
            }

            public bool TryRename(string newName) {
                var collection = BatchBuildConfig.GetConfig ().GraphCollections;
                if (collection.Find (c => c.Name.ToLower () == newName.ToLower ()) != null) {
                    return false;
                }
                m_name = newName;
                return true;
            }
		}

        [SerializeField] private List<GraphCollection> m_collections;
        [SerializeField] private List<BuildTarget> m_buildTargets;
		[SerializeField] private int m_version;

		private static BatchBuildConfig s_config;

        private static string OldBatchBuildConfigPath   
        { get { return System.IO.Path.Combine(DataModel.Version2.Settings.Path.SettingFilePath, "BatchBuildConfig.asset"); } }

		public static BatchBuildConfig GetConfig() {
			if(s_config == null) {
				if(!Load()) {
					// Create vanilla db
					s_config = ScriptableObject.CreateInstance<BatchBuildConfig>();
					s_config.m_collections = new List<GraphCollection>();
                    s_config.m_buildTargets = new List<BuildTarget> ();
					s_config.m_version = VERSION;

                    CreateBatchBuildConfigAsset (s_config);
				}
			}

			return s_config;
		}

        private static void CreateBatchBuildConfigAsset(BatchBuildConfig c) {
            var SettingDir = Path.Combine(Model.Settings.Path.SavedSettingsPath, "BatchBuildConfig");

            if (!Directory.Exists(SettingDir)) {
                Directory.CreateDirectory(SettingDir);
            }

            AssetDatabase.CreateAsset(s_config, Model.Settings.Path.BatchBuildConfigPath);
        }

		private static bool Load() {

			bool loaded = false;

			try {
                var configPath = Model.Settings.Path.BatchBuildConfigPath;
				
				if(File.Exists(configPath)) 
				{
					BatchBuildConfig db = AssetDatabase.LoadAssetAtPath<BatchBuildConfig>(configPath);
					if(db.m_version == VERSION) {
                        db.Validate();
						s_config = db;
						loaded = true;
					}
                } 

                // try loading pre-1.4 config
                else {
                    var oldConfigPath = OldBatchBuildConfigPath;
                    if(File.Exists(oldConfigPath)) 
                    {
                        BatchBuildConfig db = AssetDatabase.LoadAssetAtPath<BatchBuildConfig>(oldConfigPath);
                        if(db.m_version == VERSION) {
                            db.Validate();
                            s_config = db;
                            loaded = true;
                            AssetDatabase.MoveAsset(oldConfigPath, configPath);
                        }
                    }
                }
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			return loaded;
		}

		public static void SetConfigDirty() {
			EditorUtility.SetDirty(s_config);
		}

        public static GraphCollection CreateNewGraphCollection(string suggestedName) {
            return GraphCollection.CreateNewGraphCollection (suggestedName);
        }

		public List<GraphCollection> GraphCollections {
			get {
				return m_collections;
			}
		}

        public List<BuildTarget> BuildTargets {
            get {
                if (m_buildTargets == null) {
                    m_buildTargets = new List<BuildTarget> ();
                }
                return m_buildTargets;
            }
        }

		public GraphCollection Find(string name) {
			return m_collections.Find(c => c.Name == name);
		}

        public bool Validate() {
            var changed = false;
            foreach (var c in m_collections) {
                changed |= c.Validate ();
            }
            if (changed) {
                EditorUtility.SetDirty(this);
            }
            return changed;
        }
	}
}

