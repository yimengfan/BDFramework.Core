using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.AssetGraph.DataModel.Version2
{
    /*
     * Save data which holds all AssetBundleGraph settings and configurations.
     */
    [CreateAssetMenu(fileName = "New AssetGraph", menuName = "Asset Graph", order = 650)]
    public class ConfigGraph : ScriptableObject
    {
        /*
         * Important: 
         * ABG_FILE_VERSION must be increased by one when any structure change(s) happen
         */
        public const int ABG_FILE_VERSION = 2;

        [SerializeField] private List<NodeData> m_allNodes;
        [SerializeField] private List<ConnectionData> m_allConnections;
        [SerializeField] private string m_lastModified;
        [SerializeField] private int m_version;
        [SerializeField] private string m_graphDescription;
        [SerializeField] private bool m_useAsAssetPostprocessor;
        [SerializeField] private int m_graphExecOrderPriority;

        void OnEnable()
        {
            Initialize();
            Validate();
        }

        private string GetFileTimeUtcString()
        {
            return DateTime.UtcNow.ToFileTimeUtc().ToString();
        }

        private void Initialize()
        {
            if (string.IsNullOrEmpty(m_lastModified))
            {
                m_lastModified = GetFileTimeUtcString();
                m_allNodes = new List<NodeData>();
                m_allConnections = new List<ConnectionData>();
                m_version = ABG_FILE_VERSION;
                m_graphDescription = String.Empty;
                m_graphExecOrderPriority = Settings.GRAPHEXECPRIORITY_DEFAULT;
                EditorUtility.SetDirty(this);
            }
        }

        private void Import(AssetBundleGraph.SaveData v1)
        {
            m_lastModified = GetFileTimeUtcString();
            m_version = ABG_FILE_VERSION;

            foreach (var n in v1.Nodes)
            {
                m_allNodes.Add(new NodeData(n));
            }

            foreach (var c in v1.Connections)
            {
                m_allConnections.Add(new ConnectionData(c));
            }

            EditorUtility.SetDirty(this);
        }

        public bool UseAsAssetPostprocessor
        {
            get { return m_useAsAssetPostprocessor; }
            set
            {
                m_useAsAssetPostprocessor = value;
                SetGraphDirty();
            }
        }

        public DateTime LastModified
        {
            get
            {
                long utcFileTime = long.Parse(m_lastModified);
                DateTime d = DateTime.FromFileTimeUtc(utcFileTime);

                return d;
            }
        }

        public string Descrption
        {
            get { return m_graphDescription; }
            set
            {
                m_graphDescription = value;
                SetGraphDirty();
            }
        }

        public int Version
        {
            get { return m_version; }
        }

        public int ExecuteOrderPriority
        {
            get { return m_graphExecOrderPriority; }
            set { m_graphExecOrderPriority = value; }
        }

        public List<NodeData> Nodes
        {
            get { return m_allNodes; }
        }

        public List<ConnectionData> Connections
        {
            get { return m_allConnections; }
        }

        public List<NodeData> CollectAllLeafNodes()
        {
            var nodesWithChild = new List<NodeData>();
            foreach (var c in m_allConnections)
            {
                NodeData n = m_allNodes.Find(v => v.Id == c.FromNodeId);
                if (n != null)
                {
                    nodesWithChild.Add(n);
                }
            }

            return m_allNodes.Except(nodesWithChild).ToList();
        }

        public void Save()
        {
            m_allNodes.ForEach(n => n.Operation.Save());
            SetGraphDirty();
        }

        public void SetGraphDirty()
        {
            EditorUtility.SetDirty(this);
        }

        public string GetGraphName()
        {
            var path = AssetDatabase.GetAssetOrScenePath(this);
            return Path.GetFileNameWithoutExtension(path);
        }

        public string GetGraphGuid()
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
        }

        //
        // Save/Load to disk
        //

        public void ApplyGraph(List<NodeGUI> nodes, List<ConnectionGUI> connections)
        {
            List<NodeData> n = nodes.Select(v => v.Data).ToList();
            List<ConnectionData> c = connections.Select(v => v.Data).ToList();

            if (!Enumerable.SequenceEqual(n.OrderBy(v => v.Id), m_allNodes.OrderBy(v => v.Id)) ||
                !Enumerable.SequenceEqual(c.OrderBy(v => v.Id), m_allConnections.OrderBy(v => v.Id)))
            {
                LogUtility.Logger.Log("[ApplyGraph] SaveData updated.");

                m_version = ABG_FILE_VERSION;
                m_lastModified = GetFileTimeUtcString();
                m_allNodes = n;
                m_allConnections = c;
                Save();
            }
            else
            {
                LogUtility.Logger.Log("[ApplyGraph] SaveData update skipped. graph is equivarent.");
            }
        }
        
        public NodeData AddNode(Type nodeType)
        {
            return AddNode(string.Empty, nodeType, 100, 100);
        }

        public NodeData AddNode(string name, Type nodeType, int x, int y)
        {            
            var nodeInstance = NodeUtility.CreateNodeInstance(nodeType.AssemblyQualifiedName);
            var node = new NodeData(name, nodeInstance, x, y);
            m_allNodes.Add(node);
            return node;
        }
        
        public ConnectionData Connect(string label, ConnectionPointData outputPort, ConnectionPointData inputPort)
        {
            if (!outputPort.IsOutput || !inputPort.IsInput)
            {
                return null;
            }

            var outputNode = m_allNodes.FirstOrDefault(n => n.Id == outputPort.NodeId);
            var inputNode  = m_allNodes.FirstOrDefault(n => n.Id == inputPort.NodeId);

            if (outputNode == null || inputNode == null)
            {
                return null;
            }
            
            var existingConnections = m_allConnections
                .Where (con => con.FromNodeConnectionPointId == outputPort.Id)
                .Where(con => con.FromNodeConnectionPointId != inputPort.Id).ToList();

            foreach (var c in existingConnections)
            {
                m_allConnections.Remove(c);
            }

            var newConnection = new ConnectionData(label, outputPort, inputPort);
            m_allConnections.Add(newConnection);
            
            return newConnection;
        }
        

        public static ConfigGraph CreateNewGraph()
        {
            return CreateInstance<ConfigGraph>();
        }
        
        public static ConfigGraph CreateNewGraph(string pathToSave)
        {
            var data = CreateInstance<ConfigGraph>();
            AssetDatabase.CreateAsset(data, pathToSave);
            return data;
        }

        public static ConfigGraph CreateNewGraphFromImport(string pathToLoad)
        {
            // try load from v1.
            try
            {
                var loadedData = AssetBundleGraph.SaveData.LoadSaveData(pathToLoad);
                var newGraph = CreateNewGraph(Settings.Path.ASSETS_PATH + "importedGraph.asset");
                newGraph.Import(loadedData);

                return newGraph;
            }
            catch (Exception e)
            {
                LogUtility.Logger.LogError(LogUtility.kTag, "Failed to import graph from previous version." + e);
            }

            return null;
        }

        /*
         * Checks deserialized SaveData, and make some changes if necessary
         * return false if any changes are perfomed.
         */
        private bool Validate()
        {
            var changed = false;

            if (m_allNodes != null)
            {
                List<NodeData> removingNodes = new List<NodeData>();
                foreach (var n in m_allNodes)
                {
                    if (!n.Validate())
                    {
                        removingNodes.Add(n);
                        changed = true;
                        LogUtility.Logger.LogFormat(LogType.Error,
                            "Validation failed for node \"{0}\". Class \"{1}\" not found or failed to instantiate.", n.Name,
                            n.Operation.ClassName);
                    }
                }

                m_allNodes.RemoveAll(n => removingNodes.Contains(n));
            }

            if (m_allConnections != null)
            {
                List<ConnectionData> removingConnections = new List<ConnectionData>();
                foreach (var c in m_allConnections)
                {
                    if (!c.Validate(m_allNodes, m_allConnections))
                    {
                        removingConnections.Add(c);
                        changed = true;

                        LogUtility.Logger.LogFormat(LogType.Error,
                            "Validation failed for connection \"{0}\". One of connecting node not found.", c.Label);
                    }
                }

                m_allConnections.RemoveAll(c => removingConnections.Contains(c));
            }

            if (changed)
            {
                m_lastModified = GetFileTimeUtcString();
            }

            return !changed;
        }
    }
}