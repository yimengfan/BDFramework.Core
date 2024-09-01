using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine.Assertions;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class AssetProcessEventRecord : ScriptableObject {

        private const int VERSION = 1;

        private delegate AssetProcessEvent EventCreator ();

        [SerializeField] private List<AssetProcessEvent> m_events;
        [SerializeField] private int m_errorEventCount;
        [SerializeField] private int m_infoEventCount;
        [SerializeField] private int m_version;
        [SerializeField] private int m_processStartIndex;

        public delegate void AssetProcessEventHandler(AssetProcessEvent e);
        public static event AssetProcessEventHandler onAssetProcessEvent;

        private static AssetProcessEventRecord s_record;

	    private List<AssetProcessEvent> m_filteredEvents;
        private bool m_includeError;
        private bool m_includeInfo;
        private string m_filterKeyword;
        private string[] m_filterKeywordTokens;

        private int m_filteredErrorEventCount;
        private int m_filteredInfoEventCount;

	    private bool m_enabled;

        public List<AssetProcessEvent> Events {
            get {
                return m_filteredEvents;
            }
        }

        public int InfoEventCount {
            get {
                return m_infoEventCount;
            }
        }

        public int ErrorEventCount {
            get {
                return m_errorEventCount;
            }
        }

        public int FilteredInfoEventCount {
            get {
                return m_filteredInfoEventCount;
            }
        }

        public int FilteredErrorEventCount {
            get {
                return m_filteredErrorEventCount;
            }
        }

	    public bool EnabledRecording
	    {
	        get { return m_enabled; }
	        set { m_enabled = value; }
	    }

	    public static AssetProcessEventRecord GetRecord() {
			if(s_record == null) {
				if(!Load()) {
					// Create vanilla db
                    s_record = ScriptableObject.CreateInstance<AssetProcessEventRecord>();
                    s_record.Init ();

                    var DBDir = DataModel.Version2.Settings.Path.TemporalSettingFilePath;

					if (!Directory.Exists(DBDir)) {
						Directory.CreateDirectory(DBDir);
					}

                    AssetDatabase.CreateAsset(s_record, Model.Settings.Path.EventRecordPath);
				}
			}

			return s_record;
		}

		private static bool Load() {

			var loaded = false;

			try {
                var path = Model.Settings.Path.EventRecordPath;
				
				if(File.Exists(path)) 
				{
                    AssetProcessEventRecord record = AssetDatabase.LoadAssetAtPath<AssetProcessEventRecord>(path);

					if(record != null && record.m_version == VERSION) {
						s_record = record;
					    s_record.InitAfterDeserialize();
					    loaded = true;
					} else {
                        if(record != null) {
                            Resources.UnloadAsset(record);
                        }
                    }
				}
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			return loaded;
		}

        private static void SetRecordDirty() {
			EditorUtility.SetDirty(s_record);
		}

        private void Init() {
            m_events = new List<AssetProcessEvent>();
            m_errorEventCount = 0;
            m_infoEventCount = 0;
            m_enabled = true;
            m_version = VERSION;
            InitAfterDeserialize();
        }

	    private void InitAfterDeserialize()
        {
            // Validate events: remove all events where graph or asset is missing.
            m_events.RemoveAll(e =>
                e.GraphGuid == null ||
                e.AssetGuid == null ||
                string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(e.GraphGuid)) ||
                string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(e.AssetGuid)));
            
	        m_filteredEvents = new List<AssetProcessEvent>();
	        m_filteredInfoEventCount = 0;
	        m_filteredErrorEventCount = 0;
	        m_filterKeyword = string.Empty;
	        m_includeError = true;
	        m_includeInfo = true;
	        m_enabled = true;

	        RebuildFilteredEvents();
	    }

        public void SetFilterCondition(bool includeInfo, bool includeError) {

            if (includeInfo != m_includeInfo || includeError != m_includeError) {
                m_includeInfo = includeInfo;
                m_includeError = includeError;

                RebuildFilteredEvents ();
            }
        }

        public void SetFilterKeyword(string keyword ) {

            if (m_filterKeyword != keyword) {
                m_filterKeyword = keyword;
                m_filterKeywordTokens = m_filterKeyword.Split (' ');

                RebuildFilteredEvents ();
            }
        }

        private void RebuildFilteredEvents() {
            m_filteredEvents.Clear ();
            m_filteredEvents.Capacity = m_events.Count;
            m_filteredErrorEventCount = 0;
            m_filteredInfoEventCount = 0;

            foreach (var e in m_events) {
                if (MeetFilterCondition(e)) {
                    switch (e.Kind) {
                    case AssetProcessEvent.EventKind.Error: 
                        ++m_filteredErrorEventCount;
                        break;
                    default:
                        ++m_filteredInfoEventCount;
                        break;
                    }
                    m_filteredEvents.Add (e);
                }
            }
        }

        private bool MeetFilterCondition(AssetProcessEvent e) {
            var meetKindFilter = 
                (m_includeError && e.Kind == AssetProcessEvent.EventKind.Error) ||
                (m_includeInfo && e.Kind != AssetProcessEvent.EventKind.Error) ;

            if (string.IsNullOrEmpty (m_filterKeyword)) {
                return meetKindFilter;
            }

            bool keymatch = true;

            foreach (var token in m_filterKeywordTokens) {
                if (string.IsNullOrEmpty (token)) {
                    continue;
                }
                keymatch &= e.AssetName.IndexOf (token) >= 0;
            }
            return meetKindFilter && keymatch;
        }

        public void LogModify(AssetReference a) {
            LogModify (a.assetDatabaseId);
        }

        public void LogModify(string assetGuid) {
            if (!m_enabled)
            {
                return;
            }

            var gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("Modify event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateModifyEvent (assetGuid, gc.TargetGraph.GetGraphGuid (), gc.CurrentNode);

            AddEvent (newEvent);
        }

        public void LogError(NodeException e) {
            if (!m_enabled)
            {
                return;
            }

            var gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("Error event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateErrorEvent (e, gc.TargetGraph.GetGraphGuid ());

            AddEvent (newEvent);
        }

        private void AddEvent(AssetProcessEvent e)
        {
            Assert.IsTrue(m_enabled);
            
            m_events.Add (e);

            if (e.Kind == AssetProcessEvent.EventKind.Error) {
                ++m_errorEventCount;

                if (MeetFilterCondition (e)) {
                    m_filteredEvents.Add (e);
                    ++m_filteredErrorEventCount;
                }
            }
            if (e.Kind != AssetProcessEvent.EventKind.Error) {
                ++m_infoEventCount;

                if (MeetFilterCondition (e)) {
                    m_filteredEvents.Add (e);
                    ++m_filteredInfoEventCount;
                }
            }

            if (onAssetProcessEvent != null) {
                onAssetProcessEvent (e);
            }
            SetRecordDirty ();
        }

        public void Clear(bool executeGraphsWithError) {

            List<string> graphGuids = null;

            if (executeGraphsWithError) {
                graphGuids = m_events.Where (e => e.Kind == AssetProcessEvent.EventKind.Error).Select (e => e.GraphGuid).Distinct().ToList ();
            }

            m_events.Clear ();
            m_filteredEvents.Clear ();
            m_errorEventCount = 0;
            m_infoEventCount = 0;
            m_filteredInfoEventCount = 0;
            m_filteredErrorEventCount = 0;

            if (executeGraphsWithError) {
                var graphGuidWithoutHidden = 
                    graphGuids.Select(AssetDatabase.GUIDToAssetPath)
                        .Where(string.IsNullOrEmpty)
                        .Where(path => path.Contains(Model.Settings.HIDE_GRAPH_PREFIX))
                        .ToList();
                
                AssetGraphUtility.ExecuteAllGraphs (graphGuidWithoutHidden, true);
            }
        }
	}
}

