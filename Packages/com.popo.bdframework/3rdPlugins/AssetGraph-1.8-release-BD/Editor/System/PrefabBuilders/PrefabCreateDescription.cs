using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class PrefabCreateDescription
	{
		/// <summary>
		/// Asset path to creating prefab.
		/// </summary>
		public string prefabName;
		
		/// <summary>
		/// Paths to additional assets to take into account other than given objects from node, such as objects assigned via inspector.
		/// </summary>
		public List<string> additionalAssetPaths;

		public PrefabCreateDescription()
		{
			additionalAssetPaths = new List<string>(32);
		}

		public void Reset()
		{
			prefabName = string.Empty;
			additionalAssetPaths.Clear();
		}
	}
}