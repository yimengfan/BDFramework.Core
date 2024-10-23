using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.AssetGraph.DataModel.Version2 {
	public class Settings : ScriptableObject {
		/*
			if true, ignore .meta files inside AssetBundleGraph.
		*/
		public const bool IGNORE_META = true;

        public const string GUI_TEXT_MENU_BASE = "Window/AssetGraph";
        public const string GUI_TEXT_MENU_OPEN = GUI_TEXT_MENU_BASE + "/Open Graph Editor";
        public const string GUI_TEXT_MENU_BATCHWINDOW_OPEN = GUI_TEXT_MENU_BASE + "/Open Batch Build Window";
        public const string GUI_TEXT_MENU_ASSETLOGWINDOW_OPEN = GUI_TEXT_MENU_BASE + "/Open Asset Log Window";
        public const string GUI_TEXT_MENU_PROJECTWINDOW_OPEN = GUI_TEXT_MENU_BASE + "/Open Project Settings";
        public const string GUI_TEXT_MENU_BUILD = GUI_TEXT_MENU_BASE + "/Build Graph for Current Platform";
        public const string GUI_TEXT_MENU_GENERATE = GUI_TEXT_MENU_BASE + "/Create Node Script";
		public const string GUI_TEXT_MENU_GENERATE_MODIFIER = GUI_TEXT_MENU_GENERATE + "/Modifier Script";
        public const string GUI_TEXT_MENU_GENERATE_PREFABBUILDER = GUI_TEXT_MENU_GENERATE + "/PrefabBuilder Script";
        public const string GUI_TEXT_MENU_GENERATE_ASSETGENERATOR = GUI_TEXT_MENU_GENERATE + "/AssetGenerator Script";
        public const string GUI_TEXT_MENU_GENERATE_IMPORTSETTINGSCONFIGURATOR = GUI_TEXT_MENU_GENERATE + "/ImportSettingsConfigurator Script";
        public const string GUI_TEXT_MENU_GENERATE_CUITOOL = GUI_TEXT_MENU_BASE + "/Create CUI Tool";

		public const string GUI_TEXT_MENU_GENERATE_POSTPROCESS = GUI_TEXT_MENU_GENERATE + "/Postprocess Script";
		public const string GUI_TEXT_MENU_GENERATE_FILTER = GUI_TEXT_MENU_GENERATE + "/Filter Script";
		public const string GUI_TEXT_MENU_GENERATE_NODE = GUI_TEXT_MENU_GENERATE + "/Custom Node Script";
        public const string GUI_TEXT_MENU_DELETE_CACHE = GUI_TEXT_MENU_BASE + "/Clear Build Cache";
		
        public const string GUI_TEXT_MENU_CLEANUP_SAVEDSETTINGS = GUI_TEXT_MENU_BASE + "/Clean Up SavedSettings";

		public const string GRAPH_SEARCH_CONDITION = "t:UnityEngine.AssetGraph.DataModel.Version2.ConfigGraph";
        public const string SETTING_TEMPLATE_DIR_SEARCH_CONDITION = "SettingTemplate";

		public const string UNITY_METAFILE_EXTENSION = ".meta";
		public const string DOTSTART_HIDDEN_FILE_HEADSTRING = ".";
		public const string MANIFEST_FOOTER = ".manifest";
		public const char UNITY_FOLDER_SEPARATOR = '/';// Mac/Windows/Linux can use '/' in Unity.

		public const string BASE64_IDENTIFIER = "B64|";

		public const char KEYWORD_WILDCARD = '*';

		public const string HIDE_GRAPH_PREFIX = "__hidden__";

        public const int GRAPHEXECPRIORITY_DEFAULT = 0;


        public static class UserSettings {
	        private const string PREFKEY_AB_BUILD_CACHE_DIR = "AssetBundles.GraphTool.Cache.AssetBundle";
	        private const string PREFKEY_AB_BUILD_GRAPH_GUID = "AssetBundles.GraphTool.GraphGuid";

	        private const string PREFKEY_BATCHBUILD_LASTSELECTEDCOLLECTION = "AssetBundles.GraphTool.LastSelectedCollection";
	        private const string PREFKEY_BATCHBUILD_USECOLLECTIONSTATE = "AssetBundles.GraphTool.UseCollection";

	        private const string PREFKEY_CONFIG_BASE_DIR = "AssetBundles.GraphTool.ConfigBaseDir";

	        public static string ConfigBaseDir {
		        get {
			        var baseDir = EditorUserSettings.GetConfigValue (PREFKEY_CONFIG_BASE_DIR);
			        if (string.IsNullOrEmpty (baseDir)) {
				        return System.IO.Path.Combine(Path.DefaultBasePath, "AssetBundles");
			        }
			        return baseDir;
		        }
		        set {
			        EditorUserSettings.SetConfigValue (PREFKEY_CONFIG_BASE_DIR, value);
		        }
	        }

	        public static string AssetBundleBuildCacheDir {
                get {
                    var cacheDir = EditorUserSettings.GetConfigValue (PREFKEY_AB_BUILD_CACHE_DIR);
                    if (string.IsNullOrEmpty (cacheDir)) {
                        return System.IO.Path.Combine(Path.CachePath, "AssetBundles");
                    }
                    return cacheDir;
                }

                set {
                    EditorUserSettings.SetConfigValue (PREFKEY_AB_BUILD_CACHE_DIR, value);
                }
            }

            public static string DefaultAssetBundleBuildGraphGuid {
                get {
                    return EditorUserSettings.GetConfigValue (PREFKEY_AB_BUILD_GRAPH_GUID);
                }

                set {
                    EditorUserSettings.SetConfigValue (PREFKEY_AB_BUILD_GRAPH_GUID, value);
                }
            }

            public static string BatchBuildLastSelectedCollection {
                get {
                    return EditorUserSettings.GetConfigValue (PREFKEY_BATCHBUILD_LASTSELECTEDCOLLECTION);
                }
                set {
                    EditorUserSettings.SetConfigValue (PREFKEY_BATCHBUILD_LASTSELECTEDCOLLECTION, value);
                }
            }

            public static bool BatchBuildUseCollectionState {
                get {
                    return EditorUserSettings.GetConfigValue (PREFKEY_BATCHBUILD_USECOLLECTIONSTATE) == "True";
                }
                set {
                    EditorUserSettings.SetConfigValue (PREFKEY_BATCHBUILD_USECOLLECTIONSTATE, value.ToString());
                }
            }
        }

        public static class Path
        {

	        public static string PackagePath => AssetDatabase.GUIDToAssetPath("8549600cb67d5234aa836c3f0e2f221f");//"Packages/com.unity.assetgraph";
	        
	        public static string DefaultBasePath => "Assets/AssetGraph";

	        public static string BasePath => UserSettings.ConfigBaseDir;

	        public static void ResetBasePath(string newPath)
	        {
		        UserSettings.ConfigBaseDir = newPath;
	        }

	        /// <summary>
	        /// Name of the base directory containing the asset graph tool files.
	        /// Customize this to match your project's setup if you need to change.
	        /// </summary>
	        /// <value>The name of the base directory.</value>
	        public static string ToolDirName => "AssetGraph";

	        public const string ASSETS_PATH = "Assets/";

	        public static string CachePath                  { get { return System.IO.Path.Combine(BasePath, "Cache");; } }
	        public static string SettingFilePath            { get { return System.IO.Path.Combine(BasePath, "SettingFiles"); } }
	        public static string TemporalSettingFilePath    { get { return System.IO.Path.Combine(CachePath, "TemporalSettingFiles"); } }
	        
            public static string UserSpacePath          { get { return System.IO.Path.Combine(BasePath, "Generated/Editor"); } }
            public static string CUISpacePath           { get { return System.IO.Path.Combine(BasePath, "Generated/CUI"); } }
            public static string SavedSettingsPath      { get { return System.IO.Path.Combine(BasePath, "SavedSettings"); } }

            public static string BundleBuilderCachePath { get { return UserSettings.AssetBundleBuildCacheDir; } }

            public static string DatabasePath           { get { return System.IO.Path.Combine(TemporalSettingFilePath, "AssetReferenceDB.asset"); } }
            public static string EventRecordPath        { get { return System.IO.Path.Combine(TemporalSettingFilePath, "AssetProcessEventRecord.asset"); } }
            public static string BatchBuildConfigPath   { get { return System.IO.Path.Combine(SavedSettingsPath, "BatchBuildConfig/BatchBuildConfig.asset"); } }

            public static string ScriptTemplatePath     { get { return System.IO.Path.Combine(PackagePath, "Editor/ScriptTemplate"); } }            
            public static string GUIResourceBasePath { get { return System.IO.Path.Combine(PackagePath, "Editor/GUI/GraphicResources"); } }
        }

        public struct ToggleOption <T> {
			public readonly T option;
			public readonly string description;
			public ToggleOption(string desc, T opt) {
				option = opt;
				description = desc;
			}
		}

        public static List<ToggleOption<BuildAssetBundleOptions>> BundleOptionSettings = new List<ToggleOption<BuildAssetBundleOptions>> {
            new ToggleOption<BuildAssetBundleOptions>("Uncompressed AssetBundle", BuildAssetBundleOptions.UncompressedAssetBundle),
            new ToggleOption<BuildAssetBundleOptions>("Disable Write TypeTree", BuildAssetBundleOptions.DisableWriteTypeTree),
            new ToggleOption<BuildAssetBundleOptions>("Deterministic AssetBundle", BuildAssetBundleOptions.DeterministicAssetBundle),
            new ToggleOption<BuildAssetBundleOptions>("Force Rebuild AssetBundle", BuildAssetBundleOptions.ForceRebuildAssetBundle),
            new ToggleOption<BuildAssetBundleOptions>("Ignore TypeTree Changes", BuildAssetBundleOptions.IgnoreTypeTreeChanges),
            new ToggleOption<BuildAssetBundleOptions>("Append Hash To AssetBundle Name", BuildAssetBundleOptions.AppendHashToAssetBundleName),
            new ToggleOption<BuildAssetBundleOptions>("ChunkBased Compression", BuildAssetBundleOptions.ChunkBasedCompression),
            new ToggleOption<BuildAssetBundleOptions>("Strict Mode", BuildAssetBundleOptions.StrictMode)
        };

        public static List<ToggleOption<BuildOptions>> BuildPlayerOptionsSettings = new List<ToggleOption<BuildOptions>> {
            new ToggleOption<BuildOptions>("Accept External Modification To Player", BuildOptions.AcceptExternalModificationsToPlayer),
            new ToggleOption<BuildOptions>("Allow Debugging", BuildOptions.AllowDebugging),
            new ToggleOption<BuildOptions>("Auto Run Player", BuildOptions.AutoRunPlayer),
            new ToggleOption<BuildOptions>("Build Additional Streamed Scenes", BuildOptions.BuildAdditionalStreamedScenes),
            new ToggleOption<BuildOptions>("Build Scripts Only", BuildOptions.BuildScriptsOnly),
            new ToggleOption<BuildOptions>("Compress With LZ4", BuildOptions.CompressWithLz4),
            new ToggleOption<BuildOptions>("Compute CRC", BuildOptions.ComputeCRC),
            new ToggleOption<BuildOptions>("Connect To Host", BuildOptions.ConnectToHost),
            new ToggleOption<BuildOptions>("Connect With Profiler", BuildOptions.ConnectWithProfiler),
            new ToggleOption<BuildOptions>("Development Build", BuildOptions.Development),
            new ToggleOption<BuildOptions>("Enable Headless Mode", BuildOptions.EnableHeadlessMode),
            new ToggleOption<BuildOptions>("Force Enable Assertions", BuildOptions.ForceEnableAssertions),
            new ToggleOption<BuildOptions>("Install In Build Folder", BuildOptions.InstallInBuildFolder),
            new ToggleOption<BuildOptions>("Show Built Player", BuildOptions.ShowBuiltPlayer),
            new ToggleOption<BuildOptions>("Strict Mode", BuildOptions.StrictMode),
            new ToggleOption<BuildOptions>("Symlink Libraries", BuildOptions.SymlinkLibraries),
            new ToggleOption<BuildOptions>("Uncompressed AssetBundle", BuildOptions.UncompressedAssetBundle)
		};
        
        public static List<ToggleOption<ExportPackageOptions>> ExportPackageOptions = new List<ToggleOption<ExportPackageOptions>> {
	        new ToggleOption<ExportPackageOptions>("Interactive", UnityEditor.ExportPackageOptions.Interactive),
	        new ToggleOption<ExportPackageOptions>("Recurse", UnityEditor.ExportPackageOptions.Recurse),
	        new ToggleOption<ExportPackageOptions>("Include Dependencies", UnityEditor.ExportPackageOptions.IncludeDependencies),
	        new ToggleOption<ExportPackageOptions>("Include Library Assets", UnityEditor.ExportPackageOptions.IncludeLibraryAssets)
        };
        

		public const float WINDOW_SPAN = 20f;

		public const string GROUPING_KEYWORD_DEFAULT = "/Group_*/";
		public const string BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT = "bundle_*";

		// by default, AssetBundleGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL = "bundles";
		public const string BUNDLECONFIG_VARIANTNAME_DEFAULT = "";

		public const string DEFAULT_FILTER_KEYWORD = "";
		public const string DEFAULT_FILTER_KEYTYPE = "Any";

		public const string FILTER_KEYWORD_WILDCARD = "*";

		public const string NODE_INPUTPOINT_FIXED_LABEL = "FIXED_INPUTPOINT_ID";

		public class GUI {
			public const float NODE_BASE_WIDTH = 150;
			public const float NODE_BASE_HEIGHT = 40f;
			public const float NODE_WIDTH_MARGIN = 48f;
			public const float NODE_TITLE_HEIGHT_MARGIN = 8f;

			public const float CONNECTION_ARROW_WIDTH = 12f;
			public const float CONNECTION_ARROW_HEIGHT = 15f;

			public const float INPUT_POINT_WIDTH = 21f;
			public const float INPUT_POINT_HEIGHT = 29f;

			public const float OUTPUT_POINT_WIDTH = 10f;
			public const float OUTPUT_POINT_HEIGHT = 23f;

			public const float FILTER_OUTPUT_SPAN = 32f;

			public const float CONNECTION_POINT_MARK_SIZE = 16f;

			public const float CONNECTION_CURVE_LENGTH = 20f;

			public const float TOOLBAR_HEIGHT = 20f;
			public const float TOOLBAR_GRAPHNAMEMENU_WIDTH = 150f;
			public const int TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH = 20;

			public static readonly Color COLOR_ENABLED = new Color(0.43f, 0.65f, 1.0f, 1.0f);
			public static readonly Color COLOR_CONNECTED = new Color(0.9f, 0.9f, 0.9f, 1.0f);
			public static readonly Color COLOR_NOT_CONNECTED = Color.grey;
			public static readonly Color COLOR_CAN_CONNECT = Color.white;//new Color(0.60f, 0.60f, 1.0f, 1.0f);
			public static readonly Color COLOR_CAN_NOT_CONNECT = new Color(0.33f, 0.33f, 0.33f, 1.0f);

            public static string Skin               { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "NodeStyle.guiskin"); } }
            public static string ConnectionPoint    { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "ConnectionPoint.png"); } }
            public static string InputBG            { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "InputBG.png"); } }
            public static string OutputBG           { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "OutputBG.png"); } }
        
            public static string GraphIcon          { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "ConfigGraphIcon.psd"); } }
            public static string WindowIcon         { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "AssetGraphWindow.png"); } }
            public static string WindowIconPro      { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "d_AssetGraphWindow.png"); } }
		}
	}
}
