

using UnityEngine;

namespace BDFramework.Sql
{
    /// <summary>
    /// 这里主要是为了和主工程隔离
    /// hotfix专用的Sql Helper
    /// </summary>
   static public class SqliteHelper
   {
       
       //
       static private SQLiteService dbservice;
       
       //现在是热更层不负责加载,只负责使用
       static public SQLiteService DB
       {
           get
           {
               if (dbservice == null|| dbservice.IsClose)
               {
                   dbservice = new SQLiteService(SqliteLoder.Connection);
                   if (dbservice == null)
                   {
                       BDebug.LogError("Sql加载失败，请检查!");
                   }
               }

               return dbservice;
           }
       }

       
    }
}