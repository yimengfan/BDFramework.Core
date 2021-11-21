using UnityEditor;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class BatchBuildWindow : EditorWindow {

        private enum Mode : int
        {
            Edit,
            Build
        }

        private GraphCollectionManageTab m_manageTab;
        private GraphCollectionExecuteTab m_executeTab;
        private Mode m_mode;

		private static BatchBuildWindow s_window;

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BATCHWINDOW_OPEN, priority = 14000 + 2)]
		public static void Open () {
			GetWindow<BatchBuildWindow>();
		}

		private void Init() {
			this.titleContent = new GUIContent("Batch Build");
			this.minSize = new Vector2(350f, 300f);

            m_manageTab = new GraphCollectionManageTab ();
            m_executeTab = new GraphCollectionExecuteTab ();
            m_mode = Mode.Edit;
		}

		public void OnEnable () {
			Init();
            m_manageTab.OnEnable (position, this);
            m_executeTab.OnEnable (position, this);
		}

		public void OnFocus() {

            BatchBuildConfig.GetConfig ().Validate ();

            switch (m_mode) {
            case Mode.Edit:
                m_manageTab.Refresh ();
                break;
            case Mode.Build:
                m_executeTab.Refresh ();
                break;
            }
		}

		public void OnDisable() {
		}

        public void OnGUI () {

            var needRefresh = DrawToolBar ();

            var tabRect = GUILayoutUtility.GetRect (100f, 100f, GUILayout.ExpandHeight (true), GUILayout.ExpandWidth (true));
            var bound = new Rect (0f, 0f, tabRect.width, tabRect.height);

            GUI.BeginGroup (tabRect);

            switch (m_mode) {
            case Mode.Edit:
                if(needRefresh) {
                    m_manageTab.Refresh();
                }
                m_manageTab.OnGUI (bound);
                break;
            case Mode.Build:
                if(needRefresh) {
                    m_executeTab.Refresh ();
                }
                m_executeTab.OnGUI (bound);
                break;
            }

            GUI.EndGroup ();
        }

        private bool DrawToolBar() {

            var oldMode = m_mode;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float toolbarWidth = position.width - (20f*2f);
            string[] labels = new string[] { "Edit", "Execute" };
            m_mode = (Mode)GUILayout.Toolbar((int)m_mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            return oldMode != m_mode;
        }
	}
}
