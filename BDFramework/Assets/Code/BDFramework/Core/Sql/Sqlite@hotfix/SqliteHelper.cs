using SQLite4Unity3d;
using UnityEngine;
using Path = System.IO.Path;

namespace BDFramework.Sql
{
   static public class SqliteHelper
   {
       static public SQLiteService DB
       {
           get;
           private set;
       }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="str"></param>
       static public void InitDB(string str)
       {
           
       }
      
       /// <summary>
       /// 创建db
       /// </summary>
       /// <param name="pathme"></param>
       static public void CreateDB(string path)
       {
           var _db = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, false);
           _db.Dispose();
       }
         
    }
}