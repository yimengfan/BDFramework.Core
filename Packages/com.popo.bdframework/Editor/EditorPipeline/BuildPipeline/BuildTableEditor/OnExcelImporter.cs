using System;
using System.Collections.Generic;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using BDFramework.StringEx;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Table
{
    public class OnExcelImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            //判断设置
            var BuildSqlSetting = BDEditorApplication.BDFrameworkEditorSetting?.BuildSqlSetting;
            if (BuildSqlSetting!=null &&!BuildSqlSetting.IsAutoImportSqlWhenExcelChange)
            {
                return;
            }
            //开始导表
            var excelList = new List<string>();
            foreach (var asset in importedAssets)
            {
                if (asset.EndsWith("xlsx", StringComparison.OrdinalIgnoreCase) && asset.Contains("Table",StringComparison.OrdinalIgnoreCase) && !asset.Contains("~"))
                {
                    excelList.Add(asset);
                }
            }

            if (excelList.Count > 0)
            {
                SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                float counter = 1f;
                foreach (var excel in excelList)
                {
                    Excel2SQLiteTools.Excel2SQLite(excel, DBType.Local);
                    EditorUtility.DisplayProgressBar("自动导表", excel, counter / excelList.Count);
                    counter++;
                }
                EditorUtility.ClearProgressBar();
                BDebug.Log("自动导表完成!");
                SqliteLoder.Close();
            }
           
          

        }
    }
}