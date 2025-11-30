using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

[InitializeOnLoad]
public class Editor_ToolsBar 
{
    static Editor_ToolsBar()
    {
        if (!Application.isPlaying)
        {
            ToolbarExtender.RightToolbarGUI.Clear();
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
            
        }
    }

    private static void OnToolbarGUI()
    {
        GUI.color= Color.yellow;
        if (GUILayout.Button("StartScene" ,GUILayout.Width(80)))
        {
            var path = AssetDatabase.GUIDToAssetPath("8230003217d1e554da76c9d6d864ded0");
            Debug.Log("open:" +path);
            EditorSceneManager.OpenScene(path);
        }
        GUI.color = GUI.backgroundColor;
    }
}
