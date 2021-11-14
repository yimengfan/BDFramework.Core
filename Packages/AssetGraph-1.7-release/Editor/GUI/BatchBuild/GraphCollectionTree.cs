using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    internal class GraphCollectionTreeItem : TreeViewItem
    {
        private BatchBuildConfig.GraphCollection m_collection;

        private static int s_id = 0;

        public string Name {
            get {
                if (m_collection != null) {
                    return m_collection.Name;
                } else {
                    return string.Empty;
                }
            }
        }

        public BatchBuildConfig.GraphCollection Collection
        {
            get { return m_collection; }
        }

        public GraphCollectionTreeItem() : base(-1, -1) { }

        public GraphCollectionTreeItem(BatchBuildConfig.GraphCollection c) : base(++s_id, 0, string.Empty)
        {
            m_collection = c;
            if (c != null) {
                displayName = m_collection.Name;
            }
        }

        public bool TryRename(string newname) {
            return m_collection.TryRename (newname);
        }
    }

    internal class GraphCollectionTree : TreeView
    { 
        GraphCollectionManageTab m_controller;
        List<UnityEngine.Object> m_EmptyObjectList = new List<UnityEngine.Object>();
        bool m_ctxMenuClickOnItem;

        public GraphCollectionTree(TreeViewState state, GraphCollectionManageTab ctrl) : base(state)
        {
            m_controller = ctrl;
            showBorder = true;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item.displayName.Length > 0;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
//            var item = (args.item as GraphCollectionTreeItem);
            base.RowGUI(args);
        }

        protected override void RenameEnded(RenameEndedArgs args)
        { 
            base.RenameEnded(args);
            if (args.newName.Length > 0 && args.newName != args.originalName)
            {
                GraphCollectionTreeItem renamedItem = FindItem(args.itemID, rootItem) as GraphCollectionTreeItem;
                args.acceptedRename = renamedItem.TryRename(args.newName);
                ReloadAndSelect ();
            }
            else
            {
                args.acceptedRename = false;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new GraphCollectionTreeItem ();

            var collections = BatchBuildConfig.GetConfig ().GraphCollections;
            foreach (var c in collections) {
                root.AddChild (new GraphCollectionTreeItem (c));
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        public void ReloadAndSelect()  {
            BatchBuildConfig.GraphCollection selectedCollection = m_controller.CurrentCollection;

            Reload();
            if (selectedCollection != null) {
                foreach (var c in rootItem.children) {
                    var item = c as GraphCollectionTreeItem;
                    if (item != null && item.Collection == selectedCollection) {
                        SetSelection (new int[] { item.id });
                        break;
                    }
                }
            } else {
                SetSelection (new List<int>());
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds != null && selectedIds.Count > 0) {
                var item = FindItem (selectedIds [0], rootItem) as GraphCollectionTreeItem;
                if (item != null) {
                    m_controller.UpdateSelectedGraphCollection (item.Collection);
                }
            } else {
                m_controller.UpdateSelectedGraphCollection (null);
            }
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        protected override void ContextClicked()
        {
            if (m_ctxMenuClickOnItem) {
                m_ctxMenuClickOnItem = false;
                return;
            }

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reload Collection"), false, () => { ReloadAndSelect(); });
            menu.ShowAsContext();
        }

        private void MenuAction_RemoveCollection(object context) {
            var item = context as GraphCollectionTreeItem;

            BatchBuildConfig.GetConfig ().GraphCollections.Remove (item.Collection);
            m_controller.UpdateSelectedGraphCollection (null);
            BatchBuildConfig.SetConfigDirty ();
            ReloadAndSelect ();
        }

        protected override void ContextClickedItem(int id)
        {
            m_ctxMenuClickOnItem = true;

            var item = FindItem (id, rootItem) as GraphCollectionTreeItem;

            if (item != null) {
                GenericMenu menu = new GenericMenu();
                menu.AddItem (new GUIContent("Delete"), false, MenuAction_RemoveCollection, item);

                menu.ShowAsContext();
            }
        }

        class DragAndDropData
        {
            public GraphCollectionTreeItem draggedNode;
            public GraphCollectionTreeItem targetNode;
            public DragAndDropArgs args;
            public List<string> graphGuids;
            public List<GraphCollectionDetailTreeItem> detailDraggedNodes;

            public bool CanAcceptDropBetween {
                get {
                    return draggedNode != null && targetNode != null;
                }
            }

            public bool CanAcceptDropUpon {
                get {
                    return targetNode != null && ((graphGuids != null && graphGuids.Count > 0)|| FromDetailTree ) ;
                }
            }

            public bool CanAcceptDropOutsideItems {
                get {
                    return (graphGuids != null && graphGuids.Count > 0);
                }
            }

            public bool FromDetailTree {
                get {
                    return detailDraggedNodes != null && detailDraggedNodes.Count > 0;
                }
            }


            public DragAndDropData(DragAndDropArgs a)
            {
                args = a;
                graphGuids = null;
                draggedNode = DragAndDrop.GetGenericData("GraphCollectionTree.DraggedItem") as GraphCollectionTreeItem;
                targetNode = args.parentItem as GraphCollectionTreeItem;

                detailDraggedNodes = DragAndDrop.GetGenericData("GraphCollectionDetailTree.DraggedItems") as List<GraphCollectionDetailTreeItem>;

                foreach(var path in DragAndDrop.paths) {
                    if(TypeUtility.GetMainAssetTypeAtPath(path) == typeof(Model.ConfigGraph)) {
                        if(graphGuids == null) {
                            graphGuids = new List<string>();
                        }

                        graphGuids.Add( AssetDatabase.AssetPathToGUID(path) );
                    }
                }
            }
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;
            DragAndDropData data = new DragAndDropData(args);
            
            switch (args.dragAndDropPosition) {
            case DragAndDropPosition.UponItem:
                if (data.CanAcceptDropUpon) {
                    visualMode = HandleDragDropUpon (data);
                } else {
                    visualMode = DragAndDropVisualMode.Rejected;
                }
                break;
            case DragAndDropPosition.BetweenItems:
                if (data.CanAcceptDropBetween) {
                    visualMode = HandleDragDropBetween(data);
                } else {
                    visualMode = DragAndDropVisualMode.Rejected;
                }
                break;
            case DragAndDropPosition.OutsideItems:
                if (data.CanAcceptDropOutsideItems) {
                    visualMode = HandleDragDropOutsideItems (data);
                } else {
                    visualMode = DragAndDropVisualMode.Rejected;
                }
                break;
            }

            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropOutsideItems(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;

            if (data.args.performDrop)
            {
                var guid = data.graphGuids [0];
                var graphName = Path.GetFileNameWithoutExtension( AssetDatabase.GUIDToAssetPath (guid) );

                var c = BatchBuildConfig.CreateNewGraphCollection (graphName);

                c.AddGraphRange (data.graphGuids);

                m_controller.UpdateSelectedGraphCollection (c);
                m_controller.Refresh ();

                BatchBuildConfig.SetConfigDirty ();
            }

            return visualMode;
        }


        private DragAndDropVisualMode HandleDragDropUpon(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;

            var parent = (data.args.parentItem as GraphCollectionTreeItem);

            if (parent != null)
            {
                if (data.args.performDrop)
                {
                    if (data.FromDetailTree) {
                        var guids = data.detailDraggedNodes.Select (n => n.GraphGuid).ToList ();
                        parent.Collection.AddGraphRange ( guids );
                    } else {
                        parent.Collection.AddGraphRange ( data.graphGuids );
                    }

                    m_controller.UpdateSelectedGraphCollection (parent.Collection);

                    SetSelection (new int[] {parent.id});
                    BatchBuildConfig.SetConfigDirty ();
                }
            }

            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Move;

            var parent = (data.args.parentItem as GraphCollectionTreeItem);

            if (parent != null)
            {
                if (data.args.performDrop)
                {
                    var collection = BatchBuildConfig.GetConfig ().GraphCollections;

                    collection.Remove (data.draggedNode.Collection);

                    if (data.args.insertAtIndex < collection.Count) {
                        collection.Insert (data.args.insertAtIndex, data.draggedNode.Collection);
                    } else {
                        collection.Add (data.draggedNode.Collection);
                    }

                    BatchBuildConfig.SetConfigDirty ();

                    ReloadAndSelect ();
                }
            }

            return visualMode;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            if (args.draggedItemIDs.Count > 0) {
                var item = FindItem(args.draggedItemIDs[0], rootItem) as GraphCollectionTreeItem;
                DragAndDrop.SetGenericData("GraphCollectionTree.DraggedItem", item);
            }

            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            DragAndDrop.StartDrag("GraphCollectionTree");
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        internal void Refresh()
        {
            var selection = GetSelection();
            Reload();
            SelectionChanged(selection);
        }

        private void ReloadAndSelect(int hashCode, bool rename)
        {
            var selection = new List<int>();
            selection.Add(hashCode);
            ReloadAndSelect(selection);
            if(rename)
            {
                BeginRename(FindItem(hashCode, rootItem), 0.25f);
            }
        }
        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes, TreeViewSelectionOptions.RevealAndFrame);
            SelectionChanged(hashCodes);
        }
    }
}
