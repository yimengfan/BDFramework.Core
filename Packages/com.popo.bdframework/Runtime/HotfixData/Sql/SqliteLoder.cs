using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using SQLite4Unity3d;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
namespace BDFramework.Sql
{
    /// <summary>
    /// Sqlite 加载器。
    /// 密码获取已解耦：当显式值未设置时，通过 <see cref="PasswordFallback"/> 回调获取默认密码，
    /// 由配置层（GameCipherConfigProcessor）在启动阶段注入，避免 Sql → Configure 的双向依赖。
    /// </summary>
    static public partial class SqliteLoder
    {
        static readonly string Tag = "SQLite";

        /// <summary>
        /// 密码后备回调，由配置层注入。当 <see cref="password"/> 为空时调用。
        /// </summary>
        public static Func<string> PasswordFallback { get; set; }

        public static string password;

        /// <summary>
        /// 获取或设置 SQLite 数据库加密密码。
        /// getter 优先返回显式设置的值，为空时调用 <see cref="PasswordFallback"/> 回调。
        /// </summary>
        public static string Password
        {
            get
            {
                if (!string.IsNullOrEmpty(password))
                {
                    return password;
                }
                else //配置未初始化的情况，通过回调获取默认密码
                {
                    return PasswordFallback?.Invoke() ?? string.Empty;
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
                ApplyReadOnlyPragmas(con);
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
                    // 同步清理 SqliteHelper 中缓存的 DB 服务实例，避免 GetDB 返回已关闭的过期实例
                    // Synchronize cleanup of cached DB service in SqliteHelper to prevent GetDB from returning stale closed instances
                    SqliteHelper.RemoveDBService(dbName);
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

        #region 只读 PRAGMA 极端优化

        /// <summary>
        /// 页缓存上限（KB）。动态 cache_size 不会超过此值。
        /// Upper limit for page cache in KB. Dynamic cache_size will not exceed this value.
        /// </summary>
        const int MaxCacheSizeKB = 20000; // 20MB

        /// <summary>
        /// 页缓存下限（KB）。动态 cache_size 不会低于此值。
        /// Lower limit for page cache in KB. Dynamic cache_size will not go below this value.
        /// </summary>
        const int MinCacheSizeKB = 2000; // 2MB

        /// <summary>
        /// 页缓存相对数据库文件大小的倍数。
        /// 缓存 = fileSize * CacheSizeMultiplier，然后 clamp 到 [MinCacheSizeKB, MaxCacheSizeKB]。
        /// Multiplier of page cache relative to database file size.
        /// cache = fileSize * CacheSizeMultiplier, then clamped to [MinCacheSizeKB, MaxCacheSizeKB].
        /// </summary>
        const float CacheSizeMultiplier = 2.0f;

        /// <summary>
        /// 根据数据库文件大小计算合理的页缓存大小（KB）。
        /// 小数据库（&lt;2MB）使用最小缓存 2MB；大数据库按文件大小 ×2 计算，上限 20MB。
        /// Calculate a reasonable page cache size (KB) based on database file size.
        /// Small databases (&lt;2MB) use minimum 2MB cache; larger databases use fileSize×2, capped at 20MB.
        /// </summary>
        /// <param name="dbFilePath">数据库文件路径 / Database file path</param>
        /// <returns>cache_size 值（负数，单位 KB） / cache_size value (negative, in KB)</returns>
        static int CalculateCacheSizeKB(string dbFilePath)
        {
            long fileSizeBytes = 0;
            try
            {
                var fi = new FileInfo(dbFilePath);
                if (fi.Exists) fileSizeBytes = fi.Length;
            }
            catch
            {
                // 文件信息获取失败，使用默认值
                // File info retrieval failed, use default
            }

            // 文件大小转 KB，乘以倍数，然后 clamp 到 [Min, Max]
            // Convert file size to KB, multiply by multiplier, then clamp to [Min, Max]
            var fileSizeKB = fileSizeBytes / 1024L;
            var cacheSizeKB = (int)(fileSizeKB * CacheSizeMultiplier);
            cacheSizeKB = Math.Max(MinCacheSizeKB, Math.Min(MaxCacheSizeKB, cacheSizeKB));

            return cacheSizeKB;
        }

        /// <summary>
        /// 为只读连接应用极端优化 PRAGMA。
        /// Runtime 下 local.db 不发生写入，因此可以关闭所有与写入安全相关的机制。
        /// Apply extreme optimization PRAGMAs for read-only connections.
        /// Since local.db is never written at runtime, all write-safety mechanisms can be disabled.
        ///
        /// 优化项说明：
        /// - cache_size: 根据数据库文件大小动态计算（2MB~20MB），减少磁盘 IO 且不浪费内存
        /// - mmap_size=268435456: 内存映射 256MB，操作系统自动管理页面换入换出
        /// - journal_mode=OFF: 关闭回滚日志，只读无需事务恢复
        /// - synchronous=OFF: 关闭同步写入，只读无需确保数据落盘
        /// - temp_store=MEMORY: 临时表和排序操作在内存中执行
        /// - locking_mode=NORMAL: 允许其他进程读取（不使用 EXCLUSIVE 锁）
        /// </summary>
        static public void ApplyReadOnlyPragmas(SQLiteConnection con)
        {
            if (con == null) return;

            // 启动性能监控
            SqlitePerformanceMonitor.BeginStartupPhase();

            try
            {
                // 页缓存：根据数据库文件大小动态计算（2MB~20MB）
                // Page cache: dynamically calculated based on DB file size (2MB~20MB)
                var cacheSizeKB = CalculateCacheSizeKB(con.DatabasePath);
                con.Execute($"PRAGMA cache_size=-{cacheSizeKB}");

                // 内存映射：256MB — 操作系统按需换入页面，零拷贝读取
                con.Execute("PRAGMA mmap_size=268435456");

                // 关闭回滚日志 — 只读模式下无事务恢复需求
                con.Execute("PRAGMA journal_mode=OFF");

                // 关闭同步写入 — 只读模式下无需确保数据落盘
                con.Execute("PRAGMA synchronous=OFF");

                // 临时表和排序在内存中执行
                con.Execute("PRAGMA temp_store=MEMORY");

                // 使用 NORMAL 锁定模式 — 允许其他进程并发读取
                con.Execute("PRAGMA locking_mode=NORMAL");

                // 读取当前 PRAGMA 配置值用于监控报告
                var pageSize = con.ExecuteScalarInt("PRAGMA page_size");
                var cacheSize = con.ExecuteScalarInt("PRAGMA cache_size");
                var mmapSize = con.ExecuteScalar<long>("PRAGMA mmap_size");

                SqlitePerformanceMonitor.RecordPragmaConfig(mmapSize, cacheSize, pageSize);

                BDebug.Log(Tag,
                    $"只读优化PRAGMA已应用 — page_size:{pageSize}, cache_size:{cacheSize}({cacheSizeKB}KB), mmap_size:{mmapSize}",
                    Color.cyan);
            }
            catch (Exception e)
            {
                // PRAGMA 设置失败不应阻断数据库加载，仅记录告警
                Debug.LogWarning($"SqliteLoder: PRAGMA优化设置失败（不影响正常使用）: {e.Message}");
            }
        }

        #endregion

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
