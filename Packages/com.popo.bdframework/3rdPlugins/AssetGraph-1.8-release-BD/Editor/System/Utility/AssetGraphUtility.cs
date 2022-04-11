using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    /// <summary>
    /// Execute graph result.
    /// </summary>
	public class ExecuteGraphResult {
        private Model.ConfigGraph  	m_graph;
        private List<NodeException>	m_issues;
        private BuildTarget m_target;

        public ExecuteGraphResult(BuildTarget t, Model.ConfigGraph g, List<NodeException> i) {
            this.m_target = t;
			this.m_graph  = g;
			this.m_issues = i;
		}

        /// <summary>
        /// Gets a value indicating whether last graph execution has any issue found.
        /// </summary>
        /// <value><c>true</c> if this instance is any issue found; otherwise, <c>false</c>.</value>
		public bool IsAnyIssueFound {
			get {
                return m_issues != null && m_issues.Count > 0;
			}
		}

        /// <summary>
        /// Gets the executed build target.
        /// </summary>
        /// <value>The build target.</value>
        public BuildTarget Target {
            get {
                return m_target;
            }
        }

        /// <summary>
        /// Gets the executed graph associated with this result.
        /// </summary>
        /// <value>The graph.</value>
		public Model.ConfigGraph Graph {
			get {
				return m_graph;
			}
		}

        /// <summary>
        /// Gets the graph asset path.
        /// </summary>
        /// <value>The graph asset path.</value>
		public string GraphAssetPath {
			get {
				return AssetDatabase.GetAssetPath(m_graph);
			}
		}

        /// <summary>
        /// Gets the list of issues found during last execution.
        /// </summary>
		public IEnumerable<NodeException> Issues {
			get {
                if (m_issues != null) {
                    return m_issues.AsEnumerable();
                }
                return null;
			}
		}
	}

    /// <summary>
    /// The helper utility to execute graph and graph collection from API.
    /// </summary>
	public class AssetGraphUtility {

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="collectionName">Collection name.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(string collectionName) {
			return ExecuteGraphCollection(EditorUserBuildSettings.activeBuildTarget, collectionName);
		}

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="t">T.</param>
        /// <param name="collectionName">Collection name.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(BuildTarget t, string collectionName) {
			var c = BatchBuildConfig.GetConfig().Find(collectionName);
			if(c == null) {
				throw new AssetGraphException(
					$"Failed to build with graph collection. Graph collection '{collectionName}' not found. "
				);
			}

			return ExecuteGraphCollection(t, c);
		}

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="c">C.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(BatchBuildConfig.GraphCollection c) {
			return ExecuteGraphCollection(EditorUserBuildSettings.activeBuildTarget, c);
		}

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="t">T.</param>
        /// <param name="c">C.</param>
        public static List<ExecuteGraphResult> ExecuteGraphCollection(BuildTarget t, BatchBuildConfig.GraphCollection c, Action<Model.NodeData, string, float> updateHandler = null) {

            AssetBundleBuildMap.GetBuildMap ().Clear ();

            List<ExecuteGraphResult> resultCollection = new List<ExecuteGraphResult>(c.GraphGUIDs.Count);

			foreach(var guid in c.GraphGUIDs) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if(path != null) {
                    var r = ExecuteGraph(t, path, false, updateHandler);
					resultCollection.Add(r);
				} else {
					LogUtility.Logger.LogFormat(LogType.Warning, "Failed to build graph in collection {0}: graph with guid {1} not found.",
						c.Name, guid);
				}
			}

			return  resultCollection;
		}

        /// <summary>
        /// Executes all given graphs
        /// </summary>
        /// <returns>collection of graph execute result.</returns>
        /// <param name="graphGuids">List of graph asset's guid.</param>
        /// <param name="sortExecuteOrder">Wheather to sort execute order by ExecuteOrderPriority.</param>
        public static List<ExecuteGraphResult> ExecuteAllGraphs(List<string> graphGuids, bool sortExecuteOrder = true) {

            List<ExecuteGraphResult> resultCollection = new List<ExecuteGraphResult>(graphGuids.Count);
            List<Model.ConfigGraph> graphs = new List<Model.ConfigGraph> ();

            foreach(var guid in graphGuids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var g = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph> (path);

                if (g != null) {
                    graphs.Add (g);
                }
            }

            foreach (var graph in graphs.OrderBy(g => g.ExecuteOrderPriority)) {
                var r = ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graph, false);
                resultCollection.Add(r);
            }

            return  resultCollection;
        }


        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graphAssetPath">Graph asset path.</param>
        public static ExecuteGraphResult ExecuteGraph(string graphAssetPath, bool clearRecord = false, Action<Model.NodeData, string, float> updateHandler = null) {
            return ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graphAssetPath, clearRecord, updateHandler);
		}
            
        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graphGuid">Graph asset guid.</param>
        public static ExecuteGraphResult ExecuteGraphByGuid(string graphGuid, bool clearRecord = false, Action<Model.NodeData, string, float> updateHandler = null) {
            return ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, AssetDatabase.GUIDToAssetPath(graphGuid), clearRecord, updateHandler);
        }

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graph">Graph.</param>
        public static ExecuteGraphResult ExecuteGraph(Model.ConfigGraph graph, bool clearRecord = false, Action<Model.NodeData, string, float> updateHandler = null) {
            return ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graph, clearRecord, updateHandler);
		}

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="target">Target.</param>
        /// <param name="graphAssetPath">Graph asset path.</param>
        public static ExecuteGraphResult ExecuteGraph(BuildTarget target, string graphAssetPath, bool clearRecord = false, Action<Model.NodeData, string, float> updateHandler = null) {
            return ExecuteGraph(target, AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphAssetPath), clearRecord, updateHandler);
		}

        /// <summary>
        /// Execute Graph in Setup mode, and do not do Build.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graphAssetPath">Graph asset path.</param>
        public static ExecuteGraphResult ExecuteGraphSetup(string graphAssetPath) {
            return ExecuteGraphSetup(EditorUserBuildSettings.activeBuildTarget, graphAssetPath);
        }

        /// <summary>
        /// Execute Graph in Setup mode, and do not do Build.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graphGuid">Graph asset guid.</param>
        public static ExecuteGraphResult ExecuteGraphSetupByGuid(string graphGuid) {
            return ExecuteGraphSetup(EditorUserBuildSettings.activeBuildTarget, AssetDatabase.GUIDToAssetPath(graphGuid));
        }

        /// <summary>
        /// Execute Graph in Setup mode, and do not do Build.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graph">Graph.</param>
        public static ExecuteGraphResult ExecuteGraphSetup(Model.ConfigGraph graph) {
            return ExecuteGraphSetup(EditorUserBuildSettings.activeBuildTarget, graph);
        }

        /// <summary>
        /// Execute Graph in Setup mode, and do not do Build.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="target">Target.</param>
        /// <param name="graphAssetPath">Graph asset path.</param>
        public static ExecuteGraphResult ExecuteGraphSetup(BuildTarget target, string graphAssetPath) {
            return ExecuteGraphSetup(target, AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphAssetPath));
        }

        /// <summary>
        /// Execute Graph in Setup mode, and do not do Build.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="target">Target.</param>
        /// <param name="graph">Graph.</param>
        public static ExecuteGraphResult ExecuteGraphSetup(BuildTarget target, Model.ConfigGraph graph) {
            AssetGraphController c = new AssetGraphController(graph);
            c.Perform(target, false, true, true, null);
            return new ExecuteGraphResult(target, graph, c.Issues);
        }

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="target">Target.</param>
        /// <param name="graph">Graph.</param>
        public static ExecuteGraphResult ExecuteGraph(BuildTarget target, Model.ConfigGraph graph, bool clearRecord = false, Action<Model.NodeData, string, float> updateHandler = null) {

            if (clearRecord) {
                AssetProcessEventRecord.GetRecord().Clear (false);
            }

			string assetPath = AssetDatabase.GetAssetPath(graph);

			LogUtility.Logger.LogFormat(LogType.Log, "Executing graph:{0}", assetPath);

			AssetGraphController c = new AssetGraphController(graph);

			// perform setup. Fails if any exception raises.
			c.Perform(target, false, true, true, null);

			// if there is error reported, then run
			if(c.IsAnyIssueFound) {
				return new ExecuteGraphResult(target, graph, c.Issues);
			}

			Model.NodeData lastNodeData = null;
			float lastProgress = 0.0f;

			Action<Model.NodeData, string, float> defaultUpdateHandler = (Model.NodeData node, string message, float progress) => {
				if(node != null && lastNodeData != node) {
					lastNodeData = node;
					lastProgress = progress;

					LogUtility.Logger.LogFormat(LogType.Log, "Processing {0}", node.Name);
				}
				if(progress > lastProgress) {
					if(progress <= 1.0f) {
						LogUtility.Logger.LogFormat(LogType.Log, "{0} Complete.", node.Name);
					} else if( (progress - lastProgress) > 0.2f ) {
						LogUtility.Logger.LogFormat(LogType.Log, "{0}: {1} % : {2}", node.Name, (int)progress*100f, message);
					}
					lastProgress = progress;
				}
			};

            if (updateHandler == null) {
                updateHandler = defaultUpdateHandler;
            }

			// run datas.
			c.Perform(target, true, true, true, updateHandler);

			AssetDatabase.Refresh();

			return new ExecuteGraphResult(target, graph, c.Issues);
		}
	}
}
