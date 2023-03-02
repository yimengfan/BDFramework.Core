using System.IO;
using System.Threading;
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
            if (BDEditorApplication.EditorSetting.BuildSqlSetting.IsForceImportChangedExcelOnWillEnterPlaymode)
            {
                Debug.Log("【EditorTask】数据库校验...");
                var dbPath = SqliteLoder.GetLocalDBPath(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                //获取差异
                var (changedExcelList, newEcxcelInfoMap) = ExcelEditorTools.GetChangedExcelsFromLocalSql(dbPath);
                //
                if (changedExcelList.Count > 0)
                {
                    BDebug.Log("-----------------强制导入修改的excel文件.begin-----------------", "red");

                    SqliteLoder.LoadSQLOnEditor(dbPath);
                    {
                        //开始导入
                        foreach (var excel in changedExcelList)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(excel);
                            Excel2SQLiteTools.Excel2SQLite(path, DBType.Local);
                        }
                    }
                    SqliteLoder.Close();
                    
                    Excel2SQLiteTools.CopySqlToOther(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                    BDebug.Log("-----------------强制导入修改的excel文件.end-----------------", "red");

                    ExcelEditorTools.SaveExcelCacheInfo(newEcxcelInfoMap);
                    //db_hash
                    ExcelEditorTools.SaveLocalDBCacheInfo(dbPath);
                }
                // Thread.Sleep(1000);
                // //通过本地存储hash判断是否需要所有导表
                // var lastDBHash = ExcelEditorTools.LoadLocalDBCacheInfo();
                //TODO 这里会有文件占用问题
                // var dbhash = FileHelper.GetMurmurHash3(dbPath);
                // //所有导表
                // if (lastDBHash != dbhash)
                // {
                //     var excelPathList = ExcelEditorTools.GetAllExcelFiles();
                //     SqliteLoder.LoadSQLOnEditor(dbPath);
                //     {
                //         //开始导入
                //         foreach (var excelPath in excelPathList)
                //         {
                //             Excel2SQLiteTools.Excel2SQLite(excelPath, DBType.Local);
                //         }
                //     }
                //     SqliteLoder.Close();
                //
                //     BDebug.Log("-----------------强制导入修改的excel文件.end-----------------", "red");
                //     ExcelEditorTools.SaveLocalDBCacheInfo(dbPath);
                // }
                //保存配置
                Debug.Log("【EditorTask】校验完成!");
            }
        }
    }
}