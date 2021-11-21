using UnityEditor;

using System;
using System.Collections.Generic;
using System.Linq;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    /// <summary>
    /// Node build context.
    /// </summary>
    public class NodeBuildContext {

        /// <summary>
        /// The target.
        /// </summary>
        public BuildTarget target;

        /// <summary>
        /// The node data.
        /// </summary>
        public Model.NodeData nodeData;

        /// <summary>
        /// The incoming.
        /// </summary>
        public IEnumerable<PerformGraph.AssetGroups> incoming;

        /// <summary>
        /// The connections to output.
        /// </summary>
        public IEnumerable<Model.ConnectionData> connectionsToOutput;

        /// <summary>
        /// The output func.
        /// </summary>
        public PerformGraph.Output outputFunc;

        /// <summary>
        /// The progress func.
        /// </summary>
        public Action<Model.NodeData, string, float> progressFunc;

        public Dictionary<string, string> groupName;

        public bool HasConnectionToOutput() {
            return connectionsToOutput != null && connectionsToOutput.Any ();
        }

        public bool CanOutput() {
            return outputFunc != null && HasConnectionToOutput ();
        }

        public Model.ConnectionData GetFirstOutputConnectionData() {
            return (HasConnectionToOutput ()) ? connectionsToOutput.First () : null;
        }
	}
}
