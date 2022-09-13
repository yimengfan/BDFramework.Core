using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    public class AssetGraphEditorWindow : EditorWindow
    {
        [Serializable]
        public class SavedSelection
        {
            [SerializeField] public List<NodeGUI> nodes;
            [SerializeField] public List<ConnectionGUI> connections;
            [SerializeField] private float m_pasteOffset = kPasteOffset;

            static readonly float kPasteOffset = 20.0f;

            public SavedSelection(SavedSelection s)
            {
                nodes = new List<NodeGUI>(s.nodes);
                connections = new List<ConnectionGUI>(s.connections);
            }

            public SavedSelection()
            {
                nodes = new List<NodeGUI>();
                connections = new List<ConnectionGUI>();
            }

            public SavedSelection(IEnumerable<NodeGUI> n, IEnumerable<ConnectionGUI> c)
            {
                nodes = new List<NodeGUI>(n);
                connections = new List<ConnectionGUI>(c);
            }

            public bool IsSelected
            {
                get { return (nodes.Count + connections.Count) > 0; }
            }

            public float PasteOffset
            {
                get { return m_pasteOffset; }
            }

            public void IncrementPasteOffset()
            {
                m_pasteOffset += kPasteOffset;
            }

            public void Add(NodeGUI n)
            {
                nodes.Add(n);
            }

            public void Add(ConnectionGUI c)
            {
                connections.Add(c);
            }

            public void AddRange(IEnumerable<NodeGUI> n)
            {
                nodes.AddRange(n);
            }

            public void AddRange(IEnumerable<ConnectionGUI> c)
            {
                connections.AddRange(c);
            }


            public void Remove(NodeGUI n)
            {
                nodes.Remove(n);
            }

            public void Remove(ConnectionGUI c)
            {
                connections.Remove(c);
            }

            public void Toggle(NodeGUI n)
            {
                if (nodes.Contains(n))
                {
                    nodes.Remove(n);
                }
                else
                {
                    nodes.Add(n);
                }
            }

            public void Toggle(ConnectionGUI c)
            {
                if (connections.Contains(c))
                {
                    connections.Remove(c);
                }
                else
                {
                    connections.Add(c);
                }
            }

            public void Clear(bool deactivate = false)
            {
                if (deactivate)
                {
                    foreach (var n in nodes)
                    {
                        n.SetActive(false);
                    }

                    foreach (var c in connections)
                    {
                        c.SetActive(false);
                    }
                }

                nodes.Clear();
                connections.Clear();
            }
        }

        // hold selection start data.
        public class SelectPoint
        {
            public readonly float x;
            public readonly float y;

            public SelectPoint(Vector2 position)
            {
                this.x = position.x;
                this.y = position.y;
            }
        }

        private class UndoUtility
        {
            private UnityEngine.Object[] m_cachedUndoObjects;
            private List<UnityEngine.Object> m_objects;
            private int m_nNodes;
            private int m_nConnections;

            public UndoUtility()
            {
                m_objects = new List<UnityEngine.Object>();
                m_nNodes = 0;
                m_nConnections = 0;
            }

            public void Clear()
            {
                m_objects.Clear();
                m_nNodes = 0;
                m_nConnections = 0;
                m_cachedUndoObjects = null;
            }

            public void RecordUndo(AssetGraphEditorWindow w, List<NodeGUI> n, List<ConnectionGUI> c, string msg)
            {
                if (m_cachedUndoObjects == null ||
                    m_nNodes != (n == null ? 0 : n.Count) ||
                    m_nConnections != (c == null ? 0 : c.Count) ||
                    ArrayUtility.Contains(m_cachedUndoObjects, null))
                {
                    UpdateUndoCacheObject(w, n, c);
                }

                if (m_cachedUndoObjects != null)
                {
                    Undo.RecordObjects(m_cachedUndoObjects, msg);
                }
            }

            private void UpdateUndoCacheObject(AssetGraphEditorWindow w, List<NodeGUI> nodes, List<ConnectionGUI> conns)
            {
                m_objects.Clear();
                if (w != null)
                {
                    m_objects.Add(w);
                }

                if (nodes != null)
                {
                    foreach (var v in nodes)
                    {
                        if (v != null)
                        {
                            m_objects.Add(v);
                        }
                    }
                }

                if (conns != null)
                {
                    foreach (var v in conns)
                    {
                        if (v != null)
                        {
                            m_objects.Add(v);
                        }
                    }
                }

                m_cachedUndoObjects = m_objects.ToArray();
                m_nNodes = (nodes == null ? 0 : nodes.Count);
                m_nConnections = (conns == null ? 0 : conns.Count);
            }
        }


        public enum ModifyMode : int
        {
            NONE,
            CONNECTING,
            SELECTING,
            DRAGGING
        }

        public enum ScriptType : int
        {
            SCRIPT_MODIFIER,
            SCRIPT_PREFABBUILDER,
            SCRIPT_POSTPROCESS,
            SCRIPT_NODE,
            SCRIPT_FILTER,
            SCRIPT_ASSETGENERATOR,
            SCRIPT_IMPORTSETTINGSCONFIGURATOR
        }

        [SerializeField] private List<NodeGUI> m_nodes = new List<NodeGUI>();
        [SerializeField] private List<ConnectionGUI> m_connections = new List<ConnectionGUI>();
        [SerializeField] private string m_graphAssetPath;
        [SerializeField] private string m_graphAssetName;

        [SerializeField] private SavedSelection m_activeSelection = null;
        [SerializeField] private SavedSelection m_copiedSelection = null;

        private bool m_showErrors;
        private bool m_showVerboseLog;
        private bool m_showDescription;

        private NodeEvent m_currentEventSource;
        private ModifyMode m_modifyMode;
        private Vector2 m_spacerRectRightBottom;
        private Vector2 m_scrollPos = new Vector2(1500, 0);
        private Vector2 m_errorScrollPos = new Vector2(0, 0);
        private Rect m_graphRegion = new Rect();
        private SelectPoint m_selectStartMousePosition;
        private Texture2D m_selectionTex;

        private GraphBackground m_background = new GraphBackground();
        private GUIStyle m_descriptionStyle;
        private Texture2D m_miniInfoIcon;
        private Texture2D m_miniErrorIcon;
        private Texture2D m_refreshIcon;

        private AssetGraphController m_controller;
        private BuildTarget m_target;

        private Vector2 m_LastMousePosition;
        private Vector2 m_DragNodeDistance;
        private Dictionary<NodeGUI, Vector2> m_initialDragNodePositions = new Dictionary<NodeGUI, Vector2>();

        private UndoUtility m_undo;

        private static readonly string kPREFKEY_LASTEDITEDGRAPH = "AssetGraph.LastEditedGraph";
        static readonly int kDragNodesControlID = "AssetGraph.HandleDragNodes".GetHashCode();

        private bool IsAnyIssueFound
        {
            get
            {
                if (m_controller == null)
                {
                    return true;
                }

                return m_controller.IsAnyIssueFound;
            }
        }

        /*
		 * An alternative way to get Window, becuase
		 * GetWindow<AssetGraphEditorWindow>() forces window to be active and present
		 */
        private static AssetGraphEditorWindow Window
        {
            get
            {
                AssetGraphEditorWindow[] windows = Resources.FindObjectsOfTypeAll<AssetGraphEditorWindow>();
                if (windows.Length > 0)
                {
                    return windows[0];
                }

                return null;
            }
        }

        public static void GenerateScript(ScriptType scriptType)
        {
            var destinationBasePath = Model.Settings.Path.UserSpacePath;

            var sourceFileName = string.Empty;
            var destinationFileName = string.Empty;

            switch (scriptType)
            {
                case ScriptType.SCRIPT_MODIFIER:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyModifier.cs.template");
                    destinationFileName = "MyModifier{0}{1}";
                    break;
                }
                case ScriptType.SCRIPT_PREFABBUILDER:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyPrefabBuilder.cs.template");
                    destinationFileName = "MyPrefabBuilder{0}{1}";
                    break;
                }
                case ScriptType.SCRIPT_POSTPROCESS:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyPostprocess.cs.template");
                    destinationFileName = "MyPostprocess{0}{1}";
                    break;
                }
                case ScriptType.SCRIPT_FILTER:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyFilter.cs.template");
                    destinationFileName = "MyFilter{0}{1}";
                    break;
                }
                case ScriptType.SCRIPT_NODE:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyNode.cs.template");
                    destinationFileName = "MyNode{0}{1}";
                    break;
                }
                case ScriptType.SCRIPT_ASSETGENERATOR:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyGenerator.cs.template");
                    destinationFileName = "MyGenerator{0}{1}";
                    break;
                }
                case ScriptType.SCRIPT_IMPORTSETTINGSCONFIGURATOR:
                {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyImportSettingsConfigurator.cs.template");
                    destinationFileName = "MyImportSettingsConfigurator{0}{1}";
                    break;
                }
                default:
                {
                    LogUtility.Logger.LogError(LogUtility.kTag, "Unknown script type found:" + scriptType);
                    break;
                }
            }

            if (string.IsNullOrEmpty(sourceFileName) || string.IsNullOrEmpty(destinationFileName))
            {
                return;
            }

            var destinationPath = FileUtility.PathCombine(destinationBasePath, string.Format(destinationFileName, "", ".cs"));
            int count = 0;
            while (File.Exists(destinationPath))
            {
                destinationPath = FileUtility.PathCombine(destinationBasePath, string.Format(destinationFileName, ++count, ".cs"));
            }

            FileUtility.CopyTemplateFile(sourceFileName, destinationPath, string.Format(destinationFileName, "", ""), string.Format(destinationFileName, count == 0 ? "" : count.ToString(), ""));

            AssetDatabase.Refresh();

            //Highlight in ProjectView
            MonoScript s = AssetDatabase.LoadAssetAtPath<MonoScript>(destinationPath);
            UnityEngine.Assertions.Assert.IsNotNull(s);
            EditorGUIUtility.PingObject(s);
        }

        /*
			menu items
		*/
        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_FILTER, priority = 14500)]
        public static void GenerateCustomFilter()
        {
            GenerateScript(ScriptType.SCRIPT_FILTER);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_MODIFIER, priority = 14500)]
        public static void GenerateModifier()
        {
            GenerateScript(ScriptType.SCRIPT_MODIFIER);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_PREFABBUILDER, priority = 14500)]
        public static void GeneratePrefabBuilder()
        {
            GenerateScript(ScriptType.SCRIPT_PREFABBUILDER);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_POSTPROCESS, priority = 14500)]
        public static void GeneratePostprocess()
        {
            GenerateScript(ScriptType.SCRIPT_POSTPROCESS);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_NODE, priority = 14500)]
        public static void GenerateCustomNode()
        {
            GenerateScript(ScriptType.SCRIPT_NODE);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_ASSETGENERATOR, priority = 14500)]
        public static void GenerateAssetGenerator()
        {
            GenerateScript(ScriptType.SCRIPT_ASSETGENERATOR);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_IMPORTSETTINGSCONFIGURATOR, priority = 14500)]
        public static void GenerateImportSettingsConfigurator()
        {
            GenerateScript(ScriptType.SCRIPT_IMPORTSETTINGSCONFIGURATOR);
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_OPEN, priority = 14000)]
        public static void Open()
        {
            GetWindow<AssetGraphEditorWindow>();
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_DELETE_CACHE, priority = 14500)]
        public static void DeleteCache()
        {
            FileUtility.RemakeDirectory(Model.Settings.Path.CachePath);

            AssetDatabase.Refresh();
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_CLEANUP_SAVEDSETTINGS, priority = 14500)]
        public static void CleanupSavedSettings()
        {
            if (!Directory.Exists(Model.Settings.Path.SavedSettingsPath))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            var validNodeIds = new List<string>();

            foreach (var guid in guids)
            {
                var graphPath = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphPath);
                validNodeIds.AddRange(graph.Nodes.Select(n => n.Id));
            }

            var saveSettingRoots = Directory.GetDirectories(Model.Settings.Path.SavedSettingsPath);
            foreach (var dir in saveSettingRoots)
            {
                var nodeSettings = Directory.GetDirectories(dir);

                foreach (var nodeSettingPath in nodeSettings)
                {
                    var dirName = Path.GetFileName(nodeSettingPath);

                    if (!validNodeIds.Contains(dirName))
                    {
                        FileUtility.DeleteDirectory(nodeSettingPath, true);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_BUILD, true, priority = 14000 + 101)]
        public static bool BuildFromMenuValidator()
        {
            // Calling GetWindow<>() will force open window
            // That's not what we want to do in validator function,
            // so just reference s_currentController directly
            var w = Window;
            if (w == null)
            {
                return false;
            }

            return !w.IsAnyIssueFound;
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_BUILD, priority = 14000 + 101)]
        public static void BuildFromMenu()
        {
            var window = GetWindow<AssetGraphEditorWindow>();
            window.SaveGraph();
            window.Run();
        }


        public void OnFocus()
        {
            // update handlers. these static handlers are erase when window is full-screened and badk to normal window.
            m_modifyMode = ModifyMode.NONE;
            NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
            ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;

            HandleSelectionChange();
        }

        public void OnLostFocus()
        {
            m_modifyMode = ModifyMode.NONE;
        }

        public void OnProjectChange()
        {
            HandleSelectionChange();
            Repaint();
        }

        public void OnSelectionChange()
        {
            HandleSelectionChange();
            Repaint();
        }

        public void HandleSelectionChange()
        {
            Model.ConfigGraph selectedGraph = null;

            //			if (Selection.activeObject == null)
            //			{
            //				controller = null;
            //			}

            if (Selection.activeObject is Model.ConfigGraph && EditorUtility.IsPersistent(Selection.activeObject))
            {
                selectedGraph = Selection.activeObject as Model.ConfigGraph;
            }

            if (selectedGraph != null && (m_controller == null || selectedGraph != m_controller.TargetGraph))
            {
                OpenGraph(selectedGraph);
            }
        }

        public void SelectNode(string nodeId)
        {
            RecordUndo("Select Node");

            if (m_activeSelection == null)
            {
                m_activeSelection = new SavedSelection();
            }

            m_activeSelection.Clear();

            var selectObject = m_nodes.Find(node => node.Id == nodeId);

            foreach (var node in m_nodes)
            {
                bool isActive = node == selectObject;
                node.SetActive(isActive);
                if (isActive)
                {
                    m_activeSelection.Add(node);
                }
            }
        }

        private void Init()
        {
            var windowIcon = (EditorGUIUtility.isProSkin) ? NodeGUIUtility.windowIconPro : NodeGUIUtility.windowIcon;

            this.titleContent = new GUIContent("AssetGraph", windowIcon);
            this.minSize = new Vector2(600f, 300f);
            this.wantsMouseMove = true;

            m_undo = new UndoUtility();

            m_showDescription = true;
            m_miniInfoIcon = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
            m_miniErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            m_refreshIcon = EditorGUIUtility.Load((EditorGUIUtility.isProSkin) ? "icons/d_Refresh.png" : "icons/Refresh.png") as Texture2D;

            m_target = EditorUserBuildSettings.activeBuildTarget;

            this.m_showVerboseLog = UserPreference.DefaultVerboseLog;
            LogUtility.ShowVerboseLog(m_showVerboseLog);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged += (PlayModeStateChange s) => { OnPlaymodeChanged(s); };

            m_modifyMode = ModifyMode.NONE;
            NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
            ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;

            string lastGraphAssetPath = EditorPrefs.GetString(kPREFKEY_LASTEDITEDGRAPH);

            if (!string.IsNullOrEmpty(lastGraphAssetPath))
            {
                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(lastGraphAssetPath);
                if (graph != null)
                {
                    OpenGraph(graph);
                }
            }
        }

        private void OnPlaymodeChanged(PlayModeStateChange s)
        {
            if (m_controller != null && m_controller.TargetGraph != null)
            {
                SaveGraph();
            }

            if (s == PlayModeStateChange.EnteredEditMode || s == PlayModeStateChange.EnteredPlayMode)
            {
                CloseGraph();
                Init();
                Repaint();
            }
        }

        private void OnUndoRedoPerformed()
        {
            if (m_controller != null)
            {
                if (string.IsNullOrEmpty(m_graphAssetPath))
                {
                    CloseGraph();
                }
                else
                {
                    var graphPath = AssetDatabase.GetAssetPath(m_controller.TargetGraph);

                    // if Undo/Redo changes target graph, m_controller needs recreating
                    if (graphPath != m_graphAssetPath)
                    {
                        OpenGraph(m_graphAssetPath);
                    }

                    // otherwise, each node need OnUndoObject event
                    else
                    {
                        foreach (var n in m_nodes)
                        {
                            if (n != null)
                            {
                                n.OnUndoObject(m_controller);
                            }
                        }
                    }
                }
            }

            if (m_activeSelection == null)
            {
                m_activeSelection = new SavedSelection();
            }

            UpdateActiveObjects(m_activeSelection);

            m_initialDragNodePositions.Clear();

            Setup();
            Repaint();
        }

        private void ShowErrorOnNodes()
        {
            foreach (var node in m_nodes)
            {
                node.ResetErrorStatus();
                var errorsForeachNode = m_controller.Issues.Where(e => e.NodeId == node.Id).Select(e =>
                    $"{e.Reason}\n{e.HowToFix}").ToList();
                if (errorsForeachNode.Any())
                {
                    node.AppendErrorSources(errorsForeachNode);
                }
            }
        }

        private void SetGraphAssetPath(string newPath)
        {
            if (newPath == null)
            {
                m_graphAssetPath = null;
                m_graphAssetName = null;
            }
            else
            {
                m_graphAssetPath = newPath;
                m_graphAssetName = Path.GetFileNameWithoutExtension(m_graphAssetPath);
                if (m_graphAssetName.Length > Model.Settings.GUI.TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH)
                {
                    m_graphAssetName = m_graphAssetName.Substring(0, Model.Settings.GUI.TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH) + "...";
                }

                EditorPrefs.SetString(kPREFKEY_LASTEDITEDGRAPH, m_graphAssetPath);
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset()]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var graph = EditorUtility.InstanceIDToObject(instanceID) as Model.ConfigGraph;
            if (graph != null)
            {
                var window = GetWindow<AssetGraphEditorWindow>();
                window.OpenGraph(graph);
                return true;
            }

            return false;
        }

        public void OpenGraph(string path)
        {
            Model.ConfigGraph graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
            if (graph == null)
            {
                throw new AssetGraphException("Could not open graph:" + path);
            }

            OpenGraph(graph);
        }

        public void OpenGraph(Model.ConfigGraph graph)
        {
            if (m_controller != null && m_controller.TargetGraph == graph)
            {
                // do nothing
                return;
            }

            var graphName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(graph));

            RecordUndo("Open " + graphName);

            CloseGraph();

            SetGraphAssetPath(AssetDatabase.GetAssetPath(graph));

            m_modifyMode = ModifyMode.NONE;

            m_scrollPos = new Vector2(0, 0);
            m_errorScrollPos = new Vector2(0, 0);

            m_selectStartMousePosition = null;
            m_activeSelection = null;
            m_currentEventSource = null;

            m_controller = new AssetGraphController(graph);
            ConstructGraphGUI();
            Stopwatch sw = new Stopwatch();
            //--------测试1---------
            sw.Start();
            Setup();
            sw.Stop();
            Debug.LogFormat("<color=red>打开SG->Setup耗时:{0}ms</color>", sw.ElapsedMilliseconds);
            //--------测试2---------
            sw.Restart();
            if (m_nodes.Any())
            {
                UpdateSpacerRect();
            }

            sw.Stop();
            Debug.LogFormat("<color=red>打开SG->Update耗时:{0}ms</color>", sw.ElapsedMilliseconds);
            //设置
            Selection.activeObject = graph;
        }

        private void CloseGraph()
        {
            m_modifyMode = ModifyMode.NONE;
            SetGraphAssetPath(null);
            m_controller = null;
            m_nodes = null;
            m_connections = null;

            m_selectStartMousePosition = null;
            m_activeSelection = null;
            m_currentEventSource = null;
        }

        private void CreateNewGraphFromDialog()
        {
            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Create New Asset Graph",
                    "Asset Graph", "asset",
                    "Create a new asset graph:");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Model.ConfigGraph graph = Model.ConfigGraph.CreateNewGraph(path);
            OpenGraph(graph);
        }

        private void CreateNewGraphFromImport()
        {
            var path =
                EditorUtility.OpenFilePanel(
                    "Select previous version file",
                    "Assets", "");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var graph = Model.ConfigGraph.CreateNewGraphFromImport(path);
            OpenGraph(graph);
        }

        /**
		 * Get WindowId does not collide with other nodeGUIs
		 */
        private static int GetSafeWindowId(List<NodeGUI> nodeGUIs)
        {
            int id = -1;

            foreach (var nodeGui in nodeGUIs)
            {
                if (nodeGui.WindowId > id)
                {
                    id = nodeGui.WindowId;
                }
            }

            return id + 1;
        }

        /**
		 * Creates Graph structure with NodeGUI and ConnectionGUI from SaveData
		 */
        private void ConstructGraphGUI()
        {
            var activeGraph = m_controller.TargetGraph;

            var currentNodes = new List<NodeGUI>();
            var currentConnections = new List<ConnectionGUI>();

            foreach (var node in activeGraph.Nodes)
            {
                var newNodeGUI = NodeGUI.CreateNodeGUI(m_controller, node);
                newNodeGUI.WindowId = GetSafeWindowId(currentNodes);
                currentNodes.Add(newNodeGUI);
            }

            // load connections
            foreach (var c in activeGraph.Connections)
            {
                var startNode = currentNodes.Find(node => node.Id == c.FromNodeId);
                if (startNode == null)
                {
                    continue;
                }

                var endNode = currentNodes.Find(node => node.Id == c.ToNodeId);
                if (endNode == null)
                {
                    continue;
                }

                var startPoint = startNode.Data.FindConnectionPoint(c.FromNodeConnectionPointId);
                var endPoint = endNode.Data.FindConnectionPoint(c.ToNodeConnectionPointId);

                currentConnections.Add(ConnectionGUI.LoadConnection(c, startPoint, endPoint));
            }

            m_nodes = currentNodes;
            m_connections = currentConnections;
        }

        private void SaveGraph()
        {
            UnityEngine.Assertions.Assert.IsNotNull(m_controller);
            m_controller.TargetGraph.ApplyGraph(m_nodes, m_connections);
        }

        /**
		 * Save Graph and update all nodes & connections
		 */
        private void Setup(bool forceVisitAll = false)
        {
            EditorUtility.ClearProgressBar();
            if (m_controller == null)
            {
                return;
            }

            try
            {
                foreach (var node in m_nodes)
                {
                    node.HideProgress();
                }

                SaveGraph();

                // update static all node names.
                NodeGUIUtility.allNodeNames = new List<string>(m_nodes.Select(node => node.Name).ToList());

                m_controller.Perform(m_target, false, false, forceVisitAll, null);

                RefreshInspector(m_controller.StreamManager);
                ShowErrorOnNodes();
            }
            catch (Exception e)
            {
                LogUtility.Logger.LogError(LogUtility.kTag, e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void Validate(NodeGUI node)
        {
            EditorUtility.ClearProgressBar();
            if (m_controller == null)
            {
                return;
            }

            try
            {
                node.ResetErrorStatus();
                node.HideProgress();

                SaveGraph();

                m_controller.Validate(node, m_target);

                RefreshInspector(m_controller.StreamManager);
                ShowErrorOnNodes();
            }
            catch (Exception e)
            {
                LogUtility.Logger.LogError(LogUtility.kTag, e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }


        /// <summary>
        /// 是否正在执行build
        /// </summary>
        static public bool IsRunningBuild { get; private set; } = false;
        /**
		 * Execute the build.
		 */
        private void Run()
        {
            if (m_controller == null)
            {
                return;
            }

            try
            {
                IsRunningBuild = true;
                
                AssetDatabase.SaveAssets();
                AssetBundleBuildMap.GetBuildMap().Clear();

                if (UserPreference.ClearAssetLogOnBuild)
                {
                    AssetProcessEventRecord.GetRecord().Clear(false);
                }

                float currentCount = 0f;
                float totalCount = (float) m_controller.TargetGraph.Nodes.Count;
                Model.NodeData lastNode = null;

                Action<Model.NodeData, string, float> updateHandler = (node, message, progress) =>
                {
                    if (lastNode != node)
                    {
                        // do not add count on first node visit to 
                        // calcurate percantage correctly
                        if (lastNode != null)
                        {
                            ++currentCount;
                        }

                        lastNode = node;
                    }

                    float currentNodeProgress = progress * (1.0f / totalCount);
                    float currentTotalProgress = (currentCount / totalCount) + currentNodeProgress;

                    string title = $"Processing Asset Graph[{currentCount}/{totalCount}]";
                    string info = $"{node.Name}:{message}";

                    EditorUtility.DisplayProgressBar(title, "Processing " + info, currentTotalProgress);
                };

                // perform setup. Fails if any exception raises.
                m_controller.Perform(m_target, false, true, true, null);

                // if there is not error reported, then run
                if (!m_controller.IsAnyIssueFound)
                {
                    m_controller.Perform(m_target, true, true, true, updateHandler);
                }

                RefreshInspector(m_controller.StreamManager);
                AssetDatabase.Refresh();

                ShowErrorOnNodes();
                IsRunningBuild = false;
            }
            catch (Exception e)
            {
                if (LogUtility.Logger != null)
                {
                    LogUtility.Logger.LogError(LogUtility.kTag, e.ToString());
                }
                
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                IsRunningBuild = false;
            }
        }

        private static void RefreshInspector(AssetReferenceStreamManager streamManager)
        {
            if (Selection.activeObject == null)
            {
                return;
            }

            if (Selection.activeObject.GetType() == typeof(ConnectionGUI))
            {
                var con = Selection.activeObject as ConnectionGUI;

                // null when multiple connection deleted.
                if (string.IsNullOrEmpty(con.Id))
                {
                    return;
                }

                con.AssetGroups = streamManager.FindAssetGroup(con.Id);
            }
        }

        private void OnAssetsReimported(AssetPostprocessorContext ctx)
        {
            if (m_controller != null)
            {
                m_controller.OnAssetsReimported(ctx);
            }

            if (!string.IsNullOrEmpty(m_graphAssetPath))
            {
                if (ctx.DeletedAssetPaths.Contains(m_graphAssetPath))
                {
                    CloseGraph();
                    return;
                }

                int moveIndex = Array.FindIndex(ctx.MovedFromAssetPaths, p => p == m_graphAssetPath);
                if (moveIndex >= 0)
                {
                    SetGraphAssetPath(ctx.MovedAssetPaths[moveIndex]);
                }
            }
        }

        public static void NotifyAssetsReimportedToAllWindows(AssetPostprocessorContext ctx)
        {
            var w = Window;
            if (w != null)
            {
                w.OnAssetsReimported(ctx);
            }
        }

        private void DrawGUIToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(new GUIContent(m_graphAssetName, "Select graph"), EditorStyles.toolbarPopup, GUILayout.Width(Model.Settings.GUI.TOOLBAR_GRAPHNAMEMENU_WIDTH), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT)))
                {
                    GenericMenu menu = new GenericMenu();

                    var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);
                    var nameList = new List<string>();

                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        string name = Path.GetFileNameWithoutExtension(path);

                        // exclude graphs with hidden prefix
                        if (name.StartsWith(Model.Settings.HIDE_GRAPH_PREFIX))
                        {
                            continue;
                        }

                        // GenericMenu can't have multiple menu item with the same name
                        // Avoid name overlap
                        string menuName = name;
                        int i = 1;
                        while (nameList.Contains(menuName))
                        {
                            menuName = $"{name} ({i++})";
                        }

                        menu.AddItem(new GUIContent(menuName), false, () =>
                        {
                            if (path != m_graphAssetPath)
                            {
                                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
                                OpenGraph(graph);
                            }
                        });
                        nameList.Add(menuName);
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Create New..."), false, () => { CreateNewGraphFromDialog(); });

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Import/Import JSON Graph to current graph..."), false, () =>
                    {
                        var graph = JSONGraphUtility.ImportJSONToGraphFromDialog(m_controller.TargetGraph);
                        if (graph != null)
                        {
                            OpenGraph(graph);
                        }
                    });
                    menu.AddSeparator("Import/");
                    menu.AddItem(new GUIContent("Import/Import JSON Graph and create new..."), false, () =>
                    {
                        var graph = JSONGraphUtility.ImportJSONToGraphFromDialog(null);
                        if (graph != null)
                        {
                            OpenGraph(graph);
                        }
                    });
                    menu.AddItem(new GUIContent("Import/Import JSON Graphs in folder..."), false, () => { JSONGraphUtility.ImportAllJSONInDirectoryToGraphFromDialog(); });
                    menu.AddItem(new GUIContent("Export/Export current graph to JSON..."), false, () => { JSONGraphUtility.ExportGraphToJSONFromDialog(m_controller.TargetGraph); });
                    menu.AddItem(new GUIContent("Export/Export all graphs to JSON..."), false, () => { JSONGraphUtility.ExportAllGraphsToJSONFromDialog(); });

                    menu.AddSeparator("Import/");
                    menu.AddItem(new GUIContent("Import/Import previous version(>1.2)..."), false, CreateNewGraphFromImport);

                    menu.DropDown(new Rect(4f, 8f, 0f, 0f));
                }

                if (GUILayout.Button(new GUIContent("Edit", "Edit Graph Description"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT)))
                {
                    Selection.activeObject = m_controller.TargetGraph;
                }

                GUILayout.Space(4);

                if (GUILayout.Button(new GUIContent("Refresh", m_refreshIcon, "Refresh graph status"), EditorStyles.toolbarButton, GUILayout.Width(80), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT)))
                {
                    Setup();
                }

                GUILayout.Space(4);

                m_showErrors = GUILayout.Toggle(m_showErrors, new GUIContent(m_miniErrorIcon, "Show errors"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
                m_showDescription = GUILayout.Toggle(m_showDescription, new GUIContent(m_miniInfoIcon, "Show graph description"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
                m_showVerboseLog = GUILayout.Toggle(m_showVerboseLog, new GUIContent("Verbose Log", "Increse console log messages"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
                LogUtility.ShowVerboseLog(m_showVerboseLog);

                GUILayout.Space(4);

                m_controller.TargetGraph.UseAsAssetPostprocessor = GUILayout.Toggle(m_controller.TargetGraph.UseAsAssetPostprocessor, new GUIContent("Use As Postprocessor", "Graph will be used as asset postprocessor if enabled"),
                    EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

                GUILayout.Space(4);

                GUILayout.FlexibleSpace();

                GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);

                tbLabel.alignment = TextAnchor.MiddleCenter;

                GUIStyle tbLabelTarget = new GUIStyle(tbLabel);
                tbLabelTarget.fontStyle = FontStyle.Bold;

                GUILayout.Label("Platform:", tbLabel, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

                var supportedTargets = NodeGUIUtility.SupportedBuildTargets;
                int currentIndex = Mathf.Max(0, supportedTargets.FindIndex(t => t == m_target));

                int newIndex = EditorGUILayout.Popup(currentIndex, NodeGUIUtility.supportedBuildTargetNames,
                    EditorStyles.toolbarPopup, GUILayout.Width(150), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

                if (newIndex != currentIndex)
                {
                    m_target = supportedTargets[newIndex];
                    Setup(true);
                }

                using (new EditorGUI.DisabledScope(m_controller.IsAnyIssueFound))
                {
                    if (GUILayout.Button("Execute", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT)))
                    {
                        EditorApplication.delayCall += BuildFromMenu;
                    }
                }
            }
        }

        private const string kGUIDELINETEXT = "To configure asset workflow, create an asset graph.";
        private const string kCREATEBUTTON = "Create";

        private void DrawNoGraphGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();
                    var guideline = new GUIContent(kGUIDELINETEXT);
                    var size = GUI.skin.label.CalcSize(guideline);
                    GUILayout.Label(kGUIDELINETEXT);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var spaceWidth = (size.x - 100f) / 2f;

                        GUILayout.Space(spaceWidth);
                        if (GUILayout.Button(kCREATEBUTTON, GUILayout.Width(100f), GUILayout.ExpandWidth(false)))
                        {
                            CreateNewGraphFromDialog();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawGUINodeErrors()
        {
            m_errorScrollPos = EditorGUILayout.BeginScrollView(m_errorScrollPos, GUI.skin.box, GUILayout.Width(200));
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    foreach (NodeException e in m_controller.Issues)
                    {
                        EditorGUILayout.HelpBox($"{e.Reason}\n{e.HowToFix}", MessageType.Error);
                        if (GUILayout.Button("Go to Node"))
                        {
                            SelectNode(e.NodeId);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGUINodeGraph()
        {
            m_background.Draw(m_graphRegion, m_scrollPos);

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_scrollPos))
            {
                m_scrollPos = scrollScope.scrollPosition;

                if (m_showDescription)
                {
                    if (m_descriptionStyle == null)
                    {
                        m_descriptionStyle = new GUIStyle(EditorStyles.whiteMiniLabel);
                        m_descriptionStyle.wordWrap = true;
                        m_descriptionStyle.richText = true;
                        var styleState = new GUIStyleState();
                        styleState.textColor = Color.white;
                        m_descriptionStyle.normal = styleState;
                    }

                    var content = new GUIContent(m_controller.TargetGraph.Descrption);
                    var height = m_descriptionStyle.CalcHeight(content, position.width - 12f);
                    var oldContentColor = GUI.contentColor;

                    GUI.Label(new Rect(12f, 12f, position.width - 12f, height), content, m_descriptionStyle);
                }

                // draw connections.
                foreach (var con in m_connections)
                {
                    con.DrawConnection(m_nodes, m_controller.StreamManager.FindAssetGroup(con.Id));
                }

                // draw node window x N.
                {
                    BeginWindows();

                    m_nodes.ForEach(node => node.DrawNode());

                    HandleDragNodes();

                    EndWindows();
                }

                // draw connection input point marks.
                foreach (var node in m_nodes)
                {
                    node.DrawConnectionInputPointMark(m_currentEventSource, m_modifyMode == ModifyMode.CONNECTING);
                }

                // draw connection output point marks.
                foreach (var node in m_nodes)
                {
                    node.DrawConnectionOutputPointMark(m_currentEventSource, m_modifyMode == ModifyMode.CONNECTING, Event.current);
                }

                // draw connecting line if modifing connection.
                switch (m_modifyMode)
                {
                    case ModifyMode.CONNECTING:
                    {
                        // from start node to mouse.
                        DrawStraightLineFromCurrentEventSourcePointTo(Event.current.mousePosition, m_currentEventSource);
                        break;
                    }
                    case ModifyMode.SELECTING:
                    {
                        float lx = Mathf.Max(m_selectStartMousePosition.x, Event.current.mousePosition.x);
                        float ly = Mathf.Max(m_selectStartMousePosition.y, Event.current.mousePosition.y);
                        float sx = Mathf.Min(m_selectStartMousePosition.x, Event.current.mousePosition.x);
                        float sy = Mathf.Min(m_selectStartMousePosition.y, Event.current.mousePosition.y);

                        Rect sel = new Rect(sx, sy, lx - sx, ly - sy);
                        GUI.Label(sel, string.Empty, "SelectionRect");
                        break;
                    }
                }

                // handle Graph GUI events
                HandleGraphGUIEvents();
                HandleDragAndDropGUI(m_graphRegion);

                // set rect for scroll.
                if (m_nodes.Any())
                {
                    if (Event.current.type == EventType.Layout)
                    {
                        GUILayoutUtility.GetRect(new GUIContent(string.Empty), GUIStyle.none, GUILayout.Width(m_spacerRectRightBottom.x), GUILayout.Height(m_spacerRectRightBottom.y));
                    }
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                var newRgn = GUILayoutUtility.GetLastRect();
                if (newRgn != m_graphRegion)
                {
                    m_graphRegion = newRgn;
                    Repaint();
                }
            }
        }

        private void HandleGraphGUIEvents()
        {
            //mouse drag event handling.
            switch (Event.current.type)
            {
                // draw line while dragging.
                case EventType.MouseDrag:
                {
                    switch (m_modifyMode)
                    {
                        case ModifyMode.NONE:
                        {
                            switch (Event.current.button)
                            {
                                case 0:
                                {
                                    // left click
                                    if (m_graphRegion.Contains(Event.current.mousePosition - m_scrollPos))
                                    {
                                        m_selectStartMousePosition = new SelectPoint(Event.current.mousePosition);
                                        m_modifyMode = ModifyMode.SELECTING;
                                    }

                                    break;
                                }
                            }

                            break;
                        }
                        case ModifyMode.SELECTING:
                        {
                            // do nothing.
                            break;
                        }
                    }

                    HandleUtility.Repaint();
                    Event.current.Use();
                    break;
                }
            }

            // mouse up event handling.
            // use rawType for detect for detectiong mouse-up which raises outside of window.
            switch (Event.current.rawType)
            {
                case EventType.MouseUp:
                {
                    switch (m_modifyMode)
                    {
                        /*
                            select contained nodes & connections.
                        */
                        case ModifyMode.SELECTING:
                        {
                            if (m_selectStartMousePosition == null)
                            {
                                break;
                            }

                            var x = 0f;
                            var y = 0f;
                            var width = 0f;
                            var height = 0f;

                            if (Event.current.mousePosition.x < m_selectStartMousePosition.x)
                            {
                                x = Event.current.mousePosition.x;
                                width = m_selectStartMousePosition.x - Event.current.mousePosition.x;
                            }

                            if (m_selectStartMousePosition.x < Event.current.mousePosition.x)
                            {
                                x = m_selectStartMousePosition.x;
                                width = Event.current.mousePosition.x - m_selectStartMousePosition.x;
                            }

                            if (Event.current.mousePosition.y < m_selectStartMousePosition.y)
                            {
                                y = Event.current.mousePosition.y;
                                height = m_selectStartMousePosition.y - Event.current.mousePosition.y;
                            }

                            if (m_selectStartMousePosition.y < Event.current.mousePosition.y)
                            {
                                y = m_selectStartMousePosition.y;
                                height = Event.current.mousePosition.y - m_selectStartMousePosition.y;
                            }

                            RecordUndo("Select Objects");

                            if (m_activeSelection == null)
                            {
                                m_activeSelection = new SavedSelection();
                            }

                            // if shift key is not pressed, clear current selection
                            if (!Event.current.shift)
                            {
                                m_activeSelection.Clear();
                            }

                            var selectedRect = new Rect(x, y, width, height);

                            foreach (var node in m_nodes)
                            {
                                if (node.GetRect().Overlaps(selectedRect))
                                {
                                    m_activeSelection.Add(node);
                                }
                            }

                            foreach (var connection in m_connections)
                            {
                                // get contained connection badge.
                                if (connection.GetRect().Overlaps(selectedRect))
                                {
                                    m_activeSelection.Add(connection);
                                }
                            }

                            UpdateActiveObjects(m_activeSelection);

                            m_selectStartMousePosition = null;
                            m_modifyMode = ModifyMode.NONE;

                            HandleUtility.Repaint();
                            Event.current.Use();
                            break;
                        }
                    }

                    break;
                }
            }
        }

        private void RecordUndo(string msg)
        {
            if (m_undo == null)
            {
                m_undo = new UndoUtility();
            }

            m_undo.RecordUndo(this, m_nodes, m_connections, msg);
        }

        private void HandleDragAndDropGUI(Rect dragdropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dragdropArea.Contains(evt.mousePosition))
                        return;

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(path))
                        {
                            FileAttributes attr = File.GetAttributes(path);

                            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                break;
                            }
                            else
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                                break;
                            }
                        }
                    }

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            var path = AssetDatabase.GetAssetPath(obj);
                            FileAttributes attr = File.GetAttributes(path);

                            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                AddNodeFromGUI(new Loader(path),
                                    $"Load from {Path.GetFileName(path)}",
                                    evt.mousePosition.x, evt.mousePosition.y);
                                Setup();
                                Repaint();
                            }
                        }
                    }

                    break;
            }
        }

        public void OnEnable()
        {
            Init();
        }

        public void OnDisable()
        {
            LogUtility.Logger.Log("OnDisable");
            if (m_controller != null)
            {
                m_controller.TargetGraph.Save();
            }
        }

        public void OnGUI()
        {
            //return;
            if (m_controller == null)
            {
                DrawNoGraphGUI();
            }
            else
            {
                DrawGUIToolBar();

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawGUINodeGraph();
                    if (m_showErrors)
                    {
                        DrawGUINodeErrors();
                    }
                }

                if (!string.IsNullOrEmpty(m_graphAssetPath))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(m_graphAssetPath, "MiniLabel");
                    }
                }

                if (m_controller.IsAnyIssueFound)
                {
                    Rect msgRgn = new Rect((m_graphRegion.width - 250f) / 2f, m_graphRegion.y + 8f, 250f, 36f);
                    EditorGUI.HelpBox(msgRgn, "All errors needs to be fixed before building.", MessageType.Error);
                }

                HandleGUIEvent();
            }
        }

        private void HandleGUIEvent()
        {
            var isValidSelection = m_activeSelection != null && m_activeSelection.IsSelected;
            var isValidCopy = m_copiedSelection != null && m_copiedSelection.IsSelected;

            /*
				Event Handling:
				- Supporting dragging script into window to create node.
				- Context Menu	
				- NodeGUI connection.
				- Command(Delete, Copy, etc...)
			*/
            switch (Event.current.type)
            {
                // show context menu
                case EventType.ContextClick:
                {
                    ShowNodeCreateContextMenu(Event.current.mousePosition);
                    break;
                }

                /*
                        Handling mouseUp at empty space. 
                    */
                case EventType.MouseUp:
                {
                    m_modifyMode = ModifyMode.NONE;
                    HandleUtility.Repaint();

                    if (m_activeSelection != null && m_activeSelection.IsSelected)
                    {
                        RecordUndo("Unselect");

                        m_activeSelection.Clear();
                        UpdateActiveObjects(m_activeSelection);
                    }

                    // clear inspector
                    if (Selection.activeObject is NodeGUI || Selection.activeObject is ConnectionGUI)
                    {
                        Selection.activeObject = null;
                    }

                    break;
                }

                case EventType.ValidateCommand:
                {
                    switch (Event.current.commandName)
                    {
                        case "Delete":
                        {
                            if (isValidSelection)
                            {
                                Event.current.Use();
                            }

                            break;
                        }

                        case "Copy":
                        {
                            if (isValidSelection)
                            {
                                Event.current.Use();
                            }

                            break;
                        }

                        case "Cut":
                        {
                            if (isValidSelection)
                            {
                                Event.current.Use();
                            }

                            break;
                        }

                        case "Paste":
                        {
                            if (isValidCopy)
                            {
                                Event.current.Use();
                            }

                            break;
                        }

                        case "SelectAll":
                        {
                            Event.current.Use();
                            break;
                        }
                    }

                    break;
                }

                case EventType.ExecuteCommand:
                {
                    switch (Event.current.commandName)
                    {
                        // Delete active node or connection.
                        case "Delete":
                        {
                            if (!isValidSelection)
                            {
                                break;
                            }

                            DeleteSelected();

                            Event.current.Use();
                            break;
                        }

                        case "Copy":
                        {
                            if (!isValidSelection)
                            {
                                break;
                            }

                            m_copiedSelection = new SavedSelection(m_activeSelection);

                            Event.current.Use();
                            break;
                        }

                        case "Cut":
                        {
                            if (!isValidSelection)
                            {
                                break;
                            }

                            RecordUndo("Cut Selected");

                            m_copiedSelection = new SavedSelection(m_activeSelection);

                            foreach (var n in m_activeSelection.nodes)
                            {
                                DeleteNode(n.Id);
                            }

                            foreach (var c in m_activeSelection.connections)
                            {
                                DeleteConnection(c.Id);
                            }

                            m_activeSelection.Clear();
                            UpdateActiveObjects(m_activeSelection);

                            Setup();
                            //InitializeGraph();

                            Event.current.Use();
                            break;
                        }

                        case "Paste":
                        {
                            if (!isValidCopy)
                            {
                                break;
                            }

                            RecordUndo("Paste");

                            Dictionary<NodeGUI, NodeGUI> nodeLookup = new Dictionary<NodeGUI, NodeGUI>();

                            foreach (var copiedNode in m_copiedSelection.nodes)
                            {
                                var newNode = DuplicateNode(copiedNode, m_copiedSelection.PasteOffset);
                                nodeLookup.Add(copiedNode, newNode);
                            }

                            foreach (var copiedConnection in m_copiedSelection.connections)
                            {
                                DuplicateConnection(copiedConnection, nodeLookup);
                            }


                            m_copiedSelection.IncrementPasteOffset();

                            Setup();
                            //InitializeGraph();

                            Event.current.Use();
                            break;
                        }

                        case "SelectAll":
                        {
                            RecordUndo("Select All Objects");

                            if (m_activeSelection == null)
                            {
                                m_activeSelection = new SavedSelection();
                            }

                            m_activeSelection.Clear();
                            m_nodes.ForEach(n => m_activeSelection.Add(n));
                            m_connections.ForEach(c => m_activeSelection.Add(c));

                            UpdateActiveObjects(m_activeSelection);

                            Event.current.Use();
                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }

                    break;
                }
            }
        }

        private void DeleteSelected()
        {
            RecordUndo("Delete Selected");

            foreach (var n in m_activeSelection.nodes)
            {
                DeleteNode(n.Id);
            }

            foreach (var c in m_activeSelection.connections)
            {
                DeleteConnection(c.Id);
            }

            m_activeSelection.Clear();
            UpdateActiveObjects(m_activeSelection);

            Setup();
        }

        private void ShowNodeCreateContextMenu(Vector2 pos)
        {
            var menu = new GenericMenu();
            var customNodes = NodeUtility.CustomNodeTypes;
            for (int i = 0; i < customNodes.Count; ++i)
            {
                // workaround: avoiding compilier closure bug
                var index = i;
                var name = customNodes[index].node.Name;
                menu.AddItem(
                    new GUIContent(name),
                    false,
                    () =>
                    {
                        AddNodeFromGUI(customNodes[index].CreateInstance(), GetNodeNameFromMenu(name), pos.x + m_scrollPos.x, pos.y + m_scrollPos.y);
                        Setup();
                        Repaint();
                    }
                );
            }

            menu.ShowAsContext();
        }

        private string GetNodeNameFromMenu(string nodeMenuName)
        {
            var slashIndex = nodeMenuName.LastIndexOf('/');
            return nodeMenuName.Substring(slashIndex + 1);
        }

        private void AddNodeFromGUI(Node n, string guiName, float x, float y)
        {
            string nodeName = guiName;
            NodeGUI newNode = NodeGUI.CreateNodeGUI(m_controller, new Model.NodeData(nodeName, n, x, y));

            RecordUndo("Add " + guiName + " Node");

            AddNodeGUI(newNode);
        }

        private void DrawStraightLineFromCurrentEventSourcePointTo(Vector2 to, NodeEvent eventSource)
        {
            if (eventSource == null)
            {
                return;
            }

            var p = eventSource.point.GetGlobalPosition(eventSource.eventSourceNode);
            Handles.DrawLine(new Vector3(p.x, p.y, 0f), new Vector3(to.x, to.y, 0f));
        }

        /**
		 * Handle Node Event
		*/
        private void HandleNodeEvent(NodeEvent e)
        {
            switch (m_modifyMode)
            {
                /*
                 * During Mouse-drag opration to connect to other node
                 */
                case ModifyMode.CONNECTING:
                    switch (e.eventType)
                    {
                        /*
                                connection established between 2 nodes
                            */
                        case NodeEvent.EventType.EVENT_CONNECTION_ESTABLISHED:
                        {
                            // finish connecting mode.
                            m_modifyMode = ModifyMode.NONE;

                            if (m_currentEventSource == null)
                            {
                                break;
                            }

                            var sourceNode = m_currentEventSource.eventSourceNode;
                            var sourceConnectionPoint = m_currentEventSource.point;

                            var targetNode = e.eventSourceNode;
                            var targetConnectionPoint = e.point;

                            if (sourceNode.Id == targetNode.Id)
                            {
                                break;
                            }

                            if (!IsConnectablePointFromTo(sourceConnectionPoint, targetConnectionPoint))
                            {
                                break;
                            }

                            var startNode = sourceNode;
                            var startConnectionPoint = sourceConnectionPoint;
                            var endNode = targetNode;
                            var endConnectionPoint = targetConnectionPoint;

                            // reverse if connected from input to output.
                            if (sourceConnectionPoint.IsInput)
                            {
                                startNode = targetNode;
                                startConnectionPoint = targetConnectionPoint;
                                endNode = sourceNode;
                                endConnectionPoint = sourceConnectionPoint;
                            }

                            var outputPoint = startConnectionPoint;
                            var inputPoint = endConnectionPoint;
                            var label = startConnectionPoint.Label;

                            // if two nodes are not supposed to connect, dismiss
                            if (!Model.ConnectionData.CanConnect(startNode.Data, endNode.Data))
                            {
                                break;
                            }

                            AddConnection(label, startNode, outputPoint, endNode, inputPoint);
                            Setup();
                            break;
                        }

                        /*
                                connecting operation ended.
                            */
                        case NodeEvent.EventType.EVENT_CONNECTING_END:
                        {
                            // finish connecting mode.
                            m_modifyMode = ModifyMode.NONE;

                            /*
                                connect when dropped target is connectable from start connectionPoint.
                            */
                            var node = FindNodeByPosition(e.globalMousePosition);
                            if (node == null)
                            {
                                break;
                            }

                            // ignore if target node is source itself.
                            if (node == e.eventSourceNode)
                            {
                                break;
                            }

                            var pointAtPosition = node.FindConnectionPointByPosition(e.globalMousePosition);
                            if (pointAtPosition == null)
                            {
                                break;
                            }

                            var sourcePoint = m_currentEventSource.point;

                            // limit by connectable or not.
                            if (!IsConnectablePointFromTo(sourcePoint, pointAtPosition))
                            {
                                break;
                            }

                            var isInput = m_currentEventSource.point.IsInput;
                            var startNode = (isInput) ? node : e.eventSourceNode;
                            var endNode = (isInput) ? e.eventSourceNode : node;
                            var startConnectionPoint = (isInput) ? pointAtPosition : m_currentEventSource.point;
                            var endConnectionPoint = (isInput) ? m_currentEventSource.point : pointAtPosition;
                            var outputPoint = startConnectionPoint;
                            var inputPoint = endConnectionPoint;
                            var label = startConnectionPoint.Label;

                            // if two nodes are not supposed to connect, dismiss
                            if (!Model.ConnectionData.CanConnect(startNode.Data, endNode.Data))
                            {
                                break;
                            }

                            AddConnection(label, startNode, outputPoint, endNode, inputPoint);
                            Setup();
                            break;
                        }

                        default:
                        {
                            m_modifyMode = ModifyMode.NONE;
                            break;
                        }
                    }

                    break;
                /*
                 * 
                 */
                case ModifyMode.NONE:
                    switch (e.eventType)
                    {
                        /*
                            start connection handling.
                        */
                        case NodeEvent.EventType.EVENT_CONNECTING_BEGIN:
                            m_modifyMode = ModifyMode.CONNECTING;
                            m_currentEventSource = e;
                            break;

                        case NodeEvent.EventType.EVENT_NODE_DELETE:
                            DeleteSelected();
                            break;

                        /*
                            node clicked.
                        */
                        case NodeEvent.EventType.EVENT_NODE_CLICKED:
                        {
                            var clickedNode = e.eventSourceNode;

                            if (m_activeSelection != null && m_activeSelection.nodes.Contains(clickedNode))
                            {
                                break;
                            }

                            if (Event.current.shift)
                            {
                                RecordUndo("Toggle " + clickedNode.Name + " Selection");
                                if (m_activeSelection == null)
                                {
                                    m_activeSelection = new SavedSelection();
                                }

                                m_activeSelection.Toggle(clickedNode);
                            }
                            else
                            {
                                RecordUndo("Select " + clickedNode.Name);
                                if (m_activeSelection == null)
                                {
                                    m_activeSelection = new SavedSelection();
                                }

                                m_activeSelection.Clear();
                                m_activeSelection.Add(clickedNode);
                            }

                            UpdateActiveObjects(m_activeSelection);
                            break;
                        }
                        case NodeEvent.EventType.EVENT_NODE_UPDATED:
                        {
                            break;
                        }

                        default:
                            break;
                    }

                    break;
            }

            switch (e.eventType)
            {
                case NodeEvent.EventType.EVENT_DELETE_ALL_CONNECTIONS_TO_POINT:
                {
                    // deleting all connections to this point
                    m_connections.RemoveAll(c => (c.InputPoint == e.point || c.OutputPoint == e.point));
                    Repaint();
                    break;
                }
                case NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED:
                {
                    // deleting point is handled by caller, so we are deleting connections associated with it.
                    m_connections.RemoveAll(c => (c.InputPoint == e.point || c.OutputPoint == e.point));
                    Repaint();
                    break;
                }
                case NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED:
                {
                    // point label change is handled by caller, so we are changing label of connection associated with it.
                    var affectingConnections = m_connections.FindAll(c => c.OutputPoint.Id == e.point.Id);
                    affectingConnections.ForEach(c => c.Label = e.point.Label);
                    Repaint();
                    break;
                }
                case NodeEvent.EventType.EVENT_NODE_UPDATED:
                {
                    Validate(e.eventSourceNode);
                    break;
                }

                case NodeEvent.EventType.EVENT_RECORDUNDO:
                {
                    RecordUndo(e.message);
                    break;
                }
                case NodeEvent.EventType.EVENT_SAVE:
                    Setup();
                    Repaint();
                    break;
            }
        }

        private void HandleDragNodes()
        {
            Event evt = Event.current;
            int id = GUIUtility.GetControlID(kDragNodesControlID, FocusType.Passive);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (m_modifyMode == ModifyMode.NONE)
                    {
                        if (evt.button == 0)
                        {
                            if (m_activeSelection != null && m_activeSelection.nodes.Count > 0)
                            {
                                bool mouseInSelectedNode = false;
                                foreach (var n in m_activeSelection.nodes)
                                {
                                    if (n.GetRect().Contains(evt.mousePosition))
                                    {
                                        mouseInSelectedNode = true;
                                        break;
                                    }
                                }

                                if (mouseInSelectedNode)
                                {
                                    m_modifyMode = ModifyMode.DRAGGING;
                                    m_LastMousePosition = evt.mousePosition;
                                    m_DragNodeDistance = Vector2.zero;

                                    foreach (var n in m_activeSelection.nodes)
                                    {
                                        m_initialDragNodePositions[n] = n.GetPos();
                                    }

                                    GUIUtility.hotControl = id;
                                    evt.Use();
                                }
                            }
                        }
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        UpdateSpacerRect();
                        m_initialDragNodePositions.Clear();
                        GUIUtility.hotControl = 0;
                        m_modifyMode = ModifyMode.NONE;
                        evt.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        RecordUndo("Move Objects");

                        m_DragNodeDistance += evt.mousePosition - m_LastMousePosition;
                        m_LastMousePosition = evt.mousePosition;

                        foreach (var n in m_activeSelection.nodes)
                        {
                            Vector2 newPosition = n.GetPos();
                            Vector2 initialPosition = m_initialDragNodePositions[n];
                            newPosition.x = initialPosition.x + m_DragNodeDistance.x;
                            newPosition.y = initialPosition.y + m_DragNodeDistance.y;
                            n.SetPos(SnapPositionToGrid(newPosition));
                        }

                        evt.Use();
                    }

                    break;
            }
        }

        protected static Vector2 SnapPositionToGrid(Vector2 position)
        {
            float gridSize = UserPreference.EditorWindowGridSize;

            int xCell = Mathf.RoundToInt(position.x / gridSize);
            int yCell = Mathf.RoundToInt(position.y / gridSize);

            position.x = xCell * gridSize;
            position.y = yCell * gridSize;

            return position;
        }

        private void UpdateSpacerRect()
        {
            var rightPoint = m_nodes.OrderByDescending(node => node.GetRightPos()).First().GetRightPos() + Model.Settings.WINDOW_SPAN;
            var bottomPoint = m_nodes.OrderByDescending(node => node.GetBottomPos()).First().GetBottomPos() + Model.Settings.WINDOW_SPAN;

            m_spacerRectRightBottom = new Vector2(rightPoint, bottomPoint);
        }

        public NodeGUI DuplicateNode(NodeGUI node, float offset)
        {
            var newNode = node.Duplicate(
                m_controller,
                node.GetX() + offset,
                node.GetY() + offset
            );
            AddNodeGUI(newNode);
            return newNode;
        }

        public void DuplicateConnection(ConnectionGUI con, Dictionary<NodeGUI, NodeGUI> nodeLookup)
        {
            var srcNodes = nodeLookup.Keys;

            var srcFrom = srcNodes.Where(n => n.Id == con.Data.FromNodeId).FirstOrDefault();
            var srcTo = srcNodes.Where(n => n.Id == con.Data.ToNodeId).FirstOrDefault();

            if (srcFrom == null || srcTo == null)
            {
                return;
            }

            var fromPointIndex = srcFrom.Data.OutputPoints.FindIndex(p => p.Id == con.Data.FromNodeConnectionPointId);
            var inPointIndex = srcTo.Data.InputPoints.FindIndex(p => p.Id == con.Data.ToNodeConnectionPointId);

            if (fromPointIndex < 0 || inPointIndex < 0)
            {
                return;
            }

            var dstFrom = nodeLookup[srcFrom];
            var dstTo = nodeLookup[srcTo];
            var dstFromPoint = dstFrom.Data.OutputPoints[fromPointIndex];
            var dstToPoint = dstTo.Data.InputPoints[inPointIndex];

            AddConnection(con.Label, dstFrom, dstFromPoint, dstTo, dstToPoint);
        }

        private void AddNodeGUI(NodeGUI newNode)
        {
            int id = -1;

            foreach (var node in m_nodes)
            {
                if (node.WindowId > id)
                {
                    id = node.WindowId;
                }
            }

            newNode.WindowId = id + 1;

            m_nodes.Add(newNode);
        }

        public void DeleteNode(string deletingNodeId)
        {
            var deletedNodeIndex = m_nodes.FindIndex(node => node.Id == deletingNodeId);
            if (0 <= deletedNodeIndex)
            {
                var n = m_nodes[deletedNodeIndex];
                n.Data.Operation.Object.OnNodeDelete(n.Data);
                n.SetActive(false);
                m_nodes.RemoveAt(deletedNodeIndex);
            }
        }

        public void HandleConnectionEvent(ConnectionEvent e)
        {
            switch (m_modifyMode)
            {
                case ModifyMode.NONE:
                {
                    switch (e.eventType)
                    {
                        case ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED:
                        {
                            if (Event.current.shift)
                            {
                                RecordUndo("Toggle Select Connection");
                                if (m_activeSelection == null)
                                {
                                    m_activeSelection = new SavedSelection();
                                }

                                m_activeSelection.Toggle(e.eventSourceCon);
                                UpdateActiveObjects(m_activeSelection);
                                break;
                            }
                            else
                            {
                                RecordUndo("Select Connection");
                                if (m_activeSelection == null)
                                {
                                    m_activeSelection = new SavedSelection();
                                }

                                m_activeSelection.Clear();
                                m_activeSelection.Add(e.eventSourceCon);
                                UpdateActiveObjects(m_activeSelection);
                                break;
                            }
                        }
                        case ConnectionEvent.EventType.EVENT_CONNECTION_DELETED:
                        {
                            RecordUndo("Delete Connection");

                            var deletedConnectionId = e.eventSourceCon.Id;

                            DeleteConnection(deletedConnectionId);
                            m_activeSelection.Clear();
                            UpdateActiveObjects(m_activeSelection);

                            Setup();
                            Repaint();
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }

                    break;
                }
            }
        }

        private void UpdateActiveObjects(SavedSelection selection)
        {
            foreach (var n in m_nodes)
            {
                n.SetActive(selection.nodes.Contains(n));
            }

            foreach (var c in m_connections)
            {
                c.SetActive(selection.connections.Contains(c));
            }
        }

        /**
			create new connection if same relationship is not exist yet.
		*/
        private void AddConnection(string label, NodeGUI startNode, Model.ConnectionPointData startPoint, NodeGUI endNode, Model.ConnectionPointData endPoint)
        {
            RecordUndo("Add Connection");

            var connectionsFromThisNode = m_connections
                .Where(con => con.OutputNodeId == startNode.Id)
                .Where(con => con.OutputPoint == startPoint)
                .ToList();
            if (connectionsFromThisNode.Any())
            {
                var alreadyExistConnection = connectionsFromThisNode[0];
                DeleteConnection(alreadyExistConnection.Id);
                if (m_activeSelection != null)
                {
                    m_activeSelection.Remove(alreadyExistConnection);
                }
            }

            if (!m_connections.ContainsConnection(startPoint, endPoint))
            {
                m_connections.Add(ConnectionGUI.CreateConnection(label, startPoint, endPoint));
            }
        }

        private NodeGUI FindNodeByPosition(Vector2 globalPos)
        {
            return m_nodes.Find(n => n.Conitains(globalPos));
        }

        private bool IsConnectablePointFromTo(Model.ConnectionPointData sourcePoint, Model.ConnectionPointData destPoint)
        {
            if (sourcePoint.IsInput)
            {
                return destPoint.IsOutput;
            }
            else
            {
                return destPoint.IsInput;
            }
        }

        private void DeleteConnection(string id)
        {
            var deletedConnectionIndex = m_connections.FindIndex(con => con.Id == id);
            if (0 <= deletedConnectionIndex)
            {
                var c = m_connections[deletedConnectionIndex];
                c.SetActive(false);
                m_connections.RemoveAt(deletedConnectionIndex);
            }
        }

        public int GetUnusedWindowId()
        {
            int highest = 0;
            m_nodes.ForEach((NodeGUI n) =>
            {
                if (n.WindowId > highest)
                    highest = n.WindowId;
            });
            return highest + 1;
        }
    }
}
