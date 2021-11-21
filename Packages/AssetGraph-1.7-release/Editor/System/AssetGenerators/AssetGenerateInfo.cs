using UnityEditor;

using System;
using System.IO;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class AssetGenerateInfo : ScriptableObject {

		[Serializable]
		class UsedAsset {
			public string importFrom;
			public string assetGuid;
			public string lastUpdated; // long is not supported by Text Serializer, so save it in string.

			public UsedAsset(string importFrom) {
				this.importFrom = importFrom;
				this.assetGuid = AssetDatabase.AssetPathToGUID(importFrom);

                var importer = AssetImporter.GetAtPath (importFrom);
                if(importer != null) {
                    lastUpdated = importer.assetTimeStamp.ToString();
                } else {
                    this.lastUpdated = File.GetLastWriteTimeUtc (importFrom).ToFileTimeUtc ().ToString ();
                }
			}

			public bool IsAssetModifiedFromLastTime {
				get {
					if(!File.Exists(importFrom)) {
						return true;
					}

                    var importer = AssetImporter.GetAtPath (importFrom);
                    if(importer != null) {
                        var ts = importer.assetTimeStamp.ToString();
                        if (ts != lastUpdated) {
                            return true;
                        }
                    } else {
                        var ts = File.GetLastWriteTimeUtc (importFrom).ToFileTimeUtc ().ToString ();
                        if (ts != lastUpdated) {
                            return true;
                        }
                    }

					if(assetGuid != AssetDatabase.AssetPathToGUID(importFrom)) {
						return true;
					}

					return false;
				}
			}
		}

        [SerializeField] private string m_generatorClass;
		[SerializeField] private string m_instanceData;
        [SerializeField] private string m_generatorVersion;
		[SerializeField] private UsedAsset m_usedAsset;

        public AssetGenerateInfo() {}

        public void Initialize(string className, string instanceData, string version, AssetReference asset) {
			m_generatorClass = className;
			m_instanceData = instanceData;
			m_generatorVersion = version;
            m_usedAsset = new UsedAsset (asset.importFrom);
		}

        static public bool DoesAssetNeedRegenerate(AssetGenerator.GeneratorEntry entry, Model.NodeData node, BuildTarget target, AssetReference asset) {
            var generateInfo = GetAssetGenerateInfo(entry, node, target, asset);

			// need rebuilding if no buildInfo found
			if(generateInfo == null) {
				return true;
			}

			// need rebuilding if given builder is changed
            if(generateInfo.m_generatorClass != entry.m_instance.ClassName) {
				return true;
			}

			// need rebuilding if given builder is changed
            if(generateInfo.m_instanceData != entry.m_instance[target]) {
				return true;
			}

            var version = AssetGeneratorUtility.GetVersion(entry.m_instance.ClassName);

			// need rebuilding if given builder version is changed
            if(generateInfo.m_generatorVersion != version) {
				return true;
			}

            if (generateInfo.m_usedAsset.importFrom != asset.importFrom) {
                return true;
            }

			// If asset is modified from last time, then need rebuilding
            if(generateInfo.m_usedAsset.IsAssetModifiedFromLastTime) {
                return true;
            }

			return false;
		}

        static private AssetGenerateInfo GetAssetGenerateInfo(AssetGenerator.GeneratorEntry entry, Model.NodeData node, BuildTarget target, AssetReference asset) {

            var cacheDir = FileUtility.EnsureCacheDirExists(target, node, AssetGenerator.kCacheDirName);
            var generateInfoDir = FileUtility.PathCombine (cacheDir, entry.m_id);
            var generatorInfoPath = FileUtility.PathCombine(generateInfoDir, asset.fileNameAndExtension + ".asset");

            return AssetDatabase.LoadAssetAtPath<AssetGenerateInfo>(generatorInfoPath);
        }

        static public void SaveAssetGenerateInfo(AssetGenerator.GeneratorEntry entry, Model.NodeData node, BuildTarget target, AssetReference asset) {

            var cacheDir = FileUtility.EnsureCacheDirExists(target, node, AssetGenerator.kCacheDirName);
            var generateInfoDir = FileUtility.PathCombine (cacheDir, entry.m_id);
            if (!Directory.Exists (generateInfoDir)) {
                Directory.CreateDirectory (generateInfoDir);
            }
            var generatorInfoPath = FileUtility.PathCombine(generateInfoDir, asset.fileNameAndExtension + ".asset");

            var version = AssetGeneratorUtility.GetVersion(entry.m_instance.ClassName);

            var info = ScriptableObject.CreateInstance<AssetGenerateInfo>();
            info.Initialize(entry.m_instance.ClassName, entry.m_instance[target], version, asset);

			AssetDatabase.CreateAsset(info, generatorInfoPath);		
		}
	}
}