using UnityEditor;
using System.IO;
using System.Linq;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class AssetBundlesSettingsTab {

        internal class Styles
        {
            public static readonly GUIContent defaultAssetBundleGraph = EditorGUIUtility.TrTextContent("Default AssetBundle Graph");
            public static readonly GUIContent assetBundleBuildMapFile = EditorGUIUtility.TrTextContent("AssetBundle Build Map File");
            public static readonly GUIContent setButton = EditorGUIUtility.TrTextContent("Set");
            public static readonly GUIContent bundleCacheDirectoryLabel = EditorGUIUtility.TrTextContent("Bundle Cache Directory");
            public static readonly GUIContent configDirectoryLabel = EditorGUIUtility.TrTextContent("Config Directory");

            public static readonly GUIContent help_bundleCacheDirectory = EditorGUIUtility.TrTextContent(
                "Bundle Cache Directory is the default place to save AssetBundles when 'Build Asset Bundles' node performs build. This can be set outside of the project with relative path."
            );
            
            public static readonly GUIContent help_defaultAssetGraph = EditorGUIUtility.TrTextContent(
                "Default AssetBundle Graph is the default graph to build AssetBundles for this project. This graph will be automatically called in AssetBundle Browser integration."
            );
            public static readonly GUIContent help_assetBundleBuildMap = EditorGUIUtility.TrTextContent(
                "AssetBundle build map file is an asset used to store assets to asset bundles relationship. "
            );
        }
        
        private string[] m_graphGuids;
        private string[] m_graphNames;

	    private string m_buildmapPath;
	    private string m_buildmapMoveErrorMsg;

        public AssetBundlesSettingsTab() {
            Refresh();
        }

        private void Refresh()
        {
            m_buildmapPath = AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath;
            m_buildmapMoveErrorMsg = string.Empty;
            m_graphGuids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION)
                    .Where(guid => !AssetDatabase.GUIDToAssetPath(guid).Contains(Model.Settings.HIDE_GRAPH_PREFIX))
                    .ToArray();
            m_graphNames = new string[m_graphGuids.Length];
            for (var i = 0; i < m_graphGuids.Length; ++i) {
                m_graphNames[i] = Path.GetFileNameWithoutExtension (AssetDatabase.GUIDToAssetPath (m_graphGuids[i]));
            }
        }

        private void DrawConfigBaseDirGUI() {
            using (new EditorGUILayout.VerticalScope()) {

                string baseDir = Model.Settings.UserSettings.ConfigBaseDir;

                using (new EditorGUILayout.HorizontalScope ()) {                    
                    var newBaseDir = GUIHelper.DrawFolderSelector (Styles.configDirectoryLabel.text, "Select Config Folder", 
                        baseDir,
                        Application.dataPath + "/../",
                        (string folderSelected) => {
                            var projectPath = Directory.GetParent(Application.dataPath).ToString();

                            if(projectPath == folderSelected) {
                                folderSelected = string.Empty;
                            } else {
                                var index = folderSelected.IndexOf(projectPath);
                                if(index >= 0 ) {
                                    folderSelected = folderSelected.Substring(projectPath.Length + index);
                                    if(folderSelected.IndexOf('/') == 0) {
                                        folderSelected = folderSelected.Substring(1);
                                    }
                                }
                            }
                            return folderSelected;
                        }
                    );
                    if (newBaseDir != baseDir) {
                        Model.Settings.UserSettings.ConfigBaseDir = newBaseDir;
                    }
                }

                using (new EditorGUI.DisabledScope (!Directory.Exists (baseDir))) 
                {
                    using (new EditorGUILayout.HorizontalScope ()) {
                        GUILayout.FlexibleSpace ();
                        if (GUILayout.Button (GUIHelper.RevealInFinderLabel)) {
                            EditorUtility.RevealInFinder (baseDir);
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox (Styles.help_bundleCacheDirectory.text, MessageType.Info);
        }        
        
        private void DrawCacheDirGUI() {
            using (new EditorGUILayout.VerticalScope()) {

                string cacheDir = Model.Settings.UserSettings.AssetBundleBuildCacheDir;

                using (new EditorGUILayout.HorizontalScope ()) {                    
                    var newCacheDir = GUIHelper.DrawFolderSelector (Styles.bundleCacheDirectoryLabel.text, "Select Cache Folder", 
                        cacheDir,
                        Application.dataPath + "/../",
                        (string folderSelected) => {
                            var projectPath = Directory.GetParent(Application.dataPath).ToString();

                            if(projectPath == folderSelected) {
                                folderSelected = string.Empty;
                            } else {
                                var index = folderSelected.IndexOf(projectPath);
                                if(index >= 0 ) {
                                    folderSelected = folderSelected.Substring(projectPath.Length + index);
                                    if(folderSelected.IndexOf('/') == 0) {
                                        folderSelected = folderSelected.Substring(1);
                                    }
                                }
                            }
                            return folderSelected;
                        }
                    );
                    if (newCacheDir != cacheDir) {
                        Model.Settings.UserSettings.AssetBundleBuildCacheDir = newCacheDir;
                    }
                }

                using (new EditorGUI.DisabledScope (!Directory.Exists (cacheDir))) 
                {
                    using (new EditorGUILayout.HorizontalScope ()) {
                        GUILayout.FlexibleSpace ();
                        if (GUILayout.Button (GUIHelper.RevealInFinderLabel)) {
                            EditorUtility.RevealInFinder (cacheDir);
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox ( Styles.help_bundleCacheDirectory.text, MessageType.Info);
        }

        private void DrawABGraphList() {
            string abGraphGuid = Model.Settings.UserSettings.DefaultAssetBundleBuildGraphGuid;

            int index = ArrayUtility.IndexOf(m_graphGuids, abGraphGuid);
            var selected = EditorGUILayout.Popup (Styles.defaultAssetBundleGraph, index, m_graphNames);

            if (index != selected) {
                Model.Settings.UserSettings.DefaultAssetBundleBuildGraphGuid = m_graphGuids [selected];
            }

            EditorGUILayout.HelpBox (Styles.help_defaultAssetGraph.text, MessageType.Info);
        }

	    private void DrawABBuildMapPath()
	    {
	        using (new EditorGUILayout.HorizontalScope())
	        {
	            m_buildmapPath = EditorGUILayout.TextField(Styles.assetBundleBuildMapFile, m_buildmapPath);

	            using (new EditorGUI.DisabledScope(m_buildmapPath == AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath))
	            {
	                if (GUILayout.Button(Styles.setButton, GUILayout.Width(50)))
	                {
	                    var oldPath = AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath;
	                    m_buildmapMoveErrorMsg = string.Empty;
	                    if (File.Exists(oldPath))
	                    {
	                        m_buildmapMoveErrorMsg = AssetDatabase.MoveAsset(oldPath, m_buildmapPath);
	                    }

	                    if (string.IsNullOrEmpty(m_buildmapMoveErrorMsg))
	                    {
	                        AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath = m_buildmapPath;
	                    }
	                }
	            }
	        }

	        if (!string.IsNullOrEmpty(m_buildmapMoveErrorMsg))
	        {
	            EditorGUILayout.HelpBox (
	                m_buildmapMoveErrorMsg, 
	                MessageType.Error);
	        }
	        
	        EditorGUILayout.HelpBox (
                Styles.help_assetBundleBuildMap.text, MessageType.Info);
	    }

		public void OnGUI () {
            DrawCacheDirGUI ();

		    GUILayout.Space (20f);

		    DrawABBuildMapPath();
		    
            GUILayout.Space (20f);

            DrawABGraphList ();
		}
	}
}
