using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    internal class GraphCollectionManageTab 
    {
        [SerializeField]
        TreeViewState m_GraphCollectionTreeState;
        [SerializeField]
        TreeViewState m_GraphCollectionDetailTreeState;

        Rect m_Position;

        GraphCollectionTree m_collectionTree;
        GraphCollectionDetailTree m_detailTree;
        BatchBuildConfig.GraphCollection m_currentCollection;

        bool m_ResizingHorizontalSplitter = false;
        Rect m_HorizontalSplitterRect;

        [SerializeField]
        float m_HorizontalSplitterPercent;

        const float k_SplitterWidth = 3f;

        EditorWindow m_Parent = null;

        public BatchBuildConfig.GraphCollection CurrentCollection {
            get {
                return m_currentCollection;
            }
        }

        public GraphCollectionManageTab()
        {
            m_HorizontalSplitterPercent = 0.4f;
        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_Parent = parent;
            m_Position = pos;
            m_HorizontalSplitterRect = new Rect(
                (int)(m_Position.x + m_Position.width * m_HorizontalSplitterPercent),
                m_Position.y,
                k_SplitterWidth,
                m_Position.height);
            
            m_GraphCollectionTreeState = new TreeViewState ();
            m_collectionTree = new GraphCollectionTree(m_GraphCollectionTreeState, this);

            m_GraphCollectionDetailTreeState = new TreeViewState ();
            m_detailTree = new GraphCollectionDetailTree(m_GraphCollectionDetailTreeState, this);

            m_collectionTree.Reload ();
            m_detailTree.Reload ();
        }

        public void DrawToolBar(Rect toolbarBound) {

            GUI.BeginGroup (toolbarBound, EditorStyles.toolbar);

            var addBunttonRect = new Rect (0f, 0f, 40f, toolbarBound.height);

            if (GUI.Button (addBunttonRect, "Add", EditorStyles.toolbarButton)) {
                BatchBuildConfig.CreateNewGraphCollection ("New Collection");

                m_collectionTree.Reload ();
            }

            GUI.EndGroup();
        }

        public void OnGUI(Rect pos)
        {
            m_Position = pos;

            var toolbarBound = new Rect (0f, 0f, m_Position.width, Model.Settings.GUI.TOOLBAR_HEIGHT);

            DrawToolBar (toolbarBound);

            if ((BatchBuildConfig.GetConfig ().GraphCollections.Count == 0)) {

                var bound = new Rect(
                    toolbarBound.x + 4f,
                    toolbarBound.yMax,
                    m_Position.width - 8f,
                    m_Position.height - toolbarBound.height-4f);

                var style = GUI.skin.label;
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;

                m_collectionTree.OnGUI(bound);

                GUI.Label(new Rect(bound.x+12f, bound.y, bound.width - 24f, bound.height), 
                    new GUIContent("Drag graph assets here or press Add button to begin creating graph collections."), style);

            } else {
                HandleHorizontalResize();

                //Left half
                var leftBound = new Rect(
                    toolbarBound.x + 4f,
                    toolbarBound.yMax,
                    m_HorizontalSplitterRect.x,
                    m_Position.height - toolbarBound.height-4f);

                m_collectionTree.OnGUI(leftBound);


                //Right half.
                float panelLeft = m_HorizontalSplitterRect.x + k_SplitterWidth;
                float panelWidth =  m_Position.width - m_HorizontalSplitterRect.x - k_SplitterWidth * 2;

                var rightBound = new Rect (
                    panelLeft,
                    toolbarBound.yMax,
                    panelWidth,
                    m_Position.height - toolbarBound.height-4f);

                m_detailTree.OnGUI(rightBound);

                if (m_ResizingHorizontalSplitter) {
                    m_Parent.Repaint ();
                }
            }
        }

        private void HandleHorizontalResize()
        {
            m_HorizontalSplitterRect.x = (int)(m_Position.width * m_HorizontalSplitterPercent);
            m_HorizontalSplitterRect.y = Model.Settings.GUI.TOOLBAR_HEIGHT;
            m_HorizontalSplitterRect.height = m_Position.height - Model.Settings.GUI.TOOLBAR_HEIGHT;

            EditorGUIUtility.AddCursorRect(m_HorizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && m_HorizontalSplitterRect.Contains(Event.current.mousePosition))
                m_ResizingHorizontalSplitter = true;

            if (m_ResizingHorizontalSplitter)
            {
                m_HorizontalSplitterPercent = Mathf.Clamp(Event.current.mousePosition.x / m_Position.width, 0.1f, 0.9f);
                m_HorizontalSplitterRect.x = (int)(m_Position.width * m_HorizontalSplitterPercent);
            }

            if (Event.current.type == EventType.MouseUp)
                m_ResizingHorizontalSplitter = false;
        }

        public void UpdateSelectedGraphCollection(BatchBuildConfig.GraphCollection c) {
            m_currentCollection = c;
            m_detailTree.SetCurrentGraphCollection (c);
        }

        public void Refresh() {
            if(m_collectionTree != null) {
                m_collectionTree.ReloadAndSelect ();
            }
        }
    }
}