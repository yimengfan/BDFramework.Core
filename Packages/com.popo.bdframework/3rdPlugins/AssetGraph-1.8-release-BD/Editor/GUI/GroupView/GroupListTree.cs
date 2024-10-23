using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    public class GroupTreeViewItem : TreeViewItem
    {
        private string m_name;
        private List<AssetReference> m_assets;
        private long size;
        public string name {
            get {
                return m_name;
            }
        }
        public List<AssetReference> assets
        {
            get { return m_assets; }
        }
        public GroupTreeViewItem() : base(-1, -1) { }
        public GroupTreeViewItem(string name, List<AssetReference> assets) : base(name.GetHashCode(), 0, name)
        {
            m_name = name;
            m_assets = assets;
            size = -1L;
        }

        public int items {
            get {
                if (m_assets != null) {
                    return m_assets.Count;
                }
                return 0;
            }
        }

        public long fileSize {
            get {
                if (size < 0) {
                    Reload ();
                }
                return size;
            }
        }

        public long runtimeMemorySize {
            get {
                if (size < 0) {
                    Reload ();
                }
                return runtimeMemorySize;
            }
        }

        public void Reload() {
            if (m_assets != null) {
                size = 0;
                foreach (var a in m_assets) {
                    size += a.GetFileSize ();
                }
            }
        }
    }

    /// <summary>
    /// Display list of groups in tree view
    /// </summary>
    internal class GroupListTree : TreeView
    { 
        GroupViewController m_controller;

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
            retVal[0].headerContent = new GUIContent("Group Name", "Group name");
            retVal[0].minWidth = 500;
            retVal[0].width = 500;
            retVal[0].maxWidth = 800;
            retVal[0].headerTextAlignment = TextAlignment.Left;
            retVal[0].canSort = true;
            retVal[0].autoResize = true;

            retVal[1].headerContent = new GUIContent("Assets", "Number of assets in a group");
            retVal[1].minWidth = 50;
            retVal[1].width = 100;
            retVal[1].maxWidth = 300;
            retVal[1].headerTextAlignment = TextAlignment.Left;
            retVal[1].canSort = true;
            retVal[1].autoResize = true;

            retVal[2].headerContent = new GUIContent("Size", "Estimated size of group");
            retVal[2].minWidth = 50;
            retVal[2].width = 100;
            retVal[2].maxWidth = 300;
            retVal[2].headerTextAlignment = TextAlignment.Left;
            retVal[2].canSort = true;
            retVal[2].autoResize = true;

            return retVal;
        }

        public enum SortOption
        {
            GroupName,
            ItemInGroup,
            Size
        }

        SortOption[] m_SortOptions =
        {
            SortOption.GroupName,
            SortOption.ItemInGroup,
            SortOption.Size
        };


        public GroupListTree(GroupViewController parent, TreeViewState state, MultiColumnHeaderState mchs) : base(state, new MultiColumnHeader(mchs))
        {
            m_controller = parent;
            showBorder = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns (); ++i) {
                CellGUI (args.GetCellRect (i), args.item, args.GetColumn (i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            GroupTreeViewItem groupItem = item as GroupTreeViewItem;
            if (groupItem == null) {
                return;
            }

            switch (column)
            {
            case 0:
                DefaultGUI.Label(cellRect, groupItem.name, args.selected, args.focused);
                break;
            case 1:
                DefaultGUI.Label (cellRect, groupItem.items.ToString(), args.selected, args.focused);
                break;
            case 2:
                DefaultGUI.Label (cellRect, EditorUtility.FormatBytes(groupItem.fileSize), args.selected, args.focused);
                break;
            }
			if (Event.current.type == EventType.ContextClick && cellRect.Contains(Event.current.mousePosition))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Copy Group Name"), false, () =>
				{
					EditorGUIUtility.systemCopyBuffer = groupItem.name;
				});
				menu.ShowAsContext();
				Event.current.Use();
			}
        }

        protected override TreeViewItem BuildRoot()
        {
            var result = new GroupTreeViewItem();

            if (m_controller.GroupModel != null) {
                foreach (var groupName in m_controller.GroupModel.Keys) {
                    result.AddChild (new GroupTreeViewItem (groupName, m_controller.GroupModel[groupName]));
                }
            } else {
                result.AddChild (new GroupTreeViewItem("", null));
            }

            return result;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0) {
                m_controller.UnselectGroup ();
            }
            else {
                var groupItem = FindItem(selectedIds[0], rootItem) as GroupTreeViewItem;
                if (groupItem != null) {
                    m_controller.GroupSelectionChanged (groupItem.assets);
                } else {
                    m_controller.UnselectGroup ();
                }
            }
        }

        public void Reselect() {
            SelectionChanged (GetSelection());
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return;

            SortByColumn();

            rows.Clear();
            for (int i = 0; i < root.children.Count; i++) {
                rows.Add (root.children [i]);
            }

            Repaint();
        }

        void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
                return;

            List<GroupTreeViewItem> assetList = new List<GroupTreeViewItem>();
            foreach(var item in rootItem.children)
            {
                assetList.Add(item as GroupTreeViewItem);
            }
            var orderedItems = InitialOrder(assetList, sortedColumns);

            rootItem.children = orderedItems.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<GroupTreeViewItem> InitialOrder(IEnumerable<GroupTreeViewItem> myTypes, int[] columnList)
        {
            SortOption sortOption = m_SortOptions[columnList[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
            case SortOption.GroupName:
                return myTypes.Order(l => l.name, ascending);
            case SortOption.ItemInGroup:
                return myTypes.Order(l => l.items, ascending);
            case SortOption.Size:
            default:
                return myTypes.Order(l => l.fileSize, ascending);
            }

        }
    }
}
