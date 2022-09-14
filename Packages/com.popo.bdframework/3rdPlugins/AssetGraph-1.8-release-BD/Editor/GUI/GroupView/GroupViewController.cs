using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

#if ADDRESSABLES_1_6_OR_NEWER
using UnityEditor.AddressableAssets;
#endif

namespace UnityEngine.AssetGraph {

    [Serializable]
    public class GroupViewContext {
        public TreeViewState groupListTreeState;
        public TreeViewState assetListTreeState;
        public MultiColumnHeaderState assetListHeaderState;
        public MultiColumnHeaderState groupListHeaderState;
        public Rect groupListTreeRect;
        public Rect assetListTreeRect;
        public string filterCondition;

        public GroupViewContext() {
            groupListTreeState = new TreeViewState();
            assetListTreeState = new TreeViewState();
            groupListHeaderState   = GroupListTree.CreateDefaultMultiColumnHeaderState();
            assetListHeaderState   = GroupAssetListTree.CreateDefaultMultiColumnHeaderState();

            groupListTreeRect = new Rect(0f,0f,300f, 500f);
            assetListTreeRect = new Rect(0f,0f,300f, 300f);
        }
    }

	public class GroupViewController {

        private Dictionary<string, List<AssetReference>> m_groups;
        private Dictionary<string, List<AssetReference>> m_filteredGroups;
        private GroupViewContext m_ctx;

        private Dictionary<string, List<AssetReference>> ApplyFilter() {
            if (string.IsNullOrEmpty (m_ctx.filterCondition)) {
                return m_groups;
            }

            Regex match = new Regex(m_ctx.filterCondition);
            var newGroups = new Dictionary<string, List<AssetReference>> ();

            foreach (var key in m_groups.Keys) {
                var assets = m_groups[key];
                var filteredAssets = new List<AssetReference> ();

                foreach (var a in assets) {
                    if (match.IsMatch (a.path)) {
                        filteredAssets.Add (a);
                    }
                }

                newGroups [key] = filteredAssets;
            }

            return newGroups;
        }

        public void SetGroups(Dictionary<string, List<AssetReference>> g) {
            if (m_groups != g) {
                m_groups = g;
                m_filteredGroups = ApplyFilter ();
                ReloadAndSelect ();
            }
        }

        private GroupListTree m_groupListTree;
        private GroupAssetListTree m_assetListTree;
        private AssetReference m_selectedAsset;

        private struct ResizeContext {
            public bool isResizeNow;
            public Vector2 dragStartPt;
            public Rect dragStartRect;
        }

        private ResizeContext m_groupListResize;
        private ResizeContext m_assetListResize;

        public Dictionary<string, List<AssetReference>> GroupModel {
            get {
                return m_filteredGroups;
            }
        }

        public GroupViewController(GroupViewContext ctx) {

            m_ctx = ctx;

            m_groupListTree = new GroupListTree (this, m_ctx.groupListTreeState, m_ctx.groupListHeaderState);
            m_assetListTree = new GroupAssetListTree (this, m_ctx.assetListTreeState, m_ctx.assetListHeaderState);

            m_groupListResize = new ResizeContext ();
            m_assetListResize = new ResizeContext ();
        }

        public void OnGroupViewGUI() {

            var newFilterString = EditorGUILayout.TextField ("Filter", m_ctx.filterCondition);
            if (newFilterString != m_ctx.filterCondition) {
                m_ctx.filterCondition = newFilterString;
                m_filteredGroups = ApplyFilter ();
                ReloadAndSelect ();
            }

            Rect groupListTreeRect = GUILayoutUtility.GetRect (m_ctx.groupListTreeRect.width, m_ctx.groupListTreeRect.height, GUILayout.ExpandWidth (true));
            Rect groupListResizeRect = GUILayoutUtility.GetRect (100f, 4f, GUILayout.ExpandWidth (true));

            GUILayout.Space (8f);

            Rect assetListTreeRect = GUILayoutUtility.GetRect (m_ctx.assetListTreeRect.width, m_ctx.assetListTreeRect.height, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));
            Rect assetListResizeRect = GUILayoutUtility.GetRect (100f, 4f, GUILayout.ExpandWidth (true));

            m_groupListTree.OnGUI (groupListTreeRect);
            m_assetListTree.OnGUI (assetListTreeRect);

            HandleHorizontalResize (groupListResizeRect, ref m_ctx.groupListTreeRect, ref m_groupListResize);
            HandleHorizontalResize (assetListResizeRect, ref m_ctx.assetListTreeRect, ref m_assetListResize);

            string selectedAsset = "";

            if (m_selectedAsset != null) {
                selectedAsset = m_selectedAsset.path;
            }

            using(new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.HelpBox(selectedAsset, MessageType.None);
            }

#if ADDRESSABLES_1_6_OR_NEWER
            if (m_selectedAsset != null) {
                var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
                if (aaSettings != null) {
                    var entry = aaSettings.FindAssetEntry(m_selectedAsset.assetDatabaseId);
                    if (entry != null) {
                        GUILayout.Space (4f);
                        GUILayout.Label ("Asset Address");
                        using(new EditorGUILayout.HorizontalScope()) {
                            EditorGUILayout.HelpBox(entry.address, MessageType.None);
                        }
                    }
                }
            }
#endif
		}

        public void ReloadAndSelect() {
            m_groupListTree.Reload ();
            m_assetListTree.Reload ();

            m_groupListTree.Reselect();
        }

        public void UnselectGroup() {
            m_assetListTree.SetAssetList (null);
        }

        public void GroupSelectionChanged(List<AssetReference> assets) {
            m_assetListTree.SetAssetList (assets);
        }

        public void AssetSelectionChanged(AssetReference a) {
            m_selectedAsset = a;
        }

        private void HandleHorizontalResize(Rect horizontalSpritRect, ref Rect dragTargetRect, ref ResizeContext rc)
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
            }

            if (Event.current.type == EventType.MouseUp) {
                rc.isResizeNow = false;
            }
        }
    }
}