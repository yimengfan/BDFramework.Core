using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    internal class ExecuteResultTreeItem : TreeViewItem
    {
        private ExecuteGraphResult m_result;

        private static int s_id = 0;

        public ExecuteGraphResult Result {
            get {
                return m_result;
            }
        }

        public ExecuteResultTreeItem() : base(-1, -1) { }
        public ExecuteResultTreeItem(ExecuteGraphResult r) : base(s_id++, 0, string.Empty)
        {
            m_result = r;
            displayName =
                $"{Path.GetFileNameWithoutExtension(r.GraphAssetPath)}({BuildTargetUtility.TargetToHumaneString(r.Target)}):{((r.IsAnyIssueFound) ? "Failed" : "Good")}";
        }
    }

    internal class NodeExceptionItem : TreeViewItem
    {
        private ExecuteGraphResult m_result;
        private NodeException m_exception;

        private static int s_id = 100000;

        public ExecuteGraphResult Result {
            get {
                return m_result;
            }
        }

        public NodeException Exception {
            get {
                return m_exception;
            }
        }

        public NodeExceptionItem(ExecuteGraphResult r, NodeException e) : base(s_id++, 1, string.Empty)
        {
            m_result = r;
            m_exception = e;
            displayName = $"{m_exception.Node.Name}:{m_exception.Reason}";
        }
    }

    internal class ExecuteResultTree : TreeView
    { 
        private GraphCollectionExecuteTab m_controller;
        private long m_lastTimestamp;

        private static MultiColumnHeaderState CreateMCHeader()
        {
            return new MultiColumnHeaderState(GetColumns());
        }
        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new MultiColumnHeaderState.Column[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };
            int i = 0;

            retVal[i].headerContent = new GUIContent("", "");
            retVal[i].minWidth = 16;
            retVal[i].width = 16;
            retVal[i].maxWidth = 16;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            retVal[i].headerContent = new GUIContent("Status", "Is build successful?");
            retVal[i].minWidth = 32;
            retVal[i].width = 60;
            retVal[i].maxWidth = 150;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            retVal[i].headerContent = new GUIContent("Graph", "Graph Name");
            retVal[i].minWidth = 50;
            retVal[i].width = 100;
            retVal[i].maxWidth = 500;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            retVal[i].headerContent = new GUIContent("Platform", "Platform Name");
            retVal[i].minWidth = 30;
            retVal[i].width = 120;
            retVal[i].maxWidth = 500;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            retVal[i].headerContent = new GUIContent("Description", "Additonal Info");
            retVal[i].minWidth = 30;
            retVal[i].width = 250;
            retVal[i].maxWidth = 1000;
            retVal[i].headerTextAlignment = TextAlignment.Left;
            retVal[i].canSort = false;
            retVal[i].autoResize = true;
            ++i;

            return retVal;
        }

        public ExecuteResultTree(TreeViewState state, GraphCollectionExecuteTab ctrl) : base(state, new MultiColumnHeader(CreateMCHeader()))
        {
            m_controller = ctrl;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item) {
            return 32f;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns (); ++i) {
                CellGUI (args.GetCellRect (i), args.item, args.GetColumn (i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            ExecuteResultTreeItem resultItem    = item as ExecuteResultTreeItem;
            NodeExceptionItem exceptionItem     = item as NodeExceptionItem;

            ExecuteGraphResult r = null;
            NodeException e = null;
            if (resultItem != null) {
                r = resultItem.Result;
            } else {
                r = exceptionItem.Result;
                e = exceptionItem.Exception;
            }

            switch (column)
            {
            case 0://Collapse/Expand
                {
                    DefaultGUI.Label (cellRect, string.Empty, args.selected, args.focused);
                }
                break;
            case 1://Status
                {
                    var rect = cellRect;
                    if(e != null) {
                        rect.x += 8f;
                    }
                    DefaultGUI.Label (rect, (r.IsAnyIssueFound) ? "Fail" : "Success", args.selected, args.focused);
                }
                break;
            case 2://Graph
                {
                    var graphName = Path.GetFileNameWithoutExtension (r.GraphAssetPath);
                    DefaultGUI.Label (cellRect, graphName, args.selected, args.focused);
                }
                break;
            case 3://Platform
                DefaultGUI.Label (cellRect, BuildTargetUtility.TargetToHumaneString(r.Target), args.selected, args.focused);
                break;
            case 4://Description
                if (e != null) {
                    DefaultGUI.Label (cellRect, e.Reason, args.selected, args.focused);
                } else {
                    DefaultGUI.Label (cellRect, string.Empty, args.selected, args.focused);
                }
                break;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            m_lastTimestamp = m_controller.LastBuildTimestamp;

            var root = new ExecuteResultTreeItem ();

            foreach (var r in m_controller.CurrentResult) {
                var resultItem = new ExecuteResultTreeItem (r);
                root.AddChild (resultItem);

                if (r.IsAnyIssueFound) {
                    foreach (var e in r.Issues) {
                        resultItem.AddChild (new NodeExceptionItem(r, e));
                    }
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
            var resultItem = FindItem(id, rootItem) as ExecuteResultTreeItem;
            if (resultItem != null) {
                EditorGUIUtility.PingObject (resultItem.Result.Graph);
                Selection.activeObject = resultItem.Result.Graph;
            } else {
                var exeptionItem = FindItem(id, rootItem) as NodeExceptionItem;
                if (exeptionItem != null) {
                    var window = EditorWindow.GetWindow<AssetGraphEditorWindow>();
                    window.OpenGraph(exeptionItem.Result.GraphAssetPath);
                    window.SelectNode (exeptionItem.Exception.NodeId);
                }
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds != null && selectedIds.Count > 0)
            {
                var item = FindItem(selectedIds[0], rootItem);
                var resultItem = item as ExecuteResultTreeItem;
                if (resultItem != null) {
                    m_controller.SetSelectedExecuteResult (resultItem.Result, null);
                } else {
                    var exceptionItem = item as NodeExceptionItem;
                    if (exceptionItem != null) {
                        m_controller.SetSelectedExecuteResult (exceptionItem.Result, exceptionItem.Exception);
                    }
                }
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

        public void ReloadIfNeeded() {
            if (m_lastTimestamp != m_controller.LastBuildTimestamp) {
                Reload ();
            }
        }

        public void ReloadAndSelectLast()
        {
            ReloadIfNeeded ();
            if (rootItem.children != null && rootItem.children.Count > 0) {
                SetSelection (new int[] { rootItem.children.Last().id });
            }
        }
    }
}
