using BDFramework.Editor.Tools;
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
                //3.打包表格
                Excel2SQLiteTools.GenExcel2SQLite(Application.streamingAssetsPath,Application.platform);
                Excel2SQLiteTools.CopySqlToOther(Application.streamingAssetsPath,Application.platform);
            }

            GUILayout.EndVertical();
        }
    }
}