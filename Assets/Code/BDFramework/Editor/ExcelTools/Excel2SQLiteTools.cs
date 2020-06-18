using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using BDFramework.Helper;
using LitJson;
using BDFramework.Sql;
using Code.BDFramework.Core.Tools;
using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.TableData
{
    static public class Excel2SQLiteTools
    {
        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite", false, (int) BDEditorMenuEnum.BuildPackage_Table_GenSqlite)]
        public static void ExecuteGenSqlite()
        {
            //生成sql
            GenExcel2SQLite(Application.streamingAssetsPath, Application.platform);
            CopySqlToOther(Application.streamingAssetsPath, Application.platform);
        }

        [MenuItem("BDFrameWork工具箱/3.表格/json->生成SQLite", false, (int) BDEditorMenuEnum.BuildPackage_Table_Json2Sqlite)]
        public static void ExecuteJsonToSqlite()
        {
            GenJson2SQLite();
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

                Debug.Log("寻找到table目录：" + dir);
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

        /// <summary>
        /// 生成sqlite
        /// 默认导出到当前平台目录下
        /// </summary>
        /// <param name="root">自定义路径</param>
        public static void GenExcel2SQLite(string root, RuntimePlatform platform)
        {
            var xlslFiles = GetAllConfigFiles();

            //
            SqliteLoder.LoadOnEditor(root, platform);
            {
                foreach (var f in xlslFiles)
                {
                    var excel = new ExcelUtility(f);
                    var json  = excel.GetJson();
                    JsonContent2Sqlite(f, json);
                }
            }
            SqliteLoder.Close();
            //
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 进在Appli
        /// 拷贝当前到其他目录
        /// </summary>
        /// <param name="sourceh"></param>
        public static void CopySqlToOther(string root, RuntimePlatform sourcePlatform)
        {
            RuntimePlatform[] ps = new RuntimePlatform[]
            {
                RuntimePlatform.WindowsPlayer,
                RuntimePlatform.IPhonePlayer,
                RuntimePlatform.Android
            };

            var target = SqliteLoder.GetDBPath(root, sourcePlatform);
            var bytes  = File.ReadAllBytes(target);
            //拷贝当前到其他目录
            foreach (var p in ps)
            {
                if (p == sourcePlatform) continue;
                var outpath = SqliteLoder.GetDBPath(root, p);
                FileHelper.WriteAllBytes(outpath, bytes);
            }


            //刷新
            AssetDatabase.Refresh();
        }


        /// <summary>
        /// 导入自定义内容到 excel
        /// 默认为Application.streamingAssetsPath
        /// </summary>
        /// <param name="path"></param>
        public static void GenxslxOrJsonToSQlite(IDictionary<string, string> path)
        {
            SqliteLoder.LoadOnEditor(Application.streamingAssetsPath, Application.platform);
            {
                foreach (var f in path)
                {
                    JsonContent2Sqlite(f.Key, f.Value);
                }
            }
            SqliteLoder.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 导入 json 到excel
        /// 默认为Application.streamingAssetsPath
        /// </summary>
        /// <param name="paths"></param>
        public static void GenJsonToSQLite(List<string> paths)
        {
            SqliteLoder.LoadOnEditor(Application.streamingAssetsPath, Application.platform);
            {
                foreach (var f in paths)
                {
                    string content = File.ReadAllText(f);

                    JsonContent2Sqlite(f, content);
                }
            }
            SqliteLoder.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }


        public static void GenJson2SQLite()
        {
            var jsonFiles = Excel2SQLiteTools.GetAllConfigFiles("*.json");
            //连接数据库
            SqliteLoder.LoadOnEditor(Application.streamingAssetsPath, Application.platform);
            {
                foreach (var f in jsonFiles)
                {
                    string content = File.ReadAllText(f);
                    JsonContent2Sqlite(f, content);
                }
            }
            SqliteLoder.Close();
            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }


        /// <summary>
        /// json转sql
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="json"></param>
        static public void JsonContent2Sqlite(string filePath, string json)
        {
            var table   = Path.GetFileName(filePath).Replace(Path.GetExtension(filePath), "");
            var jsonObj = JsonMapper.ToObject(json);
            var type = typeof(BDLauncher).Assembly.GetTypes()
                .First((t) => t.FullName.StartsWith("Game.Data.") && t.Name.ToLower() == table.ToLower());
            //
            if (type == null)
            {
                Debug.LogError(table + "类不存在，请检查!");
                return;
            }

            EditorUtility.DisplayProgressBar("Excel2Sqlite",
                                             string.Format("生成：{0} 记录条目:{1}", type.FullName, jsonObj.Count), 0);
            Debug.Log("导出：" + filePath.Replace(Application.dataPath + "\\", "") + "=>【" + type.FullName + "】");

            //数据库创建表
            SqliteHelper.DB.CreateDB(type);

            //
            //
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
                    Debug.LogError("导出数据有错,跳过! 错误位置:" + type.Name + ":" + i + "-" + jsonObj.Count);
                }
            }

            EditorUtility.DisplayProgressBar("Excel2Sqlite", string.Format("生成：{0} 记录条目:{1}", type.Name, jsonObj.Count),
                                             1);
        }


        [MenuItem("Assets/单个json导入到数据库", true)]
        //当返回真时启用
        private static bool NewMenuOptionValidation()
        {
            if (Selection.activeObject && Selection.activeObject.GetType() != typeof(TextAsset))
                return false;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith(".json");
        }

        [MenuItem("Assets/单个json导入到数据库")]
        public static void Json2SqliteQuick()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            path = Path.GetFullPath(path);

            SqliteLoder.LoadOnEditor(Application.streamingAssetsPath, Application.platform);
            {
                string content = File.ReadAllText(path);
                Debug.Log(path);
                JsonContent2Sqlite(path, content);
            }
            SqliteLoder.Close();

            EditorUtility.ClearProgressBar();
            Debug.Log("导出Sqlite完成!");
            AssetDatabase.Refresh();
        }
    }
}