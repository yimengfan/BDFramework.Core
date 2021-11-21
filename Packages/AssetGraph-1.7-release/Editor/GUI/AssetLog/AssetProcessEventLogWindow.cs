using UnityEditor;

using UnityEditor.IMGUI.Controls;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class AssetProcessEventLogWindow : EditorWindow {

        private AssetProcessEventLogViewController m_logViewController;

        private bool m_showError;
        private bool m_showInfo;
        private bool m_clearOnBuild;
        private Texture2D m_errorIcon;
        private Texture2D m_infoIcon;
        private SearchField m_search;
        private string m_searchKeywords;

        [MenuItem(Model.Settings.GUI_TEXT_MENU_ASSETLOGWINDOW_OPEN, priority = 14000 + 2)]
		public static void Open () {
            GetWindow<AssetProcessEventLogWindow>();
		}

		private void Init() {
			this.titleContent = new GUIContent("Asset Log");
			this.minSize = new Vector2(150f, 100f);

            m_errorIcon = EditorGUIUtility.Load ("icons/console.erroricon.sml.png") as Texture2D;
            m_infoIcon = EditorGUIUtility.Load ("icons/console.infoicon.sml.png") as Texture2D;

            m_showError = true;
            m_showInfo = true;

            m_clearOnBuild = UserPreference.ClearAssetLogOnBuild;

            m_logViewController = new AssetProcessEventLogViewController ();
            m_search = new SearchField ();

            AssetProcessEventRecord.GetRecord ().SetFilterCondition (m_showInfo, m_showError);
            AssetProcessEventRecord.GetRecord ().SetFilterKeyword (string.Empty);
		}

		public void OnEnable () {
			Init();
            AssetProcessEventRecord.onAssetProcessEvent += this.OnNewAssetProcessEvent;

            m_logViewController.OnEnable ();
		}

		public void OnDisable() {
            AssetProcessEventRecord.onAssetProcessEvent -= this.OnNewAssetProcessEvent;
		}

        public void DrawToolBar() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {

                var r = AssetProcessEventRecord.GetRecord ();

                if (GUILayout.Button ("Clear", EditorStyles.toolbarButton, GUILayout.Height (Model.Settings.GUI.TOOLBAR_HEIGHT))) {
                    AssetProcessEventRecord.GetRecord ().Clear (true);
                    m_logViewController.EventSelectionChanged (null);
                    RefreshView ();
                }

                GUILayout.Space (4);

                var clearOnBuild = m_clearOnBuild;
                clearOnBuild = GUILayout.Toggle (m_clearOnBuild, new GUIContent ("Clear on Build"), EditorStyles.toolbarButton, GUILayout.Height (Model.Settings.GUI.TOOLBAR_HEIGHT));
                if (clearOnBuild != m_clearOnBuild) {
                    UserPreference.ClearAssetLogOnBuild = m_clearOnBuild = clearOnBuild;
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck ();

                m_searchKeywords = m_search.OnToolbarGUI (m_searchKeywords);

                if (EditorGUI.EndChangeCheck ()) {
                    r.SetFilterKeyword (m_searchKeywords);
                    m_logViewController.Reload ();
                }

                var infoEventCountStr = (string.IsNullOrEmpty (m_searchKeywords)) ? r.InfoEventCount.ToString () : r.FilteredInfoEventCount.ToString();
                var errorEventCountStr = (string.IsNullOrEmpty (m_searchKeywords)) ? r.ErrorEventCount.ToString () : r.FilteredErrorEventCount.ToString();

                var showInfo = GUILayout.Toggle(m_showInfo, new GUIContent(infoEventCountStr, m_infoIcon, "Toggle Show Info"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
                var showError = GUILayout.Toggle(m_showError, new GUIContent(errorEventCountStr, m_errorIcon, "Toggle Show Errors"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

                if(showInfo != m_showInfo || showError != m_showError) {
                    m_showInfo = showInfo;
                    m_showError = showError;
                    r.SetFilterCondition(m_showInfo, m_showError);
                    m_logViewController.Reload ();
                }
            }
        }

		public void OnGUI () {

            DrawToolBar ();

            if (m_logViewController.OnLogViewGUI (this)) {
                Repaint ();
            }
		}

        private void RefreshView() {
            m_logViewController.Reload ();
        }

        public void OnNewAssetProcessEvent(AssetProcessEvent e) {
            m_logViewController.OnNewAssetProcessEvent (e);
            Repaint ();
        }
	}
}

