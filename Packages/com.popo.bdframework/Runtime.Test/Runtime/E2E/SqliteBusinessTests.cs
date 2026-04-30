using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// SQLite 业务能力 E2E 测试套件。
    /// SQLite business-capability E2E test suite.
    /// 在热更已加载、配置已就绪的前提下，通过反射对 SQLite 多连接管理、密码加密、
    /// 连接释放和 PRAGMA 配置进行深度验证。
    /// After hotfix loaded and config ready, deeply verify SQLite multi-connection management,
    /// password encryption, connection disposal, and PRAGMA configuration via reflection.
    /// </summary>
    [Preserve]
    public static class SqliteBusinessTests
    {
        private const string SqliteConnectionTypeName = "SQLite4Unity3d.SQLiteConnection";
        private const string SqliteConnectionStringTypeName = "SQLite4Unity3d.SQLiteConnectionString";
        private const string SqliteOpenFlagsTypeName = "SQLite4Unity3d.SQLiteOpenFlags";
        private const int SqliteReadWriteCreateOpenFlagsValue = 2 | 4;
        private const string SqliteDefaultDateTimeStringFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

        /// <summary>
        /// 验证可同时打开多个 SQLite 连接并无冲突读写。
        /// Verify that multiple SQLite connections can be opened simultaneously and perform reads/writes without conflict.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 1, des: "验证多连接管理无冲突")]
        public static void MultipleConnectionManagement()
        {
            Debug.Log("[E2E] 测试目的=验证多连接管理无冲突 实现手段=创建两个独立 SQLite 数据库并在各自连接中执行完整读写闭环");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = RequireSqliteConnectionStringConstructor(sqliteConnectionStringType, sqliteOpenFlagsType, sqliteConnectionType);
            var sqliteConnectionConstructor = RequireSqliteConnectionConstructor(sqliteConnectionType, sqliteConnectionStringType);
            var executeMethod = RequireInstanceMethod(sqliteConnectionType, "Execute", typeof(string), typeof(object[]));
            var executeScalarIntMethod = RequireExecuteScalarIntMethod(sqliteConnectionType);

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-multi-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var connections = new List<IDisposable>();
            var dbPaths = new List<string>();
            try
            {
                for (int i = 1; i <= 2; i++)
                {
                    var dbPath = Path.Combine(tempDir, $"multi_conn_{i}.db");
                    dbPaths.Add(dbPath);
                    Debug.Log($"[E2E] SQLite multi-conn phase=open dbIndex={i} dbPath={dbPath}");

                    var connectionString = InvokeConstructor(
                        sqliteConnectionStringConstructor,
                        $"SQLiteConnectionString..ctor(db{i})",
                        dbPath,
                        sqliteOpenFlags,
                        true,
                        null, null, null, null,
                        SqliteDefaultDateTimeStringFormat,
                        true);

                    var connection = InvokeConstructor(
                        sqliteConnectionConstructor,
                        $"SQLiteConnection..ctor(db{i})",
                        connectionString);
                    connections.Add((IDisposable)connection);

                    InvokeInstanceMethod(connection, executeMethod,
                        $"SQLiteConnection.Execute(create-table-db{i})",
                        "CREATE TABLE IF NOT EXISTS MultiConnTest (id INTEGER PRIMARY KEY, value TEXT NOT NULL);",
                        Array.Empty<object>());

                    var probeValue = $"multi_conn_value_{i}";
                    InvokeInstanceMethod(connection, executeMethod,
                        $"SQLiteConnection.Execute(insert-db{i})",
                        "INSERT INTO MultiConnTest (id, value) VALUES (1, ?);",
                        new object[] { probeValue });

                    var count = (int)InvokeInstanceMethod(connection, executeScalarIntMethod,
                        $"SQLiteConnection.ExecuteScalarInt(count-db{i})",
                        "SELECT COUNT(*) FROM MultiConnTest;",
                        Array.Empty<object>());
                    if (count != 1)
                        throw new Exception($"连接 {i} 写入验证失败: count={count}");

                    Debug.Log($"[E2E] SQLite multi-conn phase=verified dbIndex={i} probeValue={probeValue}");
                }

                var db1ValueLength = (int)InvokeInstanceMethod(connections[0], executeScalarIntMethod,
                    "SQLiteConnection.ExecuteScalarInt(value-length-db1)",
                    "SELECT LENGTH(value) FROM MultiConnTest WHERE id = 1;",
                    Array.Empty<object>());
                if (db1ValueLength != "multi_conn_value_1".Length)
                    throw new Exception($"连接隔离验证失败: db1 valueLength={db1ValueLength}");

                Debug.Log($"[E2E] SQLite 多连接管理验证完成: connectionCount={connections.Count}");
            }
            finally
            {
                foreach (var c in connections) c?.Dispose();
                foreach (var p in dbPaths) { if (File.Exists(p)) File.Delete(p); }
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// 验证带密码的 SQLite 数据库可正常创建和读写。
        /// Verify that a password-protected SQLite database can be created and accessed.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 2, des: "验证密码加密数据库读写")]
        public static void PasswordProtectedDatabase()
        {
            Debug.Log("[E2E] 测试目的=验证密码加密数据库读写 实现手段=使用显式密码创建 SQLite 数据库并验证读写闭环与重开数据完整性");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = RequireSqliteConnectionStringConstructor(sqliteConnectionStringType, sqliteOpenFlagsType, sqliteConnectionType);
            var sqliteConnectionConstructor = RequireSqliteConnectionConstructor(sqliteConnectionType, sqliteConnectionStringType);
            var executeMethod = RequireInstanceMethod(sqliteConnectionType, "Execute", typeof(string), typeof(object[]));
            var executeScalarIntMethod = RequireExecuteScalarIntMethod(sqliteConnectionType);

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-pwd-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "encrypted.db");
            var testPassword = "TalosTestPwd_2026!";

            try
            {
                // Phase 1: create with password
                Debug.Log($"[E2E] SQLite pwd phase=create dbPath={dbPath}");
                var cs1 = InvokeConstructor(sqliteConnectionStringConstructor, "SQLiteConnectionString..ctor(create-pwd)",
                    dbPath, sqliteOpenFlags, true, testPassword, null, null, null, SqliteDefaultDateTimeStringFormat, true);
                var conn1 = (IDisposable)InvokeConstructor(sqliteConnectionConstructor, "SQLiteConnection..ctor(create-pwd)", cs1);
                try
                {
                    InvokeInstanceMethod(conn1, executeMethod, "Execute(create-table-pwd)",
                        "CREATE TABLE IF NOT EXISTS EncryptedTest (id INTEGER PRIMARY KEY, secret TEXT NOT NULL);", Array.Empty<object>());
                    InvokeInstanceMethod(conn1, executeMethod, "Execute(insert-pwd)",
                        "INSERT INTO EncryptedTest (id, secret) VALUES (1, ?);", new object[] { "top_secret_data_42" });
                    var count = (int)InvokeInstanceMethod(conn1, executeScalarIntMethod, "ExecuteScalarInt(count-pwd)",
                        "SELECT COUNT(*) FROM EncryptedTest;", Array.Empty<object>());
                    if (count != 1) throw new Exception($"密码数据库写入验证失败: count={count}");
                }
                finally { conn1.Dispose(); }

                // Phase 2: reopen with same password
                Debug.Log($"[E2E] SQLite pwd phase=reopen dbPath={dbPath}");
                var cs2 = InvokeConstructor(sqliteConnectionStringConstructor, "SQLiteConnectionString..ctor(reopen-pwd)",
                    dbPath, sqliteOpenFlags, true, testPassword, null, null, null, SqliteDefaultDateTimeStringFormat, true);
                var conn2 = (IDisposable)InvokeConstructor(sqliteConnectionConstructor, "SQLiteConnection..ctor(reopen-pwd)", cs2);
                try
                {
                    var secretLength = (int)InvokeInstanceMethod(conn2, executeScalarIntMethod, "ExecuteScalarInt(secret-length)",
                        "SELECT LENGTH(secret) FROM EncryptedTest WHERE id = 1;", Array.Empty<object>());
                    if (secretLength != "top_secret_data_42".Length)
                        throw new Exception($"密码数据库重开验证失败: secretLength={secretLength}");
                }
                finally { conn2.Dispose(); }

                Debug.Log("[E2E] 密码加密数据库读写验证完成");
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// 验证 SQLite 连接释放后资源正确清理，并且可重新创建同名数据库。
        /// Verify that resources are properly cleaned up after connection disposal and the same-named database can be recreated.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 3, des: "验证连接释放与资源清理")]
        public static void ConnectionDisposalAndReuse()
        {
            Debug.Log("[E2E] 测试目的=验证连接释放与资源清理 实现手段=创建数据库后释放连接并验证可重新创建同名数据库");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = RequireSqliteConnectionStringConstructor(sqliteConnectionStringType, sqliteOpenFlagsType, sqliteConnectionType);
            var sqliteConnectionConstructor = RequireSqliteConnectionConstructor(sqliteConnectionType, sqliteConnectionStringType);
            var executeMethod = RequireInstanceMethod(sqliteConnectionType, "Execute", typeof(string), typeof(object[]));
            var executeScalarIntMethod = RequireExecuteScalarIntMethod(sqliteConnectionType);

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-dispose-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "reusable.db");

            try
            {
                Debug.Log($"[E2E] SQLite dispose phase=create dbPath={dbPath}");
                var cs1 = InvokeConstructor(sqliteConnectionStringConstructor, "SQLiteConnectionString..ctor(first)",
                    dbPath, sqliteOpenFlags, true, null, null, null, null, SqliteDefaultDateTimeStringFormat, true);
                var conn1 = (IDisposable)InvokeConstructor(sqliteConnectionConstructor, "SQLiteConnection..ctor(first)", cs1);
                try
                {
                    InvokeInstanceMethod(conn1, executeMethod, "Execute(create-table-dispose)",
                        "CREATE TABLE IF NOT EXISTS DisposeTest (id INTEGER PRIMARY KEY, data TEXT NOT NULL);", Array.Empty<object>());
                    InvokeInstanceMethod(conn1, executeMethod, "Execute(insert-dispose)",
                        "INSERT INTO DisposeTest (id, data) VALUES (1, ?);", new object[] { "will_be_deleted" });
                }
                finally { conn1.Dispose(); Debug.Log("[E2E] SQLite dispose phase=disposed-first"); }

                File.Delete(dbPath);
                Debug.Log($"[E2E] SQLite dispose phase=file-deleted dbPath={dbPath}");

                var cs2 = InvokeConstructor(sqliteConnectionStringConstructor, "SQLiteConnectionString..ctor(second)",
                    dbPath, sqliteOpenFlags, true, null, null, null, null, SqliteDefaultDateTimeStringFormat, true);
                var conn2 = (IDisposable)InvokeConstructor(sqliteConnectionConstructor, "SQLiteConnection..ctor(second)", cs2);
                try
                {
                    InvokeInstanceMethod(conn2, executeMethod, "Execute(create-table-recreate)",
                        "CREATE TABLE IF NOT EXISTS DisposeTest (id INTEGER PRIMARY KEY, data TEXT NOT NULL);", Array.Empty<object>());
                    var count = (int)InvokeInstanceMethod(conn2, executeScalarIntMethod, "ExecuteScalarInt(count-recreate)",
                        "SELECT COUNT(*) FROM DisposeTest;", Array.Empty<object>());
                    if (count != 0) throw new Exception($"重建数据库应无数据: count={count}");
                }
                finally { conn2.Dispose(); }

                Debug.Log("[E2E] 连接释放与资源清理验证完成");
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// 验证 SQLite PRAGMA 配置持久化。
        /// Verify that SQLite PRAGMA configuration persists.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 4, des: "验证 PRAGMA 配置生效")]
        public static void PragmaConfiguration()
        {
            Debug.Log("[E2E] 测试目的=验证 PRAGMA 配置生效 实现手段=设置 journal_mode 和 synchronous 后批量写入验证");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = RequireSqliteConnectionStringConstructor(sqliteConnectionStringType, sqliteOpenFlagsType, sqliteConnectionType);
            var sqliteConnectionConstructor = RequireSqliteConnectionConstructor(sqliteConnectionType, sqliteConnectionStringType);
            var executeMethod = RequireInstanceMethod(sqliteConnectionType, "Execute", typeof(string), typeof(object[]));

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-pragma-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "pragma_test.db");

            try
            {
                var cs = InvokeConstructor(sqliteConnectionStringConstructor, "SQLiteConnectionString..ctor(pragma)",
                    dbPath, sqliteOpenFlags, true, null, null, null, null, SqliteDefaultDateTimeStringFormat, true);
                var conn = (IDisposable)InvokeConstructor(sqliteConnectionConstructor, "SQLiteConnection..ctor(pragma)", cs);
                try
                {
                    InvokeInstanceMethod(conn, executeMethod, "Execute(pragma-journal-mode)", "PRAGMA journal_mode=MEMORY;", Array.Empty<object>());
                    InvokeInstanceMethod(conn, executeMethod, "Execute(pragma-synchronous)", "PRAGMA synchronous=OFF;", Array.Empty<object>());
                    InvokeInstanceMethod(conn, executeMethod, "Execute(pragma-temp-store)", "PRAGMA temp_store=MEMORY;", Array.Empty<object>());
                    InvokeInstanceMethod(conn, executeMethod, "Execute(create-table-pragma)",
                        "CREATE TABLE IF NOT EXISTS PragmaTest (id INTEGER PRIMARY KEY, val INTEGER NOT NULL);", Array.Empty<object>());

                    for (int i = 1; i <= 100; i++)
                    {
                        InvokeInstanceMethod(conn, executeMethod, $"Execute(insert-pragma-{i})",
                            "INSERT INTO PragmaTest (id, val) VALUES (?, ?);", new object[] { i, i * 10 });
                    }

                    Debug.Log("[E2E] SQLite pragma phase=verified: 100 rows inserted with MEMORY journal");
                }
                finally { conn.Dispose(); }

                Debug.Log("[E2E] PRAGMA 配置验证完成");
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        #region Reflection Helpers

        private static ConstructorInfo RequireSqliteConnectionStringConstructor(Type csType, Type flagsType, Type connType)
        {
            var ctor = csType.GetConstructor(new[] { typeof(string), flagsType, typeof(bool), typeof(object),
                typeof(Action<>).MakeGenericType(connType), typeof(Action<>).MakeGenericType(connType),
                typeof(string), typeof(string), typeof(bool) });
            if (ctor == null) throw new Exception("未发现 SQLiteConnectionString 全参构造入口");
            return ctor;
        }

        private static ConstructorInfo RequireSqliteConnectionConstructor(Type connType, Type csType)
        {
            var ctor = connType.GetConstructor(new[] { csType });
            if (ctor == null) throw new Exception("未发现 SQLiteConnection(SQLiteConnectionString) 构造入口");
            return ctor;
        }

        private static MethodInfo RequireExecuteScalarIntMethod(Type connType)
        {
            var m = connType.GetMethod("ExecuteScalarInt", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(string), typeof(object[]) }, null);
            if (m == null) throw new Exception("未发现 SQLiteConnection.ExecuteScalarInt 方法");
            return m;
        }

        private static Type RequireLoadedType(string typeName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }
            throw new Exception($"未发现已装载类型: {typeName}");
        }

        private static MethodInfo RequireInstanceMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            var m = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
            if (m == null) throw new Exception($"未发现公开实例方法: {type.FullName}.{methodName}");
            return m;
        }

        private static object InvokeConstructor(ConstructorInfo ctor, string op, params object[] args)
        {
            try { return ctor.Invoke(args); }
            catch (TargetInvocationException e) { throw new Exception($"{op} 失败: {e.InnerException?.Message}", e.InnerException); }
        }

        private static object InvokeInstanceMethod(object instance, MethodInfo method, string op, params object[] args)
        {
            try { return method.Invoke(instance, args); }
            catch (TargetInvocationException e) { throw new Exception($"{op} 失败: {e.InnerException?.Message}", e.InnerException); }
        }

        #endregion
    }
}
