using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BDFramework.Helper;
using LitJson;
using BDFramework.Sql;
using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    static public class Excel2SQLiteTools
    {
        public static void GenSQLite(string outPath)
        {
            var tablePath = IPath.Combine(Application.dataPath, "Resource/Table");
            var tableDir = Path.GetDirectoryName(tablePath);
            var xlslFiles = Directory.GetFiles(tableDir, "*.xlsx", SearchOption.AllDirectories);
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
            var _path = IPath.Combine(outPath,  "Local.db");
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

        public static void GenxslxOrJsonToSQlite(IDictionary<string ,string> path)
        {
            var outPath = IPath.Combine(Application.streamingAssetsPath,
                Utils.GetPlatformPath(Application.platform));
            var _path = IPath.Combine(outPath,  "Local.db");
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
                Utils.GetPlatformPath(Application.platform));
            var _path = IPath.Combine(outPath,  "Local.db");
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
//            Debug.Log("class name：" + classname);
            var jsonObj = JsonMapper.ToObject(json);

            var assPath = IPath.Combine(Application.dataPath.Replace("Assets", ""),
                "Library/ScriptAssemblies/Assembly-CSharp.dll");
            var ass = Assembly.LoadFile("file:///" + assPath);
            //
            var t = ass.GetType(classname);


            if (t == null)
            {
                Debug.LogError(classname + "类不存在，请检查!");
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
                EditorUtility.DisplayProgressBar("Excel2Sqlite", "正在导出:" + classname + " " + i + "-" + jsonObj.Count,
                    i / jsonObj.Count);
                try
                {
                    sql.Connection.Insert(jo);
                }
                catch
                {
                    Debug.LogError("导出数据有错,跳过! 错误位置:" + classname + ":" + i + "-" + jsonObj.Count);
                }
            }
        }
    }
}