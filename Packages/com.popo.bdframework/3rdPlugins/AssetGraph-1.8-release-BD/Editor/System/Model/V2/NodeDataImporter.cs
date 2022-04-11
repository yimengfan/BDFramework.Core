using V1=AssetBundleGraph;

namespace UnityEngine.AssetGraph.DataModel.Version2 {
	public interface NodeDataImporter {
		void Import(V1.NodeData v1, NodeData v2);
	}
}
