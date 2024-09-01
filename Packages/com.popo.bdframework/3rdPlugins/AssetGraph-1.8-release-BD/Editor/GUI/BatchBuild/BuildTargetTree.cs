using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    internal class BuildTargetTreeItem : TreeViewItem
    {
        private BuildTargetGroup    m_group;
        private BuildTarget         m_target;

        public BuildTargetGroup Group {
            get {
                return m_group;
            }
            set {
                m_group = value;
            }
        }

        public BuildTarget Target {
            get {
                return m_target;
            }
            set {
                m_target = value;
            }
        }

        public BuildTargetTreeItem() : base(-1, -1) { }
        public BuildTargetTreeItem(BuildTarget t) : base((int)t, 0, string.Empty)
        {
            m_group = BuildTargetUtility.TargetToGroup(t);
            m_target = t;
            displayName = BuildTargetUtility.TargetToHumaneString (m_target);
        }
    }

    internal class BuildTargetTree : TreeView
    { 
        private bool m_ctxMenuClickOnItem;
        List<UnityEngine.Object> m_EmptyObjectList = new List<UnityEngine.Object>();

        public BuildTargetTree(TreeViewState state) : base(state)
        {
            showBorder = true;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new BuildTargetTreeItem ();

            foreach (var t in BatchBuildConfig.GetConfig().BuildTargets) {
                root.AddChild (new BuildTargetTreeItem (t));
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
        }

        internal void Refresh()
        {
            var selection = GetSelection();
            Reload();
            SelectionChanged(selection);
        }

        protected override void ContextClicked()
        {
            if (m_ctxMenuClickOnItem) {
                m_ctxMenuClickOnItem = false;
                return;
            }

            GenericMenu menu = new GenericMenu();

            var currentTargets = BatchBuildConfig.GetConfig ().BuildTargets;

            foreach(var t in NodeGUIUtility.SupportedBuildTargets) {
                if (!currentTargets.Contains (t)) {
                    menu.AddItem(new GUIContent($"Add {BuildTargetUtility.TargetToHumaneString(t)}"), false, () => { MenuAction_AddTarget(t); });
                }
            }

            menu.ShowAsContext();
        }

        private void MenuAction_AddTarget(BuildTarget t) {

            var targets = BatchBuildConfig.GetConfig ().BuildTargets;
            if (!targets.Contains (t)) {
                targets.Add (t);
            }

            BatchBuildConfig.SetConfigDirty ();
            Reload ();
        }


        private void MenuAction_RemoveCollection(object context) {

            var item = context as BuildTargetTreeItem;

            BatchBuildConfig.GetConfig ().BuildTargets.Remove (item.Target);

            BatchBuildConfig.SetConfigDirty ();
            Reload ();
        }

        protected override void ContextClickedItem(int id)
        {
            m_ctxMenuClickOnItem = true;

            var item = FindItem (id, rootItem) as BuildTargetTreeItem;

            if (item != null) {
                GenericMenu menu = new GenericMenu();
                menu.AddItem (new GUIContent("Remove Target"), false, MenuAction_RemoveCollection, item);

                menu.ShowAsContext();
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        class DragAndDropData
        {
            public List<BuildTargetTreeItem>  draggedNodes;
            public DragAndDropArgs args;

            public bool DraggedNode {
                get {
                    return draggedNodes != null && draggedNodes.Count > 0;
                }
            }

            public DragAndDropData(DragAndDropArgs a)
            {
                args = a;
                draggedNodes = DragAndDrop.GetGenericData("BuildTargetTreeItem.DraggedItems") as List<BuildTargetTreeItem>;
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
                visualMode = DragAndDropVisualMode.Rejected;
                break;
            }

            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Rejected;

            var parent = (data.args.parentItem as BuildTargetTreeItem);

            if (parent != null && data.DraggedNode)
            {
                visualMode = DragAndDropVisualMode.Move;

                if (data.args.performDrop)
                {
                    var targets = BatchBuildConfig.GetConfig().BuildTargets;

                    var movingTargets = data.draggedNodes.Select (n => n.Target).ToList();

                    foreach (var t in movingTargets) {
                        targets.Remove (t);
                    }

                    if (data.args.insertAtIndex < targets.Count) {
                        targets.InsertRange (data.args.insertAtIndex, movingTargets);
                    } else {
                        targets.AddRange (movingTargets);
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
                var nodes = new List<BuildTargetTreeItem>();

                foreach (var id in args.draggedItemIDs) {
                    var item = FindItem(id, rootItem) as BuildTargetTreeItem;
                    if (item != null) {
                        nodes.Add (item);
                    }
                }

                DragAndDrop.SetGenericData("BuildTargetTreeItem.DraggedItems", nodes);
            }

            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            DragAndDrop.StartDrag("BuildTargetTreeItem");
        }
    }
}
