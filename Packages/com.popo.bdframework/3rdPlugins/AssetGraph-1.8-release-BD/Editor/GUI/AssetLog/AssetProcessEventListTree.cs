using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System.IO;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    public class AssetProcessEventListTreeItem : TreeViewItem
    {
        private AssetProcessEvent m_event;
        public AssetProcessEvent Event
        {
            get { return m_event; }
        }
        public AssetProcessEventListTreeItem() : base(-1, -1) { }
        public AssetProcessEventListTreeItem(AssetProcessEvent e) : base(e.GetHashCode(), 0, string.Empty)
        {
            m_event = e;
        }
    }

    internal class AssetProcessEventListTree : TreeView
    {
        private AssetProcessEventLogViewController m_controller;
        private Texture2D m_errorIcon;
        private Texture2D m_infoIcon;

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }
        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new MultiColumnHeaderState.Column[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };
            int i = 0;

            retVal[i].headerContent = new GUIContent("", "Event Type.");
            retVal[i].minWidth = 32;
            retVal[i].width = 32;
            retVal[i].maxWidth = 32;
            retVal[i].headerTextAlignment = TextAlignment.Center;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            retVal[i].headerContent = new GUIContent("Asset", "Asset Name");
            retVal[i].minWidth = 50;
            retVal[i].width = 250;
            retVal[i].maxWidth = 500;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            retVal[i].headerContent = new GUIContent("Graph", "Graph.");
            retVal[i].minWidth = 30;
            retVal[i].width = 300;
            retVal[i].maxWidth = 1000;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            return retVal;
        }

        public AssetProcessEventListTree(AssetProcessEventLogViewController parent, TreeViewState state, MultiColumnHeaderState mchs ) : base(state, new MultiColumnHeader(mchs))
        {
            m_controller = parent;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            //DefaultStyles.label.richText = true;

            m_errorIcon = EditorGUIUtility.Load ("icons/console.erroricon.png") as Texture2D;
            m_infoIcon = EditorGUIUtility.Load ("icons/console.infoicon.png") as Texture2D;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item) {
            return 32f;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        public void OnNewAssetProcessEvent(AssetProcessEvent e) {
            Reload();
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new AssetProcessEventListTreeItem ();

            var r = AssetProcessEventRecord.GetRecord ();

            if (r != null && r.Events != null) {
                foreach (var e in r.Events) {
                    root.AddChild (new AssetProcessEventListTreeItem (e));
                }
            }

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns (); ++i) {
                CellGUI (args.GetCellRect (i), args.item as AssetProcessEventListTreeItem, args.GetColumn (i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, AssetProcessEventListTreeItem item, int column, ref RowGUIArgs args)
        {
            Color oldColor = GUI.color;
            CenterRectUsingSingleLineHeight(ref cellRect);
            var assetGuid = item.Event.AssetGuid;
            var assetPath = AssetDatabase.GUIDToAssetPath (assetGuid);
            var isAssetAvailable = string.IsNullOrEmpty (assetPath);

            var graphGuid = item.Event.GraphGuid;
            var graphPath = AssetDatabase.GUIDToAssetPath (graphGuid);
            var hasGraph = !string.IsNullOrEmpty (graphPath);

            var isError = item.Event.Kind == AssetProcessEvent.EventKind.Error;

            switch (column)
            {
            case 0://Error?
                {
                    var iconRect = new Rect (cellRect.x, cellRect.y - 6, 32, 32);
                    GUI.DrawTexture (iconRect, ((isError)? m_errorIcon:m_infoIcon));
                }
                break;
            case 1://Asset
                {
                    Texture2D icon = null;
                    if (!isAssetAvailable) {
                        icon = AssetDatabase.GetCachedIcon (assetPath) as Texture2D;
                    }
                    var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                    if (icon != null) {
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }
                    DefaultGUI.Label(
                        new Rect(cellRect.x + iconRect.width + 4, cellRect.y, cellRect.width - iconRect.width, cellRect.height), 
                        (!isAssetAvailable)? Path.GetFileName(assetPath) : $"{item.Event.AssetName} (Removed)", 
                        args.selected, 
                        args.focused);
                }
                break;
            case 2://Graph
                if (hasGraph) {
                    DefaultGUI.Label(cellRect, $"{Path.GetFileNameWithoutExtension(graphPath)}.{item.Event.NodeName}", args.selected, args.focused);
                }
                break;
            }
            GUI.color = oldColor;
        }

        protected override void DoubleClickedItem(int id)
        {
            PingAssetAtId (id);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0) {
                m_controller.EventSelectionChanged (null);
            }
            else {
                var item = FindItem(selectedIds[0], rootItem) as AssetProcessEventListTreeItem;
                if (item != null) {
                    m_controller.EventSelectionChanged (item.Event);
                    PingAssetAtId (selectedIds[0]);
                }
            }
        }

        private void PingAssetAtId(int id) {
            var item = FindItem(id, rootItem) as AssetProcessEventListTreeItem;
            if (item != null)
            {
                var assetGuid = item.Event.AssetGuid;
                if (!string.IsNullOrEmpty (assetGuid)) {
                    var obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGuid));
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
        }
    }
}
