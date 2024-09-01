using System.Linq;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class AssetReferenceStreamManager {

		// key: connectiondata id
		private Dictionary<string, Dictionary<string, List<AssetReference>>> m_connectionStreamMap;
		private Model.ConfigGraph m_targetGraph;

		public AssetReferenceStreamManager(Model.ConfigGraph graph) {
			m_connectionStreamMap = new Dictionary<string, Dictionary<string, List<AssetReference>>>();
			m_targetGraph = graph;
		}

		public IEnumerable<Dictionary<string, List<AssetReference>>> EnumurateIncomingAssetGroups(Model.ConnectionPointData inputPoint) {
			UnityEngine.Assertions.Assert.IsNotNull(inputPoint);
			UnityEngine.Assertions.Assert.IsTrue (inputPoint.IsInput);

			var connections = m_targetGraph.Connections;

			return m_connectionStreamMap.Where(v => { 
				var conn = connections.Find(c => c.Id == v.Key);
				return conn!= null && conn.ToNodeConnectionPointId == inputPoint.Id;
			}).Select(v => v.Value);
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(string connectionId) {

			if (!m_connectionStreamMap.ContainsKey(connectionId)) {
				m_connectionStreamMap[connectionId] = new Dictionary<string, List<AssetReference>>();
			}

			return m_connectionStreamMap[connectionId];
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(Model.ConnectionData connection) {
			if (!m_connectionStreamMap.ContainsKey(connection.Id)) {
				m_connectionStreamMap[connection.Id] = new Dictionary<string, List<AssetReference>>();
			}

			return m_connectionStreamMap[connection.Id];
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(Model.ConnectionPointData point) {

			var connection = (point.IsInput) ?
				m_targetGraph.Connections.Find(c => c.ToNodeConnectionPointId == point.Id):
				m_targetGraph.Connections.Find(c => c.FromNodeConnectionPointId == point.Id);

			if(connection == null) {
				return new Dictionary<string, List<AssetReference>>();
			}

			if (!m_connectionStreamMap.ContainsKey(connection.Id)) {
				m_connectionStreamMap[connection.Id] = new Dictionary<string, List<AssetReference>>();
			}

			return m_connectionStreamMap[connection.Id];
		}


		public void AssignAssetGroup(Model.ConnectionData connection, Dictionary<string, List<AssetReference>> groups) {
			m_connectionStreamMap[connection.Id] = groups;
		}

		public void RemoveAssetGroup(Model.ConnectionData connection) {
			if (m_connectionStreamMap.ContainsKey(connection.Id)) { 
				m_connectionStreamMap.Remove(connection.Id);
			}
		}
	}
}
