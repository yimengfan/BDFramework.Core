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
                if (asset.EndsWith("xlsx") && asset.Contains("Table") && !asset.Contains("~"))
                {
                    excelList.Add(asset);
                }
            }

           
            if (excelList.Count > 0)
            {
                SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath, Application.platform);
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