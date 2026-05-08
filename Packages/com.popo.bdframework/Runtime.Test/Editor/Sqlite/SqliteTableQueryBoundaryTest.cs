using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Sql;
using BDFramework.Test;
using NUnit.Framework;
using SQLite4Unity3d;
using UnityEngine;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// TableQueryForILRuntime 边界与稳定性测试 — 覆盖 Prepared Statement 缓存触发条件、
    /// Where/WhereIn/WhereEqual 边界输入、Limit/OrderBy 组合和 SqliteHelper 连接生命周期。
    /// 测试目的=验证 TableQueryForILRuntime 在边界参数（null、空集合、缓存开关）下的稳定性，
    /// 确保链式查询构建不会因异常输入崩溃且缓存复用路径正确工作。
    /// 实现手段=创建临时数据库并注入测试数据，通过 TableQueryForILRuntime 链式 API 查询并断言结果。
    ///
    /// TableQueryForILRuntime boundary and stability tests — covers Prepared Statement cache trigger
    /// conditions, Where/WhereIn/WhereEqual boundary inputs, Limit/OrderBy combinations,
    /// and SqliteHelper connection lifecycle.
    /// Test purpose=verify TableQueryForILRuntime stability under boundary parameters
    /// (null, empty collections, cache toggle), ensuring fluent query builder doesn't crash on
    /// abnormal input and the cache reuse path works correctly.
    /// Method=create temporary database with test data, query via TableQueryForILRuntime fluent API, and assert results.
    /// </summary>
    [TestFixture]
    public class SqliteTableQueryBoundaryTest
    {
        private const string TestDbName = "Test_QueryBoundary";
        private string _dbPath;

        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} " +
                      "测试目的=验证 TableQueryForILRuntime 边界参数和缓存复用路径的稳定性。 " +
                      "实现手段=创建临时数据库并通过链式 API 查询断言。");
            _dbPath = Path.Combine(Application.persistentDataPath, TestDbName);
            CleanupTestDb();

            // 创建测试数据库和表
            // Create test database and table
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            con.CreateTable<QueryBoundaryPoco>();
            for (int i = 0; i < 20; i++)
            {
                con.Insert(new QueryBoundaryPoco
                {
                    Id = i + 1,
                    Name = $"item-{i + 1}",
                    Score = i * 10,
                    IsActive = i % 2 == 0
                });
            }
            con.Close();
            con = null;
            GC.Collect();

            // 以只读方式打开供查询
            // Open as read-only for queries
            SqliteLoder.LoadDBReadOnly(_dbPath);
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
        // Where 边界测试 / Where boundary tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 Where 传入 null 参数值不崩溃。
        /// Verify Where with null parameter value doesn't crash.
        /// </summary>
        [Test]
        public void Where_NullValue_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                var results = table.Where("Name = {0}", (object)null).FromAll<QueryBoundaryPoco>();
                // null 值在 SQL 中转为空，可能返回 0 结果
                // null value in SQL becomes empty, may return 0 results
            }, "Where 传入 null 不应抛异常。");
        }

        /// <summary>
        /// 验证 Where 使用不存在的列名不崩溃。
        /// Verify Where with non-existent column name doesn't crash (SQL error is expected).
        /// </summary>
        [Test]
        public void Where_NonExistentColumn_ThrowsSqlException()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.Throws<SQLiteException>(() =>
            {
                table.Where("NonExistentCol = 1").FromAll<QueryBoundaryPoco>();
            }, "使用不存在的列名应抛 SQLiteException。");
        }

        // ═══════════════════════════════════════════
        // WhereIn 边界测试 / WhereIn boundary tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 WhereIn 传入空 params 数组不崩溃。
        /// Verify WhereIn with empty params array doesn't crash.
        /// </summary>
        [Test]
        public void WhereIn_EmptyParams_ReturnsEmptyResults()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.WhereIn("Id", new object[] { }).FromAll<QueryBoundaryPoco>();
            Assert.IsNotNull(results, "空 WhereIn 结果不应为 null。");
            Assert.AreEqual(0, results.Count, "空 WhereIn 应返回 0 条结果。");
        }

        /// <summary>
        /// 验证 WhereIn 传入 null params 数组时，FromAll 应返回空或能处理。
        /// Verify WhereIn with null params array is handled safely.
        /// </summary>
        [Test]
        public void WhereIn_NullParams_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                // params object[] 传 null 时数组本身为 null
                // When passing null as params object[], the array itself is null
                var results = table.WhereIn("Id", null as object[]).FromAll<QueryBoundaryPoco>();
                Assert.IsNotNull(results);
            }, "WhereIn 传入 null params 不应抛异常。");
        }

        /// <summary>
        /// 验证 WhereIn 传入空 IEnumerable 返回空结果。
        /// Verify WhereIn with empty IEnumerable returns empty results.
        /// </summary>
        [Test]
        public void WhereIn_EmptyEnumerable_ReturnsEmptyResults()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.WhereIn("Id", new List<int>()).FromAll<QueryBoundaryPoco>();
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count, "空集合 WhereIn 应返回 0 条。");
        }

        /// <summary>
        /// 验证 WhereIn 传入字符串集合正确构建 IN 子句。
        /// Verify WhereIn with string collection correctly builds IN clause.
        /// </summary>
        [Test]
        public void WhereIn_StringCollection_ReturnsCorrectResults()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var names = new List<string> { "item-1", "item-2", "item-3" };
            var results = table.WhereIn("Name", names).FromAll<QueryBoundaryPoco>();
            Assert.AreEqual(3, results.Count);
        }

        // ═══════════════════════════════════════════
        // WhereEqual 边界测试 / WhereEqual boundary tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 WhereEqual 传入空字符串正确匹配。
        /// Verify WhereEqual with empty string matches correctly.
        /// </summary>
        [Test]
        public void WhereEqual_EmptyString_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                var results = table.WhereEqual("Name", "").FromAll<QueryBoundaryPoco>();
                Assert.IsNotNull(results);
            }, "WhereEqual 传入空字符串不应抛异常。");
        }

        /// <summary>
        /// 验证 WhereEqual 传入 null 字符串正确查询。
        /// Verify WhereEqual with null string queries correctly.
        /// </summary>
        [Test]
        public void WhereEqual_NullString_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                var results = table.WhereEqual("Name", (string)null).FromAll<QueryBoundaryPoco>();
                Assert.IsNotNull(results);
            }, "WhereEqual 传入 null 字符串不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // WhereAnd / WhereOr 边界测试 / WhereAnd/WhereOr boundary tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 WhereAnd 传入空 params 数组不崩溃。
        /// Verify WhereAnd with empty params array doesn't crash.
        /// </summary>
        [Test]
        public void WhereAnd_EmptyParams_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                var results = table.WhereAnd("Id", "=", new object[] { }).FromAll<QueryBoundaryPoco>();
                Assert.IsNotNull(results);
            }, "WhereAnd 传入空 params 不应抛异常。");
        }

        /// <summary>
        /// 验证 WhereOr 传入空 params 数组不崩溃。
        /// Verify WhereOr with empty params array doesn't crash.
        /// </summary>
        [Test]
        public void WhereOr_EmptyParams_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                var results = table.WhereOr("Id", "=", new object[] { }).FromAll<QueryBoundaryPoco>();
                Assert.IsNotNull(results);
            }, "WhereOr 传入空 params 不应抛异常。");
        }

        /// <summary>
        /// 验证 WhereOr 传入字符串值正确带引号。
        /// Verify WhereOr with string values correctly quotes them.
        /// </summary>
        [Test]
        public void WhereOr_StringValues_ReturnsCorrectResults()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.WhereOr("Name", "=", "item-1", "item-5").FromAll<QueryBoundaryPoco>();
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results[0].Id);
            Assert.AreEqual(5, results[1].Id);
        }

        // ═══════════════════════════════════════════
        // Limit 边界测试 / Limit boundary tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 Limit(0) 不崩溃。
        /// Verify Limit(0) doesn't crash.
        /// </summary>
        [Test]
        public void Limit_Zero_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                var results = table.Where("Id >= 1").Limit(0).FromAll<QueryBoundaryPoco>();
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count, "Limit(0) 应返回空结果。");
            }, "Limit(0) 不应抛异常。");
        }

        /// <summary>
        /// 验证 Limit(1) 的 From 返回单条而非列表。
        /// Verify Limit(1) with From returns single object, not list.
        /// </summary>
        [Test]
        public void Limit_Single_WithFrom_ReturnsSingleObject()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var result = table.Where("Id = 1").Limit(1).From<QueryBoundaryPoco>();
            Assert.IsNotNull(result, "From 应返回单条非 null 结果。");
            Assert.AreEqual(1, result.Id);
        }

        /// <summary>
        /// 验证 Limit 无匹配结果时 From 返回 null。
        /// Verify From returns null when Limit finds no matching results.
        /// </summary>
        [Test]
        public void Limit_NoMatch_FromReturnsNull()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var result = table.Where("Id = 99999").Limit(1).From<QueryBoundaryPoco>();
            Assert.IsNull(result, "无匹配结果时 From 应返回 null。");
        }

        /// <summary>
        /// 验证 Limit 无匹配结果时 FromAll 返回空列表。
        /// Verify FromAll returns empty list when no matching results.
        /// </summary>
        [Test]
        public void Limit_NoMatch_FromAllReturnsEmptyList()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.Where("Id = 99999").FromAll<QueryBoundaryPoco>();
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count, "无匹配时 FromAll 应返回空列表。");
        }

        // ═══════════════════════════════════════════
        // OrderBy 测试 / OrderBy tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 OrderByDesc 降序排列正确。
        /// Verify OrderByDesc returns descending order.
        /// </summary>
        [Test]
        public void OrderByDesc_ReturnsDescendingOrder()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.Where("Id >= 1").OrderByDesc("Score").FromAll<QueryBoundaryPoco>();
            Assert.AreEqual(20, results.Count);
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.GreaterOrEqual(results[i].Score, results[i + 1].Score,
                    $"索引 {i}: Score 应呈降序排列。");
            }
        }

        /// <summary>
        /// 验证 OrderBy 升序排列正确。
        /// Verify OrderBy returns ascending order.
        /// </summary>
        [Test]
        public void OrderBy_ReturnsAscendingOrder()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.Where("Id >= 1").OrderBy("Score").FromAll<QueryBoundaryPoco>();
            Assert.AreEqual(20, results.Count);
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.LessOrEqual(results[i].Score, results[i + 1].Score,
                    $"索引 {i}: Score 应呈升序排列。");
            }
        }

        // ═══════════════════════════════════════════
        // Exec (原始 SQL) 测试 / Exec (raw SQL) tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 Exec 原始 SQL 正确执行。
        /// Verify Exec with raw SQL works correctly.
        /// </summary>
        [Test]
        public void Exec_RawSql_ReturnsCorrectResults()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            var results = table.Exec("select * from QueryBoundaryPoco where Score > 100").FromAll<QueryBoundaryPoco>();
            Assert.Greater(results.Count, 0, "Score > 100 应有匹配结果。");
            foreach (var r in results)
            {
                Assert.Greater(r.Score, 100, $"Id={r.Id} 的 Score={r.Score} 应 > 100。");
            }
        }

        /// <summary>
        /// 验证 Exec 无效 SQL 时抛异常。
        /// Verify Exec with invalid SQL throws exception.
        /// </summary>
        [Test]
        public void Exec_InvalidSql_ThrowsSqlException()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.Throws<SQLiteException>(() =>
            {
                table.Exec("SELECT * FROM NonExistentTable").FromAll<QueryBoundaryPoco>();
            }, "无效表名的 SQL 应抛 SQLiteException。");
        }

        // ═══════════════════════════════════════════
        // Prepared Statement 缓存测试 / Prepared Statement cache tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 EnableSqlCahce 不抛异常。
        /// Verify EnableSqlCahce doesn't throw.
        /// </summary>
        [Test]
        public void PsCache_Enable_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            Assert.DoesNotThrow(() =>
            {
                table.EnableSqlCahce(triggerCacheNum: 3);
            }, "EnableSqlCahce 不应抛异常。");
        }

        /// <summary>
        /// 验证 PS 缓存启用后多次相同查询不崩溃。
        /// Verify repeated identical queries don't crash when PS cache is enabled.
        /// </summary>
        [Test]
        public void PsCache_RepeatedQuery_DoesNotThrow()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            table.EnableSqlCahce(triggerCacheNum: 3);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var results = table.Where("Id = 1").FromAll<QueryBoundaryPoco>();
                    Assert.AreEqual(1, results.Count);
                    Assert.AreEqual(1, results[0].Id);
                }
            }, "PS 缓存模式下多次相同查询不应崩溃。");
        }

        /// <summary>
        /// 验证 PS 缓存启用后查询结果始终正确。
        /// Verify query results are always correct when PS cache is enabled.
        /// </summary>
        [Test]
        public void PsCache_ResultsCorrect()
        {
            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            table.EnableSqlCahce(triggerCacheNum: 2);

            for (int i = 0; i < 15; i++)
            {
                // 交替查询两个不同条件
                // Alternate between two different conditions
                if (i % 2 == 0)
                {
                    var results = table.Where("Id = 5").FromAll<QueryBoundaryPoco>();
                    Assert.AreEqual(1, results.Count, $"迭代 {i}: Id=5 应返回 1 条。");
                }
                else
                {
                    var results = table.Where("Id = 10").FromAll<QueryBoundaryPoco>();
                    Assert.AreEqual(1, results.Count, $"迭代 {i}: Id=10 应返回 1 条。");
                }
            }
        }

        /// <summary>
        /// 验证 PS 缓存跨不同表查询不混淆。
        /// Verify PS cache doesn't confuse queries across different tables.
        /// 测试目的=确保 Prepared Statement 缓存在多表查询场景下不会返回错误类型的数据。
        /// </summary>
        [Test]
        public void PsCache_CrossTable_NoConfusion()
        {
            // 创建第二个测试表
            // Create a second test table
            var con = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            con.CreateTable<SecondTablePoco>();
            con.Insert(new SecondTablePoco { Id = 100, Data = "second-table-data" });
            con.Close();
            con = null;
            GC.Collect();

            SqliteLoder.LoadDBReadOnly(_dbPath);

            var table = SqliteHelper.GetDB(TestDbName).GetTableRuntime();
            table.EnableSqlCahce(triggerCacheNum: 2);

            // 查询两个不同表，确保结果类型正确
            // Query two different tables, ensure result types are correct
            for (int i = 0; i < 8; i++)
            {
                var results1 = table.Where("Id >= 1").FromAll<QueryBoundaryPoco>();
                Assert.Greater(results1.Count, 0, $"迭代 {i}: QueryBoundaryPoco 应有结果。");
                Assert.IsInstanceOf<QueryBoundaryPoco>(results1[0]);

                var results2 = table.Where("Id = 100").FromAll<SecondTablePoco>();
                Assert.AreEqual(1, results2.Count, $"迭代 {i}: SecondTablePoco 应有 1 条结果。");
                Assert.IsInstanceOf<SecondTablePoco>(results2[0]);
                Assert.AreEqual("second-table-data", results2[0].Data);
            }
        }

        // ═══════════════════════════════════════════
        // SqliteHelper 连接生命周期测试 / SqliteHelper connection lifecycle tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 GetDB 对已关闭的连接返回 null。
        /// Verify GetDB returns null for closed connections.
        /// </summary>
        [Test]
        public void GetDB_AfterClose_ReturnsNull()
        {
            var dbBeforeClose = SqliteHelper.GetDB(TestDbName);
            Assert.IsNotNull(dbBeforeClose, "关闭前的 GetDB 应返回有效服务。");

            SqliteLoder.Close(TestDbName);

            var dbAfterClose = SqliteHelper.GetDB(TestDbName);
            Assert.IsNull(dbAfterClose, "关闭后的 GetDB 应返回 null（IsClose=true 导致不返回旧实例）。");
        }

        /// <summary>
        /// 验证 SQLiteService.IsClose 在连接关闭后为 true。
        /// Verify SQLiteService.IsClose returns true after connection is closed.
        /// </summary>
        [Test]
        public void SQLiteService_IsClose_AfterDispose_ReturnsTrue()
        {
            var db = SqliteHelper.GetDB(TestDbName);
            Assert.IsNotNull(db);
            Assert.IsFalse(db.IsClose, "活动连接应返回 IsClose=false。");

            SqliteLoder.Close(TestDbName);

            Assert.IsTrue(db.IsClose, "连接关闭后 IsClose 应为 true。");
        }

        /// <summary>
        /// 验证 SQLiteService.DBPath 返回正确路径。
        /// Verify SQLiteService.DBPath returns correct path.
        /// </summary>
        [Test]
        public void SQLiteService_DBPath_ReturnsCorrectPath()
        {
            var db = SqliteHelper.GetDB(TestDbName);
            Assert.IsNotNull(db);
            Assert.AreEqual(_dbPath, db.DBPath, "DBPath 应与创建路径一致。");
        }

        // ═══════════════════════════════════════════
        // 辅助 POCO / Helper POCOs
        // ═══════════════════════════════════════════

        public class QueryBoundaryPoco
        {
            [PrimaryKey]
            public int Id { get; set; }
            public string Name { get; set; }
            public int Score { get; set; }
            public bool IsActive { get; set; }
        }

        public class SecondTablePoco
        {
            [PrimaryKey]
            public int Id { get; set; }
            public string Data { get; set; }
        }
    }
}
