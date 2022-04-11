using UnityEditor;
using UnityEditor.Build.Reporting;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using V1 = AssetBundleGraph;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{

    [CustomNode("Build/Build Player", 90)]
    public class PlayerBuilder : Node
    {

        [SerializeField] private SerializableMultiTargetInt m_buildOptions;
        [SerializeField] private SerializableMultiTargetString m_buildLocations;
        [SerializeField] private SerializableMultiTargetString m_playerName;
        [SerializeField] private SerializableMultiTargetString m_scenes;

        [SerializeField] private Vector2 m_scroll;

        public override string ActiveStyle => "node 5 on";

        public override string InactiveStyle => "node 5";

        public override string Category => "Build";

        public override Model.NodeOutputSemantics NodeInputType => Model.NodeOutputSemantics.AssetBundles;

        public override Model.NodeOutputSemantics NodeOutputType => Model.NodeOutputSemantics.None;

        public override void Initialize(Model.NodeData data)
        {
            m_buildOptions = new SerializableMultiTargetInt();
            m_buildLocations = new SerializableMultiTargetString();
            m_playerName = new SerializableMultiTargetString();
            m_scenes = new SerializableMultiTargetString();
            data.AddDefaultInputPoint();
            m_scroll = Vector2.zero;
        }

        public override Node Clone(Model.NodeData newData)
        {
            var newNode = new PlayerBuilder();

            newNode.m_buildOptions = new SerializableMultiTargetInt(m_buildOptions);
            newNode.m_buildLocations = new SerializableMultiTargetString(m_buildLocations);
            newNode.m_playerName = new SerializableMultiTargetString(m_playerName);
            newNode.m_scenes = new SerializableMultiTargetString(m_scenes);
            newNode.m_scroll = m_scroll;

            newData.AddDefaultInputPoint();

            return newNode;
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {

            EditorGUILayout.HelpBox("Build Player: Build Player.", MessageType.Info);
            editor.UpdateNodeName(node);

            if (m_buildOptions == null)
            {
                return;
            }

            GUILayout.Space(10f);

            //Show target configuration tab
            editor.DrawPlatformSelector(node);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var disabledScope = editor.DrawOverrideTargetToggle(node, m_buildOptions.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) =>
                {
                    using (new RecordUndoScope("Remove Target Build Settings", node, true))
                    {
                        if (enabled)
                        {
                            m_buildOptions[editor.CurrentEditingGroup] = m_buildOptions.DefaultValue;
                            m_buildLocations[editor.CurrentEditingGroup] = m_buildLocations.DefaultValue;
                            m_playerName[editor.CurrentEditingGroup] = m_playerName.DefaultValue;
                            m_scenes[editor.CurrentEditingGroup] = m_scenes.DefaultValue;
                        }
                        else
                        {
                            m_buildOptions.Remove(editor.CurrentEditingGroup);
                            m_buildLocations.Remove(editor.CurrentEditingGroup);
                            m_playerName.Remove(editor.CurrentEditingGroup);
                            m_scenes.Remove(editor.CurrentEditingGroup);
                        }
                        onValueChanged();
                    }
                });

                using (disabledScope)
                {
                    using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_scroll))
                    {
                        m_scroll = scrollScope.scrollPosition;
                        GUILayout.Label("Player Build Location", "BoldLabel");
                        var newBuildLocation = editor.DrawFolderSelector("", "Select Build Location",
                            m_buildLocations[editor.CurrentEditingGroup],
                            Application.dataPath + "/../"
                        );
                        if (newBuildLocation.StartsWith(Application.dataPath))
                        {
                            throw new NodeException("You can not build player inside Assets directory.", 
                                "Select build location outside Assets directory.",
                                node.Data);
                        }

                        if (newBuildLocation != m_buildLocations[editor.CurrentEditingGroup])
                        {
                            using (new RecordUndoScope("Change Build Location", node, true))
                            {
                                m_buildLocations[editor.CurrentEditingGroup] = newBuildLocation;
                                onValueChanged();
                            }
                        }
                        GUILayout.Space(4f);
                        var newPlayerName = EditorGUILayout.TextField("Player Name", m_playerName[editor.CurrentEditingGroup]);
                        if (newPlayerName != m_playerName[editor.CurrentEditingGroup])
                        {
                            using (new RecordUndoScope("Change Player Name", node, true))
                            {
                                m_playerName[editor.CurrentEditingGroup] = newPlayerName;
                                onValueChanged();
                            }
                        }

                        GUILayout.Space(10f);
                        GUILayout.Label("Build Options", "BoldLabel");
                        int buildOptions = m_buildOptions[editor.CurrentEditingGroup];
                        foreach (var option in Model.Settings.BuildPlayerOptionsSettings)
                        {

                            // contains keyword == enabled. if not, disabled.
                            bool isEnabled = (buildOptions & (int)option.option) != 0;

                            var result = EditorGUILayout.ToggleLeft(option.description, isEnabled);
                            if (result != isEnabled)
                            {
                                using (new RecordUndoScope("Change Build Option", node, true))
                                {
                                    buildOptions = (result) ?
                                    ((int)option.option | buildOptions) :
                                    (((~(int)option.option)) & buildOptions);
                                    m_buildOptions[editor.CurrentEditingGroup] = buildOptions;
                                    onValueChanged();
                                }
                            }
                        }

                        var scenesInProject = AssetDatabase.FindAssets("t:Scene");
                        if (scenesInProject.Length > 0)
                        {
                            GUILayout.Space(10f);
                            GUILayout.Label("Scenes", "BoldLabel");

                            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                            {
                                var scenesSelected = m_scenes[editor.CurrentEditingGroup].Split(',');

                                foreach (var sceneGUID in scenesInProject)
                                {
                                    var path = AssetDatabase.GUIDToAssetPath(sceneGUID);
                                    if (string.IsNullOrEmpty(path))
                                    {
                                        ArrayUtility.Remove(ref scenesSelected, sceneGUID);
                                        m_scenes[editor.CurrentEditingGroup] = string.Join(",", scenesSelected);
                                        onValueChanged();
                                        continue;
                                    }
                                    var type = TypeUtility.GetMainAssetTypeAtPath(path);
                                    if (type != typeof(UnityEditor.SceneAsset))
                                    {
                                        continue;
                                    }

                                    var selected = scenesSelected.Contains(sceneGUID);
                                    var newSelected = EditorGUILayout.ToggleLeft(path, selected);
                                    if (newSelected != selected)
                                    {
                                        using (new RecordUndoScope("Change Scene Selection", node, true))
                                        {
                                            if (newSelected)
                                            {
                                                ArrayUtility.Add(ref scenesSelected, sceneGUID);
                                            }
                                            else
                                            {
                                                ArrayUtility.Remove(ref scenesSelected, sceneGUID);
                                            }
                                            m_scenes[editor.CurrentEditingGroup] = string.Join(",", scenesSelected);
                                            onValueChanged();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Prepare(BuildTarget target,
            Model.NodeData node,
            IEnumerable<PerformGraph.AssetGroups> incoming,
            IEnumerable<Model.ConnectionData> connectionsToOutput,
            PerformGraph.Output Output)
        {
            // BundleBuilder do nothing without incoming connections
            if (incoming == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(m_buildLocations[target]))
            {
                throw new NodeException("Build location is empty.", "Set valid build location from inspector.", node);
            }

            if (string.IsNullOrEmpty(m_playerName[target]))
            {
                throw new NodeException("Player name is empty.", "Set valid player name from inspector.", node);
            }
        }

        public override void Build(BuildTarget target,
            Model.NodeData node,
            IEnumerable<PerformGraph.AssetGroups> incoming,
            IEnumerable<Model.ConnectionData> connectionsToOutput,
            PerformGraph.Output Output,
            Action<Model.NodeData, string, float> progressFunc)
        {
            if (incoming == null)
            {
                return;
            }

            if (!Directory.Exists(m_buildLocations[target]))
            {
                Directory.CreateDirectory(m_buildLocations[target]);
            }

            var sceneGUIDs = m_scenes[target].Split(',');

            string manifestPath = string.Empty;

            foreach (var ag in incoming)
            {
                foreach (var assets in ag.assetGroups.Values)
                {
                    var manifestBundle = assets.Where(a => a.assetType == typeof(AssetBundleManifestReference));
                    if (manifestBundle.Any())
                    {
                        manifestPath = manifestBundle.First().importFrom;
                    }
                }
            }

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
               options = (BuildOptions)m_buildOptions[target],
                locationPathName = m_buildLocations[target] + "/" + m_playerName[target],
                assetBundleManifestPath = manifestPath,
                scenes = sceneGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(path => !string.IsNullOrEmpty(path) && !path.Contains("__DELETED_GUID_Trash")).ToArray(),
                target = target,
                targetGroup = BuildTargetUtility.TargetToGroup(target)
            };

            var report = BuildPipeline.BuildPlayer(opt);
            var summary = report.summary;

            switch(summary.result) {
            case BuildResult.Failed:
                throw new NodeException(
                    $"Player build failed. ({summary.totalErrors} errors)", 
                    summary.ToString(), node);
            case BuildResult.Cancelled:
                LogUtility.Logger.Log(LogUtility.kTag, "Player build cancelled.");
                break;
            case BuildResult.Unknown:
                throw new NodeException(
                    "Player build ended with Unknown state.", 
                    summary.ToString(), node);
            }
        }
    }
}