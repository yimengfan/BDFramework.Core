using System.Collections;
using System.Collections.Generic;
using BDFramework.Editor;
using BDFramework.Editor.Tools;
using BDFramework.Helper;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.TableData
{
    public class EditorWindow_Table : EditorWindow
    {
        public void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("3.表格打包", EditorGUIHelper.TitleStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("表格导出成Sqlite", GUILayout.Width(300), GUILayout.Height(30)))
            {
                var outPath = Application.persistentDataPath + "/" + BDUtils.GetPlatformPath(RuntimePlatform.Android);
                //3.打包表格
                Excel2SQLiteTools.GenSQLite(outPath);
            }

            GUILayout.EndVertical();
        }
    }
}