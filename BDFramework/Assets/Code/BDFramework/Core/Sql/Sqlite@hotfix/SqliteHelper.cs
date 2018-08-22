using SQLite4Unity3d;
using UnityEngine;
using Path = System.IO.Path;

namespace BDFramework.Sql
{
   static public class SqliteHelper
   {

       static SqliteHelper()
       {
           if (DB == null)
           {
               DB = new SQLiteService("LocalDB");
           }
       }

       static public SQLiteService DB
       {
           get;
           private set;
       }
       
      
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
         
    }
}