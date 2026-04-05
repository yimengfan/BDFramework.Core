using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using SQLite4Unity3d;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
namespace BDFramework.Sql
{
    /// <summary>
    /// Sqlite 加载器
    /// </summary>
    static public partial class SqliteLoder
    {
        static readonly string Tag = "SQLite";

        public static string password;

        /// <summary>
        /// Password
        /// </summary>
        public static string Password
        {
            get
            {
                if (!string.IsNullOrEmpty(password))
                {
                    return password;
                }
                else //配置未初始化的情况
                {
                    var conf = GameConfigManager.Inst.GetConfig<GameCipherConfigProcessor.Config>();
                    return conf.SqlitePassword;
                }
            }
            set { password = value; }
        }

        /// <summary>
        /// 本地DB Path
        /// </summary>
        public readonly static string LOCAL_DB_PATH = "local.db";

        /// <summary>
        /// ServerDB Path
        /// </summary>
        public readonly static string SERVER_DB_PATH = "server.db";

        /// <summary>
        /// sql驱动对象
        /// </summary>
        static public SQLiteConnection Connection { get; set; }

        /// <summary>
        /// DB连接库
        /// </summary>
        private static Dictionary<string, SQLiteConnection> SqLiteConnectionMap = new Dictionary<string, SQLiteConnection>();

        /// <summary>
        /// runtime下加载
        /// </summary>
        /// <param name="str"></param>
        static public void Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir)
        {
            Connection?.Dispose();
            //db 一定在第一寻址路径
            var db_path = IPath.Combine(firstDir, LOCAL_DB_PATH);
            Connection = LoadDBReadOnly(db_path);
            BDebug.Log(Tag, $"加载路径:{db_path}",Color.green );
          
        }


        /// <summary>
        /// 加载db 只读
        /// </summary>
        static public SQLiteConnection LoadDBReadOnly(string path)
        {
            if (File.Exists(path))
            {
                BDebug.Log(Tag, $"加载路径:{path} psw:{Password}", Color.green);
                SQLiteConnectionString cs = new SQLiteConnectionString(path, SQLiteOpenFlags.ReadOnly, true, key: Password);
                var con = new SQLiteConnection(cs);
                SqLiteConnectionMap[Path.GetFileNameWithoutExtension(path)] = con;
                return con;
            }
            else
            {
                Debug.LogError("DB不存在:" + path);
                return null;
            }
        }

        /// <summary>
        /// 加载db ReadWriteCreate
        /// </summary>
        static public SQLiteConnection LoadDBReadWriteCreate(string path, bool isUsePsw = true)
        {
            BDebug.Log($" DB Path:{path}  <color=yellow>password:{Password}</color>");
            EnsureEditorSqlCipherReady();
            SQLiteConnectionString cs;
            if (isUsePsw)
            {
                cs = new SQLiteConnectionString(path,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true, key: Password);
            }
            else
            {
                cs = new SQLiteConnectionString(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true);
            }

            var con = new SQLiteConnection(cs);
            SqLiteConnectionMap[Path.GetFileNameWithoutExtension(path)] = con;
            return con;
        }


        /// <summary>
        /// sqliteConnect
        /// </summary>
        /// <param name="dbname"></param>
        /// <returns></returns>
        static public SQLiteConnection GetSqliteConnect(string dbname)
        {
            SqLiteConnectionMap.TryGetValue(dbname, out var con);
            return con;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        static public void Close(string dbName = "")
        {
            if (string.IsNullOrEmpty(dbName))
            {
                Connection?.Dispose();
                Connection = null;
            }
            else
            {
                var ret = SqLiteConnectionMap.TryGetValue(dbName, out var con);
                if (ret)
                {
                    con.Dispose();
                    SqLiteConnectionMap.Remove(dbName);
                }
            }
        }


        /// <summary>
        /// 获取DB路径
        /// </summary>
        static public string GetServerDBPath(string root)
        {
            return IPath.Combine(root, "server_data", SERVER_DB_PATH);
        }

#if UNITY_EDITOR
        private static void EnsureEditorSqlCipherReady()
        {
            if (!Application.isEditor)
            {
                return;
            }

            try
            {
                SQLite3.LibVersionNumber();
            }
            catch (Exception e)
            {
                var pluginPath = IPath.Combine(Application.dataPath, "../Packages/com.popo.bdframework/Plugins/Sqlite/macOS/sqlcipher.bundle");
                pluginPath = Path.GetFullPath(pluginPath);
                var pluginExists = File.Exists(pluginPath);
                var editorCpu = SystemInfo.processorType;
                var message =
                    $"SQLCipher 原生库加载失败!\n" +
                    $"Plugin: {pluginPath}\n" +
                    $"Exists: {pluginExists}\n" +
                    $"OS: {SystemInfo.operatingSystem}\n" +
                    $"CPU: {editorCpu}\n" +
                    $"Unity: {Application.unityVersion}\n" +
                    $"Active BuildTarget: {EditorUserBuildSettings.activeBuildTarget}\n" +
                    $"Exception: {e}";
                Debug.LogError(message);
                throw new Exception(message, e);
            }
        }
#else
        private static void EnsureEditorSqlCipherReady()
        {
        }
#endif

        #region Editor下加载

        /// <summary>
        /// 获取DB路径
        /// </summary>
        static public string GetLocalDBPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BApplication.GetPlatformLoadPath(platform), LOCAL_DB_PATH);
        }

        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public string LoadLocalDBOnEditor(string root, RuntimePlatform platform)
        {
            //用当前平台目录进行加载
            var path = GetLocalDBPath(root, platform);
            LoadSQLOnEditor(path);

            return path;
        }

        static public string LoadLocalDBOnEditor()
        {
            var ret = LoadLocalDBOnEditor(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
            return ret;
        }


        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public void LoadServerDBOnEditor(string root)
        {
            //用当前平台目录进行加载
            BDebug.Log("Server.db 不使用加密,否则服务器不好处理!!!", Color.yellow);
            var path = GetServerDBPath(root);
            LoadSQLOnEditor(path, false);
        }

        /// <summary>
        /// 加载Sql
        /// </summary>
        /// <param name="sqlPath"></param>
        static public void LoadSQLOnEditor(string sqlPath, bool isUsePsw = true)
        {
            //
            Connection?.Dispose();
            //
            var dir = Path.GetDirectoryName(sqlPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            //编辑器下打开
            if (Application.isEditor)
            {
                //editor下 不在执行的时候，直接创建
                Connection = LoadDBReadWriteCreate(sqlPath, isUsePsw);
                BDebug.Log("DB加载路径:" + sqlPath, Color.red);
            }
        }

        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <param name="root"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string DeleteLocalDBFile(string root, RuntimePlatform platform)
        {
            //用当前平台目录进行加载
            var path = GetLocalDBPath(root, platform);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return path;
        }

        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <param name="root"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string DeleteServerDBFile(string root)
        {
            //用当前平台目录进行加载
            var path = GetServerDBPath(root);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return path;
        }

        #endregion
    }

}
