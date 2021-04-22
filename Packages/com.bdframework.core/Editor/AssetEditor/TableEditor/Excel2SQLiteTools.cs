using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Editor.EditorLife;
using LitJson;
using BDFramework.Sql;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.TableData
{
    public enum DBType
    {
      Local,
      Server,
    }
    static public class Excel2SQLiteTools
    {

        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite", false, (int) BDEditorMenuEnum.BuildPackage_Table_GenSqlite)]
        public static void ExecuteGenSqlite()
        {
            //生成sql
            AllExcel2SQLite(Application.streamingAssetsPath, Application.platform);
            CopySqlToOther(Application.streamingAssetsPath, Application.platform);
            AssetDatabase.Refresh();
        }

        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite[Server]", false, (int) BDEditorMenuEnum.BuildPackage_Table_Json2Sqlite)]
        public static void ExecuteJsonToSqlite()
        {
            AllExcel2SQLite(Application.streamingAssetsPath, Application.platform, DBType.Server);
            AssetDatabase.Refresh();
            Debug.Log("表格导出完毕");
        }

        /// <summary>
        /// 获取所有的xlsx文件
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllConfigFiles(string filetype = "*.xlsx")
        {
            List<string> tableRoot = new List<string>();
            foreach (var p in Directory.GetDirectories(Application.dataPath, "*", SearchOption.TopDirectoryOnly))
            {
                var dir = p + "/Table";
                if (!Directory.Exists(dir)) continue;

                tableRoot.Add(dir);
            }

            //table发现
            List<string> xlslFiles = new List<string>();
            foreach (var r in tableRoot)
            {
                var fs = Directory.GetFiles(r, filetype, SearchOption.AllDirectories);
                xlslFiles.AddRange(fs);
            }

            return xlslFiles;
        }

        #region Excel2Sql

        /// <summary>
        /// 生成sqlite
        /// 默认导出到当前平台目录下
        /// </summary>
        /// <param name="ouptputPath">自定义路径</param>
        public static void AllExcel2SQLite(string ouptputPath, RuntimePlatform platform,DBType dbType = DBType.Local )
        {
            //触发bd环境周期
            BDFrameEditorBehaviorHelper.OnBeginBuildSqlite();
            var xlslFiles = GetAllConfigFiles();
            switch (dbType)
            {
                case DBType.Local:
                    SqliteLoder.LoadLocalDBOnEditor(ouptputPath, platform);
                    break;
                case DBType.Server:
                    SqliteLoder.LoadServerDBOnEditor(ouptputPath, platform);
                    break;
            }
            {
                foreach (var f in xlslFiles)
                {
                    try
                    {
                        Excel2SQLite(f,dbType);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("导表失败:" + f);
                        EditorUtility.ClearProgressBar();
                    }
                }
            }
            SqliteLoder.Close();
            //
            EditorUtility.ClearProgressBar();
            //触发bd环境周期
            BDFrameEditorBehaviorHelper.OnEndBuildSqlite(ouptputPath);
            AssetHelper.AssetHelper.GenPackageBuildInfo(ouptputPath, platform);
            Debug.Log("导出Sqlite完成!");
        }

        /// <summary>
        /// excel导出sqlite
        /// 需要主动连接数据库
        /// </summary>
        /// <param name="filePath"></param>
        public static void Excel2SQLite(string filePath,DBType dbType)
        {
            var excel = new ExcelUtility(filePath);
            var json  = excel.GetJson(dbType);
            Json2Sqlite(filePath, json);
        }

        #endregion

        #region Json2Sql

        /// <summary>
        /// json转sql
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="jsonContent"></param>
        static public void Json2Sqlite(string filePath, string jsonContent)
        {
            var table      = Path.GetFileName(filePath).Replace(Path.GetExtension(filePath), "");
            var jsonObj    = JsonMapper.ToObject(jsonContent);
            var dbname     = Path.GetFileNameWithoutExtension(SqliteHelper.DB.DBPath);
            var @namespace = "Game.Data." + dbname + ".";
            var type       = BDFrameEditorLife.Types.FirstOrDefault((t) => t.FullName.StartsWith(@namespace) && t.Name.ToLower() == table.ToLower());
            if (type == null)
            {
                Debug.LogError(table + "类不存在，请检查!");
                return;
            }
            //
            EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", type.FullName, jsonObj.Count), 0);
            Debug.LogFormat("导出 [{0}]:{1}"  ,dbname, filePath.Replace(Application.dataPath + "\\", "") + "=>【" + type.FullName + "】");
            //数据库创建表
            SqliteHelper.DB.CreateTable(type);
            for (int i = 0; i < jsonObj.Count; i++)
            {
                var _json = jsonObj[i].ToJson();
                var jo    = JsonMapper.ToObject(type, _json);
                try
                {
                    SqliteHelper.DB.Insert(jo);
                }
                catch
                {
                    Debug.LogError("导出数据有错,跳过! 错误位置:" + type.Name + ":" + i + "/" + jsonObj.Count);
                }
            }

            EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", type.Name, jsonObj.Count), 1);
        }

        #endregion

        /// <summary>
        /// 拷贝当前到其他目录
        /// </summary>
        /// <param name="sourceh"></param>
        public static void CopySqlToOther(string root, RuntimePlatform sourcePlatform)
        {
            RuntimePlatform[] ps = new RuntimePlatform[] {RuntimePlatform.WindowsEditor, RuntimePlatform.IPhonePlayer, RuntimePlatform.Android};

            var target = SqliteLoder.GetLocalDBPath(root, sourcePlatform);
            var bytes  = File.ReadAllBytes(target);
            //拷贝当前到其他目录

            foreach (var p in ps)
            {
                var outpath = SqliteLoder.GetLocalDBPath(root, p);
                if (target == outpath) continue;
                FileHelper.WriteAllBytes(outpath, bytes);
            }
        }



        #region 按钮 Excel2Sqlite

        //当返回真时启用
        [MenuItem("Assets/Excel导入到数据库", true)]
        private static bool MenuItem_Excel2SqliteValidation()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.ToLower().EndsWith(".xlsx");
        }

        [MenuItem("Assets/Excel导入到数据库")]
        public static void MenuItem_Excel2Sqlite()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            path = Path.GetFullPath(path);

            SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath, Application.platform);
            {
                Excel2SQLite(path, DBType.Local);
            }
            SqliteLoder.Close();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("提示", Path.GetFileName(path) + "\n恭喜你又成功导入一次表格吶~\n呐!呐!呐!呐! ", "OK");
            AssetDatabase.Refresh();
        }

        #endregion
    }
}