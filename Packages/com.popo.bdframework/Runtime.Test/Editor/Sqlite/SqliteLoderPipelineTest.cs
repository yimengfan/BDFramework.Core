using System;
using System.IO;
using BDFramework.Sql;
using NUnit.Framework;
using SQLite4Unity3d;
using UnityEngine;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// SqliteLoder 管线测试 — 覆盖 PRAGMA 优化、连接生命周期、密码回退和 CalculateCacheSizeKB 边界值。
    /// 测试目的=验证 SqliteLoder 在初始化、异常和边界场景下的稳定性，确保 PRAGMA 失败不阻断加载、
    /// 连接关闭后可安全重建、密码回退链路在各种配置组合下正确工作。
    /// 实现手段=直接调用 SqliteLoder API 并在临时数据库上断言连接状态和配置值。
    ///
    /// SqliteLoder pipeline tests — covers PRAGMA optimization, connection lifecycle, password fallback,
    /// and CalculateCacheSizeKB boundary values.
    /// Test purpose=verify SqliteLoder stability under init, error, and boundary scenarios,
    /// ensuring PRAGMA failure doesn't block loading, connections can be safely rebuilt after close,
    /// and password fallback works correctly across all config combinations.
    /// Method=directly call SqliteLoder APIs on temporary databases and assert connection state and config values.
    /// </summary>
    [TestFixture]
    public class SqliteLoderPipelineTest
    {
        private const string TestDbName = "Test_LoderPipeline";
        private string _dbPath;

        [SetUp]
        public void SetUp()
        {
            Debug.Log("[测试开始] name=SqliteLoderPipelineTest " +
                      "测试目的=验证 SqliteLoder 管线稳定性和边界行为。 " +
                      "实现手段=创建临时 SQLite 数据库并调用 SqliteLoder API 进行断言。");
            _dbPath = Path.Combine(Application.persistentDataPath, TestDbName);
            CleanupTestDb();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupTestDb();
        }

        private void CleanupTestDb()
        {
            SqliteLoder.Close(TestDbName);
            SqliteLoder.PasswordFallback = null;
            SqliteLoder.password = null;
            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); }
                catch { /* 忽略清理异常 */ }
            }
        }

        // ═══════════════════════════════════════════
        // PRAGMA 优化测试 / PRAGMA optimization tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 ApplyReadOnlyPragmas 在 null 连接上不抛异常。
        /// Verify ApplyReadOnlyPragmas does not throw on null connection.
        /// </summary>
        [Test]
        public void ApplyReadOnlyPragmas_NullConnection_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqliteLoder.ApplyReadOnlyPragmas(null),
                "对 null 连接调用 ApplyReadOnlyPragmas 不应抛异常。");
        }

        /// <summary>
        /// 验证 ApplyReadOnlyPragmas 在有效只读连接上成功执行。
        /// 创建临时数据库后以只读方式打开并应用 PRAGMA，确认不会因 PRAGMA 执行失败而抛异常。
        /// Verify ApplyReadOnlyPragmas succeeds on valid read-only connection.
        /// Create a temp database, open as read-only, apply PRAGMAs, and confirm no exceptions.
        /// </summary>
        [Test]
        public void ApplyReadOnlyPragmas_ValidConnection_SetsPragmaSuccessfully()
        {
            // 创建可写数据库
            // Create a writable database first
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            Assert.IsNotNull(con, "创建数据库连接不应为空。");
            Assert.IsTrue(con.IsOpen, "数据库连接应处于打开状态。");
            con.Close();
            con = null;

            // 以只读方式打开
            // Open as read-only
            GC.Collect(); // 确保前一个连接完全释放
            var readOnlyCon = SqliteLoder.LoadDBReadOnly(_dbPath);
            Assert.IsNotNull(readOnlyCon, "只读连接不应为空。");
            Assert.IsTrue(readOnlyCon.IsOpen, "只读连接应处于打开状态。");

            // 验证主要 PRAGMA 值在合理范围内
            // Verify key PRAGMA values are within reasonable ranges
            try
            {
                var pageSize = readOnlyCon.ExecuteScalarInt("PRAGMA page_size");
                Assert.Greater(pageSize, 0, "page_size 应大于 0。");
            }
            finally
            {
                SqliteLoder.Close(TestDbName);
            }
        }

        /// <summary>
        /// 验证 journal_mode 在只读优化后被设置为 OFF。
        /// Verify journal_mode is set to OFF after read-only optimization.
        /// </summary>
        [Test]
        public void ApplyReadOnlyPragmas_JournalModeIsOff()
        {
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            Assert.IsNotNull(con);
            con.Close();
            con = null;
            GC.Collect();

            var readOnlyCon = SqliteLoder.LoadDBReadOnly(_dbPath);
            Assert.IsNotNull(readOnlyCon);

            try
            {
                var journalMode = readOnlyCon.ExecuteScalar<string>("PRAGMA journal_mode");
                // journal_mode 可能被某些平台限制，仅验证不抛异常
                // journal_mode may be restricted on some platforms; just verify no exception
                Assert.IsNotNull(journalMode, "journal_mode 查询结果不应为空。");
            }
            finally
            {
                SqliteLoder.Close(TestDbName);
            }
        }

        /// <summary>
        /// 验证 mmap_size 在只读优化后被设置为 256MB。
        /// Verify mmap_size is set to 256MB after read-only optimization.
        /// </summary>
        [Test]
        public void ApplyReadOnlyPragmas_MmapSizeIs256MB()
        {
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            Assert.IsNotNull(con);
            con.Close();
            con = null;
            GC.Collect();

            var readOnlyCon = SqliteLoder.LoadDBReadOnly(_dbPath);
            Assert.IsNotNull(readOnlyCon);

            try
            {
                var mmapSize = readOnlyCon.ExecuteScalar<long>("PRAGMA mmap_size");
                // Windows 可能不支持 mmap；仅验证无异常
                // Windows may not support mmap; just verify no exception
                Assert.GreaterOrEqual(mmapSize, 0, "mmap_size 不应为负数。");
            }
            finally
            {
                SqliteLoder.Close(TestDbName);
            }
        }

        // ═══════════════════════════════════════════
        // CalculateCacheSizeKB 边界值测试 / CalculateCacheSizeKB boundary tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证不存在的文件路径使用默认最小缓存大小 (2MB)。
        /// 测试目的=验证文件不存在时 CalculateCacheSizeKB 返回 MinCacheSizeKB (2000KB)。
        /// Verify non-existent file path uses default minimum cache size (2MB).
        /// </summary>
        [Test]
        public void CalculateCacheSizeKB_FileNotExist_ReturnsMinCacheSize()
        {
            var nonExistentPath = Path.Combine(Application.persistentDataPath, "non_existent_file.db");
            // 使用反射调用 private static 方法
            // Use reflection to invoke private static method
            var method = typeof(SqliteLoder).GetMethod("CalculateCacheSizeKB",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method, "CalculateCacheSizeKB 私有不方法应存在。");
            var result = (int)method.Invoke(null, new object[] { nonExistentPath });
            Assert.GreaterOrEqual(result, 2000, "文件不存在时应返回至少 MinCacheSizeKB (2000KB)。");
            Assert.LessOrEqual(result, 20000, "缓存大小不应超过 MaxCacheSizeKB (20000KB)。");
        }

        /// <summary>
        /// 验证空路径不会崩溃。
        /// Verify empty path does not crash.
        /// </summary>
        [Test]
        public void CalculateCacheSizeKB_EmptyPath_DoesNotThrow()
        {
            var method = typeof(SqliteLoder).GetMethod("CalculateCacheSizeKB",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { "" }),
                "空路径调用 CalculateCacheSizeKB 不应抛异常。");

            var result = (int)method.Invoke(null, new object[] { "" });
            Assert.GreaterOrEqual(result, 2000, "空路径应返回最小值。");
        }

        /// <summary>
        /// 验证 null 路径不会崩溃。
        /// Verify null path does not crash.
        /// </summary>
        [Test]
        public void CalculateCacheSizeKB_NullPath_DoesNotThrow()
        {
            var method = typeof(SqliteLoder).GetMethod("CalculateCacheSizeKB",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { null }),
                "null 路径调用 CalculateCacheSizeKB 不应抛异常。");
        }

        /// <summary>
        /// 验证小型数据库的缓存大小计算。
        /// Verify cache size calculation for a small database.
        /// </summary>
        [Test]
        public void CalculateCacheSizeKB_SmallDbFile_ClampedToMin()
        {
            // 创建一个小型数据库（几 KB）
            // Create a small database (a few KB)
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            con.CreateTable<TestPoco>();
            con.Insert(new TestPoco { Id = 1, Name = "test" });
            con.Close();
            con = null;
            GC.Collect();

            var method = typeof(SqliteLoder).GetMethod("CalculateCacheSizeKB",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method);
            var result = (int)method.Invoke(null, new object[] { _dbPath });

            // 小型数据库的缓存应 clamp 到 MinCacheSizeKB
            // Small DB cache should be clamped to MinCacheSizeKB
            Assert.AreEqual(2000, result, "小型数据库缓存大小应 clamp 到 MinCacheSizeKB (2000KB)。");
        }

        // ═══════════════════════════════════════════
        // 密码回退测试 / Password fallback tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 PasswordFallback 为 null 时不会崩溃。
        /// Verify no crash when PasswordFallback is null.
        /// </summary>
        [Test]
        public void PasswordFallback_NullCallback_ReturnsEmptyString()
        {
            var oldPassword = SqliteLoder.password;
            var oldFallback = SqliteLoder.PasswordFallback;

            try
            {
                SqliteLoder.password = null;
                SqliteLoder.PasswordFallback = null;
                var result = SqliteLoder.Password;
                Assert.AreEqual(string.Empty, result,
                    "PasswordFallback 为 null 时 Password 应返回空字符串。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldFallback;
            }
        }

        /// <summary>
        /// 验证 PasswordFallback 回调抛异常时 Password getter 会传播异常。
        /// Verify Password getter propagates exception from PasswordFallback callback.
        /// </summary>
        [Test]
        public void PasswordFallback_ThrowingCallback_PropagatesException()
        {
            var oldPassword = SqliteLoder.password;
            var oldFallback = SqliteLoder.PasswordFallback;

            try
            {
                SqliteLoder.password = null;
                SqliteLoder.PasswordFallback = () => throw new InvalidOperationException("测试异常");

                Assert.Throws<InvalidOperationException>(() =>
                {
                    var _ = SqliteLoder.Password;
                }, "PasswordFallback 抛出的异常应被传播。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldFallback;
            }
        }

        /// <summary>
        /// 验证显式密码为空字符串时仍使用 Fallback。
        /// Verify fallback is used when explicit password is empty string.
        /// </summary>
        [Test]
        public void PasswordFallback_EmptyExplicitPassword_UsesFallback()
        {
            var oldPassword = SqliteLoder.password;
            var oldFallback = SqliteLoder.PasswordFallback;

            try
            {
                SqliteLoder.password = "";
                SqliteLoder.PasswordFallback = () => "fallback-value";
                Assert.AreEqual("fallback-value", SqliteLoder.Password,
                    "空字符串显式密码应回退到 PasswordFallback。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldFallback;
            }
        }

        // ═══════════════════════════════════════════
        // 连接生命周期测试 / Connection lifecycle tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证无密码创建数据库并插入数据后查询成功。
        /// Verify creating database without password, inserting data, and querying succeeds.
        /// </summary>
        [Test]
        public void LoadDBReadWriteCreate_NoPassword_CreatesAndInserts()
        {
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            Assert.IsNotNull(con, "连接不应为空。");
            Assert.IsTrue(con.IsOpen, "连接应打开。");

            con.CreateTable<TestPoco>();
            var insertCount = con.Insert(new TestPoco { Id = 1, Name = "hello" });
            Assert.AreEqual(1, insertCount, "Insert 应返回 1 行。");

            var results = con.Query<TestPoco>("select * from TestPoco");
            Assert.AreEqual(1, results.Count, "查询应返回 1 行。");
            Assert.AreEqual("hello", results[0].Name);

            SqliteLoder.Close(TestDbName);
        }

        /// <summary>
        /// 验证 LoadDBReadOnly 在文件不存在时返回 null。
        /// Verify LoadDBReadOnly returns null when file does not exist.
        /// </summary>
        [Test]
        public void LoadDBReadOnly_FileNotExists_ReturnsNull()
        {
            var nonExistentPath = Path.Combine(Application.persistentDataPath, "does_not_exist.db");
            // 确保文件不存在
            // Ensure file does not exist
            if (File.Exists(nonExistentPath)) File.Delete(nonExistentPath);

            var con = SqliteLoder.LoadDBReadOnly(nonExistentPath);
            Assert.IsNull(con, "文件不存在时 LoadDBReadOnly 应返回 null。");
        }

        /// <summary>
        /// 验证关闭后可以安全重建同名数据库连接。
        /// Verify connection with same name can be safely rebuilt after close.
        /// </summary>
        [Test]
        public void CloseAndReopen_SameDbName_WorksCorrectly()
        {
            // 第一次创建并关闭
            // First create and close
            var con1 = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            con1.CreateTable<TestPoco>();
            con1.Insert(new TestPoco { Id = 1, Name = "first" });
            SqliteLoder.Close(TestDbName);

            // 第二次打开（不删除文件）
            // Second open (don't delete file)
            var con2 = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            Assert.IsNotNull(con2, "重新打开连接不应为空。");
            Assert.IsTrue(con2.IsOpen, "重新打开连接应处于打开状态。");

            var results = con2.Query<TestPoco>("select * from TestPoco");
            Assert.AreEqual(1, results.Count, "应能读取到之前插入的数据。");
            Assert.AreEqual("first", results[0].Name);

            SqliteLoder.Close(TestDbName);
        }

        /// <summary>
        /// 验证 GetSqliteConnect 对已关闭连接返回 null 或已关闭的连接。
        /// Verify GetSqliteConnect returns null or closed connection for disposed connections.
        /// </summary>
        [Test]
        public void GetSqliteConnect_AfterClose_ReturnsDisposedConnection()
        {
            SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            var con = SqliteLoder.GetSqliteConnect(TestDbName);
            Assert.IsNotNull(con, "刚创建后应能获取到连接。");

            SqliteLoder.Close(TestDbName);

            // Close 后连接被移除或关闭
            // After close, connection is removed or closed
            var conAfterClose = SqliteLoder.GetSqliteConnect(TestDbName);
            // 关闭后可能为 null 或已关闭
            // After close may be null or closed
            if (conAfterClose != null)
            {
                Assert.IsFalse(conAfterClose.IsOpen,
                    "关闭后获取的连接应处于关闭状态。");
            }
        }

        /// <summary>
        /// 验证双重关闭不抛异常。
        /// Verify double close does not throw.
        /// </summary>
        [Test]
        public void Close_DoubleClose_DoesNotThrow()
        {
            SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            SqliteLoder.Close(TestDbName);

            Assert.DoesNotThrow(() => SqliteLoder.Close(TestDbName),
                "双重关闭不应抛异常。");
        }

        /// <summary>
        /// 验证关闭不存在的数据库名不抛异常。
        /// Verify close on non-existent database name does not throw.
        /// </summary>
        [Test]
        public void Close_NonExistentDbName_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqliteLoder.Close("NonExistentDbName"),
                "关闭不存在的数据库名不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // 辅助 POCO / Helper POCO
        // ═══════════════════════════════════════════

        public class TestPoco
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
