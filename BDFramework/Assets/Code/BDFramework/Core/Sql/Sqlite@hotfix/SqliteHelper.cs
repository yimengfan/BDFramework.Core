using System.IO;
using BDFramework.Helper;
using SQLite4Unity3d;
using UnityEngine;
using Path = System.IO.Path;

namespace BDFramework.Sql
{
   static public class SqliteHelper
   {
       static private SQLiteService db;
       //现在是热更层不负责加载,只负责使用
       static public SQLiteService DB
       {
           get
           {
               return db;
           }
       }

       /// <summary>
       /// 初始化
       /// </summary>
       static public void Init()
       {
           if (db == null)
           {
               db = new SQLiteService(SqliteLoder.Connection);
           } 
       }   
    }
}