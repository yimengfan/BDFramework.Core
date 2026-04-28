using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SQLite4Unity3d;
using BDFramework;
using BDFramework.Sql;
using BDFramework.Core.Tools;
using Talos.E2E;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// SQLite 数据库 E2E 测试套件。
    /// 验证 BDFramework 的 SQLite 存储系统功能。
    /// SQLite database E2E test suite.
    /// Verifies the BDFramework SQLite storage system functionality.
    /// 
    /// 测试范围：
    /// - 数据库连接与初始化
    /// - 表创建
    /// - CRUD 操作
    /// - 查询功能
    /// 
    /// Coverage:
    /// - Database connection and initialization
    /// - Table creation
    /// - CRUD operations
    /// - Query functionality
    /// 
    /// 注意：直接使用 SQLiteConnection + SQLiteService 进行测试，
    /// 避免框架 SqliteLoder 的连接池映射与测试数据库冲突。
    /// Note: Uses SQLiteConnection + SQLiteService directly to avoid
    /// framework SqliteLoder connection-pool mapping conflicts with the test database.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    static public class SqliteTests
    {
        /// <summary>
        /// 测试用数据表结构，映射所有 SQLite 支持的基础类型。
        /// </summary>
        private class E2ETestRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public float Score { get; set; }
            public bool IsActive { get; set; }
        }

        private const string TestDbName = "TalosE2ETestDB";

        /// <summary>
        /// 测试专用的 SQLiteConnection，直接持有，不经过框架连接池。
        /// </summary>
        private static SQLiteConnection TestConnection;

        /// <summary>
        /// 获取测试数据库路径——使用 persistentDataPath 确保可写。
        /// </summary>
        static private string GetTestDbPath()
        {
            return IPath.Combine(Application.persistentDataPath, TestDbName);
        }

        /// <summary>
        /// 验证 SQLite 数据库可被创建和打开。
        /// 直接创建 SQLiteConnection，不经过框架连接池，避免名称冲突。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite", order: 1, des: "创建并打开测试数据库")]
        static public void CreateAndOpenDatabase()
        {
            var dbPath = GetTestDbPath();
            // 确保测试从干净状态开始
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            // 直接创建连接，不使用密码，不注册到框架连接池
            var cs = new SQLiteConnectionString(dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true);
            TestConnection = new SQLiteConnection(cs);
            Debug.Log($"[E2E] 测试数据库已创建（直连模式）: {dbPath}");
        }

        /// <summary>
        /// 验证表创建和批量插入。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite", order: 2, des: "创建表并插入数据")]
        static public void CreateTableAndInsert()
        {
            if (TestConnection == null)
            {
                throw new Exception($"测试数据库连接为 null，请先执行 CreateAndOpenDatabase");
            }

            // 创建表
            TestConnection.CreateTable<E2ETestRow>();

            // 批量插入
            var rows = new List<E2ETestRow>();
            for (int i = 0; i < 100; i++)
            {
                rows.Add(new E2ETestRow
                {
                    Id = i,
                    Name = $"TestRow_{i}",
                    Score = i * 0.5f,
                    IsActive = i % 2 == 0
                });
            }

            foreach (var row in rows)
            {
                TestConnection.Insert(row);
            }

            Debug.Log($"[E2E] 表创建并插入 {rows.Count} 条数据完成");
        }

        /// <summary>
        /// 验证条件查询功能。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite", order: 3, des: "条件查询数据")]
        static public void QueryWithCondition()
        {
            if (TestConnection == null)
            {
                throw new Exception($"测试数据库连接为 null，请先执行 CreateAndOpenDatabase");
            }

            var results = TestConnection.Table<E2ETestRow>().Where(r => r.Id == 1);
            E2ETestRow found = null;
            int count = 0;
            foreach (var r in results)
            {
                count++;
                found = r;
            }

            if (count != 1 || found == null)
            {
                throw new Exception($"条件查询结果不符合预期: count={count}");
            }

            if (found.Id != 1)
            {
                throw new Exception($"查询结果的 Id 不匹配: expected=1, actual={found.Id}");
            }

            Debug.Log($"[E2E] 条件查询验证通过: Id={found.Id}, Name={found.Name}");
        }

        /// <summary>
        /// 验证全表查询功能。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite", order: 4, des: "全表查询数据")]
        static public void QueryAll()
        {
            if (TestConnection == null)
            {
                throw new Exception($"测试数据库连接为 null，请先执行 CreateAndOpenDatabase");
            }

            var results = TestConnection.Table<E2ETestRow>();
            int count = 0;
            foreach (var _ in results)
            {
                count++;
            }

            if (count != 100)
            {
                throw new Exception($"全表查询结果数量不符合预期: expected=100, actual={count}");
            }

            Debug.Log($"[E2E] 全表查询验证通过: 共 {count} 条");
        }

        /// <summary>
        /// 清理测试数据库。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite", order: 999, des: "清理测试数据库")]
        static public void CleanupDatabase()
        {
            // 关闭连接
            TestConnection?.Dispose();
            TestConnection = null;

            var dbPath = GetTestDbPath();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                Debug.Log("[E2E] 测试数据库已清理");
            }
        }
    }
}
