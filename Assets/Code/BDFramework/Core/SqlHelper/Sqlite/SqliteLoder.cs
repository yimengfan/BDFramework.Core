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
            Connection?.Dispose();

            //先以外部传入的 作为 firstpath
            firstPath = IPath.Combine(root, BDUtils.GetPlatformPath(Application.platform) + "/Local.db");

#if UNITY_EDITOR
            
            Connection = new SQLiteConnection(firstPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            BDebug.Log("DB加载路径:" + firstPath, "red");
            return;
#endif
            
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
        /// 创建连接
        /// </summary>
        static public SQLiteConnection CreateConnetion(string path)
        {  
            Connection?.Dispose();
            Connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            return Connection;
        }


        /// <summary>
        /// 异步拷贝,加载
        /// </summary>
        /// <returns></returns>
        static private IEnumerator IE_LoadSqlite()
        {
            //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
            //可寻址目录也只有 StreamingAsset
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
                BDebug.Log("拷贝DB成功:" + firstPath, "red");
                Connection = new SQLiteConnection(firstPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            }
            else
            {
                BDebug.LogError(www.error+ "\n 拷贝DB失败:" +secPath);
            }
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


        /// <summary>
        /// 关闭
        /// </summary>
        static public void Close()
        {
            Connection?.Dispose();
            Connection = null;
        }
    }
}