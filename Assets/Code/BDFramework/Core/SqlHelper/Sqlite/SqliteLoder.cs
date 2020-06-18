using System;
using System.Collections;
using System.IO;
using BDFramework.Helper;
using UnityEngine;
using BDFramework;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQLite4Unity3d;

namespace BDFramework.Sql
{
    static public class SqliteLoder
    {
        private static string           DBName = "Local.db";
        static public  SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public void LoadOnEditor(string root ,RuntimePlatform platform)
        {
            //
            Connection?.Dispose();
            //用当前平台目录进行加载
            var path  = GetDBPath(root, platform);
            //
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            //编辑器下打开
            if (Application.isEditor && !Application.isPlaying)
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
        static public void LoadOnRuntime(string path)
        {
            Connection?.Dispose();
            //用当前平台目录进行加载
            path = GetDBPath(path, Application.platform);
            if (File.Exists(path))
            {
                Connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);
                BDebug.Log("DB加载路径:" + path, "red");
            }
            else if (!Application.isEditor)
            {
                //执行手机端操作，进行寻址 方便测试
                IEnumeratorTool.StartCoroutine(IE_LoadSqliteOnMobile());
                BDebug.Log("DB加载路径:" + path, "red");
            }
            else
            {
                Debug.LogError("DB不存在:" + path);
            }
        }


        /// <summary>
        /// 异步拷贝,加载
        /// </summary>
        /// <returns></returns>
        static private IEnumerator IE_LoadSqliteOnMobile()
        {
            //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
            //可寻址目录也只有 StreamingAsset
            var firstPath = GetDBPath(Application.persistentDataPath, Application.platform);
            var secPath   = GetDBPath(Application.streamingAssetsPath, Application.platform);
            WWW www       = new WWW(secPath);
            yield return www;

            if (www.isDone && www.error == null)
            {
                FileHelper.WriteAllBytes(firstPath, www.bytes);
                BDebug.Log("拷贝DB成功:" + firstPath, "red");
                Connection = new SQLiteConnection(firstPath, SQLiteOpenFlags.ReadOnly);
            }
            else
            {
                BDebug.LogError(" 第一目录DB不存在:" + firstPath);
                BDebug.LogError(" 第二目录DB不存在:" + secPath);
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
        static public string GetDBPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BDUtils.GetPlatformPath(platform), DBName);
        }
    }
}