using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BDFramework.Helper;
using LitJson;
using BDFramework.Sql;
using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.TableData
{
    static public class Excel2SQLiteTools
    {
        
        
        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite", false, (int)BDEditorMenuEnum.BuildPackage_Table_GenSqlite)]
        public static void ExecuteGenSqlite()
        {
            var outpath_win =
                IPath.Combine(Application.streamingAssetsPath, BDUtils.GetPlatformPath(Application.platform));
            Excel2SQLiteTools.GenSQLite(outpath_win);

            var outpath_android = Application.streamingAssetsPath + "/" +
                                  BDUtils.GetPlatformPath(RuntimePlatform.Android) + "/Local.db";
            var outpath_ios = Application.streamingAssetsPath + "/" +
                              BDUtils.GetPlatformPath(RuntimePlatform.IPhonePlayer) + "/Local.db";
            var source = outpath_win + "/Local.db"; 
            if(source != outpath_android)
                File.Copy(source, outpath_android, true);
            if(source != outpath_ios)
                File.Copy(source, outpath_ios, true);

            AssetDatabase.Refresh();
        }
        
        [MenuItem("BDFrameWork工具箱/3.表格/json->生成SQLite", false, (int)BDEditorMenuEnum.BuildPackage_Table_Json2Sqlite)]
        public static void ExecuteJsonToSqlite()
        {
            Excel2SQLiteTools.GenJsonToSQLite(IPath.Combine(Application.streamingAssetsPath,
                BDUtils.GetPlatformPath(Application.platform)));
            Debug.Log("表格导出完毕");
        }
        
        
        public static void GenSQLite(string outPath)
        {
            var tablePath = IPath.Combine(Application.dataPath, "Resource/Table/");
            var xlslFiles = Directory.GetFiles(tablePath, "*.xlsx", SearchOption.AllDirectories);
            //
            if (Directory.Exists(outPath) == false)
            {
                Directory.CreateDirectory(outPath);
            }
            
            var _path = IPath.Combine(outPath, "Local.db");
            //
            sql = new SQLiteService(SqliteLoder.CreateConnetion(_path));
            foreach (var f in xlslFiles)
            {
                var excel = new ExcelUtility(f);
                var json = excel.GetJson();
                Json2Sqlite(f, json);
            }

            sql.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        public static void GenJsonToSQLite(string outPath)
        {
            var tablePath = IPath.Combine(Application.dataPath, "Resource/Table");
            var tableDir = Path.GetDirectoryName(tablePath);
            var jsonFiles = Directory.GetFiles(tableDir, "*.json", SearchOption.AllDirectories);
            var _path = IPath.Combine(outPath, "Local.db");
            sql = new SQLiteService(SqliteLoder.CreateConnetion(_path));
            foreach (var f in jsonFiles)
            {
                string content = File.ReadAllText(f);
                Json2Sqlite(f, content);
            }

            sql.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        //数据库准备
        static private SQLiteService sql;

        public static void GenxslxOrJsonToSQlite(IDictionary<string, string> path)
        {
            var outPath = IPath.Combine(Application.streamingAssetsPath,
                BDUtils.GetPlatformPath(Application.platform));
            var _path = IPath.Combine(outPath, "Local.db");
            sql = new SQLiteService(SqliteLoder.CreateConnetion(_path));
            foreach (var f in path)
            {
                Json2Sqlite(f.Key, f.Value);
            }

            sql.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        public static void GenJsonToSQLite(List<string> paths)
        {
            var outPath = IPath.Combine(Application.streamingAssetsPath,
                BDUtils.GetPlatformPath(Application.platform));
            var _path = IPath.Combine(outPath, "Local.db");
            sql = new SQLiteService(SqliteLoder.CreateConnetion(_path));
            foreach (var f in paths)
            {
                string content = File.ReadAllText(f);

                Json2Sqlite(f, content);
            }

            sql.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        static private void Json2Sqlite(string f, string json)
        {
            //
            var table = Path.GetFileName(f).Replace(Path.GetExtension(f), "");
            var classname = "Game.Data." + table;
            var jsonObj = JsonMapper.ToObject(json);
            var assPath = IPath.Combine(Application.dataPath.Replace("/Assets", ""),
                "Library/ScriptAssemblies/Assembly-CSharp.dll");
            var ass = Assembly.LoadFile(assPath);
            //
            var t = ass.GetType(classname);
            //
            EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", classname, jsonObj.Count),
                0);

            if (t == null)
            {
                Debug.LogError(classname + "类不存在，请检查!");
                return;
            }
            else
            {
                Debug.Log("导出：" + classname);
            }
            //数据库创建表
            //sql.DB.Delete<>()
            sql.Connection.DropTableByType(t);
            //   Debug.Log(t.FullName);
            sql.Connection.CreateTableByType(t);

            EditorUtility.ClearProgressBar();
            //
            for (int i = 0; i < jsonObj.Count; i++)
            {
                var j = jsonObj[i];
                var jo = JsonMapper.ToObject(t, j.ToJson());
                try
                {
                    sql.Connection.Insert(jo);
                }
                catch
                {
                    Debug.LogError("导出数据有错,跳过! 错误位置:" + classname + ":" + i + "-" + jsonObj.Count);
                }
            }

            EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", classname, jsonObj.Count),
                1);
        }

        [MenuItem("Assets/单个json导入到数据库")]
        public static void Json2SqliteQuick()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            path = Path.GetFullPath(path);
            var outPath = IPath.Combine(Application.streamingAssetsPath,
                BDUtils.GetPlatformPath(Application.platform));
            var _path = IPath.Combine(outPath, "Local.db");
            sql = new SQLiteService(SqliteLoder.CreateConnetion(_path));
            string content = File.ReadAllText(path);
            Debug.Log(path);
            Json2Sqlite(path, content);
            sql.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/单个json导入到数据库", true)]
        //当返回真时启用
        private static bool NewMenuOptionValidation()
        {
            if (Selection.activeObject.GetType() != typeof(TextAsset)) return false;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith(".json");
        }
    }
}
