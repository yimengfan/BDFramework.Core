using System;
using System.Collections.Generic;
using V1=AssetBundleGraph;

namespace UnityEngine.AssetGraph.DataModel.Version2 {

    /// <summary>
    /// Node output semantics.
    /// </summary>
	public enum NodeOutputSemantics : uint {
        /// <summary>
        /// The none.
        /// </summary>
		None 						= 0,

        /// <summary>
        /// Any.
        /// </summary>
		Any  						= 0xFFFFFFFF,

        /// <summary>
        /// The assets.
        /// </summary>
		Assets						= 1,

        /// <summary>
        /// The asset bundle configurations.
        /// </summary>
		AssetBundleConfigurations 	= 2,

        /// <summary>
        /// The asset bundles.
        /// </summary>
		AssetBundles 				= 4
	}

    /// <summary>
    /// Connection data.
    /// </summary>
	[Serializable]
	public class ConnectionData {

		[SerializeField] private string m_id;
		[SerializeField] private string m_fromNodeId;
		[SerializeField] private string m_fromNodeConnectionPointId;
		[SerializeField] private string m_toNodeId;
		[SerializeField] private string m_toNodeConnectionPoiontId;
		[SerializeField] private string m_label;

		private ConnectionData() {
			m_id = Guid.NewGuid().ToString();
		}

		public ConnectionData(string label, ConnectionPointData output, ConnectionPointData input) {
			m_id = Guid.NewGuid().ToString();
			m_label = label;
			m_fromNodeId = output.NodeId;
			m_fromNodeConnectionPointId = output.Id;
			m_toNodeId = input.NodeId;
			m_toNodeConnectionPoiontId = input.Id;
		}

		public ConnectionData(V1.ConnectionData v1) {
			m_id = v1.Id;
			m_fromNodeId = v1.FromNodeId;
			m_fromNodeConnectionPointId = v1.FromNodeConnectionPointId;
			m_toNodeId = v1.ToNodeId;
			m_toNodeConnectionPoiontId = v1.ToNodeConnectionPointId;
			m_label = v1.Label;
		}

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
		public string Id {
			get {
				return m_id;
			}
		}

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
		public string Label {
			get {
				return m_label;
			}

			set {
				m_label = value;
			}
		}

        /// <summary>
        /// Gets from node identifier.
        /// </summary>
        /// <value>From node identifier.</value>
		public string FromNodeId {
			get {
				return m_fromNodeId;
			}
		}

        /// <summary>
        /// Gets from node connection point identifier.
        /// </summary>
        /// <value>From node connection point identifier.</value>
		public string FromNodeConnectionPointId {
			get {
				return m_fromNodeConnectionPointId;
			}
		}

        /// <summary>
        /// Gets to node identifier.
        /// </summary>
        /// <value>To node identifier.</value>
		public string ToNodeId {
			get {
				return m_toNodeId;
			}
		}

        /// <summary>
        /// Gets to node connection point identifier.
        /// </summary>
        /// <value>To node connection point identifier.</value>
		public string ToNodeConnectionPointId {
			get {
				return m_toNodeConnectionPoiontId;
			}
		}

		public ConnectionData Duplicate(bool keepGuid = false) {
			ConnectionData newData = new ConnectionData();
			if(keepGuid) {
				newData.m_id = m_id;
			}
			newData.m_label = m_label;
			newData.m_fromNodeId = m_fromNodeId;
			newData.m_fromNodeConnectionPointId = m_fromNodeConnectionPointId;
			newData.m_toNodeId = m_toNodeId;
			newData.m_toNodeConnectionPoiontId = m_toNodeConnectionPoiontId;

			return newData;
		}

		public bool Validate (List<NodeData> allNodes, List<ConnectionData> allConnections) {

			var fromNode = allNodes.Find(n => n.Id == this.FromNodeId);
			var toNode   = allNodes.Find(n => n.Id == this.ToNodeId);

			if(fromNode == null) {
				return false;
			}

			if(toNode == null) {
				return false;
			}

			var outputPoint = fromNode.FindOutputPoint(this.FromNodeConnectionPointId);
			var inputPoint  = toNode.FindInputPoint(this.ToNodeConnectionPointId);

			if(null == outputPoint) {
				return false;
			}

			if(null == inputPoint) {
				return false;
			}

			// update connection label if not matching with outputPoint label
			if( outputPoint.Label != m_label ) {
				m_label = outputPoint.Label;
			}

			return true;
		}

        /// <summary>
        /// Determines if can connect the specified from to.
        /// </summary>
        /// <returns><c>true</c> if can connect the specified from to; otherwise, <c>false</c>.</returns>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
		public static bool CanConnect (NodeData from, NodeData to) {

			var inType  = (uint)to.Operation.Object.NodeInputType;
			var outType = (uint)from.Operation.Object.NodeOutputType;

			return (inType & outType) > 0;
		}
	}
}
