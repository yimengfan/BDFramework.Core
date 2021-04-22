using System;
using System.Collections;
using System.IO;
using BDFramework.Core.Tools;
using UnityEngine;
using SQLite4Unity3d;

namespace BDFramework.Sql
{
    static public class SqliteLoder
    {
        public readonly static string           LOCAL_DB_PATH = "Local.db";
        public readonly static string           SERVER_DB_PATH  = "Server.db";
        static public          SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public void LoadLocalDBOnEditor(string root, RuntimePlatform platform)
        {
            //
            Connection?.Dispose();
            //用当前平台目录进行加载
            var path = GetLocalDBPath(root, platform);
            //
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            //编辑器下打开
            if (Application.isEditor)
            {
                //editor下 不在执行的时候，直接创建
                Connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
                BDebug.Log("DB加载路径:" + path, "red");
            }
        }
        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public void LoadServerDBOnEditor(string root, RuntimePlatform platform)
        {
            //
            Connection?.Dispose();
            //用当前平台目录进行加载
            var path = GetServerDBPath(root, platform);
            //
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            //编辑器下打开
            if (Application.isEditor)
            {
                //editor下 不在执行的时候，直接创建
                Connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
                BDebug.Log("DB加载路径:" + path, "red");
            }
        }
        /// <summary>
        /// runtime下加载，只读
        /// </summary>
        /// <param name="str"></param>
        static public void Load(AssetLoadPath loadPath)
        {
            Connection?.Dispose();
            var path = "";
            if (Application.isEditor)
            {
                if (loadPath == AssetLoadPath.Persistent)
                {
                    path = Application.persistentDataPath;
                }
                else if (loadPath == AssetLoadPath.Editor || loadPath == AssetLoadPath.StreamingAsset)
                {
                    path = Application.streamingAssetsPath;
                }
            }
            else
            {
                //真机环境db在persistent下，因为需要io
                path = Application.persistentDataPath;
            }

            //用当前平台目录进行加载
            path = GetLocalDBPath(path, Application.platform);
            if (File.Exists(path))
            {
                Connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);
                BDebug.Log("DB加载路径:" + path, "red");
            }
            else
            {
                Debug.LogError("DB不存在:" + path);
            }
        }
        
        /// <summary>
        /// 关闭
        /// </summary>
        static public void Close()
        {
            Connection?.Dispose();
            Connection = null;
        }


        /// <summary>
        /// 获取DB路径
        /// </summary>
        static public string GetLocalDBPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BDApplication.GetPlatformPath(platform), LOCAL_DB_PATH);
        }
        
        /// <summary>
        /// 获取DB路径
        /// </summary>
        static public string GetServerDBPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BDApplication.GetPlatformPath(platform), SERVER_DB_PATH);
        }
    }
}