using System.IO;
using BDFramework.Helper;
using SQLite4Unity3d;
using UnityEngine;
using Path = System.IO.Path;

namespace BDFramework.Sql
{
   static public class SqliteHelper
   {
       /// <summary>
       /// 静态构造初始化
       /// </summary>
       static SqliteHelper()
       {
           if (DB == null)
           {
               DB = new SQLiteService(SqliteLoder.Connection);
           } 
       }   
       
       //现在是热更层不负责加载,只负责使用
       static public SQLiteService DB
       {
           get;
           private set;
       }

       
    }
}