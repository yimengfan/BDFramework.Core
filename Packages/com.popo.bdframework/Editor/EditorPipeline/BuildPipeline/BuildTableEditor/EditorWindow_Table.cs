using System;
using BDFramework.Core.Tools;
using BDFramework.Editor.Tools;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Table
{
    public class EditorWindow_Table : EditorWindow
    {
        public void OnGUI()
        {
            if (BDEditorApplication.EditorSetting == null)
            {
                return;
            }
            var BuildSqlSetting = BDEditorApplication.EditorSetting.BuildSqlSetting;
            GUILayout.BeginVertical();
            GUILayout.Label("3.表格打包", EditorGUIHelper.LabelH2);
            GUILayout.Space(5);
            if (GUILayout.Button("表格导出成Sqlite", GUILayout.Width(300), GUILayout.Height(30)))
            {
                //3.打包表格
                Excel2SQLiteTools.BuildAllExcel2SQLite(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
                Excel2SQLiteTools.CopySqlToOther(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
            }

            GUILayout.Space(10);
            if (BuildSqlSetting != null)
            {
                BuildSqlSetting.IsForceImportChangedExcelOnWillEnterPlaymode = EditorGUILayout.Toggle("PlayMode强制导表", BuildSqlSetting.IsForceImportChangedExcelOnWillEnterPlaymode);
                BuildSqlSetting.IsAutoImportSqlWhenExcelChange = EditorGUILayout.Toggle("Excel修改自动导表", BuildSqlSetting.IsAutoImportSqlWhenExcelChange);
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
            Debug.Log("表格hash预览:"+JsonMapper.ToJson(hashmap, true));
            // //获取差异文件
            // var changeExcelList = ExcelEditorTools.GetChangedExcels();
            //
            // //保存
            // if (changeExcelList.Count > 0)
            // {
            //
            //     for (int i = 0; i < changeExcelList.Count; i++)
            //     {
            //         changeExcelList[i] = AssetDatabase.GUIDToAssetPath(changeExcelList[i]);
            //     }
            //     
            //     Debug.Log("变动的Excel文件:" + JsonMapper.ToJson(changeExcelList, true));
            //     ExcelEditorTools.SaveExcelCacheInfo(hashmap);
            // }
            // else
            // {
            //     Debug.Log("无变动的文件:" + JsonMapper.ToJson(changeExcelList, true));
            // }

            //显示
            base.Show();
        }


        private void OnDisable()
        {
            BDEditorApplication.EditorSetting.Save();
        }
    }
}
