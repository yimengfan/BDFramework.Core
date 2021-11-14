using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class PrefabBuildInfo : ScriptableObject {

		[Serializable]
		private class UsedAsset {
			public string importFrom;
			public string assetGuid;
			public string lastUpdated; // long is not supported by Text Serializer, so save it in string.

			public UsedAsset(string _importFrom) {
				importFrom = _importFrom;
				assetGuid = AssetDatabase.AssetPathToGUID(importFrom);
				lastUpdated = File.GetLastWriteTimeUtc(importFrom).ToFileTimeUtc().ToString();
			}

			public bool IsAssetModifiedFromLastTime {
				get {
					if(!File.Exists(importFrom)) {
						return true;
					}
					if(lastUpdated != File.GetLastWriteTimeUtc(importFrom).ToFileTimeUtc().ToString()) {
						return true;
					}
					if(assetGuid != AssetDatabase.AssetPathToGUID(importFrom)) {
						return true;
					}

					return false;
				}
			}
		}

		[SerializeField] private string m_groupKey;
		[SerializeField] private string m_builderClass;
		[SerializeField] private string m_instanceData;
		[SerializeField] private string m_prefabBuilderVersion;
		[SerializeField] private List<UsedAsset> m_usedAssets;
		[SerializeField] private string m_usedAssetsHash;
        [SerializeField] private string m_buildDir;

		private void Initialize(string buildDir, string groupKey, string className, string instanceData, string version, 
			List<AssetReference> assets, PrefabCreateDescription createDescription) 
		{
			m_groupKey = groupKey;
			m_builderClass = className;
			m_instanceData = instanceData;
			m_prefabBuilderVersion = version;
            m_buildDir = buildDir;

			m_usedAssets = new List<UsedAsset> ();
			assets.ForEach(a => m_usedAssets.Add(new UsedAsset(a.importFrom)));
			createDescription.additionalAssetPaths.ForEach(path => m_usedAssets.Add(new UsedAsset(path)));

			var hash1 = MD5.Create();
			assets.ForEach(a => hash1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(a.importFrom)));
			createDescription.additionalAssetPaths.ForEach(path => hash1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path)));
			m_usedAssetsHash = hash1.ToString();
		}

		private static PrefabBuildInfo GetPrefabBuildInfo(PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey) {

            var prefabCacheDir = FileUtility.EnsureCacheDirExists(target, node, PrefabBuilder.kCacheDirName);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			return AssetDatabase.LoadAssetAtPath<PrefabBuildInfo>(buildInfoPath);
		}

		public static bool DoesPrefabNeedRebuilding(string buildPath, PrefabBuilder builder, Model.NodeData node, BuildTarget target, 
			string groupKey, List<AssetReference> assets, PrefabCreateDescription createDescription) 
		{
			var buildInfo = GetPrefabBuildInfo(builder, node, target, groupKey);

			// need rebuilding if no buildInfo found
			if(buildInfo == null) {
				return true;
			}

            // need rebuilding if build path is changed
            if(buildInfo.m_buildDir != buildPath) {
                return true;
            }

            // need rebuilding if given builder is changed
			if(buildInfo.m_builderClass != builder.Builder.ClassName) {
				return true;
			}

			// need rebuilding if given builder is changed
			if(buildInfo.m_instanceData != builder.Builder[target]) {
				return true;
			}

			var builderVersion = PrefabBuilderUtility.GetPrefabBuilderVersion(builder.Builder.ClassName);

			// need rebuilding if given builder version is changed
			if(buildInfo.m_prefabBuilderVersion != builderVersion) {
				return true;
			}

			// need rebuilding if given groupKey changed
			if(buildInfo.m_groupKey != groupKey) {
				return true;
			}
			
			var hash1 = MD5.Create();
			assets.ForEach(a => hash1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(a.importFrom)));			
			createDescription.additionalAssetPaths.ForEach(path => hash1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path)));
			if (buildInfo.m_usedAssetsHash != hash1.ToString())
			{
				return true;
			}

			// If any asset is modified from last time, then need rebuilding
			foreach(var usedAsset in buildInfo.m_usedAssets) {
				if(usedAsset.IsAssetModifiedFromLastTime) {
					return true;
				}
			}

			return false;
		}

		public static void SavePrefabBuildInfo(string buildPath, PrefabBuilder builder, Model.NodeData node, BuildTarget target, 
			string groupKey, List<AssetReference> assets, PrefabCreateDescription description) 
		{

            var prefabCacheDir = FileUtility.EnsureCacheDirExists(target, node, PrefabBuilder.kCacheDirName);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			var version = PrefabBuilderUtility.GetPrefabBuilderVersion(builder.Builder.ClassName);

			var buildInfo = CreateInstance<PrefabBuildInfo>();
            buildInfo.Initialize(buildPath, groupKey, builder.Builder.ClassName, builder.Builder[target], version, assets, description);

			AssetDatabase.CreateAsset(buildInfo, buildInfoPath);		
		}
	}
}