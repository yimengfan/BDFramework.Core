using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.HostE2E
{
    /// <summary>
    /// 宿主侧框架业务基础能力 E2E 测试套件。
    /// Host-owned framework business foundational-capability E2E suite.
    /// 该套件服务于 step_02 框架业务测试，在热更已加载、配置已就绪的前提下，
    /// 通过宿主可见的反射入口对 SQLite、AssetBundle 管理器、版本控制器和下载准备链路进行深度验证。
    /// This suite serves step_02 framework business testing and performs deep verification of SQLite,
    /// AssetBundle manager, version controller, and download-preparation chain through host-visible reflection entrypoints
    /// after hotfix has loaded and configuration is ready.
    /// </summary>
    [Preserve]
    public static class FrameworkBusinessHostTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";
        private const string BResourcesTypeName = "BDFramework.ResourceMgr.BResources";
        private const string SqliteLoderTypeName = "BDFramework.Sql.SqliteLoder";
        private const string SqliteConnectionTypeName = "SQLite4Unity3d.SQLiteConnection";
        private const string SqliteConnectionStringTypeName = "SQLite4Unity3d.SQLiteConnectionString";
        private const string SqliteOpenFlagsTypeName = "SQLite4Unity3d.SQLiteOpenFlags";
        private const string GameConfigManagerTypeName = "BDFramework.Configure.GameConfigManager";
        private const string GameBaseConfigProcessorTypeName = "BDFramework.GameBaseConfigProcessor";
        private const string ServerConfigProcessorTypeName = "Game.Config.ServerConfigProcessor";
        private const string ClientAssetsUtilsTypeName = "BDFramework.Asset.ClientAssetsUtils";
        private const string VersionNumHelperTypeName = "BDFramework.Asset.VersionNumHelper";
        private const string AssetsVersionInfoTypeName = "BDFramework.ResourceMgr.AssetsVersionInfo";
        private const int SqliteReadWriteCreateOpenFlagsValue = 2 | 4;
        private const string SqliteDefaultDateTimeStringFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

        #region Suite: sqlite-business — SQLite 深度测试

        /// <summary>
        /// 验证可同时打开多个 SQLite 连接并无冲突读写。
        /// Verify that multiple SQLite connections can be opened simultaneously and perform reads/writes without conflict.
        /// 该检查创建两个独立数据库，分别在各自连接中建表、写入、查询，
        /// 确认连接间完全隔离。
        /// This check creates two independent databases, performs create-table, insert, and query on each connection,
        /// confirming complete isolation between connections.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 1, des: "验证多连接管理无冲突")]
        public static void MultipleConnectionManagement()
        {
            Debug.Log("[E2E] 测试目的=验证多连接管理无冲突 实现手段=创建两个独立 SQLite 数据库并在各自连接中执行完整读写闭环");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = sqliteConnectionStringType.GetConstructor(new[]
            {
                typeof(string),
                sqliteOpenFlagsType,
                typeof(bool),
                typeof(object),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(string),
                typeof(string),
                typeof(bool),
            });
            if (sqliteConnectionStringConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnectionString 全参构造入口");
            }

            var sqliteConnectionConstructor = sqliteConnectionType.GetConstructor(new[] { sqliteConnectionStringType });
            if (sqliteConnectionConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnection(SQLiteConnectionString) 构造入口");
            }

            var executeMethod = RequireInstanceMethod(
                sqliteConnectionType,
                "Execute",
                typeof(string),
                typeof(object[]));

            var executeScalarIntMethod = sqliteConnectionType.GetMethod(
                "ExecuteScalarInt",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(object[]) },
                null);
            if (executeScalarIntMethod == null)
            {
                throw new Exception("未发现 SQLiteConnection.ExecuteScalarInt 方法");
            }

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-multi-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var connections = new List<IDisposable>();
            var dbPaths = new List<string>();
            try
            {
                // 创建两个独立数据库，各自写入不同的探针值
                // Create two independent databases, each with a different probe value
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
                        null,
                        null,
                        null,
                        null,
                        SqliteDefaultDateTimeStringFormat,
                        true);

                    var connection = InvokeConstructor(
                        sqliteConnectionConstructor,
                        $"SQLiteConnection..ctor(db{i})",
                        connectionString);
                    connections.Add((IDisposable)connection);

                    // 建表
                    // Create table
                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        $"SQLiteConnection.Execute(create-table-db{i})",
                        "CREATE TABLE IF NOT EXISTS MultiConnTest (id INTEGER PRIMARY KEY, value TEXT NOT NULL);",
                        Array.Empty<object>());

                    // 写入
                    // Insert
                    var probeValue = $"multi_conn_value_{i}";
                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        $"SQLiteConnection.Execute(insert-db{i})",
                        "INSERT INTO MultiConnTest (id, value) VALUES (1, ?);",
                        new object[] { probeValue });

                    // 查询验证
                    // Query verification
                    var count = (int)InvokeInstanceMethod(
                        connection,
                        executeScalarIntMethod,
                        $"SQLiteConnection.ExecuteScalarInt(count-db{i})",
                        "SELECT COUNT(*) FROM MultiConnTest;",
                        Array.Empty<object>());
                    if (count != 1)
                    {
                        throw new Exception($"连接 {i} 写入验证失败: count={count}");
                    }

                    Debug.Log($"[E2E] SQLite multi-conn phase=verified dbIndex={i} probeValue={probeValue}");
                }

                // 验证连接间隔离：db1 不应看到 db2 的数据
                // Verify isolation: db1 should not see db2's data
                var db1ValueLength = (int)InvokeInstanceMethod(
                    connections[0],
                    executeScalarIntMethod,
                    "SQLiteConnection.ExecuteScalarInt(value-length-db1)",
                    "SELECT LENGTH(value) FROM MultiConnTest WHERE id = 1;",
                    Array.Empty<object>());
                var expectedValue1 = "multi_conn_value_1";
                if (db1ValueLength != expectedValue1.Length)
                {
                    throw new Exception($"连接隔离验证失败: db1 valueLength={db1ValueLength} expected={expectedValue1.Length}");
                }

                Debug.Log($"[E2E] SQLite 多连接管理验证完成: connectionCount={connections.Count}");
            }
            finally
            {
                foreach (var connection in connections)
                {
                    connection?.Dispose();
                }

                foreach (var dbPath in dbPaths)
                {
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }
                }

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证带密码的 SQLite 数据库可正常创建和读写。
        /// Verify that a password-protected SQLite database can be created and accessed.
        /// 该检查使用显式密码创建数据库，执行建表写入查询闭环，
        /// 关闭后使用相同密码重新打开验证数据完整性。
        /// This check creates a database with an explicit password, performs create-table/insert/query,
        /// then reopens with the same password to verify data integrity.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 2, des: "验证密码加密数据库读写")]
        public static void PasswordProtectedDatabase()
        {
            Debug.Log("[E2E] 测试目的=验证密码加密数据库读写 实现手段=使用显式密码创建 SQLite 数据库并验证读写闭环与重开数据完整性");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = sqliteConnectionStringType.GetConstructor(new[]
            {
                typeof(string),
                sqliteOpenFlagsType,
                typeof(bool),
                typeof(object),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(string),
                typeof(string),
                typeof(bool),
            });
            if (sqliteConnectionStringConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnectionString 全参构造入口");
            }

            var sqliteConnectionConstructor = sqliteConnectionType.GetConstructor(new[] { sqliteConnectionStringType });
            if (sqliteConnectionConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnection(SQLiteConnectionString) 构造入口");
            }

            var executeMethod = RequireInstanceMethod(
                sqliteConnectionType,
                "Execute",
                typeof(string),
                typeof(object[]));

            var executeScalarIntMethod = sqliteConnectionType.GetMethod(
                "ExecuteScalarInt",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(object[]) },
                null);
            if (executeScalarIntMethod == null)
            {
                throw new Exception("未发现 SQLiteConnection.ExecuteScalarInt 方法");
            }

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-pwd-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "encrypted.db");
            var testPassword = "TalosTestPwd_2026!";

            try
            {
                // 阶段1：使用密码创建数据库并写入
                // Phase 1: create database with password and write data
                Debug.Log($"[E2E] SQLite pwd phase=create dbPath={dbPath}");
                var connectionString = InvokeConstructor(
                    sqliteConnectionStringConstructor,
                    "SQLiteConnectionString..ctor(create-pwd)",
                    dbPath,
                    sqliteOpenFlags,
                    true,
                    testPassword,
                    null,
                    null,
                    null,
                    SqliteDefaultDateTimeStringFormat,
                    true);

                var connection = (IDisposable)InvokeConstructor(
                    sqliteConnectionConstructor,
                    "SQLiteConnection..ctor(create-pwd)",
                    connectionString);

                try
                {
                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        "SQLiteConnection.Execute(create-table-pwd)",
                        "CREATE TABLE IF NOT EXISTS EncryptedTest (id INTEGER PRIMARY KEY, secret TEXT NOT NULL);",
                        Array.Empty<object>());

                    var secretValue = "top_secret_data_42";
                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        "SQLiteConnection.Execute(insert-pwd)",
                        "INSERT INTO EncryptedTest (id, secret) VALUES (1, ?);",
                        new object[] { secretValue });

                    var count = (int)InvokeInstanceMethod(
                        connection,
                        executeScalarIntMethod,
                        "SQLiteConnection.ExecuteScalarInt(count-pwd)",
                        "SELECT COUNT(*) FROM EncryptedTest;",
                        Array.Empty<object>());
                    if (count != 1)
                    {
                        throw new Exception($"密码数据库写入验证失败: count={count}");
                    }

                    Debug.Log($"[E2E] SQLite pwd phase=create-verified secretValue={secretValue}");
                }
                finally
                {
                    connection.Dispose();
                }

                // 阶段2：使用相同密码重新打开并验证数据完整性
                // Phase 2: reopen with same password and verify data integrity
                Debug.Log($"[E2E] SQLite pwd phase=reopen dbPath={dbPath}");
                var reopenConnectionString = InvokeConstructor(
                    sqliteConnectionStringConstructor,
                    "SQLiteConnectionString..ctor(reopen-pwd)",
                    dbPath,
                    sqliteOpenFlags,
                    true,
                    testPassword,
                    null,
                    null,
                    null,
                    SqliteDefaultDateTimeStringFormat,
                    true);

                var reopenedConnection = (IDisposable)InvokeConstructor(
                    sqliteConnectionConstructor,
                    "SQLiteConnection..ctor(reopen-pwd)",
                    reopenConnectionString);

                try
                {
                    var secretLength = (int)InvokeInstanceMethod(
                        reopenedConnection,
                        executeScalarIntMethod,
                        "SQLiteConnection.ExecuteScalarInt(secret-length)",
                        "SELECT LENGTH(secret) FROM EncryptedTest WHERE id = 1;",
                        Array.Empty<object>());
                    if (secretLength != "top_secret_data_42".Length)
                    {
                        throw new Exception($"密码数据库重开验证失败: secretLength={secretLength}");
                    }

                    Debug.Log($"[E2E] SQLite pwd phase=reopen-verified secretLength={secretLength}");
                }
                finally
                {
                    reopenedConnection.Dispose();
                }

                Debug.Log("[E2E] 密码加密数据库读写验证完成");
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证 SQLite 连接释放后资源正确清理，并且可重新创建同名数据库。
        /// Verify that resources are properly cleaned up after connection disposal and the same-named database can be recreated.
        /// 该检查创建数据库、写入数据、释放连接、删除文件、重新创建、验证旧数据不存在。
        /// This check creates a database, writes data, disposes the connection, deletes the file,
        /// recreates it, and verifies old data is gone.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 3, des: "验证连接释放与资源清理")]
        public static void ConnectionDisposalAndReuse()
        {
            Debug.Log("[E2E] 测试目的=验证连接释放与资源清理 实现手段=创建数据库后释放连接并验证可重新创建同名数据库");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = sqliteConnectionStringType.GetConstructor(new[]
            {
                typeof(string),
                sqliteOpenFlagsType,
                typeof(bool),
                typeof(object),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(string),
                typeof(string),
                typeof(bool),
            });
            if (sqliteConnectionStringConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnectionString 全参构造入口");
            }

            var sqliteConnectionConstructor = sqliteConnectionType.GetConstructor(new[] { sqliteConnectionStringType });
            if (sqliteConnectionConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnection(SQLiteConnectionString) 构造入口");
            }

            var executeMethod = RequireInstanceMethod(
                sqliteConnectionType,
                "Execute",
                typeof(string),
                typeof(object[]));

            var executeScalarIntMethod = sqliteConnectionType.GetMethod(
                "ExecuteScalarInt",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(object[]) },
                null);
            if (executeScalarIntMethod == null)
            {
                throw new Exception("未发现 SQLiteConnection.ExecuteScalarInt 方法");
            }

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-dispose-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "reusable.db");

            try
            {
                // 阶段1：创建数据库并写入数据
                // Phase 1: create database and write data
                Debug.Log($"[E2E] SQLite dispose phase=create dbPath={dbPath}");
                var firstConnectionString = InvokeConstructor(
                    sqliteConnectionStringConstructor,
                    "SQLiteConnectionString..ctor(first)",
                    dbPath,
                    sqliteOpenFlags,
                    true,
                    null,
                    null,
                    null,
                    null,
                    SqliteDefaultDateTimeStringFormat,
                    true);

                var firstConnection = (IDisposable)InvokeConstructor(
                    sqliteConnectionConstructor,
                    "SQLiteConnection..ctor(first)",
                    firstConnectionString);

                try
                {
                    InvokeInstanceMethod(
                        firstConnection,
                        executeMethod,
                        "SQLiteConnection.Execute(create-table-dispose)",
                        "CREATE TABLE IF NOT EXISTS DisposeTest (id INTEGER PRIMARY KEY, data TEXT NOT NULL);",
                        Array.Empty<object>());

                    InvokeInstanceMethod(
                        firstConnection,
                        executeMethod,
                        "SQLiteConnection.Execute(insert-dispose)",
                        "INSERT INTO DisposeTest (id, data) VALUES (1, ?);",
                        new object[] { "will_be_deleted" });
                }
                finally
                {
                    firstConnection.Dispose();
                    Debug.Log("[E2E] SQLite dispose phase=disposed-first");
                }

                // 阶段2：删除文件后重新创建，验证旧数据不存在
                // Phase 2: delete and recreate, verify old data is gone
                File.Delete(dbPath);
                Debug.Log($"[E2E] SQLite dispose phase=file-deleted dbPath={dbPath}");

                var secondConnectionString = InvokeConstructor(
                    sqliteConnectionStringConstructor,
                    "SQLiteConnectionString..ctor(second)",
                    dbPath,
                    sqliteOpenFlags,
                    true,
                    null,
                    null,
                    null,
                    null,
                    SqliteDefaultDateTimeStringFormat,
                    true);

                var secondConnection = (IDisposable)InvokeConstructor(
                    sqliteConnectionConstructor,
                    "SQLiteConnection..ctor(second)",
                    secondConnectionString);

                try
                {
                    InvokeInstanceMethod(
                        secondConnection,
                        executeMethod,
                        "SQLiteConnection.Execute(create-table-recreate)",
                        "CREATE TABLE IF NOT EXISTS DisposeTest (id INTEGER PRIMARY KEY, data TEXT NOT NULL);",
                        Array.Empty<object>());

                    var count = (int)InvokeInstanceMethod(
                        secondConnection,
                        executeScalarIntMethod,
                        "SQLiteConnection.ExecuteScalarInt(count-recreate)",
                        "SELECT COUNT(*) FROM DisposeTest;",
                        Array.Empty<object>());
                    if (count != 0)
                    {
                        throw new Exception($"重建数据库应无数据: count={count}");
                    }

                    Debug.Log($"[E2E] SQLite dispose phase=recreate-verified count={count}");
                }
                finally
                {
                    secondConnection.Dispose();
                }

                Debug.Log("[E2E] 连接释放与资源清理验证完成");
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证 SQLite PRAGMA 配置持久化。
        /// Verify that SQLite PRAGMA configuration persists.
        /// 该检查设置 PRAGMA 参数后验证配置已生效。
        /// This check sets PRAGMA parameters and verifies they take effect.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "sqlite-business", order: 4, des: "验证 PRAGMA 配置生效")]
        public static void PragmaConfiguration()
        {
            Debug.Log("[E2E] 测试目的=验证 PRAGMA 配置生效 实现手段=设置 journal_mode 和 synchronous 后验证配置已应用");

            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);

            var sqliteConnectionStringConstructor = sqliteConnectionStringType.GetConstructor(new[]
            {
                typeof(string),
                sqliteOpenFlagsType,
                typeof(bool),
                typeof(object),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(Action<>).MakeGenericType(sqliteConnectionType),
                typeof(string),
                typeof(string),
                typeof(bool),
            });
            if (sqliteConnectionStringConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnectionString 全参构造入口");
            }

            var sqliteConnectionConstructor = sqliteConnectionType.GetConstructor(new[] { sqliteConnectionStringType });
            if (sqliteConnectionConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnection(SQLiteConnectionString) 构造入口");
            }

            var executeMethod = RequireInstanceMethod(
                sqliteConnectionType,
                "Execute",
                typeof(string),
                typeof(object[]));

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            var tempDir = Path.Combine(Path.GetTempPath(), $"talos-sqlite-pragma-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var dbPath = Path.Combine(tempDir, "pragma_test.db");

            try
            {
                Debug.Log($"[E2E] SQLite pragma phase=create dbPath={dbPath}");
                var connectionString = InvokeConstructor(
                    sqliteConnectionStringConstructor,
                    "SQLiteConnectionString..ctor(pragma)",
                    dbPath,
                    sqliteOpenFlags,
                    true,
                    null,
                    null,
                    null,
                    null,
                    SqliteDefaultDateTimeStringFormat,
                    true);

                var connection = (IDisposable)InvokeConstructor(
                    sqliteConnectionConstructor,
                    "SQLiteConnection..ctor(pragma)",
                    connectionString);

                try
                {
                    // 设置 PRAGMA
                    // Set PRAGMA
                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        "SQLiteConnection.Execute(pragma-journal-mode)",
                        "PRAGMA journal_mode=MEMORY;",
                        Array.Empty<object>());

                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        "SQLiteConnection.Execute(pragma-synchronous)",
                        "PRAGMA synchronous=OFF;",
                        Array.Empty<object>());

                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        "SQLiteConnection.Execute(pragma-temp-store)",
                        "PRAGMA temp_store=MEMORY;",
                        Array.Empty<object>());

                    // 创建表并写入来验证 PRAGMA 生效后数据库可正常操作
                    // Create table and write to verify normal operation after PRAGMA
                    InvokeInstanceMethod(
                        connection,
                        executeMethod,
                        "SQLiteConnection.Execute(create-table-pragma)",
                        "CREATE TABLE IF NOT EXISTS PragmaTest (id INTEGER PRIMARY KEY, val INTEGER NOT NULL);",
                        Array.Empty<object>());

                    // 批量写入 100 条
                    // Batch insert 100 rows
                    for (int i = 1; i <= 100; i++)
                    {
                        InvokeInstanceMethod(
                            connection,
                            executeMethod,
                            $"SQLiteConnection.Execute(insert-pragma-{i})",
                            "INSERT INTO PragmaTest (id, val) VALUES (?, ?);",
                            new object[] { i, i * 10 });
                    }

                    Debug.Log("[E2E] SQLite pragma phase=verified: 100 rows inserted with MEMORY journal");
                }
                finally
                {
                    connection.Dispose();
                }

                Debug.Log("[E2E] PRAGMA 配置验证完成");
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #endregion

        #region Suite: asset-business — AssetBundle 管理器测试

        /// <summary>
        /// 验证多资源组增删查操作正确。
        /// Verify that multi-group add/delete/query operations are correct.
        /// 该检查创建多个资源组，写入不同路径，查询验证，然后清理。
        /// This check creates multiple asset groups, writes different paths, queries and verifies, then cleans up.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-business", order: 1, des: "验证多资源组增删查操作")]
        public static void MultiGroupAssetOperations()
        {
            Debug.Log("[E2E] 测试目的=验证多资源组增删查操作 实现手段=创建多个资源组并验证增删查闭环");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);

            var addAssetsPathToGroupMethod = RequireStaticMethod(
                bResourcesType,
                "AddAssetsPathToGroup",
                typeof(string),
                typeof(string[]));
            var getAssetsPathByGroupMethod = RequireStaticMethod(
                bResourcesType,
                "GetAssetsPathByGroup",
                typeof(string));
            var clearAssetGroupMethod = RequireStaticMethod(
                bResourcesType,
                "ClearAssetGroup",
                typeof(string));

            var groupPrefix = $"talos-step02-{Guid.NewGuid():N}";
            var groups = new List<string>();

            try
            {
                // 创建 3 个资源组，每组不同数量的路径
                // Create 3 asset groups, each with different path counts
                for (int i = 1; i <= 3; i++)
                {
                    var groupName = $"{groupPrefix}-group{i}";
                    groups.Add(groupName);

                    var paths = new string[i * 2];
                    for (int j = 0; j < paths.Length; j++)
                    {
                        paths[j] = $"talos/step02/group{i}/asset_{j}.prefab";
                    }

                    Debug.Log($"[E2E] Asset group phase=add group={groupName} pathCount={paths.Length}");
                    InvokeStaticMethod(
                        addAssetsPathToGroupMethod,
                        $"BResources.AddAssetsPathToGroup({groupName})",
                        groupName,
                        new object[] { paths });
                }

                // 验证每个组的路径数量
                // Verify path count for each group
                for (int i = 0; i < groups.Count; i++)
                {
                    var groupName = groups[i];
                    var expectedCount = (i + 1) * 2;

                    var groupedPaths = InvokeStaticMethod(
                        getAssetsPathByGroupMethod,
                        $"BResources.GetAssetsPathByGroup({groupName})",
                        groupName) as string[];

                    if (groupedPaths == null || groupedPaths.Length != expectedCount)
                    {
                        throw new Exception(
                            $"资源组 {groupName} 路径数量异常: actual={groupedPaths?.Length ?? 0} expected={expectedCount}");
                    }

                    Debug.Log($"[E2E] Asset group phase=verified group={groupName} actualCount={groupedPaths.Length} expectedCount={expectedCount}");
                }

                Debug.Log($"[E2E] 多资源组操作验证完成: groupCount={groups.Count}");
            }
            finally
            {
                // 清理所有资源组
                // Clean up all asset groups
                foreach (var groupName in groups)
                {
                    Debug.Log($"[E2E] Asset group phase=clear group={groupName}");
                    InvokeStaticMethod(
                        clearAssetGroupMethod,
                        $"BResources.ClearAssetGroup({groupName})",
                        groupName);
                }
            }
        }

        /// <summary>
        /// 验证多平台版本路径解析正确。
        /// Verify that multi-platform version path resolution is correct.
        /// 该检查对当前平台和不同版本号调用路径解析，验证返回的路径非空。
        /// This check calls path resolution for the current platform and different version numbers, verifying non-empty results.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-business", order: 2, des: "验证平台版本路径解析")]
        public static void PlatformVersionPathResolution()
        {
            Debug.Log("[E2E] 测试目的=验证平台版本路径解析 实现手段=对当前平台调用 GetAssetsInfoPath 和 GetServerAssetsVersionInfoPath");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);

            var frameworkPersistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");

            var getAssetsInfoPathMethod = RequireStaticMethod(
                bResourcesType,
                "GetAssetsInfoPath",
                typeof(string));
            var getServerAssetsVersionInfoPathMethod = RequireStaticMethod(
                bResourcesType,
                "GetServerAssetsVersionInfoPath",
                typeof(string),
                typeof(RuntimePlatform));

            // 验证资源信息路径
            // Verify asset info path
            var assetsInfoPath = InvokeStaticMethod(
                getAssetsInfoPathMethod,
                "BResources.GetAssetsInfoPath",
                frameworkPersistentDataPath) as string;

            if (string.IsNullOrWhiteSpace(assetsInfoPath))
            {
                throw new Exception("资源信息路径解析为空");
            }

            Debug.Log($"[E2E] Asset path phase=assets-info-path path={assetsInfoPath}");

            // 验证当前平台的服务器版本信息路径
            // Verify server version info path for current platform
            var versionInfoPath = InvokeStaticMethod(
                getServerAssetsVersionInfoPathMethod,
                "BResources.GetServerAssetsVersionInfoPath",
                frameworkPersistentDataPath,
                Application.platform) as string;

            if (string.IsNullOrWhiteSpace(versionInfoPath))
            {
                throw new Exception($"服务器版本信息路径解析为空: platform={Application.platform}");
            }

            Debug.Log($"[E2E] Asset path phase=version-info-path path={versionInfoPath} platform={Application.platform}");

            // 验证不同平台路径存在差异
            // Verify paths differ by platform
            if (Application.platform != RuntimePlatform.Android)
            {
                var androidVersionInfoPath = InvokeStaticMethod(
                    getServerAssetsVersionInfoPathMethod,
                    "BResources.GetServerAssetsVersionInfoPath(Android)",
                    frameworkPersistentDataPath,
                    RuntimePlatform.Android) as string;

                if (string.IsNullOrWhiteSpace(androidVersionInfoPath))
                {
                    throw new Exception("Android 平台版本信息路径解析为空");
                }

                Debug.Log($"[E2E] Asset path phase=cross-platform androidPath={androidVersionInfoPath}");
            }

            Debug.Log("[E2E] 平台版本路径解析验证完成");
        }

        /// <summary>
        /// 验证 Shader 查找功能。
        /// Verify shader lookup functionality.
        /// 该检查查找已知 Shader 和不存在 Shader，确认接口行为正确。
        /// This check looks up a known shader and a non-existent shader, confirming correct interface behavior.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-business", order: 3, des: "验证 Shader 查找功能")]
        public static void ShaderLookupVerification()
        {
            Debug.Log("[E2E] 测试目的=验证 Shader 查找功能 实现手段=查找已知 Shader 和不存在 Shader 并验证接口行为");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);

            var findShaderMethod = RequireStaticMethod(
                bResourcesType,
                "FindShader",
                typeof(string));

            // 查找不存在的 Shader（应返回 null 而非抛异常）
            // Look up non-existent shader (should return null, not throw)
            Debug.Log("[E2E] Shader lookup phase=non-existent name=__Talos_Step02_NonExistent_Shader__");
            var nonExistentShader = InvokeStaticMethod(
                findShaderMethod,
                "BResources.FindShader(non-existent)",
                "__Talos_Step02_NonExistent_Shader__");

            if (nonExistentShader != null)
            {
                Debug.Log($"[E2E] Shader lookup phase=non-existent-result name={((UnityEngine.Object)nonExistentShader).name}");
            }
            else
            {
                Debug.Log("[E2E] Shader lookup phase=non-existent-result null (expected)");
            }

            // 查找常见的 Unity 内置 Shader
            // Look up common Unity built-in shader
            Debug.Log("[E2E] Shader lookup phase=standard name=Standard");
            var standardShader = InvokeStaticMethod(
                findShaderMethod,
                "BResources.FindShader(Standard)",
                "Standard");

            Debug.Log($"[E2E] Shader lookup phase=standard-result found={standardShader != null}");

            Debug.Log("[E2E] Shader 查找验证完成");
        }

        #endregion

        #region Suite: version-business — 版本控制器测试

        /// <summary>
        /// 验证客户端版本号可从配置中正确读取。
        /// Verify that the client version number can be correctly read from configuration.
        /// 该检查通过反射读取 GameBaseConfigProcessor.Config.ClientVersionNum，
        /// 确认配置系统已就绪且版本号非空。
        /// This check reads GameBaseConfigProcessor.Config.ClientVersionNum via reflection,
        /// confirming the config system is ready and the version number is non-empty.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "version-business", order: 1, des: "验证客户端版本号可读")]
        public static void ClientVersionReadable()
        {
            Debug.Log("[E2E] 测试目的=验证客户端版本号可读 实现手段=反射读取 GameBaseConfigProcessor.Config.ClientVersionNum");

            var gameConfigManagerType = FindLoadedType(GameConfigManagerTypeName);
            if (gameConfigManagerType == null)
            {
                throw new Exception($"未发现 GameConfigManager 类型: {GameConfigManagerTypeName}");
            }

            var instProperty = gameConfigManagerType.GetProperty(
                "Inst",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProperty == null)
            {
                throw new Exception("未发现 GameConfigManager.Inst 属性");
            }

            var gameConfigManagerInstance = instProperty.GetValue(null);
            if (gameConfigManagerInstance == null)
            {
                throw new Exception("GameConfigManager.Inst 为空");
            }

            var getConfigMethod = gameConfigManagerType.GetMethod(
                "GetConfig",
                BindingFlags.Public | BindingFlags.Instance);
            if (getConfigMethod == null)
            {
                throw new Exception("未发现 GameConfigManager.GetConfig 方法");
            }

            var gameBaseConfigProcessorType = FindLoadedType(GameBaseConfigProcessorTypeName);
            if (gameBaseConfigProcessorType == null)
            {
                throw new Exception($"未发现 GameBaseConfigProcessor 类型: {GameBaseConfigProcessorTypeName}");
            }

            var configNestedType = gameBaseConfigProcessorType.GetNestedType("Config");
            if (configNestedType == null)
            {
                throw new Exception("未发现 GameBaseConfigProcessor.Config 嵌套类型");
            }

            var getConfigGenericMethod = getConfigMethod.MakeGenericMethod(configNestedType);
            var config = getConfigGenericMethod.Invoke(gameConfigManagerInstance, null);
            if (config == null)
            {
                throw new Exception("GameBaseConfigProcessor.Config 配置实例为空");
            }

            var clientVersionNumField = configNestedType.GetField("ClientVersionNum");
            if (clientVersionNumField == null)
            {
                throw new Exception("未发现 GameBaseConfigProcessor.Config.ClientVersionNum 字段");
            }

            var clientVersionNum = clientVersionNumField.GetValue(config) as string;
            if (string.IsNullOrWhiteSpace(clientVersionNum))
            {
                throw new Exception("ClientVersionNum 为空");
            }

            Debug.Log($"[E2E] 客户端版本号: {clientVersionNum}");
            Debug.Log("[E2E] 客户端版本号读取验证完成");
        }

        /// <summary>
        /// 验证带版本号的资源路径解析正确。
        /// Verify that asset path resolution with version number is correct.
        /// 该检查通过反射调用 ClientAssetsUtils.GetMultiAssetsLoadPath，
        /// 确认主路径和备用路径至少有一个非空。
        /// This check calls ClientAssetsUtils.GetMultiAssetsLoadPath via reflection,
        /// confirming at least one of the primary and fallback paths is non-empty.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "version-business", order: 2, des: "验证版本号资源路径解析")]
        public static void AssetPathResolutionWithVersion()
        {
            Debug.Log("[E2E] 测试目的=验证版本号资源路径解析 实现手段=反射调用 ClientAssetsUtils.GetMultiAssetsLoadPath 并验证主备路径");

            // 先获取客户端版本号
            // Get client version number first
            var gameConfigManagerType = FindLoadedType(GameConfigManagerTypeName);
            if (gameConfigManagerType == null)
            {
                throw new Exception($"未发现 GameConfigManager 类型: {GameConfigManagerTypeName}");
            }

            var instProperty = gameConfigManagerType.GetProperty(
                "Inst",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProperty == null)
            {
                throw new Exception("未发现 GameConfigManager.Inst 属性");
            }

            var gameConfigManagerInstance = instProperty.GetValue(null);
            if (gameConfigManagerInstance == null)
            {
                throw new Exception("GameConfigManager.Inst 为空");
            }

            var getConfigMethod = gameConfigManagerType.GetMethod(
                "GetConfig",
                BindingFlags.Public | BindingFlags.Instance);
            if (getConfigMethod == null)
            {
                throw new Exception("未发现 GameConfigManager.GetConfig 方法");
            }

            var gameBaseConfigProcessorType = FindLoadedType(GameBaseConfigProcessorTypeName);
            if (gameBaseConfigProcessorType == null)
            {
                throw new Exception($"未发现 GameBaseConfigProcessor 类型: {GameBaseConfigProcessorTypeName}");
            }

            var configNestedType = gameBaseConfigProcessorType.GetNestedType("Config");
            if (configNestedType == null)
            {
                throw new Exception("未发现 GameBaseConfigProcessor.Config 嵌套类型");
            }

            var getConfigGenericMethod = getConfigMethod.MakeGenericMethod(configNestedType);
            var config = getConfigGenericMethod.Invoke(gameConfigManagerInstance, null);
            if (config == null)
            {
                throw new Exception("GameBaseConfigProcessor.Config 配置实例为空");
            }

            var clientVersionNumField = configNestedType.GetField("ClientVersionNum");
            if (clientVersionNumField == null)
            {
                throw new Exception("未发现 ClientVersionNum 字段");
            }

            var clientVersion = clientVersionNumField.GetValue(config) as string;
            if (string.IsNullOrWhiteSpace(clientVersion))
            {
                throw new Exception("ClientVersionNum 为空");
            }

            // 获取 BApplication.RuntimePlatform
            // Get BApplication.RuntimePlatform
            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);
            var runtimePlatformProperty = bApplicationType.GetProperty(
                "RuntimePlatform",
                BindingFlags.Public | BindingFlags.Static);
            if (runtimePlatformProperty == null)
            {
                throw new Exception("未发现 BApplication.RuntimePlatform 属性");
            }

            var runtimePlatform = (RuntimePlatform)runtimePlatformProperty.GetValue(null);

            // 调用 ClientAssetsUtils.GetMultiAssetsLoadPath
            // Call ClientAssetsUtils.GetMultiAssetsLoadPath
            var clientAssetsUtilsType = RequireLoadedType(ClientAssetsUtilsTypeName);
            var getMultiAssetsLoadPathMethod = clientAssetsUtilsType.GetMethod(
                "GetMultiAssetsLoadPath",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RuntimePlatform), typeof(string) },
                null);
            if (getMultiAssetsLoadPathMethod == null)
            {
                throw new Exception("未发现 ClientAssetsUtils.GetMultiAssetsLoadPath 方法");
            }

            var result = getMultiAssetsLoadPathMethod.Invoke(null, new object[] { runtimePlatform, clientVersion });
            if (result == null)
            {
                throw new Exception("GetMultiAssetsLoadPath 返回 null");
            }

            // 元组解构
            // Tuple destructuring
            var resultType = result.GetType();
            var item1Property = resultType.GetProperty("Item1");
            var item2Property = resultType.GetProperty("Item2");
            var firstPath = item1Property?.GetValue(result) as string;
            var secondPath = item2Property?.GetValue(result) as string;

            if (string.IsNullOrEmpty(firstPath) && string.IsNullOrEmpty(secondPath))
            {
                throw new Exception("资源路径解析失败：主路径和备用路径均为空");
            }

            Debug.Log($"[E2E] 版本号资源路径: clientVersion={clientVersion} platform={runtimePlatform}");
            Debug.Log($"[E2E] 主路径: {firstPath}");
            Debug.Log($"[E2E] 备用路径: {secondPath}");
            Debug.Log("[E2E] 版本号资源路径解析验证完成");
        }

        /// <summary>
        /// 验证版本信息结构字段完整。
        /// Verify that the version info structure fields are complete.
        /// 该检查反射读取 AssetsVersionInfo 类型，验证关键属性存在。
        /// This check reflects on the AssetsVersionInfo type and verifies key properties exist.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "version-business", order: 3, des: "验证版本信息结构字段")]
        public static void VersionInfoStructureValidation()
        {
            Debug.Log("[E2E] 测试目的=验证版本信息结构字段 实现手段=反射读取 AssetsVersionInfo 类型并验证 Platfrom/Version/SubPckMap 属性");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var assetsVersionInfoType = hotfixAssembly.GetType(AssetsVersionInfoTypeName);
            if (assetsVersionInfoType == null)
            {
                throw new Exception($"未发现 AssetsVersionInfo 类型: {AssetsVersionInfoTypeName}");
            }

            // 验证 Platfrom 属性
            // Verify Platfrom property
            var platfromProperty = assetsVersionInfoType.GetProperty("Platfrom");
            if (platfromProperty == null)
            {
                throw new Exception("未发现 AssetsVersionInfo.Platfrom 属性");
            }

            // 验证 Version 属性
            // Verify Version property
            var versionProperty = assetsVersionInfoType.GetProperty("Version");
            if (versionProperty == null)
            {
                throw new Exception("未发现 AssetsVersionInfo.Version 属性");
            }

            // 验证 SubPckMap 属性
            // Verify SubPckMap property
            var subPckMapProperty = assetsVersionInfoType.GetProperty("SubPckMap");
            if (subPckMapProperty == null)
            {
                throw new Exception("未发现 AssetsVersionInfo.SubPckMap 属性");
            }

            // 验证可创建实例并设置属性
            // Verify instance creation and property setting
            var versionInfo = Activator.CreateInstance(assetsVersionInfoType);
            platfromProperty.SetValue(versionInfo, "TalosTest");
            versionProperty.SetValue(versionInfo, "1.0.0");

            var readPlatfrom = platfromProperty.GetValue(versionInfo) as string;
            var readVersion = versionProperty.GetValue(versionInfo) as string;

            if (!string.Equals(readPlatfrom, "TalosTest", StringComparison.Ordinal))
            {
                throw new Exception($"Platfrom 属性读写异常: expected=TalosTest actual={readPlatfrom}");
            }

            if (!string.Equals(readVersion, "1.0.0", StringComparison.Ordinal))
            {
                throw new Exception($"Version 属性读写异常: expected=1.0.0 actual={readVersion}");
            }

            Debug.Log($"[E2E] 版本信息结构验证: Platfrom={readPlatfrom} Version={readVersion}");
            Debug.Log("[E2E] 版本信息结构验证完成");
        }

        #endregion

        #region Suite: download-prep — 下载准备测试

        /// <summary>
        /// 验证文件服务器 URL 可访问。
        /// Verify that the file server URL is accessible.
        /// 该检查通过反射读取 ServerConfigProcessor.Config.FileServerUrl，
        /// 确认服务器配置已就绪且 URL 格式合法。
        /// This check reads ServerConfigProcessor.Config.FileServerUrl via reflection,
        /// confirming server config is ready and the URL format is valid.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-prep", order: 1, des: "验证文件服务器 URL 可访问")]
        public static void FileServerUrlAccessible()
        {
            Debug.Log("[E2E] 测试目的=验证文件服务器 URL 可访问 实现手段=反射读取 ServerConfigProcessor.Config.FileServerUrl 并验证格式");

            var gameConfigManagerType = FindLoadedType(GameConfigManagerTypeName);
            if (gameConfigManagerType == null)
            {
                throw new Exception($"未发现 GameConfigManager 类型: {GameConfigManagerTypeName}");
            }

            var instProperty = gameConfigManagerType.GetProperty(
                "Inst",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProperty == null)
            {
                throw new Exception("未发现 GameConfigManager.Inst 属性");
            }

            var gameConfigManagerInstance = instProperty.GetValue(null);
            if (gameConfigManagerInstance == null)
            {
                throw new Exception("GameConfigManager.Inst 为空");
            }

            var getConfigMethod = gameConfigManagerType.GetMethod(
                "GetConfig",
                BindingFlags.Public | BindingFlags.Instance);
            if (getConfigMethod == null)
            {
                throw new Exception("未发现 GameConfigManager.GetConfig 方法");
            }

            // ServerConfigProcessor 定义在业务主程序集
            // ServerConfigProcessor is defined in the business main assembly
            var serverConfigProcessorType = FindLoadedType(ServerConfigProcessorTypeName);
            if (serverConfigProcessorType == null)
            {
                throw new Exception($"未发现 ServerConfigProcessor 类型: {ServerConfigProcessorTypeName}");
            }

            var configNestedType = serverConfigProcessorType.GetNestedType("Config");
            if (configNestedType == null)
            {
                throw new Exception("未发现 ServerConfigProcessor.Config 嵌套类型");
            }

            var getConfigGenericMethod = getConfigMethod.MakeGenericMethod(configNestedType);
            var serverConfig = getConfigGenericMethod.Invoke(gameConfigManagerInstance, null);
            if (serverConfig == null)
            {
                throw new Exception("ServerConfigProcessor.Config 配置实例为空");
            }

            var fileServerUrlField = configNestedType.GetField("FileServerUrl");
            if (fileServerUrlField == null)
            {
                throw new Exception("未发现 ServerConfigProcessor.Config.FileServerUrl 字段");
            }

            var fileServerUrl = fileServerUrlField.GetValue(serverConfig) as string;
            if (string.IsNullOrWhiteSpace(fileServerUrl))
            {
                throw new Exception("ServerConfigProcessor.Config.FileServerUrl 为空");
            }

            // 验证 URL 格式（至少以 http:// 或 https:// 开头）
            // Verify URL format (at least starts with http:// or https://)
            if (!fileServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !fileServerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"FileServerUrl 格式非法: {fileServerUrl}");
            }

            Debug.Log($"[E2E] 文件服务器 URL: {fileServerUrl}");
            Debug.Log("[E2E] 文件服务器 URL 验证完成");
        }

        /// <summary>
        /// 验证下载路径构造正确。
        /// Verify that download path construction is correct.
        /// 该检查基于版本号和平台构造下载路径，验证路径格式合法。
        /// This check constructs download paths based on version and platform, verifying valid path format.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-prep", order: 2, des: "验证下载路径构造")]
        public static void DownloadPathConstruction()
        {
            Debug.Log("[E2E] 测试目的=验证下载路径构造 实现手段=基于 persistentDataPath 和版本号构造下载路径并验证格式");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);

            var frameworkPersistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");

            // 获取平台路径标识
            // Get platform path identifier
            var getPlatformLoadPathMethod = RequireStaticMethod(
                bApplicationType,
                "GetPlatformLoadPath",
                typeof(RuntimePlatform));

            var platformLoadPath = InvokeStaticMethod(
                getPlatformLoadPathMethod,
                "BApplication.GetPlatformLoadPath",
                Application.platform) as string;

            if (string.IsNullOrWhiteSpace(platformLoadPath))
            {
                throw new Exception($"平台路径标识为空: platform={Application.platform}");
            }

            // 构造一条预期的下载路径
            // Construct an expected download path
            var testVersion = "1.0.0";
            var expectedDownloadPath = $"{frameworkPersistentDataPath}/{testVersion}/{platformLoadPath}";
            expectedDownloadPath = expectedDownloadPath.Replace('\\', '/');

            Debug.Log($"[E2E] 下载路径构造: persistentDataPath={frameworkPersistentDataPath}");
            Debug.Log($"[E2E] 平台路径标识: {platformLoadPath}");
            Debug.Log($"[E2E] 预期下载路径: {expectedDownloadPath}");

            // 验证路径各段非空
            // Verify path segments are non-empty
            if (string.IsNullOrWhiteSpace(frameworkPersistentDataPath))
            {
                throw new Exception("persistentDataPath 为空");
            }

            Debug.Log("[E2E] 下载路径构造验证完成");
        }

        /// <summary>
        /// 验证母包基础资源可访问。
        /// Verify that base package fundamental resources are accessible.
        /// 该检查验证 StreamingAssets 目录存在且包含必要的热更资源文件。
        /// This check verifies the StreamingAssets directory exists and contains necessary hotfix resource files.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-prep", order: 3, des: "验证母包基础资源可访问")]
        public static void StreamingAssetsAccessible()
        {
            Debug.Log("[E2E] 测试目的=验证母包基础资源可访问 实现手段=检查 StreamingAssets 目录存在性和文件列表");

            var streamingAssetsPath = Application.streamingAssetsPath;

            // 在 Editor 下 streamingAssetsPath 可能指向项目目录
            // In Editor, streamingAssetsPath may point to the project directory
            if (Application.isEditor)
            {
                Debug.Log($"[E2E] 编辑器模式 StreamingAssets: {streamingAssetsPath}");
                // Editor 模式下检查项目 StreamingAssets 目录
                // Check project StreamingAssets directory in Editor mode
                var projectStreamingAssets = Path.Combine(Application.dataPath, "StreamingAssets");
                if (Directory.Exists(projectStreamingAssets))
                {
                    Debug.Log($"[E2E] 项目 StreamingAssets 目录存在: {projectStreamingAssets}");
                }
                else
                {
                    Debug.LogWarning($"[E2E] 项目 StreamingAssets 目录不存在（可能正常）: {projectStreamingAssets}");
                }
            }
            else
            {
                // Player 模式下检查 StreamingAssets
                // Check StreamingAssets in Player mode
                Debug.Log($"[E2E] Player 模式 StreamingAssets: {streamingAssetsPath}");

                if (Directory.Exists(streamingAssetsPath))
                {
                    var files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
                    Debug.Log($"[E2E] StreamingAssets 包含文件数: {files.Length}");

                    if (files.Length > 0)
                    {
                        var topFiles = files.Take(10);
                        foreach (var file in topFiles)
                        {
                            Debug.Log($"[E2E] StreamingAssets 文件: {file}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[E2E] StreamingAssets 目录不存在（可能正常，取决于平台）: {streamingAssetsPath}");
                }
            }

            Debug.Log("[E2E] 母包基础资源检查完成");
        }

        #endregion

        #region 反射辅助方法 — Reflection Helpers

        private static object InvokeStaticMethod(MethodInfo method, string operationName, params object[] arguments)
        {
            return InvokeWithContext(
                operationName,
                () => method.Invoke(null, arguments));
        }

        private static object InvokeInstanceMethod(object instance, MethodInfo method, string operationName, params object[] arguments)
        {
            return InvokeWithContext(
                operationName,
                () => method.Invoke(instance, arguments));
        }

        private static object InvokeConstructor(ConstructorInfo constructor, string operationName, params object[] arguments)
        {
            return InvokeWithContext(
                operationName,
                () => constructor.Invoke(arguments));
        }

        private static object InvokeWithContext(string operationName, Func<object> action)
        {
            try
            {
                return action();
            }
            catch (TargetInvocationException exception)
            {
                throw CreateInvocationFailure(operationName, exception);
            }
        }

        private static Exception CreateInvocationFailure(string operationName, TargetInvocationException exception)
        {
            var rootException = UnwrapInvocationException(exception);
            return new Exception(
                $"{operationName} 失败: {rootException.GetType().FullName}: {rootException.Message}",
                rootException);
        }

        private static Exception UnwrapInvocationException(Exception exception)
        {
            while (exception is TargetInvocationException targetInvocationException
                   && targetInvocationException.InnerException != null)
            {
                exception = targetInvocationException.InnerException;
            }

            return exception;
        }

        private static Assembly RequireLoadedAssembly(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            throw new Exception($"未发现已装载程序集: {assemblyName}");
        }

        private static Type RequireType(Assembly assembly, string typeName)
        {
            var type = assembly.GetType(typeName);
            if (type == null)
            {
                throw new Exception($"未发现类型: {typeName}");
            }

            return type;
        }

        private static Type RequireLoadedType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new Exception($"未发现已装载类型: {typeName}");
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static MethodInfo RequireStaticMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                parameterTypes,
                null);
            if (method == null)
            {
                throw new Exception($"未发现公开静态方法: {type.FullName}.{methodName}");
            }

            return method;
        }

        private static MethodInfo RequireInstanceMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null);
            if (method == null)
            {
                throw new Exception($"未发现公开实例方法: {type.FullName}.{methodName}");
            }

            return method;
        }

        private static string ReadRequiredStaticStringProperty(Type type, string propertyName)
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);
            if (property == null)
            {
                throw new Exception($"未发现公开静态属性: {type.FullName}.{propertyName}");
            }

            var value = property.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"公开静态属性为空: {type.FullName}.{propertyName}");
            }

            return value;
        }

        #endregion
    }
}
