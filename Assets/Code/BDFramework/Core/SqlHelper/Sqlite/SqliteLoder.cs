using System;
using System.Collections;
using System.IO;
using BDFramework.Helper;
using UnityEngine;
using BDFramework;

namespace SQLite4Unity3d
{
    static public class SqliteLoder
    {
        static public SQLiteConnection Connection { get; private set; }
        private static string firstPath = "";

        /// <summary>
        /// 初始化DB
        /// </summary>
        /// <param name="str"></param>
        static public void Load(string root)
        {
            if (Connection != null)
            {
                Connection.Dispose();
            }

            //persistent和streaming中,必须有存在一个
            firstPath = IPath.Combine(root, Utils.GetPlatformPath(Application.platform) + "/Local.db");
            if (!File.Exists(firstPath))
            {
                //这里sqlite 如果不在firstPath下，就到Streamming下面拷贝到第一路径
                IEnumeratorTool.StartCoroutine(IE_LoadSqlite());
            }
            else
            {
                Connection = new SQLiteConnection(firstPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
                BDebug.Log("DB加载路径:" + firstPath, "red");
            }
        }


        /// <summary>
        /// 异步拷贝,加载
        /// </summary>
        /// <returns></returns>
        static private IEnumerator IE_LoadSqlite()
        {
            //StreamAsset 默认有前缀
            var secPath = IPath.Combine(Application.streamingAssetsPath,
                Utils.GetPlatformPath(Application.platform) + "/Local.db");
            if (Application.isEditor)
            {
                secPath = "file://" + secPath;
            }
            WWW www = new WWW(secPath);

            yield return www;

            if (www.isDone && www.error == null)
            {
                var direct = Path.GetDirectoryName(firstPath);
                if (!Directory.Exists(direct))
                {
                    Directory.CreateDirectory(direct);
                }
                //
                File.WriteAllBytes(firstPath, www.bytes);
                BDebug.Log("DB加载路径:" + firstPath, "red");
                Connection = new SQLiteConnection(firstPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            }
            else
            {
                BDebug.LogError("加载失败DB:" + www.error);
            }
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