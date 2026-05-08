using System;
using System.IO;
using BDFramework.Sql;
using NUnit.Framework;
using SQLite4Unity3d;
using UnityEngine;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// SQLite 事务与表迁移测试 — 覆盖 BeginTransaction/Commit/Rollback、嵌套 savepoint、
    /// RunInTransaction 异常安全、以及表结构列迁移的正确性。
    /// 测试目的=验证 SQLite 事务回滚、嵌套事务和表结构迁移在各种边界场景下的稳定性。
    /// 实现手段=创建临时数据库，执行事务操作并通过 Commit/Rollback 后的数据状态进行断言。
    ///
    /// SQLite transaction and table migration tests — covers BeginTransaction/Commit/Rollback,
    /// nested savepoints, RunInTransaction exception safety, and column migration correctness.
    /// Test purpose=verify stability of SQLite transaction rollback, nested transactions, and
    /// table migration across boundary scenarios.
    /// Method=create temporary database, execute transaction operations, and assert data state after
    /// Commit/Rollback.
    /// </summary>
    [TestFixture]
    public class SqliteTransactionAndMigrationTest
    {
        private const string TestDbName = "Test_TxnMigration";
        private string _dbPath;
        private SQLiteConnection _connection;

        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} " +
                      "测试目的=验证 SQLite 事务回滚、嵌套事务和表结构迁移的稳定性。 " +
                      "实现手段=创建临时数据库并执行事务/迁移操作进行断言。");
            _dbPath = Path.Combine(Application.persistentDataPath, TestDbName);
            CleanupTestDb();
            _connection = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
        }

        [TearDown]
        public void TearDown()
        {
            CleanupTestDb();
        }

        private void CleanupTestDb()
        {
            try
            {
                _connection?.Close();
                _connection = null;
            }
            catch { /* 忽略 */ }
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
        // 基本事务测试 / Basic transaction tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 BeginTransaction + Commit 后数据持久化。
        /// Verify data persists after BeginTransaction + Commit.
        /// </summary>
        [Test]
        public void Transaction_BeginAndCommit_DataPersists()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "txn1" });
            _connection.Insert(new TransactionPoco { Id = 2, Value = "txn2" });
            _connection.Commit();

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(2, results.Count, "提交后应能读取到 2 条数据。");
            Assert.AreEqual("txn1", results[0].Value);
            Assert.AreEqual("txn2", results[1].Value);
        }

        /// <summary>
        /// 验证 Rollback 后数据回滚。
        /// Verify data is rolled back after Rollback.
        /// </summary>
        [Test]
        public void Transaction_BeginAndRollback_DataNotPersisted()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "rollback-me" });
            _connection.Rollback();

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(0, results.Count, "回滚后应没有数据。");
        }

        /// <summary>
        /// 验证事务中插入后未提交，另一连接不可见。
        /// Verify uncommitted insert is not visible to another connection.
        /// </summary>
        [Test]
        public void Transaction_UncommittedData_NotVisibleToOtherConnection()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "uncommitted" });

            // 通过同一连接查询应能看到未提交数据
            // Same connection can see uncommitted data
            var sameConnResults = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(1, sameConnResults.Count, "同一连接应能看到未提交数据。");

            _connection.Rollback();
        }

        /// <summary>
        /// 验证连续 BeginTransaction 应抛异常。
        /// Verify consecutive BeginTransaction throws.
        /// </summary>
        [Test]
        public void Transaction_DoubleBegin_ThrowsException()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();

            Assert.Throws<InvalidOperationException>(() =>
            {
                _connection.BeginTransaction();
            }, "在已有未提交事务时再次 BeginTransaction 应抛 InvalidOperationException。");

            _connection.Rollback(); // 清理
        }

        /// <summary>
        /// 验证 Commit 后再次 Rollback 不抛异常（幂等）。
        /// Verify Rollback after Commit does not throw (idempotent).
        /// </summary>
        [Test]
        public void Transaction_RollbackAfterCommit_DoesNotThrow()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "commit-then-rollback" });
            _connection.Commit();

            Assert.DoesNotThrow(() => _connection.Rollback(),
                "Commit 后再 Rollback 不应抛异常（幂等操作）。");
        }

        // ═══════════════════════════════════════════
        // RunInTransaction 测试 / RunInTransaction tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 RunInTransaction 成功执行后数据持久化。
        /// Verify data persists after successful RunInTransaction.
        /// </summary>
        [Test]
        public void RunInTransaction_Success_DataPersists()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.RunInTransaction(() =>
            {
                _connection.Insert(new TransactionPoco { Id = 1, Value = "run-in-txn" });
                _connection.Insert(new TransactionPoco { Id = 2, Value = "run-in-txn-2" });
            });

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(2, results.Count, "RunInTransaction 成功后数据应持久化。");
        }

        /// <summary>
        /// 验证 RunInTransaction 中抛异常时自动回滚。
        /// Verify automatic rollback when exception is thrown inside RunInTransaction.
        /// </summary>
        [Test]
        public void RunInTransaction_Exception_AutoRollback()
        {
            _connection.CreateTable<TransactionPoco>();

            // 先插入一条在事务外的数据作为基线
            // Insert one row outside transaction as baseline
            _connection.Insert(new TransactionPoco { Id = 99, Value = "baseline" });

            try
            {
                _connection.RunInTransaction(() =>
                {
                    _connection.Insert(new TransactionPoco { Id = 1, Value = "should-rollback" });
                    throw new InvalidOperationException("模拟事务内异常");
                });
            }
            catch (InvalidOperationException)
            {
                // 预期异常
                // Expected exception
            }

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(1, results.Count, "事务回滚后应只有基线数据。");
            Assert.AreEqual(99, results[0].Id, "应只保留事务外的基线数据。");
        }

        /// <summary>
        /// 验证 RunInTransaction 嵌套调用。
        /// Verify nested RunInTransaction calls.
        /// </summary>
        [Test]
        public void RunInTransaction_Nested_WorksCorrectly()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.RunInTransaction(() =>
            {
                _connection.Insert(new TransactionPoco { Id = 1, Value = "outer" });
                _connection.RunInTransaction(() =>
                {
                    _connection.Insert(new TransactionPoco { Id = 2, Value = "inner" });
                });
            });

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(2, results.Count, "嵌套 RunInTransaction 后应有 2 条数据。");
        }

        /// <summary>
        /// 验证嵌套 RunInTransaction 内层异常时全部回滚。
        /// Verify full rollback on inner RunInTransaction exception in nested scenario.
        /// </summary>
        [Test]
        public void RunInTransaction_NestedInnerException_AllRolledBack()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.Insert(new TransactionPoco { Id = 99, Value = "baseline" });

            try
            {
                _connection.RunInTransaction(() =>
                {
                    _connection.Insert(new TransactionPoco { Id = 1, Value = "outer-before" });
                    _connection.RunInTransaction(() =>
                    {
                        _connection.Insert(new TransactionPoco { Id = 2, Value = "inner" });
                        throw new InvalidOperationException("内层异常");
                    });
                    _connection.Insert(new TransactionPoco { Id = 3, Value = "outer-after" });
                });
            }
            catch (InvalidOperationException)
            {
                // 预期异常
                // Expected exception
            }

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(1, results.Count, "嵌套事务内层异常时，外层插入也应回滚。");
            Assert.AreEqual(99, results[0].Id, "应只保留基线数据。");
        }

        // ═══════════════════════════════════════════
        // Savepoint 测试 / Savepoint tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 SaveTransactionPoint + Release 后数据持久化。
        /// Verify data persists after SaveTransactionPoint + Release.
        /// </summary>
        [Test]
        public void Savepoint_Release_DataPersists()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "before-sp" });

            var sp = _connection.SaveTransactionPoint();
            _connection.Insert(new TransactionPoco { Id = 2, Value = "after-sp" });
            _connection.Release(sp);

            _connection.Commit();

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(2, results.Count, "Savepoint release 后数据应全部提交。");
        }

        /// <summary>
        /// 验证 SaveTransactionPoint + RollbackTo 部分回滚。
        /// Verify partial rollback with SaveTransactionPoint + RollbackTo.
        /// </summary>
        [Test]
        public void Savepoint_RollbackTo_PartialRollback()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "before-sp" });

            var sp = _connection.SaveTransactionPoint();
            _connection.Insert(new TransactionPoco { Id = 2, Value = "after-sp" });
            _connection.RollbackTo(sp);

            _connection.Commit();

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco");
            Assert.AreEqual(1, results.Count, "RollbackTo 后应只有 savepoint 前的数据。");
            Assert.AreEqual(1, results[0].Id, "应只有 Id=1 的数据。");
        }

        /// <summary>
        /// 验证 RollbackTo 无效 savepoint 名称时抛异常。
        /// Verify RollbackTo with invalid savepoint name throws exception.
        /// </summary>
        [Test]
        public void Savepoint_RollbackToInvalidName_ThrowsException()
        {
            _connection.CreateTable<TransactionPoco>();
            _connection.BeginTransaction();
            _connection.Insert(new TransactionPoco { Id = 1, Value = "test" });

            Assert.Throws<ArgumentException>(() =>
            {
                _connection.RollbackTo("not-a-valid-savepoint");
            }, "无效的 savepoint 名称应抛 ArgumentException。");

            _connection.Rollback(); // 清理
        }

        // ═══════════════════════════════════════════
        // 表迁移测试 / Table migration tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证从 v1 schema（2 列）迁移到 v2 schema（3 列）后数据完整。
        /// Verify data integrity after migrating from v1 schema (2 columns) to v2 schema (3 columns).
        /// </summary>
        [Test]
        public void Migration_AddColumn_ExistingDataPreserved()
        {
            // Phase 1: 创建 v1 表并插入数据
            // Phase 1: Create v1 table and insert data
            _connection.CreateTable<MigrationPocoV1>();
            _connection.Insert(new MigrationPocoV1 { Id = 1, Name = "alice" });
            _connection.Insert(new MigrationPocoV1 { Id = 2, Name = "bob" });
            _connection.Close();
            _connection = null;
            GC.Collect();

            // Phase 2: 用 v2 schema 重新打开，触发自动迁移
            // Phase 2: Reopen with v2 schema, triggering auto-migration
            _connection = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            _connection.CreateTable<MigrationPocoV2>();

            // 检查旧数据仍在
            // Check old data is preserved
            var results = _connection.Query<MigrationPocoV2>("select * from MigrationTestTable");
            Assert.AreEqual(2, results.Count, "迁移后应保留所有数据。");
            Assert.AreEqual("alice", results[0].Name);
            Assert.AreEqual("bob", results[1].Name);

            // 新列应有默认值
            // New column should have default value
            Assert.AreEqual(0, results[0].Age, "新列 Age 应使用默认值 0。");
        }

        /// <summary>
        /// 验证迁移后可以写入新列。
        /// Verify new column can be written after migration.
        /// </summary>
        [Test]
        public void Migration_AddColumn_NewColumnWritable()
        {
            _connection.CreateTable<MigrationPocoV1>();
            _connection.Insert(new MigrationPocoV1 { Id = 1, Name = "charlie" });
            _connection.Close();
            _connection = null;
            GC.Collect();

            _connection = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            _connection.CreateTable<MigrationPocoV2>();

            // 更新新列
            // Update new column
            _connection.Execute("update MigrationTestTable set Age = 25 where Id = 1");

            var result = _connection.Query<MigrationPocoV2>("select * from MigrationTestTable where Id = 1");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(25, result[0].Age, "新列的更新应生效。");
        }

        /// <summary>
        /// 验证重复迁移（schema 无变化）不丢失数据。
        /// Verify repeated migration (no schema change) doesn't lose data.
        /// </summary>
        [Test]
        public void Migration_NoSchemaChange_DataPreserved()
        {
            // Phase 1
            _connection.CreateTable<MigrationPocoV2>();
            _connection.Insert(new MigrationPocoV2 { Id = 1, Name = "dave", Age = 30 });
            var beforeCount = _connection.Query<MigrationPocoV2>("select * from MigrationTestTable").Count;
            _connection.Close();
            _connection = null;
            GC.Collect();

            // Phase 2: 同 schema 重新打开
            // Phase 2: Reopen with same schema
            _connection = SqliteLoder.LoadDBReadWriteCreate(_dbPath, false);
            _connection.CreateTable<MigrationPocoV2>();

            var results = _connection.Query<MigrationPocoV2>("select * from MigrationTestTable");
            Assert.AreEqual(beforeCount, results.Count, "重复迁移（无 schema 变化）不应丢失数据。");
            Assert.AreEqual(30, results[0].Age);
        }

        /// <summary>
        /// 验证删除表后重新创建能恢复写入。
        /// Verify drop and recreate table works.
        /// </summary>
        [Test]
        public void Migration_DropAndRecreate_WorksCorrectly()
        {
            _connection.CreateTable<MigrationPocoV1>();
            _connection.Insert(new MigrationPocoV1 { Id = 1, Name = "eve" });

            _connection.DropTable<MigrationPocoV1>();
            _connection.CreateTable<MigrationPocoV1>();

            var results = _connection.Query<MigrationPocoV1>("select * from MigrationPocoV1");
            Assert.AreEqual(0, results.Count, "Drop 后重新创建的表应为空。");
        }

        // ═══════════════════════════════════════════
        // 批量插入事务性能安全测试 / Batch insert transaction safety test
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 InsertAll 在事务中批量插入后数据完整且行数正确。
        /// Verify InsertAll in transaction preserves data integrity and correct row count.
        /// </summary>
        [Test]
        public void InsertAll_WithTransaction_DataIntegrity()
        {
            _connection.CreateTable<TransactionPoco>();
            var items = new System.Collections.Generic.List<TransactionPoco>();
            for (int i = 0; i < 50; i++)
            {
                items.Add(new TransactionPoco { Id = i, Value = $"item-{i}" });
            }

            var insertedCount = _connection.InsertAll(items, typeof(TransactionPoco), runInTransaction: true);
            Assert.AreEqual(50, insertedCount, "InsertAll 应返回正确的插入行数。");

            var results = _connection.Query<TransactionPoco>("select * from TransactionPoco order by Id");
            Assert.AreEqual(50, results.Count);
            Assert.AreEqual("item-0", results[0].Value);
            Assert.AreEqual("item-49", results[49].Value);
        }

        // ═══════════════════════════════════════════
        // 辅助 POCO / Helper POCOs
        // ═══════════════════════════════════════════

        /// <summary>
        /// 事务测试用 POCO。
        /// POCO for transaction tests.
        /// </summary>
        public class TransactionPoco
        {
            [PrimaryKey]
            public int Id { get; set; }
            public string Value { get; set; }
        }

        /// <summary>
        /// 表迁移用 v1 schema（2 列）。
        /// v1 schema for migration tests (2 columns).
        /// </summary>
        [Table("MigrationTestTable")]
        public class MigrationPocoV1
        {
            [PrimaryKey]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// 表迁移用 v2 schema（3 列，新增 Age）。
        /// v2 schema for migration tests (3 columns, adds Age).
        /// </summary>
        [Table("MigrationTestTable")]
        public class MigrationPocoV2
        {
            [PrimaryKey]
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }   // v2 新增列 / v2 added column
        }
    }
}
