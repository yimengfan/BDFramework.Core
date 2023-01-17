using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    /// <summary>
    /// Asset reference.
    /// </summary>
	[System.Serializable]
	public class AssetReference {

		[SerializeField] private Guid m_guid;
		[SerializeField] private string m_assetDatabaseId;
		[SerializeField] private string m_importFrom;
		[SerializeField] private string m_exportTo;
		[SerializeField] private string m_variantName;

		private UnityEngine.Object[] m_data;
		private UnityEngine.SceneManagement.Scene m_scene;
        private Type m_assetType;
        private Type m_filterType;
        private Type m_importerType;

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
		public string id {
			get {
				return m_guid.ToString();
			}
		}

        /// <summary>
        /// Gets the asset database identifier.
        /// </summary>
        /// <value>The asset database identifier.</value>
		public string assetDatabaseId {
			get {
				return m_assetDatabaseId;
			}
		}

        /// <summary>
        /// Gets or sets the import from.
        /// </summary>
        /// <value>The import from.</value>
		public string importFrom {
			get {
				return m_importFrom;
			}
			set {
				m_importFrom = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

        /// <summary>
        /// Gets or sets the export to.
        /// </summary>
        /// <value>The export to.</value>
		public string exportTo {
			get {
				return m_exportTo;
			}
			set {
				m_exportTo = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

        /// <summary>
        /// Gets or sets the name of the variant.
        /// </summary>
        /// <value>The name of the variant.</value>
		public string variantName {
			get {
				return m_variantName;
			}
			set {
				m_variantName = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

        /// <summary>
        /// Gets main asset type.
        /// </summary>
        /// <value>The type of the asset.</value>
		public Type assetType {
			get {
                if (m_assetType == null) {
                    m_assetType = TypeUtility.GetMainAssetTypeAtPath (importFrom);
                }
                return m_assetType;
			}
		}

        /// <summary>
        /// Get type of asset used by filtering.
        /// filterType will be variation of AssetImporter if asset has importer,
        /// otherwise returns asset type.
        /// filterType may be null if this asset should be ignored by loading/filtering (manifest, etc)
        /// </summary>
        /// <value>The type of the filter.</value>
		public Type filterType {
			get {
				if(m_filterType == null) {
                    m_filterType = FilterTypeUtility.FindAssetFilterType (m_importFrom);
				}
				return m_filterType;
			}
		}

        /// <summary>
        /// Get type of asset importer. Returns null if asset does not have importer.
        /// </summary>
        /// <value>The type of the filter.</value>
        public Type importerType {
            get {
                if(m_importerType == null) {
                    m_importerType = TypeUtility.GetAssetImporterTypeAtPath (m_importFrom);
                }
                return m_importerType;
            }
        }


        /// <summary>
        /// Gets the file name and extension.
        /// </summary>
        /// <value>The file name and extension.</value>
		public string fileNameAndExtension {
			get {
				if(m_importFrom != null) {
					return Path.GetFileName(m_importFrom);
				}
				if(m_exportTo != null) {
					return Path.GetFileName(m_exportTo);
				}
				return null;
			}
		}

        /// <summary>
        /// Gets extension.
        /// </summary>
        /// <value>The extension of the file name.</value>
        public string extension {
            get {
                if(m_importFrom != null) {
                    return Path.GetExtension(m_importFrom);
                }
                if(m_exportTo != null) {
                    return Path.GetExtension(m_exportTo);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
		public string fileName {
			get {
				if(m_importFrom != null) {
					return Path.GetFileNameWithoutExtension(m_importFrom);
				}
				if(m_exportTo != null) {
					return Path.GetFileNameWithoutExtension(m_exportTo);
				}
				return null;
			}
		}

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
		public string path {
			get {
				if(m_importFrom != null) {
					return m_importFrom;
				}
				if(m_exportTo != null) {
					return m_exportTo;
				}
				return null;
			}
		}

        /// <summary>
        /// Gets the absolute path.
        /// </summary>
        /// <value>The absolute path.</value>
		public string absolutePath {
			get {
                return Application.dataPath + m_importFrom.Substring (6); // 6 = "Assets"
			}
		}

        /// <summary>
        /// File size (byte)
        /// </summary>
        /// <value>File size (byte)</value>
        public long runtimeMemorySize {
            get {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(absolutePath);
                if (fileInfo.Exists) {
                    return fileInfo.Length;
                }
                return 0L;
            }
        }

        /// <summary>
        /// Gets all data.
        /// </summary>
        /// <value>All data.</value>
		public UnityEngine.Object[] allData {
			get {
				if(m_data == null || m_data.Length == 0) {
					if (isSceneAsset) {
						if(!m_scene.isLoaded) {
							m_scene = EditorSceneManager.OpenScene (importFrom);
						}
						m_data = m_scene.GetRootGameObjects ();
					} else {
						m_data = AssetDatabase.LoadAllAssetsAtPath (importFrom);
					}
				}
				return m_data;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:UnityEngine.AssetGraph.AssetReference"/> is referencing a scene asset.
		/// </summary>
		/// <value><c>true</c> if is a scene asset; otherwise, <c>false</c>.</value>
		public bool isSceneAsset => assetType == typeof (UnityEditor.SceneAsset);

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:UnityEngine.AssetGraph.AssetReference"/> is a reference of asset in this Project.
		/// Asset outside of project means this file is outside of Assets/ directory.
		/// </summary>
		/// <value><c>true</c> if is a project asset; otherwise, <c>false</c>.</value>
		public bool isProjectAsset => !string.IsNullOrEmpty(m_assetDatabaseId);

		/// <summary>
		/// Gets the scene.
		/// </summary>
		/// <value>The loaded Scene object.</value>
		public UnityEngine.SceneManagement.Scene scene {
			get {
				return m_scene;
			}
		}

        public override int GetHashCode ()
        {
            return m_guid.GetHashCode ();
        }

        public long GetFileSize() {
            if (string.IsNullOrEmpty (m_importFrom)) {
                return 0L;
            }
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(absolutePath);
            if (fileInfo.Exists) {
                return fileInfo.Length;
            }
            return 0L;
        }

        /// <summary>
        /// Sets the dirty.
        /// </summary>
		public void SetDirty() {
			if(isSceneAsset) {
				if(m_scene.isLoaded) {
					EditorSceneManager.MarkSceneDirty (m_scene);
				}
			}
			else if(m_data != null) {
				foreach(var o in m_data) {
					if(o == null) {
						continue;
					}
					EditorUtility.SetDirty(o);
				}
			}
		}

        /// <summary>
        /// Releases the data.
        /// </summary>
		public void ReleaseData() {
			if (isSceneAsset) {
				if(m_scene.isLoaded) {
					// unloading last scene is not supported. omit closing if this is the last scene.
					if(EditorSceneManager.sceneCount > 1) {
						EditorSceneManager.CloseScene (m_scene, true);
					}
				}
				m_data = null;
			}
			else if(m_data != null) {
				foreach(var o in m_data) {
					if (o == null) {
						continue;
					}
					if(o is UnityEngine.GameObject || o is UnityEngine.Component) {
						// do nothing.
						// NOTE: DestroyImmediate() will destroy persistant GameObject in prefab. Do not call it.
					} else {
						LogUtility.Logger.LogFormat(LogType.Log, "Unloading {0} ({1})", importFrom, o.GetType().ToString());
						Resources.UnloadAsset(o);
					}
				}
				m_data = null;

			}
		}

        public void InvalidateTypeCache() {
            m_assetType = null;
            m_importerType = null;
            m_filterType = null;
        }

        /// <summary>
        /// Touchs the import asset.
        /// </summary>
		public void TouchImportAsset() {
	        if (File.Exists(importFrom))
	        {
		        System.IO.File.SetLastWriteTime(importFrom, DateTime.UtcNow);
	        }
		}

        /// <summary>
        /// Creates the reference.
        /// </summary>
        /// <returns>The reference.</returns>
        /// <param name="importFrom">Import from.</param>
		public static AssetReference CreateReference (string importFrom) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:TypeUtility.GetMainAssetTypeAtPath(importFrom)
			);
		}

        /// <summary>
        /// Creates the reference.
        /// </summary>
        /// <returns>The reference.</returns>
        /// <param name="importFrom">Import from.</param>
        /// <param name="assetType">Asset type.</param>
        public static AssetReference CreateReference (string importFrom, Type assetType) {
            return new AssetReference(
                guid: Guid.NewGuid(),
                assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
                importFrom:importFrom,
                assetType:assetType
            );
        }

        /// <summary>
        /// Creates the prefab reference.
        /// </summary>
        /// <returns>The prefab reference.</returns>
        /// <param name="importFrom">Import from.</param>
		public static AssetReference CreatePrefabReference (string importFrom) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:typeof(GameObject)
			);
		}

        /// <summary>
        /// Creates the asset bundle reference.
        /// </summary>
        /// <returns>The asset bundle reference.</returns>
        /// <param name="path">Path.</param>
		public static AssetReference CreateAssetBundleReference (string path) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(path),
				importFrom:path,
				assetType:typeof(AssetBundleReference)
			);
		}

        /// <summary>
        /// Creates the asset bundle manifest reference.
        /// </summary>
        /// <returns>The asset bundle manifest reference.</returns>
        /// <param name="path">Path.</param>
        public static AssetReference CreateAssetBundleManifestReference (string path) {
            return new AssetReference(
                guid: Guid.NewGuid(),
                assetDatabaseId:AssetDatabase.AssetPathToGUID(path),
                importFrom:path,
                assetType:typeof(AssetBundleManifestReference)
            );
        }
        
        /// <summary>
        /// Creates the unity package reference.
        /// </summary>
        /// <returns>The unity package reference.</returns>
        /// <param name="path">Path.</param>
        public static AssetReference CreateUnityPackageReference (string path) {
	        return new AssetReference(
		        guid: Guid.NewGuid(),
		        assetDatabaseId:string.Empty,
		        importFrom:path,
		        assetType:typeof(UnityPackageReference)
	        );
        }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityEngine.AssetGraph.AssetReference"/> class.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="assetDatabaseId">Asset database identifier.</param>
        /// <param name="importFrom">Import from.</param>
        /// <param name="exportTo">Export to.</param>
        /// <param name="assetType">Asset type.</param>
        /// <param name="variantName">Variant name.</param>
		public AssetReference (
			Guid guid,
			string assetDatabaseId = null,
			string importFrom = null,
			string exportTo = null,
			Type assetType = null,
			string variantName = null
		) {
			if(assetType == null) {
				throw new AssetReferenceException(importFrom, "Invalid type of asset created:" + importFrom);
			}

			this.m_guid = guid;
			this.m_importFrom = importFrom;
			this.m_exportTo = exportTo;
			this.m_assetDatabaseId = assetDatabaseId;
			this.m_variantName = variantName;
			this.m_assetType = assetType;
        }
	}

    public class AssetBundleReference {}
    public class AssetBundleManifestReference {}

    public class UnityPackageReference { }
}