using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    internal class GraphCollectionDetailTreeItem : TreeViewItem
    {
        private string m_graphGuid;
        private string m_graphName;
        private Model.ConfigGraph m_graph;

        private static int s_id = 0;

        public string GraphGuid {
            get {
                return m_graphGuid;
            }
            set {
                m_graphGuid = value;
            }
        }

        public string GraphName {
            get {
                return m_graphName;
            }
        }

        public Model.ConfigGraph Graph {
            get {
                return m_graph;
            }
        }

        public GraphCollectionDetailTreeItem() : base(-1, -1) { }
        public GraphCollectionDetailTreeItem(string graphGuid) : base(++s_id, 0, string.Empty)
        {
            m_graphGuid = graphGuid;
            if (graphGuid != null) {
                var path = AssetDatabase.GUIDToAssetPath (m_graphGuid);
                m_graphName = Path.GetFileNameWithoutExtension (path);
                m_graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph> (path);

                this.displayName = GraphName;
            }
        }
    }

    internal class GraphCollectionDetailTree : TreeView
    {
        private BatchBuildConfig.GraphCollection m_graphCollection;
        private GraphCollectionManageTab m_controller;
        private List<UnityEngine.Object> m_EmptyObjectList = new List<UnityEngine.Object>();
        private int m_itemCount;
        private bool m_ctxMenuClickOnItem;

        public GraphCollectionDetailTree(TreeViewState state, GraphCollectionManageTab ctrl ) : base(state)
        {
            m_controller = ctrl;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new GraphCollectionDetailTreeItem ();

            if (m_graphCollection != null) {
                foreach (var guid in m_graphCollection.GraphGUIDs) {
                    root.AddChild ( new GraphCollectionDetailTreeItem(guid) );
                }
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as GraphCollectionDetailTreeItem;
            if (item != null)
            {
                EditorGUIUtility.PingObject(item.Graph);
                Selection.activeObject = item.Graph;
            }
        }

        protected override void ContextClicked()
        {
            if (m_ctxMenuClickOnItem) {
                m_ctxMenuClickOnItem = false;
                return;
            }

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reload Collection"), false, () => { Reload(); });
            menu.ShowAsContext();
        }

        private void MenuAction_RemoveCollection(object context) {

            var item = context as GraphCollectionDetailTreeItem;

            m_controller.CurrentCollection.RemoveGraph (item.GraphGuid);

            BatchBuildConfig.SetConfigDirty ();
            Reload ();
        }

        protected override void ContextClickedItem(int id)
        {
            m_ctxMenuClickOnItem = true;

            var item = FindItem (id, rootItem) as GraphCollectionDetailTreeItem;

            if (item != null) {
                GenericMenu menu = new GenericMenu();
                menu.AddItem (new GUIContent("Delete"), false, MenuAction_RemoveCollection, item);

                menu.ShowAsContext();
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        class DragAndDropData
        {
            public List<GraphCollectionDetailTreeItem>  draggedNodes;
            public List<string>                         graphGuids;
            public DragAndDropArgs args;

            public bool DraggedNode {
                get {
                    return draggedNodes != null && draggedNodes.Count > 0;
                }
            }

            public bool DraggedGraphs {
                get {
                    return graphGuids != null && graphGuids.Count > 0;
                }
            }

            public DragAndDropData(DragAndDropArgs a)
            {
                args = a;
                draggedNodes = DragAndDrop.GetGenericData("GraphCollectionDetailTree.DraggedItems") as List<GraphCollectionDetailTreeItem>;

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
                visualMode = DragAndDropVisualMode.Rejected;
                break;
            case DragAndDropPosition.BetweenItems:
                visualMode = HandleDragDropBetween(data);
                break;
            case DragAndDropPosition.OutsideItems:
                visualMode = HandleDragDropOutsideItems(data);
                break;
            }

            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropOutsideItems(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Rejected;

            if (data.DraggedGraphs && m_controller.CurrentCollection != null)
            {
                visualMode = DragAndDropVisualMode.Copy;

                if (data.args.performDrop)
                {
                    var collection = m_controller.CurrentCollection;

                    collection.AddGraphRange (data.graphGuids);

                    BatchBuildConfig.SetConfigDirty ();

                    Reload ();
                }
            }

            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Rejected;

            var parent = (data.args.parentItem as GraphCollectionDetailTreeItem);

            if (parent != null && ( data.DraggedNode || data.DraggedGraphs ))
            {
                visualMode = DragAndDropVisualMode.Move;
                    
                if (data.args.performDrop)
                {
                    var collection = m_controller.CurrentCollection;
                    List<string> guids = null;

                    if (data.DraggedNode) {
                        guids = data.draggedNodes.Select (n => n.GraphGuid).ToList();
                    } else {
                        guids = data.graphGuids;
                    }

                    collection.RemoveGraphRange (guids);

                    if (data.args.insertAtIndex < collection.GraphGUIDs.Count) {
                        collection.InsertGraphRange (data.args.insertAtIndex, guids);
                    } else {
                        collection.AddGraphRange (guids);
                    }

                    BatchBuildConfig.SetConfigDirty ();

                    Reload ();
                }
            }

            return visualMode;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            if (args.draggedItemIDs.Count > 0) {
                var nodes = new List<GraphCollectionDetailTreeItem>();

                foreach (var id in args.draggedItemIDs) {
                    var item = FindItem(id, rootItem) as GraphCollectionDetailTreeItem;
                    if (item != null) {
                        nodes.Add (item);
                    }
                }

                DragAndDrop.SetGenericData("GraphCollectionDetailTree.DraggedItems", nodes);
            }

            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            DragAndDrop.StartDrag("GraphCollectionTree");
        }

        public void SetCurrentGraphCollection(BatchBuildConfig.GraphCollection c) {
            m_graphCollection = c;
            Reload ();
        }
    }
}
