using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.Task;
using BDFramework.Sql;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Table
{
    /// <summary>
    /// 编辑器任务
    /// </summary>
    public class TableEditorTask
    {
        /// <summary>
        /// 强制导入改变的excel
        /// </summary>
        [EditorTask.EditorTaskOnWillEnterPlaymodeAttribute("强制导入修改的Excel")]
        static public void OnForceImpotChangedExcel()
        {
            //判断是否导入设置
            if (BDEditorApplication.BDFrameworkEditorSetting.BuildSqlSetting
                .IsForceImportChangedExcelOnWillEnterPlaymode)
            {
                var dbPath = SqliteLoder.GetLocalDBPath(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                var (changedExcelList, newEcxcelInfoMap) = ExcelEditorTools.GetChangedExcels();
                if (changedExcelList.Count > 0)
                {
                    BDebug.Log("-----------------强制导入修改的excel文件.begin-----------------", "red");

                    SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                    {
                        //开始导入
                        foreach (var excel in changedExcelList)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(excel);
                            Excel2SQLiteTools.Excel2SQLite(path, DBType.Local);
                        }
                    }
                    SqliteLoder.Close();
                    BDebug.Log("-----------------强制导入修改的excel文件.end-----------------", "red");

                    ExcelEditorTools.SaveExcelCacheInfo(newEcxcelInfoMap);
                    //db_hash
                    ExcelEditorTools.SaveLocalDBCacheInfo(dbPath);
                }
                //通过本地存储hash判断是否需要所有导表
                var hash = ExcelEditorTools.LoadLocalDBCacheInfo();
                var curhash = FileHelper.GetMurmurHash3(dbPath);
                //所有导表
                if (hash != curhash)
                {
                    var excelPathList = ExcelEditorTools.GetAllExcelFiles();

                    SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                    {
                        //开始导入
                        foreach (var excelPath in excelPathList)
                        {
                            Excel2SQLiteTools.Excel2SQLite(excelPath, DBType.Local);
                        }
                    }
                    SqliteLoder.Close();

                    BDebug.Log("-----------------强制导入修改的excel文件.end-----------------", "red");
                    ExcelEditorTools.SaveLocalDBCacheInfo(dbPath);
                }
                //保存配置
            }
        }
    }
}