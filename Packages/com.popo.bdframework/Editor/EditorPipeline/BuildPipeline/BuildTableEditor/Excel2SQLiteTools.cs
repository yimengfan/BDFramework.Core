using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.Environment;
using LitJson;
using BDFramework.Sql;
using Telepathy;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Table
{
    public enum DBType
    {
        Local,
        Server,
    }

    /// <summary>
    /// Excel转Sqlite工具
    /// </summary>
    static public class Excel2SQLiteTools
    {
        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite", false,
            (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Table_GenSqlite)]
        public static void ExecuteGenSqlite()
        {
            //生成sql
            BuildAllExcel2SQLite(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
            CopySqlToOther(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
            AssetDatabase.Refresh();
        }

        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite[Server]", false,
            (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Table_Json2Sqlite)]
        public static void ExecuteJsonToSqlite()
        {
            BuildAllExcel2SQLite(BApplication.streamingAssetsPath, BApplication.RuntimePlatform, DBType.Server);
            AssetDatabase.Refresh();
            Debug.Log("表格导出完毕");
        }


        #region Excel2Sql

        /// <summary>
        /// 生成sqlite
        /// 默认导出到当前平台目录下
        /// </summary>
        /// <param name="ouptputPath">自定义路径</param>
        public static bool BuildAllExcel2SQLite(string ouptputPath, RuntimePlatform platform, DBType dbType = DBType.Local, bool isUseCache = false)
        {
            //触发bd环境周期
            BDFrameworkPipelineHelper.OnBeginBuildSqlite();
            //删除旧的，重新创建
            if (!isUseCache)
            {
                if (dbType == DBType.Local)
                {
                    SqliteLoder.DeleteLocalDBFile(ouptputPath, platform);
                }
                else
                {
                    SqliteLoder.DeleteServerDBFile(ouptputPath);
                }
            }

            //清空表日志
            //SqliteHelper.DB.Connection.DropTable<TableLog>();
            //
            var xlslFiles = ExcelEditorTools.GetAllExcelFiles();
            switch (dbType)
            {
                case DBType.Local:
                    SqliteLoder.LoadLocalDBOnEditor(ouptputPath, platform);
                    break;
                case DBType.Server:
                    SqliteLoder.LoadServerDBOnEditor(ouptputPath);
                    break;
            }

            var isSuccess = true;

            foreach (var f in xlslFiles)
            {
                try
                {
                    Excel2SQLite(f, dbType);
                }
                catch (Exception e)
                {
                    Debug.LogError($"导表失败:{f} \n{e}");
                    isSuccess = false;
                }
            }

            //关闭sql
            SqliteLoder.Close();
            //
            EditorUtility.ClearProgressBar();
            //触发bd环境周期
            BDFrameworkPipelineHelper.OnEndBuildSqlite(ouptputPath);

            var version = BDFrameworkPipelineHelper.GetTableSVCNum(platform, ouptputPath);
            ClientAssetsHelper.GenBasePackageBuildInfo(ouptputPath, platform, tableSVC: version);
            Debug.Log("导出Sqlite完成!");


            return isSuccess;
        }

        /// <summary>
        /// excel导出sqlite
        /// 需要主动连接数据库
        /// </summary>
        /// <param name="excelPath"></param>
        public static bool Excel2SQLite(string excelPath, DBType dbType, bool isForce = false)
        {
            BDebug.Log($"导表:{excelPath}", Color.yellow);

            bool isSuccess = false;
            excelPath = IPath.FormatPathOnUnity3d(excelPath);
            //收集所有的类型
            CollectTableTypes();
            var excelHash = FileHelper.GetMurmurHash3(excelPath);
            //table判断
            SqliteHelper.DB.Connection.CreateTable<TableLog>();
            var table = SqliteHelper.DB.GetTable<TableLog>();
            var importLog = table?.Where((ie) => ie.Path == excelPath).FirstOrDefault();
            if (isForce || importLog == null || !importLog.Hash.Equals(excelHash))
            {
                //开始导表
                var excel = new ExcelExchangeTools(excelPath);
                var (ret, jsonArray) = excel.GetJson(dbType);

                if (ret)
                {
                    isSuccess = Json2Sqlite(excelPath, jsonArray);

                    //插入新版本数据
                    if (isSuccess)
                    {
                        var date = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
                        if (importLog == null)
                        {
                            importLog = new TableLog();
                            importLog.Path = excelPath;
                            importLog.Hash = excelHash;
                            importLog.Date = date;
                            importLog.UnityVersion = Application.unityVersion;
                            SqliteHelper.DB.Insert(importLog);
                        }
                        else
                        {
                            importLog.Hash = excelHash;
                            importLog.Date = date;
                            importLog.UnityVersion = Application.unityVersion;
                            SqliteHelper.DB.Connection.Update(importLog);
                        }
                    }
                }
            }
            else
            {
                isSuccess = true;
                Debug.Log(
                    $"<color=green>【Excel2Sql】内容一致,无需导入 {Path.GetFileName(excelPath)} - Hash :{excelHash} </color>");
            }

            if (!isSuccess)
            {
                BDebug.LogError("注意报错信息，如果提示“SQLiteException: file is not a database”，可能是密码不对!");
            }

            return isSuccess;
        }

        #endregion

        static private Type[] TABLE_TYPES = null;

        /// <summary>
        /// 搜集table类型
        /// </summary>
        static public void CollectTableTypes()
        {
            if (TABLE_TYPES != null)
            {
                return;
            }

            List<Type> retTypeList = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                //只搜集editor
                foreach (var type in types)
                {
                    //
                    if (type != null && type.IsClass && type.Namespace != null &&
                        type.Namespace.StartsWith("Game.Data."))
                    {
                        retTypeList.Add(type);
                    }
                }
            }

            TABLE_TYPES = retTypeList.ToArray();
        }

        #region Json2Sql

        /// <summary>
        /// json转sql
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="jsonContent"></param>
        static private bool Json2Sqlite(string filePath, string jsonContent)
        {
            var table = Path.GetFileName(filePath).Replace(Path.GetExtension(filePath), "");
            var jsonObj = JsonMapper.ToObject(jsonContent);
            var dbname = Path.GetFileNameWithoutExtension(SqliteHelper.DB.DBPath);
            var @namespace = "Game.Data." + dbname + ".";
            var type = TABLE_TYPES.FirstOrDefault((t) =>
                t.FullName.ToLower().StartsWith(@namespace.ToLower()) && t.Name.ToLower() == table.ToLower());
            if (type == null)
            {
                type = TABLE_TYPES.FirstOrDefault((t) =>
                    t.FullName.ToLower().StartsWith("Game.Data.".ToLower()) && t.Name.ToLower() == table.ToLower());
            }

            if (type == null)
            {
                Debug.LogError(table + "类不存在，请检查!");
                return false;
            }

            //
            EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", type.FullName, jsonObj.Count), 1);
            Debug.LogFormat("导出 [{0}]:{1}", dbname, filePath.Replace(Application.dataPath + "\\", "") + "=>【" + type.FullName + "】");
            //数据库创建表
            bool ret = true;
            SqliteHelper.DB.CreateTable(type);
            for (int i = 0; i < jsonObj.Count; i++)
            {
                var json = jsonObj[i].ToJson();
                try
                {
                    var jobj = JsonMapper.ToObject(type, json);
                    SqliteHelper.DB.Insert(jobj);
                }
                catch (Exception e)
                {
                    ret = false;
                    BDebug.Log(json);
                    Debug.LogError($"导出数据有错,跳过!字段:{type.Name} 行号: {i}/{jsonObj.Count} \n{e}");
                }
            }

            //回调通知
            BDFrameworkPipelineHelper.OnExportExcel(type);
            //
            EditorUtility.ClearProgressBar();
            // EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", type.Name, jsonObj.Count), 1);

            return ret;
        }

        #endregion

        /// <summary>
        /// 拷贝当前到其他目录
        /// </summary>
        /// <param name="sourceh"></param>
        public static void CopySqlToOther(string root, RuntimePlatform sourcePlatform)
        {
            var target = SqliteLoder.GetLocalDBPath(root, sourcePlatform);
            var bytes = File.ReadAllBytes(target);
            //拷贝当前到其他目录
            foreach (var p in BApplication.SupportPlatform)
            {
                var outpath = SqliteLoder.GetLocalDBPath(root, p);
                if (target == outpath)
                {
                    continue;
                }

                FileHelper.WriteAllBytes(outpath, bytes);
            }
        }


        #region 按钮 Excel2Sqlite

        //当返回真时启用
        [MenuItem("Assets/BDFramework工具箱/ExcelTools/Excel导入到数据库", true)]
        private static bool MenuItem_Excel2SqliteValidation()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.ToLower().EndsWith(".xlsx");
        }

        [MenuItem("Assets/BDFramework工具箱/ExcelTools/Excel导入到数据库")]
        public static void MenuItem_Excel2Sqlite()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            bool isSuccess = false;
            SqliteLoder.LoadLocalDBOnEditor(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
            {
                isSuccess = Excel2SQLite(path, DBType.Local, true);
            }
            SqliteLoder.Close();
            EditorUtility.ClearProgressBar();
            //
            if (isSuccess)
            {
                EditorUtility.DisplayDialog("提示", Path.GetFileName(path) + "\n恭喜你又成功导入一次表格吶~\n呐!呐!呐!呐! ", "OK");
            }

            AssetDatabase.Refresh();
        }

        #endregion
    }
}
