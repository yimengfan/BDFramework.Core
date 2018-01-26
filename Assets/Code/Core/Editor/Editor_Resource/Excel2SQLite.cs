using  System.IO;
using System.Reflection;
using LitJson;
using  UnityEditor;
using  UnityEngine;
using Game.Data;
namespace BDFramework.Editor
{
   
   static public class Excel2SQLite
    {
        public static void GenSQLite()
        {
            var tablePath = Path.Combine(Application.dataPath, FrameDataHelper.FrameWorkSetting.EditorTablePath);
            var tableDir = Path.GetDirectoryName(tablePath);
            var xlslFiles = Directory.GetFiles(tableDir, "*.xlsx", SearchOption.AllDirectories);

            foreach (var f in xlslFiles)
            {
                var excel = new ExcelUtility(f);
                var json = excel.GetJson();
                Json2Sqlite(f, json);
            }

            AssetDatabase.Refresh();
        }
        
        //数据库准备
       static private SQLiteService sql;
        static  private void Json2Sqlite(string f, string json)
        {
            //
            sql =  new SQLiteService("LocalDB");
            
            //
            var table = Path.GetFileName(f).ToLower().Replace(".xlsx", "");
            var classname = "Game.Data." + table;
            var jsonObj = JsonMapper.ToObject(json);

            var assPath = Path.Combine(Application.dataPath.Replace("Assets", "") , "Library/ScriptAssemblies/Assembly-CSharp.dll");
            var ass = Assembly.LoadFile("file:///" + assPath);
            //
            var t = ass.GetType(classname);
            
            
            //数据库创建表
           // sql.DB.Delete<>()
            sql.DB.DropTableByType(t);
            sql.DB.CreateTableByType(t);
            
            EditorUtility.ClearProgressBar();
            //
            for (int i = 0; i < jsonObj.Count; i++)
            {
                var j = jsonObj[i];
                var jo = JsonMapper.ToObject(t,j.ToJson());
               EditorUtility.DisplayProgressBar("Excel2Sqlite" , "正在导出:"+ i + "-" + jsonObj.Count , i / jsonObj.Count );
                sql.DB.Insert(jo);
                
            }
            

            TestSql();
            //
            sql.Close();

            EditorUtility.DisplayDialog("提示" , "导出Sqlite完成!" ,"确定");
        }

        static private void TestSql()
        {
            //测试查询
            var result = sql.DB.Table<achievetable>().Where(a => a.Id == 607).First();
            
            Debug.Log(result.IconPath[0]);
        }
    }
    

}

