using System.IO;
using BDFramework.Helper;
using UnityEngine;

namespace SQLite4Unity3d
{
    static public class SqliteLoder
    {
        static public SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// 初始化DB
        /// </summary>
        /// <param name="str"></param>
        static public void Init()
        {
            //persistent和streaming中,必须有存在一个
            var path = Path.Combine(Application.persistentDataPath, Utils.GetPlatformPath(Application.platform) + "/LocalDB");
            path = File.Exists(path) ? path :  Path.Combine(Application.streamingAssetsPath, Utils.GetPlatformPath(Application.platform) + "/LocalDB");
            //
            Connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            BDebug.Log("open db:" + path);
        }


        /// <summary>
        /// 创建连接
        /// </summary>
        static public SQLiteConnection CreateConnetion(string path)
        {
            return new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
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