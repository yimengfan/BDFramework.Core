using UnityEditor;
using System;
using System.Linq;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    public class NodeGUIInspector 
    {
        public static BuildTargetGroup currentEditingGroup = BuildTargetUtility.DefaultTarget;

        [NonSerialized]
        private IModifier m_modifier;

        [NonSerialized]
        private IPrefabBuilder m_prefabBuilder;

        public BuildTargetGroup CurrentEditingGroup
        {
            get { return currentEditingGroup; }
        }



        public  void OnInspectorGUI(NodeGUI node)
        {
           
            var controller = node.Controller;

            if (controller == null)
            {
                return;
            }

            node.Data.Operation.Object.OnInspectorGUI(node, controller.StreamManager, this, () =>
            {
                node.Data.Operation.Save();
                node.ParentGraph.SetGraphDirty();
            });

            var errors = node.Errors;
            if (errors != null && errors.Any())
            {
                foreach (var error in errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
        }


        public void UpdateNodeName(NodeGUI node)
        {
            var newName = EditorGUILayout.TextField("Node Name", node.Name);

            if (NodeGUIUtility.allNodeNames != null)
            {
                var overlapping = NodeGUIUtility.allNodeNames.GroupBy(x => x)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);
                if (overlapping.Any() && overlapping.Contains(newName))
                {
                    EditorGUILayout.HelpBox("There are node with the same name. You may want to rename to avoid confusion:" + newName, MessageType.Info);
                }
            }

            if (newName != node.Name)
            {
                using (new RecordUndoScope("Change Node Name", node, true))
                {
                    node.Name = newName;
                }
            }
        }

        /*
         *  Return true if Platform is changed
         */
        public bool DrawPlatformSelector(NodeGUI node)
        {
            BuildTargetGroup g = currentEditingGroup;
            bool editGroupChanged = false;

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                var choosenIndex = -1;
                for (var i = 0; i < NodeGUIUtility.platformButtons.Length; i++)
                {
                    var onOffBefore = NodeGUIUtility.platformButtons[i].targetGroup == currentEditingGroup;
                    var onOffAfter = onOffBefore;

                    GUIStyle toolbarbutton = new GUIStyle("toolbarbutton");

                    if (NodeGUIUtility.platformButtons[i].targetGroup == BuildTargetUtility.DefaultTarget)
                    {
                        onOffAfter = GUILayout.Toggle(onOffBefore, NodeGUIUtility.platformButtons[i].ui, toolbarbutton);
                    }
                    else
                    {
                        var width = Mathf.Max(32f, toolbarbutton.CalcSize(NodeGUIUtility.platformButtons[i].ui).x);
                        onOffAfter = GUILayout.Toggle(onOffBefore, NodeGUIUtility.platformButtons[i].ui, toolbarbutton, GUILayout.Width(width));
                    }

                    if (onOffBefore != onOffAfter)
                    {
                        choosenIndex = i;
                        break;
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    g = NodeGUIUtility.platformButtons[choosenIndex].targetGroup;
                }
            }

            if (g != currentEditingGroup)
            {
                currentEditingGroup = g;
                editGroupChanged = true;
                GUI.FocusControl(string.Empty);
            }

            return editGroupChanged;
        }

        public EditorGUI.DisabledScope DrawOverrideTargetToggle(NodeGUI node, bool status, Action<bool> onStatusChange)
        {
            if (currentEditingGroup == BuildTargetUtility.DefaultTarget)
            {
                return new EditorGUI.DisabledScope(false);
            }

            bool newStatus = GUILayout.Toggle(status,
                "Override for " + NodeGUIUtility.GetPlatformButtonFor(currentEditingGroup).ui.tooltip);

            if (newStatus != status && onStatusChange != null)
            {
                onStatusChange(newStatus);
            }

            return new EditorGUI.DisabledScope(!newStatus);
        }

        public string DrawFolderSelector(string label,
            string dialogTitle,
            string dirTextFieldValue,
            string directoryOpenPath,
            Func<string, string> onValidFolderSelected = null)
        {
            string newDirPath;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (string.IsNullOrEmpty(label))
                {
                    newDirPath = EditorGUILayout.TextField(dirTextFieldValue);
                }
                else
                {
                    newDirPath = EditorGUILayout.TextField(label, dirTextFieldValue);
                }

                if (GUILayout.Button("Select", GUILayout.Width(50f)))
                {
                    var folderSelected =
                        EditorUtility.OpenFolderPanel(dialogTitle, directoryOpenPath, "");
                    if (!string.IsNullOrEmpty(folderSelected))
                    {
                        if (onValidFolderSelected != null)
                        {
                            newDirPath = onValidFolderSelected(folderSelected);
                        }
                        else
                        {
                            newDirPath = folderSelected;
                        }
                    }
                }
            }

            return newDirPath;
        }
    }
}
