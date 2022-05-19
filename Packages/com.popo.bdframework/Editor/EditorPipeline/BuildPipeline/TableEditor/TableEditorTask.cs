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
            if (BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildSetting.IsForceImportChangedExcelOnWillEnterPlaymode)
            {
                var (changedExcelList,newEcxcelInfoMap )= ExcelEditorTools.GetChangedExcels();
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
                }
                //保存配置
                ExcelEditorTools.SaveExcelCacheInfo(newEcxcelInfoMap);
            }
        }
    }
}
