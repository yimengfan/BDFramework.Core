using System;
using System.Collections;
using System.IO;
using BDFramework.Helper;
using UnityEngine;
using BDFramework;
using SQLite4Unity3d;

namespace BDFramework.Sql
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

            //先以外部传入的 作为 firstpath
            firstPath = IPath.Combine(root, BDUtils.GetPlatformPath(Application.platform) + "/Local.db");
            
            //firstpath不存在 或者 不支持io操作，
            //则默认情况生效，persistent为firstpath
            if (!File.Exists(firstPath))
            {
                //这里sqlite 如果不在firstPath下，就到Streamming下面拷贝到第一路径
                IEnumeratorTool.StartCoroutine(IE_LoadSqlite());
            }
            else
            {
                BDebug.Log("DB加载路径:" + firstPath, "red");
                Connection = new SQLiteConnection(firstPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

            }
        }


        /// <summary>
        /// 异步拷贝,加载
        /// </summary>
        /// <returns></returns>
        static private IEnumerator IE_LoadSqlite()
        {
            //从StreamingAsset拷贝到Persistent
            //此时persistent为 firstPath
            //StreamingAsset 为 SecPath
            firstPath = IPath.Combine(Application.persistentDataPath,BDUtils.GetPlatformPath(Application.platform) + "/Local.db");
            var secPath = IPath.Combine(Application.streamingAssetsPath,BDUtils.GetPlatformPath(Application.platform) + "/Local.db");
            if (Application.isEditor)
            {
                secPath = "file://" + secPath;
            }
            WWW www = new WWW(secPath);

            yield return www;

            if (www.isDone && www.error == null)
            {
                FileHelper.WriteAllBytes(firstPath, www.bytes);
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