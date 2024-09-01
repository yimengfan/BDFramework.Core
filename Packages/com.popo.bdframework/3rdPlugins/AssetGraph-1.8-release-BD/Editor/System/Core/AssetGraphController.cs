using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    public class AssetGraphController
    {
        private List<NodeException> m_nodeExceptions;
        private AssetReferenceStreamManager m_streamManager;
        private Model.ConfigGraph m_targetGraph;
        private PerformGraph[] m_performGraph;
        private Model.NodeData m_currentNode;
        private int gIndex;

        private BuildTarget m_lastTarget;

        private bool m_isBuilding;

        public bool IsAnyIssueFound
        {
            get { return m_nodeExceptions.Count > 0; }
        }

        public Model.NodeData CurrentNode
        {
            get { return m_currentNode; }
        }

        public List<NodeException> Issues
        {
            get { return m_nodeExceptions; }
        }

        public AssetReferenceStreamManager StreamManager
        {
            get { return m_streamManager; }
        }

        public Model.ConfigGraph TargetGraph
        {
            get { return m_targetGraph; }
        }

        public BuildTarget ActiveTarget
        {
            get { return m_lastTarget; }
            set { Perform(value, false, false, false, null); }
        }

        public AssetGraphController(Model.ConfigGraph graph)
        {
            m_targetGraph = graph;
            m_nodeExceptions = new List<NodeException>();
            m_streamManager = new AssetReferenceStreamManager(m_targetGraph);
            m_performGraph = new PerformGraph[]
            {
                new PerformGraph(m_streamManager),
                new PerformGraph(m_streamManager)
            };
            gIndex = 0;
            m_currentNode = null;
        }

        /**
		 * Execute Run operations using current graph
		 */
        public bool Perform(
            BuildTarget target,
            bool isBuild,
            bool logIssues,
            bool forceVisitAll,
            Action<Model.NodeData, string, float> updateHandler)
        {
            if (m_targetGraph == null)
            {
                LogUtility.Logger.LogError(LogUtility.kTag, "Attempted to execute invalid graph (null)");
                return false;
            }

            AssetGraphPostprocessor.Postprocessor.PushController(this);

            LogUtility.Logger.Log(LogType.Log, (isBuild) ? "---Build BEGIN---" : "---Setup BEGIN---");
            m_isBuilding = true;

            if (isBuild)
            {
                AssetBundleBuildReport.ClearReports();
            }

            foreach (var e in m_nodeExceptions)
            {
                var errorNode = m_targetGraph.Nodes.Find(n => n.Id == e.NodeId);
                // errorNode may not be found if user delete it on graph
                if (errorNode != null)
                {
                    LogUtility.Logger.LogFormat(LogType.Log, "[Perform] {0} is marked to revisit due to last error", errorNode.Name);
                    errorNode.NeedsRevisit = true;
                }
            }

            m_nodeExceptions.Clear();
            m_lastTarget = target;

            try
            {
                PerformGraph oldGraph = m_performGraph[gIndex];
                gIndex = (gIndex + 1) % 2;
                PerformGraph newGraph = m_performGraph[gIndex];
                newGraph.BuildGraphFromSaveData(m_targetGraph, target, oldGraph);

                PerformGraph.Perform performFunc =
                    (Model.NodeData data,
                        IEnumerable<PerformGraph.AssetGroups> incoming,
                        IEnumerable<Model.ConnectionData> connectionsToOutput,
                        PerformGraph.Output outputFunc) =>
                    {
                        DoNodeOperation(target, data, incoming, connectionsToOutput, outputFunc, isBuild, updateHandler);
                    };

                newGraph.VisitAll(performFunc, forceVisitAll);

                if (isBuild && m_nodeExceptions.Count == 0)
                {
                    Postprocess();
                }
            }
            catch (NodeException e)
            {
                m_nodeExceptions.Add(e);
            }
            // Minimize impact of errors happened during node operation
            catch (Exception e)
            {
                LogUtility.Logger.LogException(e);
            }

            m_isBuilding = false;
            LogUtility.Logger.Log(LogType.Log, (isBuild) ? "---Build END---" : "---Setup END---");

            if (logIssues)
            {
                LogIssues();
            }

            AssetGraphPostprocessor.Postprocessor.PopController();

            return true;
        }

        public void Validate(
            NodeGUI node,
            BuildTarget target)
        {
            m_nodeExceptions.RemoveAll(e => e.NodeId == node.Data.Id);

            try
            {
                LogUtility.Logger.LogFormat(LogType.Log, "[validate] {0} validate", node.Name);
                m_isBuilding = true;

                AssetGraphPostprocessor.Postprocessor.PushController(this);
                DoNodeOperation(target, node.Data, null, null,
                    (Model.ConnectionData dst, Dictionary<string, List<AssetReference>> outputGroupAsset) => { },
                    false, null);
                AssetGraphPostprocessor.Postprocessor.PopController();

                if (!IsAnyIssueFound)
                {
                    LogUtility.Logger.LogFormat(LogType.Log, "[Perform] {0} ", node.Name);
                    Perform(target, false, false, false, null);
                }

                m_isBuilding = false;
            }
            catch (NodeException e)
            {
                m_nodeExceptions.Add(e);
            }
        }

        private void LogIssues()
        {
            var r = AssetProcessEventRecord.GetRecord();
            foreach (var e in m_nodeExceptions)
            {
                r.LogError(e);
            }
        }

        /**
			Perform Run or Setup from parent of given terminal node recursively.
		*/
        private void DoNodeOperation(
            BuildTarget target,
            Model.NodeData currentNodeData,
            IEnumerable<PerformGraph.AssetGroups> incoming,
            IEnumerable<Model.ConnectionData> connectionsToOutput,
            PerformGraph.Output outputFunc,
            bool isActualRun,
            Action<Model.NodeData, string, float> updateHandler)
        {
            try
            {
                m_currentNode = currentNodeData;

                if (updateHandler != null)
                {
                    updateHandler(currentNodeData, "Starting...", 0f);
                }

                if (isActualRun)
                {
                    currentNodeData.Operation.Object.Build(target, currentNodeData, incoming, connectionsToOutput, outputFunc, updateHandler);
                }
                else
                {
                    currentNodeData.Operation.Object.Prepare(target, currentNodeData, incoming, connectionsToOutput, outputFunc);
                }

                if (updateHandler != null)
                {
                    updateHandler(currentNodeData, "Completed.", 1f);
                }
            }
            catch (NodeException e)
            {
                m_nodeExceptions.Add(e);
            }
            // Minimize impact of errors happened during node operation
            catch (Exception e)
            {
                m_nodeExceptions.Add(new NodeException($"{e.GetType().ToString()}:{e.Message}", "See Console for detail.", currentNodeData));
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }

            m_currentNode = null;
        }

        private void Postprocess()
        {
            var postprocessType = typeof(IPostprocess);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var ppTypes = assembly.GetTypes().Select(v => v).Where(v => v != postprocessType && postprocessType.IsAssignableFrom(v)).ToList();
                    foreach (var t in ppTypes)
                    {
                        var postprocessScriptInstance = assembly.CreateInstance(t.FullName);
                        if (postprocessScriptInstance == null)
                        {
                            throw new AssetGraphException("Postprocess " + t.Name + " failed to run (failed to create instance from assembly).");
                        }

                        var postprocessInstance = (IPostprocess) postprocessScriptInstance;
                        postprocessInstance.DoPostprocess(AssetBundleBuildReport.BuildReports, AssetBundleBuildReport.ExportReports);
                    }
                }
                catch (Exception e)
                {
                    
                }
            
            }
        }

        public void OnAssetsReimported(AssetPostprocessorContext ctx)
        {
            // ignore asset reimport event during build
            if (m_isBuilding)
            {
                return;
            }

            if (m_targetGraph.Nodes == null)
            {
                return;
            }

            bool isAnyNodeAffected = false;

            foreach (var n in m_targetGraph.Nodes)
            {
                bool affected = n.Operation.Object.OnAssetsReimported(n, m_streamManager, m_lastTarget, ctx, false);
                if (affected)
                {
                    n.NeedsRevisit = true;
                }

                isAnyNodeAffected |= affected;
            }

            if (isAnyNodeAffected)
            {
                Perform(m_lastTarget, false, false, false, null);
            }
        }
    }
}
