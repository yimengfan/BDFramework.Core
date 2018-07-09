using SQLite4Unity3d;
using UnityEngine;
using Path = System.IO.Path;

namespace Game.Data
{
   static public class SqliteHelper
   {

        static public SQLiteService DB 
        {
            get
            {
                if (db == null)
                {
                    db = new SQLiteService("LocalDB");
                }

                return db;
            }
        }

       static private SQLiteService db;

       /// <summary>
       /// 创建db
       /// </summary>
       /// <param name="dbName"></param>
       static public void CreateDBInAsset(string dbName)
       {
           var path = Path.Combine(Application.streamingAssetsPath, dbName);
           var _db = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, false);
           _db.Dispose();
       }
       
       
       static string  CheackPath()
       {
          string str = "";
#if UNITY_EDITOR
            
#endif
         return str;
       }
        
        
    }
}