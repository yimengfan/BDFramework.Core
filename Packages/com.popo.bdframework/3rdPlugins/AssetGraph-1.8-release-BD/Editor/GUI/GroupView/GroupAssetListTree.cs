using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    public class AssetReferenceTreeItem : TreeViewItem
    {
        private AssetReference m_asset;
        public AssetReference asset
        {
            get { return m_asset; }
        }
        public AssetReferenceTreeItem() : base(-1, -1) { }
        public AssetReferenceTreeItem(AssetReference a) : base(a.path.GetHashCode(), 0, a.fileName)
        {
            m_asset = a;
        }

        public string fileSize {
            get {
                return EditorUtility.FormatBytes(m_asset.GetFileSize());
            }
        }
    }

    /// <summary>
    /// Display list of assets in selected group in treeview
    /// </summary>
    internal class GroupAssetListTree : TreeView
    {
        private GroupViewController m_controller;
        private List<AssetReference> m_assets = new List<AssetReference>();

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
            retVal[0].headerContent = new GUIContent("Asset", "Short name of asset. For full name select asset and see message below");
            retVal[0].minWidth = 50;
            retVal[0].width = 200;
            retVal[0].maxWidth = 300;
            retVal[0].headerTextAlignment = TextAlignment.Left;
            retVal[0].canSort = true;
            retVal[0].autoResize = true;

            retVal[1].headerContent = new GUIContent("Variant", "Variant name");
            retVal[1].minWidth = 50;
            retVal[1].width = 100;
            retVal[1].maxWidth = 300;
            retVal[1].headerTextAlignment = TextAlignment.Left;
            retVal[1].canSort = true;
            retVal[1].autoResize = true;

            retVal[2].headerContent = new GUIContent("Size", "Size on disk");
            retVal[2].minWidth = 30;
            retVal[2].width = 75;
            retVal[2].maxWidth = 100;
            retVal[2].headerTextAlignment = TextAlignment.Left;
            retVal[2].canSort = true;
            retVal[2].autoResize = true;

            return retVal;
        }
        public enum SortOption
        {
            Asset,
            Variant,
            Size
        }
        SortOption[] m_SortOptions =
        {
            SortOption.Asset,
            SortOption.Variant,
            SortOption.Size
        };

        public GroupAssetListTree(GroupViewController parent, TreeViewState state, MultiColumnHeaderState mchs ) : base(state, new MultiColumnHeader(mchs))
        {
            m_controller = parent;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            //DefaultStyles.label.richText = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }

        public void SetAssetList(List<AssetReference> assets) {
            if (assets == null) {
                m_assets = new List<AssetReference>();
            } else {
                m_assets = new List<AssetReference>(assets);
            }
            Reload ();
        }
            
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new AssetReferenceTreeItem ();

            foreach (var a in m_assets) {
                root.AddChild (new AssetReferenceTreeItem (a));
            }

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns (); ++i) {
                CellGUI (args.GetCellRect (i), args.item as AssetReferenceTreeItem, args.GetColumn (i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, AssetReferenceTreeItem item, int column, ref RowGUIArgs args)
        {
            Color oldColor = GUI.color;
            CenterRectUsingSingleLineHeight(ref cellRect);
            GUI.color = Color.white;

            switch (column)
            {
            case 0:
                {
                    var icon = item.asset.isProjectAsset ? AssetDatabase.GetCachedIcon (item.asset.importFrom) : null;
                    if (icon == null) {
                        icon = AssetPreview.GetMiniTypeThumbnail (item.asset.assetType);
                    } else {
                        this.Repaint ();
                    }
                    
                    var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                    if (icon != null) {
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }
                    DefaultGUI.Label(
                        new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width, cellRect.height), 
                        item.displayName, 
                        args.selected, 
                        args.focused);
                }
                break;
            case 1:
                DefaultGUI.Label(cellRect, item.asset.variantName, args.selected, args.focused);
                break;
            case 2:
                DefaultGUI.Label(cellRect, item.fileSize, args.selected, args.focused);
                break;
            }
            GUI.color = oldColor;
			if (Event.current.type == EventType.ContextClick && cellRect.Contains(Event.current.mousePosition))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Copy Asset Path"), false, () =>
				{
					EditorGUIUtility.systemCopyBuffer = item.asset.path;
				});
                if (File.Exists (item.asset.importFrom)) {
                    menu.AddItem (new GUIContent ("Open"), false, () => {
                        AssetDatabase.OpenAsset (item.asset.allData);
                    });
                }
				menu.ShowAsContext();
				Event.current.Use();
			}
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetItem = FindItem(id, rootItem) as AssetReferenceTreeItem;
            if (assetItem != null && assetItem.asset.isProjectAsset)
            {
                var o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetItem.asset.importFrom);
                EditorGUIUtility.PingObject(o);
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0) {
                m_controller.AssetSelectionChanged (null);
            }
            else {
                var item = FindItem(selectedIds[0], rootItem) as AssetReferenceTreeItem;

                m_controller.AssetSelectionChanged (item.asset);
            }
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
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

            List<AssetReferenceTreeItem> assetList = new List<AssetReferenceTreeItem>();
            foreach(var item in rootItem.children)
            {
                assetList.Add(item as AssetReferenceTreeItem);
            }
            var orderedItems = InitialOrder(assetList, sortedColumns);

            rootItem.children = orderedItems.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<AssetReferenceTreeItem> InitialOrder(IEnumerable<AssetReferenceTreeItem> myTypes, int[] columnList)
        {
            SortOption sortOption = m_SortOptions[columnList[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
            case SortOption.Asset:
                return myTypes.Order(l => l.displayName, ascending);
            case SortOption.Size:
                return myTypes.Order(l => l.fileSize, ascending);
            case SortOption.Variant:
            default:
                return myTypes.Order(l => l.asset.variantName, ascending);
            }

        }
    }

    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
