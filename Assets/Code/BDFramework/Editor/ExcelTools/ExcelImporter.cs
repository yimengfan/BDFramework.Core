using System.Collections.Generic;
using BDFramework.Sql;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.TableData
{
    public class ExcelImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var excelList = new List<string>();
            foreach (var asset in importedAssets)
            {
                if (asset.EndsWith("xlsx") && asset.Contains("Table"))
                {
                    excelList.Add(asset);
                }
            }

            SqliteLoder.LoadOnEditor(Application.streamingAssetsPath, Application.platform);
            if (excelList.Count > 0)
            {
                
                float counter = 1f;
                foreach (var excel in excelList)
                    Excel2SQLiteTools.Excel2SQLite(excel);
                    EditorUtility.DisplayProgressBar("自动导表", excel, counter / excelList.Count);
                    counter++;
                }
                EditorUtility.ClearProgressBar();
            }
            SqliteLoder.Close();
           Debug.Log("自动导表完成!");

        }
    }
}