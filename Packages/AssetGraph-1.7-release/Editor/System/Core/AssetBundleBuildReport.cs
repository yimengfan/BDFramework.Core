using UnityEditor;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    /// <summary>
    /// Asset bundle build report.
    /// </summary>
	public class AssetBundleBuildReport {
		private class AssetBundleBuildReportManager {

			private List<AssetBundleBuildReport> m_buildReports;
			private List<ExportReport> m_exportReports;

			public List<AssetBundleBuildReport> BuildReports {
				get {
					return m_buildReports;
				}
			}

			public List<ExportReport> ExportReports {
				get {
					return m_exportReports;
				}
			}

			public AssetBundleBuildReportManager() {
				m_buildReports = new List<AssetBundleBuildReport>();
				m_exportReports = new List<ExportReport>();
			}
		}

		private static AssetBundleBuildReportManager s_mgr;
		private static AssetBundleBuildReportManager Manager {
			get {
				if(s_mgr == null) {
					s_mgr = new AssetBundleBuildReportManager();
				}
				return s_mgr;
			}
		}

		static public void ClearReports() {
			Manager.BuildReports.Clear();
			Manager.ExportReports.Clear();
		}

		static public void AddBuildReport(AssetBundleBuildReport r) {
			Manager.BuildReports.Add(r);
		}
		static public void AddExportReport(ExportReport r) {
			Manager.ExportReports.Add(r);
		}

		static public IEnumerable<AssetBundleBuildReport> BuildReports {
			get {
				return Manager.BuildReports;
			}
		}

		static public IEnumerable<ExportReport> ExportReports {
			get {
				return Manager.ExportReports;
			}
		}

		private Model.NodeData m_node;
		private AssetBundleManifest m_manifest;
        private string m_manifestFileName;
		private AssetBundleBuild[] m_bundleBuild;
		private List<AssetReference> m_builtBundles;
		private Dictionary<string, List<AssetReference>> m_assetGroups;
		private Dictionary<string, List<string>> m_bundleNamesAndVariants;

        /// <summary>
        /// Gets the node.
        /// </summary>
        /// <value>The node.</value>
		public Model.NodeData Node {
			get {
				return m_node;
			}
		}

        /// <summary>
        /// Gets the manifest.
        /// </summary>
        /// <value>The manifest.</value>
		public AssetBundleManifest Manifest {
			get {
				return m_manifest;
			}
		}

        /// <summary>
        /// Gets the name of the manifest file.
        /// </summary>
        /// <value>The name of the manifest file.</value>
        public string ManifestFileName {
            get {
                return m_manifestFileName;
            }
        }

        /// <summary>
        /// Gets the bundle build.
        /// </summary>
        /// <value>The bundle build.</value>
        public AssetBundleBuild[] BundleBuild {
			get {
				return m_bundleBuild;
			}
		}

        /// <summary>
        /// Gets the built bundle files.
        /// </summary>
        /// <value>The built bundle files.</value>
		public List<AssetReference> BuiltBundleFiles {
			get {
				return m_builtBundles;
			}
		}

        /// <summary>
        /// Gets the asset groups.
        /// </summary>
        /// <value>The asset groups.</value>
		public Dictionary<string, List<AssetReference>> AssetGroups {
			get {
				return m_assetGroups;
			}
		}

        /// <summary>
        /// Gets the bundle names.
        /// </summary>
        /// <value>The bundle names.</value>
		public IEnumerable<string> BundleNames {
			get {
				return m_bundleNamesAndVariants.Keys;
			}
		}

        /// <summary>
        /// Gets the variant names.
        /// </summary>
        /// <returns>The variant names.</returns>
        /// <param name="bundleName">Bundle name.</param>
        public List<string> GetVariantNames(string bundleName) {
			if(m_bundleNamesAndVariants.ContainsKey(bundleName)) {
				return m_bundleNamesAndVariants[bundleName];
			}
			return null;
		}

		public AssetBundleBuildReport(
			Model.NodeData node,
			AssetBundleManifest m,
            string manifestFileName,
			AssetBundleBuild[] bb, 
			List<AssetReference> builtBundles,
			Dictionary<string, List<AssetReference>> ag, 
			Dictionary<string, List<string>> names) {
			m_node = node;
			m_manifest = m;
            m_manifestFileName = manifestFileName;
			m_bundleBuild = bb;
			m_builtBundles = builtBundles;
			m_assetGroups = ag;
			m_bundleNamesAndVariants = names;
		}
	}

    /// <summary>
    /// Export report.
    /// </summary>
	public class ExportReport {

        /// <summary>
        /// Entry.
        /// </summary>
		public class Entry {
            /// <summary>
            /// The source.
            /// </summary>
			public string source;

            /// <summary>
            /// The destination.
            /// </summary>
			public string destination;
			public Entry(string src, string dst) {
				source = src;
				destination = dst;
			}
		}

        /// <summary>
        /// Error entry.
        /// </summary>
		public class ErrorEntry {
            /// <summary>
            /// The source.
            /// </summary>
			public string source;
            /// <summary>
            /// The destination.
            /// </summary>
			public string destination;
            /// <summary>
            /// The reason.
            /// </summary>
			public string reason;
			public ErrorEntry(string src, string dst, string r) {
				source = src;
				destination = dst;
				reason = r;
			}
		}

		private Model.NodeData m_nodeData;

		private List<Entry> m_exportedItems;
		private List<ErrorEntry> m_failedItems;

        /// <summary>
        /// Gets the exported items.
        /// </summary>
        /// <value>The exported items.</value>
		public List<Entry> ExportedItems {
			get {
				return m_exportedItems;
			}
		}

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>The errors.</value>
		public List<ErrorEntry> Errors {
			get {
				return m_failedItems;
			}
		}

        /// <summary>
        /// Gets the node.
        /// </summary>
        /// <value>The node.</value>
		public Model.NodeData Node {
			get {
				return m_nodeData;
			}
		}

		public ExportReport(Model.NodeData node) {
			m_nodeData = node;

			m_exportedItems = new List<Entry>();
			m_failedItems = new List<ErrorEntry> ();
		}

		public void AddExportedEntry(string src, string dst) {
			m_exportedItems.Add(new Entry(src, dst));
		}

		public void AddErrorEntry(string src, string dst, string reason) {
			m_failedItems.Add(new ErrorEntry(src, dst, reason));
		}
	}
}

