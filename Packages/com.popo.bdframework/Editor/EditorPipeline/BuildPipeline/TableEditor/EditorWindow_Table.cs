using System;
using BDFramework.Editor.Tools;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Table
{
    public class EditorWindow_Table : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/3.表格/表格预览", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Table_GenSqlite - 1)]
        public static void Open()
        {
            var win = EditorWindow.GetWindow<EditorWindow_Table>();
            win.Show();
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("3.表格打包", EditorGUIHelper.TitleStyle);
            GUILayout.Space(5);
            if (GUILayout.Button("表格导出成Sqlite", GUILayout.Width(300), GUILayout.Height(30)))
            {
                //3.打包表格
                Excel2SQLiteTools.AllExcel2SQLite(Application.streamingAssetsPath, Application.platform);
                Excel2SQLiteTools.CopySqlToOther(Application.streamingAssetsPath, Application.platform);
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 重写
        /// </summary>
        new public void Show()
        {
            //计算hash
            var (hash, hashmap) = ExcelEditorTools.GetExcelsHash();
            Debug.Log(hash);
            Debug.Log(JsonMapper.ToJson(hashmap, true));
            //获取差异文件
            var changeExcelList = ExcelEditorTools.GetChangedExcels();

            //保存
            if (changeExcelList.Count > 0)
            {

                for (int i = 0; i < changeExcelList.Count; i++)
                {
                    changeExcelList[i] = AssetDatabase.GUIDToAssetPath(changeExcelList[i]);
                }
                
                Debug.Log("变动的Excel文件:" + JsonMapper.ToJson(changeExcelList, true));
                ExcelEditorTools.SaveExcelCacheInfo(hashmap);
            }
            else
            {
                Debug.Log("无变动的文件:" + JsonMapper.ToJson(changeExcelList, true));
            }

            //显示
            base.Show();
        }
    }
}
