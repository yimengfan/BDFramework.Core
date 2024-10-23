using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System.IO;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class AssetProcessEventLogViewController {

        private TreeViewState m_assetLogListTreeState;
        private MultiColumnHeaderState m_assetLogListHeaderState;
        private Rect m_assetLogListTreeRect;
        private Rect m_detailRect;

        private AssetProcessEventListTree m_assetLogListTree;
        private AssetProcessEvent m_selectedEvent;

        private struct ResizeContext {
            public bool isResizeNow;
            public Vector2 dragStartPt;
            public Rect dragStartRect;
        }

        private ResizeContext m_detailViewResize;

        public AssetProcessEventLogViewController() {

            m_assetLogListTreeState = new TreeViewState();
            m_assetLogListHeaderState   = AssetProcessEventListTree.CreateDefaultMultiColumnHeaderState();

            m_assetLogListTreeRect = new Rect(0f,0f,300f, 300f);
            m_detailRect = new Rect(0f,0f,300f, 180f);
            m_assetLogListTree = new AssetProcessEventListTree (this, m_assetLogListTreeState, m_assetLogListHeaderState);

            m_detailViewResize = new ResizeContext ();
        }

        public void OnEnable() {
            m_assetLogListTree.Reload ();
            m_assetLogListTree.Repaint ();
        }

        public bool OnLogViewGUI(AssetProcessEventLogWindow w) {

            Rect detailRect = GUILayoutUtility.GetRect (m_detailRect.width, m_detailRect.height, GUILayout.ExpandWidth (true));

            DrawLogDetail (w, detailRect);

            Rect resizeRect = GUILayoutUtility.GetRect (100f, 4f, GUILayout.ExpandWidth (true));

            Rect treeRect = GUILayoutUtility.GetRect (m_assetLogListTreeRect.width, m_assetLogListTreeRect.height, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight(true));
            m_assetLogListTree.OnGUI (treeRect);

            return HandleHorizontalResize (resizeRect, ref m_detailRect, ref m_detailViewResize);
		}

        public void DrawLogDetail(AssetProcessEventLogWindow w, Rect detailRect) {

            if (m_selectedEvent != null) {
                var e = m_selectedEvent;
                var isError = e.Kind == AssetProcessEvent.EventKind.Error;

                var assetGuid = e.AssetGuid;
                var assetPath = AssetDatabase.GUIDToAssetPath (assetGuid);
                var graphGuid = e.GraphGuid;
                var graphPath = AssetDatabase.GUIDToAssetPath (graphGuid);

                var boldWrapStyle = new GUIStyle (EditorStyles.boldLabel);
                boldWrapStyle.wordWrap = true;
                boldWrapStyle.fontSize = 9;
                boldWrapStyle.alignment = TextAnchor.MiddleCenter;

                var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                var preview = AssetPreview.GetAssetPreview (obj);

                if (preview == null)
                {
                    if (obj != null) {
                        bool isLoadingAssetPreview = AssetPreview.IsLoadingAssetPreview (obj.GetInstanceID ());
                        if (isLoadingAssetPreview) {
                            w.Repaint ();
                        }
                        preview = AssetPreview.GetMiniThumbnail (obj);
                    }
                }

                var msg = (preview != null) ? string.Empty : "No Preview";
                var previewSizeMax = 128f;

                var previewBaseRect = new Rect (detailRect.x + 8, detailRect.y + 8, previewSizeMax, previewSizeMax);
                var assetNameRect = new Rect (detailRect.x + 2, previewBaseRect.yMax, previewBaseRect.width + 12, 28);

                var previewBaseStyle = new GUIStyle ();
                previewBaseStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label (previewBaseRect, new GUIContent(msg), previewBaseStyle);

                if (preview != null) {
                    var previewWidth = (preview != null) ? preview.width : previewSizeMax;
                    var previewHeight = (preview != null) ? preview.height : previewSizeMax;

                    var pwMargin = (previewSizeMax - previewWidth) / 2f;
                    var phMargin = (previewSizeMax - previewHeight) / 2f;

                    var previewRect = new Rect (detailRect.x + pwMargin + 8, detailRect.y + phMargin + 8, previewWidth, previewHeight);

                    GUI.Label (previewRect, new GUIContent(preview, "Asset Preview"), previewBaseStyle);
                }
                GUI.Label (assetNameRect, Path.GetFileName(assetPath), boldWrapStyle);

                if (isError) {
                    GUI.Label(new Rect(previewBaseRect.xMin, previewBaseRect.yMin, 36f, 36f), EditorGUIUtility.IconContent ("console.erroricon"));
                }
                var timestampRect = new Rect (detailRect.x + 2, assetNameRect.yMax, previewBaseRect.width + 12, 20);
                GUI.Label (timestampRect, e.Timestamp.ToString(), boldWrapStyle);

                var graphNameStyle = new GUIStyle (EditorStyles.boldLabel);
                graphNameStyle.wordWrap = true;

                var graphButtonRect = new Rect (detailRect.xMax - 44, detailRect.y + 8, 40f, 20f);
                var graphNameRect = new Rect (previewBaseRect.xMax + 20, detailRect.y + 8, detailRect.width - previewBaseRect.width - graphButtonRect.width - 4, 40f);

                GUI.Label (graphNameRect, string.Format("{2}: {0}.{1}", Path.GetFileNameWithoutExtension(graphPath), e.NodeName, e.Kind), graphNameStyle);

                if (GUI.Button (graphButtonRect, "Open")) {
                    var window = EditorWindow.GetWindow<AssetGraphEditorWindow>();
                    window.OpenGraph(graphPath);
                    window.SelectNode (e.NodeId);
                }

                if (isError) {

                    var label1Rect  = new Rect (graphNameRect.xMin, graphNameRect.yMax + 8, detailRect.width - previewBaseRect.width - 32f, 16f);

                    var msg1Height = EditorStyles.helpBox.CalcHeight (new GUIContent (e.Description), label1Rect.width);
                    var msg2Height = EditorStyles.helpBox.CalcHeight (new GUIContent (e.HowToFix), label1Rect.width);

                    var msg1Rect    = new Rect (graphNameRect.xMin, label1Rect.yMax + 2,    label1Rect.width, Mathf.Max(msg1Height, 32f));

                    var label2Rect  = new Rect (graphNameRect.xMin, msg1Rect.yMax + 8,      label1Rect.width, label1Rect.height);
                    var msg2Rect    = new Rect (graphNameRect.xMin, label2Rect.yMax + 2,    label1Rect.width, Mathf.Max(msg2Height, 32f));

                    GUI.Label (label1Rect, "What's wrong?", EditorStyles.miniBoldLabel);
                    GUI.Label (msg1Rect, e.Description, EditorStyles.helpBox);
                    GUI.Label (label2Rect, "How do I fix this?", EditorStyles.miniBoldLabel);
                    GUI.Label (msg2Rect, e.HowToFix, EditorStyles.helpBox);
                }
            }
        }

        public void OnNewAssetProcessEvent(AssetProcessEvent e) {
            m_assetLogListTree.OnNewAssetProcessEvent (e);
        }

        public void Reload() {
            m_assetLogListTree.Reload ();
        }

        public void EventSelectionChanged(AssetProcessEvent e) {
            m_selectedEvent = e;
        }

        private bool HandleHorizontalResize(Rect horizontalSpritRect, ref Rect dragTargetRect, ref ResizeContext rc)
        {
            EditorGUIUtility.AddCursorRect(horizontalSpritRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown &&
                horizontalSpritRect.Contains (Event.current.mousePosition)) 
            {
                rc.isResizeNow = true;
                rc.dragStartPt = Event.current.mousePosition;
                rc.dragStartRect = dragTargetRect;
            }

            if (Event.current.type == EventType.MouseDrag && rc.isResizeNow)
            {
                var yDiff = Event.current.mousePosition.y - rc.dragStartPt.y;
                Rect newRect = rc.dragStartRect;
                newRect.height = Mathf.Max(70f, newRect.height + yDiff);
                dragTargetRect = newRect;

                return true;
            }

            if (Event.current.type == EventType.MouseUp) {
                rc.isResizeNow = false;
            }

            return false;
        }
    }
}
