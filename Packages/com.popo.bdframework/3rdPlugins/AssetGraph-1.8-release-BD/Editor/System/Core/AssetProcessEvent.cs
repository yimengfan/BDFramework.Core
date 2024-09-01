using UnityEditor;
using System;
using System.IO;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    public class AssetProcessEvent {

        public enum EventKind
        {
            Modify,
            Error
        }

        [SerializeField] private EventKind m_kind;
        [SerializeField] private long m_timestampUtc;
        [SerializeField] private string m_assetName;
        [SerializeField] private string m_assetGuid;
        [SerializeField] private string m_graphGuid;
        [SerializeField] private string m_nodeId;
        [SerializeField] private string m_nodeName;
        [SerializeField] private string m_description;
        [SerializeField] private string m_howToFix;

        public EventKind Kind {
            get { 
                return m_kind; 
            }
        }

        public DateTime Timestamp {
            get {
                return DateTime.FromFileTimeUtc(m_timestampUtc);
            }
        }

        public string AssetGuid {
            get {
                return m_assetGuid;
            }
        }

        public string AssetName {
            get {
                return m_assetName;
            }
        }

        public string GraphGuid {
            get {
                return m_graphGuid;
            }
        }

        public string NodeId {
            get {
                return m_nodeId;
            }
        }

        public string NodeName {
            get {
                return m_nodeName;
            }
        }

        public string Description {
            get {
                return m_description;
            }
        }

        public string HowToFix {
            get {
                return m_howToFix;
            }
        }

        private AssetProcessEvent() {}

        private void Init(EventKind k, string assetName, string assetGuid, string graphGuid, string nodeId, string nodeName, string desc, string howto) {
            m_kind = k;
            m_assetName = assetName;
            m_assetGuid = assetGuid;
            m_graphGuid = graphGuid;
            m_nodeId = nodeId;
            m_nodeName = nodeName;
            m_timestampUtc = DateTime.Now.ToFileTimeUtc();
            m_description = desc;
            m_howToFix = howto;
        }

        public static AssetProcessEvent CreateModifyEvent(string assetGuid, string graphGuid, Model.NodeData n) {
            var ev = new AssetProcessEvent();
            var path = AssetDatabase.GUIDToAssetPath (assetGuid);
            var assetName = Path.GetFileName (path);
            assetName = (assetName == null) ? string.Empty : assetName;
            ev.Init (EventKind.Modify, assetName, assetGuid, graphGuid, n.Id, n.Name, string.Empty, string.Empty);
            return ev;
        }

        public static AssetProcessEvent CreateErrorEvent(NodeException e, string graphGuid) {
            var ev = new AssetProcessEvent();
            var assetId = (e.Asset == null) ? null : e.Asset.assetDatabaseId;
            var filename = (e.Asset == null) ? string.Empty : e.Asset.fileName;
            ev.Init (EventKind.Error, filename, assetId, graphGuid, e.Node.Id, e.Node.Name, e.Reason, e.HowToFix);
            return ev;
        }
    }
}

