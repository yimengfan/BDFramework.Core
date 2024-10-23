/**
 * AssetBundles-Browser integration
 * 
 * This code will setup the output of the graph tool to be viewable in the browser.
 * 
 * AssetBundles-Browser is available from Unity Package Manager.
 */

#if ASSETBUNDLEBROWSER_1_7_OR_NEWER

using UnityEditor;
using Model = UnityEngine.AssetGraph.DataModel.Version2;
using System;
using System.IO;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

    public class GraphToolABDataSource : AssetBundleBrowser.AssetBundleDataSource.ABDataSource
    {
        public static List<AssetBundleBrowser.AssetBundleDataSource.ABDataSource> CreateDataSources()
        {
            var op = new GraphToolABDataSource();
            var retList = new List<AssetBundleBrowser.AssetBundleDataSource.ABDataSource>();
            retList.Add(op);
            return retList;
        }

        private string m_graphGuid;
        private string m_graphName;

        public string Name {
			get {
                return m_graphName;
			}
		}

		public string ProviderName {
			get {
				return "AssetGraph";
			}
		}

        GraphToolABDataSource() {
            UpdateGraphInfo ();
        }

        private void UpdateGraphInfo() {
            var guid = Model.Settings.UserSettings.DefaultAssetBundleBuildGraphGuid;
            if (guid != m_graphGuid) {
                m_graphGuid = guid;
                if (!string.IsNullOrEmpty (m_graphGuid)) {
                    m_graphName = Path.GetFileNameWithoutExtension( AssetDatabase.GUIDToAssetPath (m_graphGuid));
                }
            }
        }

		public string[] GetAssetPathsFromAssetBundle (string assetBundleName) {
			return AssetBundleBuildMap.GetBuildMap ().GetAssetPathsFromAssetBundle (assetBundleName);
		}

		public string GetAssetBundleName(string assetPath) {
			return AssetBundleBuildMap.GetBuildMap ().GetAssetBundleName (assetPath);
		}

		public string GetImplicitAssetBundleName(string assetPath) {
			return AssetBundleBuildMap.GetBuildMap ().GetImplicitAssetBundleName (assetPath);
		}

		public string[] GetAllAssetBundleNames() {
            UpdateBuildMap ();
			return AssetBundleBuildMap.GetBuildMap ().GetAllAssetBundleNames ();
		}

		public bool IsReadOnly() {
			return true;
		}

		public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {
			// readonly. do nothing
		}

		public void RemoveUnusedAssetBundleNames() {
			// readonly. do nothing
		}

		public bool CanSpecifyBuildTarget {
			get { return true; } 
		}
		public bool CanSpecifyBuildOutputDirectory { 
			get { return false; } 
		}

		public bool CanSpecifyBuildOptions { 
			get { return false; } 
		}

        private void UpdateBuildMap() {
            UpdateGraphInfo ();

            string path = AssetDatabase.GUIDToAssetPath(m_graphGuid);

            if(string.IsNullOrEmpty(path)) {
                return;
            }

            AssetBundleBuildMap.GetBuildMap ().Clear ();
            AssetGraphUtility.ExecuteGraphSetup (path);
        }

        public bool BuildAssetBundles (AssetBundleBrowser.AssetBundleDataSource.ABBuildInfo info) {
			
            AssetBundleBuildMap.GetBuildMap ().Clear ();

            UpdateGraphInfo ();

            if (string.IsNullOrEmpty (m_graphGuid)) {
                return false;
            }

            string path = AssetDatabase.GUIDToAssetPath(m_graphGuid);
            if(string.IsNullOrEmpty(path)) {
                return false;
            }

            var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);

            Type infoType = info.GetType();

            var fieldInfo = infoType.GetField ("buildTarget");
            if (fieldInfo != null) {
                BuildTarget target = (BuildTarget)fieldInfo.GetValue (info);
                var result = AssetGraphUtility.ExecuteGraph(target, graph);
                if (result.IsAnyIssueFound)
                {
                    return false;
                }
            }

			return true;
		}
    }
}

#endif