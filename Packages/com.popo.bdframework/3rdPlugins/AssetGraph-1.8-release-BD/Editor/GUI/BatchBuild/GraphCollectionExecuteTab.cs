using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    internal class GraphCollectionExecuteTab 
    {
        private const float k_SplitterHeight = 4f;

        [SerializeField]
        private TreeViewState m_buildTargetTreeState;

        [SerializeField]
        private TreeViewState m_executeResultTreeState;

        [SerializeField]
        private float m_verticalSplitterPercent;

        [SerializeField]
        private float m_msgVerticalSplitterPercent;

        [SerializeField]
        private string m_selectedCollectionGuid;

        private BatchBuildConfig.GraphCollection m_currentCollection;
        private List<ExecuteGraphResult> m_result;

        private BuildTargetTree m_buildTargetTree;
        private ExecuteResultTree m_executeResultTree;
        private string[] m_collectionNames;
        private int m_selectedCollectionIndex = -1;

        private bool m_resizingVerticalSplitter = false;
        private Rect m_verticalSplitterRect;

        private bool m_msgResizingVerticalSplitter = false;
        private Rect m_msgVerticalSplitterRect;

        private EditorWindow m_parent = null;
        private long m_lastBuildTimestamp;

        private ExecuteGraphResult m_selectedResult;
        private NodeException m_selectedException;
        private Vector2 m_msgScrollPos;

        private Vector2 kSritRange = new Vector2 (0.2f, 0.8f);
        private Vector2 kMsgSpritRange = new Vector2 (0.2f, 0.6f);

        public GraphCollectionExecuteTab()
        {
            m_verticalSplitterPercent = 0.2f;
            m_msgVerticalSplitterPercent = 0.5f;
            m_verticalSplitterRect = new Rect(0,0,0, k_SplitterHeight);
            m_msgVerticalSplitterRect = new Rect(0,0,0, k_SplitterHeight);
            m_msgScrollPos = new Vector2 (0f, 0f);
        }

        public List<ExecuteGraphResult> CurrentResult {
            get {
                return m_result;
            }
        }

        public long LastBuildTimestamp {
            get {
                return m_lastBuildTimestamp;
            }
        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_parent = parent;

            m_result = new List<ExecuteGraphResult> ();

            m_buildTargetTreeState = new TreeViewState ();
            m_buildTargetTree = new BuildTargetTree(m_buildTargetTreeState);

            m_executeResultTreeState = new TreeViewState ();
            m_executeResultTree = new ExecuteResultTree(m_executeResultTreeState, this);

            m_buildTargetTree.Reload ();
            m_executeResultTree.Reload ();
        }

        public void Refresh() {
            if (m_buildTargetTree != null) {
                m_buildTargetTree.Reload ();
                m_executeResultTree.ReloadIfNeeded ();
                var collection = BatchBuildConfig.GetConfig ().GraphCollections;
                m_collectionNames = collection.Select (c => c.Name).ToArray ();
                m_selectedCollectionIndex = collection.FindIndex (c => c.Guid == m_selectedCollectionGuid);
                if (m_selectedCollectionIndex >= 0) {
                    m_currentCollection = collection [m_selectedCollectionIndex];
                } else {
                    m_currentCollection = null;
                }
            }
        }

        private void DrawBuildDropdown(Rect region) {

            var popupRgn  = new Rect (region.x+20f, region.y, region.width - 120f, region.height);
            var buttonRgn = new Rect (popupRgn.xMax+8f, popupRgn.y, 80f, popupRgn.height);

            using (new EditorGUI.DisabledGroupScope (BatchBuildConfig.GetConfig ().GraphCollections.Count == 0)) {

                var newIndex = EditorGUI.Popup (popupRgn, "Graph Collection", m_selectedCollectionIndex, m_collectionNames);
                if (newIndex != m_selectedCollectionIndex) {
                    m_selectedCollectionIndex = newIndex;
                    m_currentCollection = BatchBuildConfig.GetConfig ().GraphCollections [m_selectedCollectionIndex];
                    m_selectedCollectionGuid = m_currentCollection.Guid;
                }

                using (new EditorGUI.DisabledGroupScope (m_currentCollection == null || BatchBuildConfig.GetConfig ().BuildTargets.Count == 0)) {
                    if (GUI.Button (buttonRgn, "Execute")) {
                        Build ();
                    }
                }
            }
        }

        private void DrawSelectedExecuteResultMessage(Rect region) {

            string msg = null;

            // no item selected
            if (m_selectedResult == null) {
                msg = string.Empty;
            } 
            // build result
            else if (m_selectedException == null) {
                var graphName = Path.GetFileNameWithoutExtension (m_selectedResult.GraphAssetPath);
                msg = string.Format ("Build {2}.\n\nGraph:{0}\nPlatform:{1}", 
                    graphName, 
                    BuildTargetUtility.TargetToHumaneString(m_selectedResult.Target),
                    m_selectedResult.IsAnyIssueFound ? "Failed" : "Successful"
                );
            }
            // build result with exception
            else {
                var graphName = Path.GetFileNameWithoutExtension (m_selectedResult.GraphAssetPath);
                msg =
                    $"{m_selectedException.Reason}\n\nHow to fix:\n{m_selectedException.HowToFix}\n\nWhere:'{m_selectedException.Node.Name}' in {graphName}\nPlatform:{BuildTargetUtility.TargetToHumaneString(m_selectedResult.Target)}";
            }

            var msgStyle = GUI.skin.label;
            msgStyle.alignment = TextAnchor.UpperLeft;
            msgStyle.wordWrap = true;

            var content = new GUIContent(msg);
            var height = msgStyle.CalcHeight(content, region.width - 16f);

            var msgRect = new Rect (0f, 0f, region.width - 16f, height);

            m_msgScrollPos = GUI.BeginScrollView(region, m_msgScrollPos, msgRect);

            GUI.Label (msgRect, content);

            GUI.EndScrollView();
        }

        public void SetSelectedExecuteResult(ExecuteGraphResult r, NodeException e) {
            m_selectedResult = r;
            m_selectedException = e;
            m_msgScrollPos = new Vector2 (0f, 0f);
        }

        public void OnGUI(Rect pos)
        {
            var dropdownUIBound = new Rect (0f, 0f, pos.width, 16f);
            var labelUIBound = new Rect (4f, dropdownUIBound.yMax, 80f, 24f);
            var listviewUIBound = new Rect (4f, labelUIBound.yMax, dropdownUIBound.width -8f, pos.height - dropdownUIBound.yMax);

            DrawBuildDropdown (dropdownUIBound);

            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.LowerLeft;
            GUI.Label (labelUIBound, "Build Targets", labelStyle);

            using (new GUI.GroupScope (listviewUIBound)) {

                var groupUIBound = new Rect (0f, 0f, listviewUIBound.width, listviewUIBound.height);

                HandleVerticalResize (groupUIBound, ref m_verticalSplitterRect, ref m_verticalSplitterPercent, ref m_resizingVerticalSplitter, ref kSritRange);

                var boundTop = new Rect (
                               4f,
                               0f,
                               groupUIBound.width -8f,
                               m_verticalSplitterRect.y);
        
                var bottomLabelUIBound = new Rect (4f, m_verticalSplitterRect.yMax, 80f, 24f);
                var boundBottom = new Rect (
                                  boundTop.x,
                                  bottomLabelUIBound.yMax,
                                  boundTop.width,
                                  groupUIBound.height - m_verticalSplitterRect.yMax);
                var bottomUIBound = new Rect (0f, 0f, boundBottom.width, boundBottom.height);

                GUI.Label (bottomLabelUIBound, "Build Results", labelStyle);
                m_buildTargetTree.OnGUI (boundTop);

                if (BatchBuildConfig.GetConfig ().BuildTargets.Count == 0) {
                    var style = GUI.skin.label;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.wordWrap = true;

                    GUI.Label (new Rect (boundTop.x + 12f, boundTop.y, boundTop.width - 24f, boundTop.height), 
                        new GUIContent ("Right click here and add targets to build."), style);
                }

                using (new GUI.GroupScope (boundBottom, EditorStyles.helpBox)) {
                    HandleVerticalResize (bottomUIBound, ref m_msgVerticalSplitterRect, ref m_msgVerticalSplitterPercent, ref m_msgResizingVerticalSplitter, ref kMsgSpritRange);

                    var execResultBound = new Rect (
                        0f,
                        0f,
                        bottomUIBound.width,
                        m_msgVerticalSplitterRect.y);
                    
                    var msgBound = new Rect (
                        execResultBound.x,
                        m_msgVerticalSplitterRect.yMax,
                        execResultBound.width,
                        bottomUIBound.height - m_msgVerticalSplitterRect.yMax - 52f);

                    m_executeResultTree.OnGUI (execResultBound);

                    DrawSelectedExecuteResultMessage (msgBound);
                }

                if (m_resizingVerticalSplitter || m_msgResizingVerticalSplitter) {
                    m_parent.Repaint ();
                }
            }
        }

        private static void HandleVerticalResize(Rect bound, ref Rect splitterRect, ref float percent, ref bool splitting, ref Vector2 range)
        {
            var height = bound.height - splitterRect.height;
            splitterRect.x = bound.x;
            splitterRect.y = (int)(height * percent);
            splitterRect.width = bound.width;

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

            var mousePt = Event.current.mousePosition;

            if (Event.current.type == EventType.MouseDown && splitterRect.Contains (mousePt)) {
                splitting = true;
            }

            if (splitting)
            {
                percent = Mathf.Clamp(mousePt.y / bound.height, range.x, range.y);
                splitterRect.y = bound.y + (int)(height * percent);
            }

            if (Event.current.type == EventType.MouseUp)
            {
                splitting = false;
            }
        }

        private int GetTotalNodeCount(BatchBuildConfig.GraphCollection collection) {
            var c = 0;

            foreach(var guid in collection.GraphGUIDs) {
                var path = AssetDatabase.GUIDToAssetPath (guid);
                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph> (path);
                if (graph != null) {
                    c += graph.Nodes.Count;
                }
            }

            return c;
        }

        public void Build() {
            m_result.Clear ();

            var currentCount = 0f;
            var totalCount = (float)GetTotalNodeCount (m_currentCollection) * BatchBuildConfig.GetConfig ().BuildTargets.Count;
            Model.NodeData lastNode = null;

            foreach (var t in BatchBuildConfig.GetConfig ().BuildTargets) {

                Action<Model.NodeData, string, float> updateHandler = (node, message, progress) => {

                    if(lastNode != node) {
                        // do not add count on first node visit to 
                        // calcurate percantage correctly
                        if(lastNode != null) {
                            ++currentCount;
                        }
                        lastNode = node;
                    }

                    var currentNodeProgress = progress * (1.0f / totalCount);
                    var currentTotalProgress = (currentCount/totalCount) + currentNodeProgress;

                    var title = string.Format("{2} - Processing Asset Graphs[{0}/{1}]", currentCount, totalCount, BuildTargetUtility.TargetToHumaneString(t));
                    var info  = $"{node.Name}:{message}";

                    EditorUtility.DisplayProgressBar(title, "Processing " + info, currentTotalProgress);
                };

                var result = AssetGraphUtility.ExecuteGraphCollection(t, m_currentCollection, updateHandler);
                EditorUtility.ClearProgressBar();
                m_result.AddRange (result);

                m_lastBuildTimestamp = DateTime.UtcNow.ToFileTimeUtc ();

                m_executeResultTree.ReloadAndSelectLast ();
                m_parent.Repaint ();
            }
        }
    }
}