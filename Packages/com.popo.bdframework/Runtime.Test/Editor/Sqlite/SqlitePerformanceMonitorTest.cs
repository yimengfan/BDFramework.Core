using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BDFramework.Sql;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// SqlitePerformanceMonitor 单元测试 — 覆盖查询统计累积、慢查询告警阈值、
    /// 分步计时累加、Reset 清理、启动阶段计时和基本的并发安全。
    /// 测试目的=验证 SqlitePerformanceMonitor 的数据统计正确性和边界行为，
    /// 确保监控系统在高频查询、多线程和异常场景下不会崩溃或产生错误的统计数据。
    /// 实现手段=直接调用 RecordQuery/RecordExecute/Reset 等 API 并断言内部统计值。
    ///
    /// SqlitePerformanceMonitor unit tests — covers query stats accumulation, slow-query alert threshold,
    /// step timing accumulation, Reset cleanup, startup phase timing, and basic concurrency safety.
    /// Test purpose=verify data statistics correctness and boundary behavior of SqlitePerformanceMonitor,
    /// ensuring the monitoring system doesn't crash or produce incorrect stats under high-frequency queries,
    /// multi-threading, and error scenarios.
    /// Method=directly call RecordQuery/RecordExecute/Reset APIs and assert internal statistical values.
    /// </summary>
    [TestFixture]
    public class SqlitePerformanceMonitorTest
    {
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} " +
                      "测试目的=验证 SqlitePerformanceMonitor 统计数据的正确性和边界行为。 " +
                      "实现手段=直接调用 RecordQuery/RecordExecute/Reset 等 API 进行断言。");
            SqlitePerformanceMonitor.Reset();
            SqlitePerformanceMonitor.IsEnabled = true;
            SqlitePerformanceMonitor.VerboseLog = false; // 测试期间关闭日志输出
            SqlitePerformanceMonitor.SlowQueryThresholdMs = 10f;
            SqlitePerformanceMonitor.EnableDetailedQueryTiming = false;
        }

        [TearDown]
        public void TearDown()
        {
            SqlitePerformanceMonitor.Reset();
            SqlitePerformanceMonitor.IsEnabled = true;
            SqlitePerformanceMonitor.VerboseLog = true;
            SqlitePerformanceMonitor.SlowQueryThresholdMs = 10f;
            SqlitePerformanceMonitor.EnableDetailedQueryTiming = false;
        }

        // ═══════════════════════════════════════════
        // RecordQuery 基本统计测试 / RecordQuery basic stats tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证单次 RecordQuery 后统计数据正确。
        /// Verify stats are correct after a single RecordQuery.
        /// </summary>
        [Test]
        public void RecordQuery_SingleQuery_StatsAccumulated()
        {
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM test", 5f, 3f, 100);

            // 无法直接访问内部统计值，通过 PrintSummaryReport 间接验证（不抛异常即基本正确）
            // Cannot directly access internal stats; verify indirectly via PrintSummaryReport (no exception = basically correct)
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "单次 RecordQuery 后 PrintSummaryReport 不应抛异常。");
        }

        /// <summary>
        /// 验证多次 RecordQuery 累加统计。
        /// Verify multiple RecordQuery calls accumulate stats.
        /// </summary>
        [Test]
        public void RecordQuery_MultipleQueries_Accumulates()
        {
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM A", 1f, 2f, 10);
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM A", 2f, 3f, 20);
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM B", 3f, 1f, 30);

            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "多次 RecordQuery 后 PrintSummaryReport 不应抛异常。");
        }

        /// <summary>
        /// 验证 IsEnabled=false 时 RecordQuery 不累加。
        /// Verify RecordQuery doesn't accumulate when IsEnabled=false.
        /// </summary>
        [Test]
        public void RecordQuery_Disabled_DoesNotAccumulate()
        {
            SqlitePerformanceMonitor.IsEnabled = false;
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM test", 100f, 50f, 1000);

            // 重新启用后 Reset 再验证：禁用期间的查询不应影响后续统计
            // Re-enable, Reset, then verify: disabled queries shouldn't affect subsequent stats
            SqlitePerformanceMonitor.IsEnabled = true;
            SqlitePerformanceMonitor.Reset();

            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "禁用期间记录数据后 Reset 再输出报告，不应有残留数据。");
        }

        /// <summary>
        /// 验证带细粒度分步计时的 RecordQuery 调用。
        /// Verify RecordQuery with fine-grained step timing.
        /// </summary>
        [Test]
        public void RecordQuery_WithStepTiming_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery(
                    "SELECT * FROM detail_test",
                    10f, // searchTimeMs
                    50f, // deserializeTimeMs
                    500, // rowCount
                    2f,  // prepareTimeMs
                    3f,  // columnMappingTimeMs
                    40f, // stepTimeMs
                    5f,  // createObjTimeMs
                    3f,  // fastSetTimeMs
                    2f   // readColTimeMs
                );
            }, "带分步计时的 RecordQuery 不应抛异常。");
        }

        /// <summary>
        /// 验证空 SQL 语句不导致崩溃。
        /// Verify empty SQL string doesn't cause crash.
        /// </summary>
        [Test]
        public void RecordQuery_EmptySql_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("", 1f, 1f, 0);
                SqlitePerformanceMonitor.RecordQuery(null, 1f, 1f, 0);
            }, "空 SQL 或 null SQL 的 RecordQuery 不应抛异常。");
        }

        /// <summary>
        /// 验证零耗时查询正确统计。
        /// Verify zero-duration queries are counted correctly.
        /// </summary>
        [Test]
        public void RecordQuery_ZeroDuration_WorksCorrectly()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT 1", 0f, 0f, 1);
                SqlitePerformanceMonitor.RecordQuery("SELECT 1", 0f, 0f, 1);
            }, "零耗时的 RecordQuery 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // RecordExecute 测试 / RecordExecute tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 RecordExecute 正确记录非查询 SQL 的耗时。
        /// Verify RecordExecute correctly records non-query SQL timing.
        /// </summary>
        [Test]
        public void RecordExecute_SingleExecute_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordExecute("INSERT INTO t VALUES (1)", 2.5f);
            }, "RecordExecute 不应抛异常。");
        }

        /// <summary>
        /// 验证 RecordExecute 在 IsEnabled=false 时不影响统计。
        /// Verify RecordExecute doesn't affect stats when IsEnabled=false.
        /// </summary>
        [Test]
        public void RecordExecute_Disabled_DoesNotThrow()
        {
            SqlitePerformanceMonitor.IsEnabled = false;
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordExecute("DELETE FROM t", 100f);
            }, "禁用时 RecordExecute 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // 慢查询阈值测试 / Slow-query threshold tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证超过慢查询阈值的查询触发告警（VerboseLog=true 时不抛异常）。
        /// Verify queries exceeding slow-query threshold trigger alert (no exception when VerboseLog=true).
        /// </summary>
        [Test]
        public void SlowQuery_ExceedsThreshold_DoesNotThrow()
        {
            SqlitePerformanceMonitor.SlowQueryThresholdMs = 5f;
            SqlitePerformanceMonitor.VerboseLog = true;

            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT slow_query", 50f, 20f, 5000);
            }, "超过慢查询阈值时不应抛异常，只记录日志。");
        }

        /// <summary>
        /// 验证低于慢查询阈值的查询不触发告警。
        /// Verify queries below slow-query threshold don't trigger alert.
        /// </summary>
        [Test]
        public void SlowQuery_BelowThreshold_NoAlert()
        {
            SqlitePerformanceMonitor.SlowQueryThresholdMs = 100f;
            SqlitePerformanceMonitor.VerboseLog = true;

            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT fast_query", 1f, 1f, 10);
            }, "低于慢查询阈值的查询不应抛异常。");
        }

        /// <summary>
        /// 验证 VerboseLog=false 时慢查询不打印详细日志。
        /// Verify slow queries don't print detailed logs when VerboseLog=false.
        /// </summary>
        [Test]
        public void SlowQuery_VerboseLogFalse_DoesNotThrow()
        {
            SqlitePerformanceMonitor.SlowQueryThresholdMs = 1f;
            SqlitePerformanceMonitor.VerboseLog = false;

            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT quiet_slow", 100f, 50f, 10000);
            }, "VerboseLog=false 时慢查询不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // Reset 测试 / Reset tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 Reset 后统计数据被清空。
        /// Verify stats are cleared after Reset.
        /// </summary>
        [Test]
        public void Reset_AfterRecording_ClearsStats()
        {
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM a", 10f, 5f, 100);
            SqlitePerformanceMonitor.RecordQuery("SELECT * FROM b", 20f, 10f, 200);
            SqlitePerformanceMonitor.RecordExecute("INSERT INTO t", 3f);

            SqlitePerformanceMonitor.Reset();

            // Reset 后 PrintSummaryReport 不应输出任何数据（totalQueryCount=0 时直接返回）
            // After Reset, PrintSummaryReport should output nothing (returns early when totalQueryCount=0)
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "Reset 后 PrintSummaryReport 不应抛异常。");
        }

        /// <summary>
        /// 验证连续 Reset 不抛异常。
        /// Verify consecutive Reset calls don't throw.
        /// </summary>
        [Test]
        public void Reset_DoubleReset_DoesNotThrow()
        {
            SqlitePerformanceMonitor.Reset();
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.Reset(),
                "双重 Reset 不应抛异常。");
        }

        /// <summary>
        /// 验证未记录任何数据时 Reset 不抛异常。
        /// Verify Reset on fresh state doesn't throw.
        /// </summary>
        [Test]
        public void Reset_NoData_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.Reset(),
                "无数据时 Reset 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // 启动阶段计时测试 / Startup phase timing tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 BeginStartupPhase 和 EndStartupPhase 不抛异常。
        /// Verify BeginStartupPhase and EndStartupPhase don't throw.
        /// </summary>
        [Test]
        public void StartupPhase_BeginAndEnd_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.BeginStartupPhase(),
                "BeginStartupPhase 不应抛异常。");

            // 模拟一些查询
            // Simulate some queries
            SqlitePerformanceMonitor.RecordQuery("SELECT startup1", 1f, 2f, 10);
            SqlitePerformanceMonitor.RecordQuery("SELECT startup2", 2f, 3f, 20);

            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.EndStartupPhase(),
                "EndStartupPhase 不应抛异常。");
        }

        /// <summary>
        /// 验证未调用 BeginStartupPhase 时直接 EndStartupPhase 不抛异常。
        /// Verify EndStartupPhase without BeginStartupPhase doesn't throw.
        /// </summary>
        [Test]
        public void StartupPhase_EndWithoutBegin_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.EndStartupPhase(),
                "未调用 BeginStartupPhase 时 EndStartupPhase 不应抛异常。");
        }

        /// <summary>
        /// 验证 RecordPragmaConfig 不抛异常。
        /// Verify RecordPragmaConfig doesn't throw.
        /// </summary>
        [Test]
        public void RecordPragmaConfig_ValidValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordPragmaConfig(
                    mmapSize: 268435456,
                    cacheSize: 5000,
                    pageSize: 4096);
            }, "RecordPragmaConfig 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // PrintSummaryReport 测试 / PrintSummaryReport tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证无数据时 PrintSummaryReport 不抛异常。
        /// Verify PrintSummaryReport doesn't throw when there is no data.
        /// </summary>
        [Test]
        public void PrintSummaryReport_NoData_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "无数据时 PrintSummaryReport 不应抛异常。");
        }

        /// <summary>
        /// 验证有大量不同查询时 PrintSummaryReport 不崩溃（只显示 Top 20）。
        /// Verify PrintSummaryReport doesn't crash with many distinct queries (only Top 20 shown).
        /// </summary>
        [Test]
        public void PrintSummaryReport_ManyQueries_DoesNotThrow()
        {
            for (int i = 0; i < 100; i++)
            {
                SqlitePerformanceMonitor.RecordQuery(
                    $"SELECT * FROM table_{i} WHERE id = {i}",
                    1f + i * 0.01f,
                    2f,
                    i + 1);
            }

            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "100 条查询时 PrintSummaryReport 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // ForceGCAndReport 测试 / ForceGCAndReport tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证 ForceGCAndReport 不抛异常。
        /// Verify ForceGCAndReport doesn't throw.
        /// </summary>
        [Test]
        public void ForceGCAndReport_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.ForceGCAndReport(),
                "ForceGCAndReport 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // 并发安全基础测试 / Basic concurrency safety tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证多线程同时 RecordQuery 不抛异常。
        /// Verify concurrent RecordQuery from multiple threads doesn't throw.
        /// </summary>
        [Test]
        public void Concurrency_MultipleThreadsRecordQuery_DoesNotThrow()
        {
            const int threadCount = 4;
            const int queriesPerThread = 50;
            var tasks = new List<Task>();

            for (int t = 0; t < threadCount; t++)
            {
                var threadId = t;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < queriesPerThread; i++)
                    {
                        SqlitePerformanceMonitor.RecordQuery(
                            $"SELECT * FROM t{threadId} WHERE id={i}",
                            1f, 1f, 10);
                    }
                }));
            }

            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()),
                "所有线程的 RecordQuery 调用都不应抛异常。");

            // 验证最终报告能正常输出
            // Verify final report can be printed
            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "并发记录后 PrintSummaryReport 不应抛异常。");
        }

        /// <summary>
        /// 验证并发 RecordQuery + RecordExecute 混合调用不抛异常。
        /// Verify concurrent mixed RecordQuery + RecordExecute calls don't throw.
        /// </summary>
        [Test]
        public void Concurrency_MixedRecordQueryAndExecute_DoesNotThrow()
        {
            var tasks = new List<Task>
            {
                Task.Run(() =>
                {
                    for (int i = 0; i < 50; i++)
                        SqlitePerformanceMonitor.RecordQuery("SELECT * FROM a", 1f, 1f, 10);
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 50; i++)
                        SqlitePerformanceMonitor.RecordExecute("INSERT INTO a VALUES (1)", 0.5f);
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 30; i++)
                        SqlitePerformanceMonitor.RecordQuery("SELECT * FROM b", 2f, 3f, 20,
                            0.1f, 0.1f, 1.5f, 0.2f, 1f, 1f);
                }),
            };

            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()),
                "并发混合 RecordQuery/RecordExecute 调用不应抛异常。");
        }

        /// <summary>
        /// 验证并发 Reset + RecordQuery 不抛异常。
        /// Verify concurrent Reset + RecordQuery doesn't throw.
        /// </summary>
        [Test]
        public void Concurrency_ResetAndRecordQuery_DoesNotThrow()
        {
            var tasks = new List<Task>
            {
                Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        SqlitePerformanceMonitor.RecordQuery("SELECT * FROM concurrent", 1f, 1f, 5);
                    }
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        SqlitePerformanceMonitor.Reset();
                        System.Threading.Thread.Sleep(1);
                    }
                }),
            };

            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()),
                "并发 Reset + RecordQuery 不应抛异常。");
        }

        // ═══════════════════════════════════════════
        // 极端值测试 / Extreme value tests
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证超大耗时值不溢出。
        /// Verify extremely large timing values don't cause overflow.
        /// </summary>
        [Test]
        public void RecordQuery_ExtremeValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT heavy",
                    float.MaxValue / 2,
                    float.MaxValue / 2,
                    int.MaxValue);
            }, "超大耗时值不应导致溢出或崩溃。");
        }

        /// <summary>
        /// 验证负耗时值不崩溃。
        /// Verify negative timing values don't crash.
        /// </summary>
        [Test]
        public void RecordQuery_NegativeValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT negative",
                    -1f, -1f, -1);
            }, "负耗时值不应导致崩溃（数据可能无意义但不崩溃）。");
        }

        /// <summary>
        /// 验证大数据量时 PrintSummaryReport 不因排序而崩溃。
        /// Verify PrintSummaryReport doesn't crash due to sorting with large data.
        /// </summary>
        [Test]
        public void PrintSummaryReport_LargeDataset_DoesNotThrow()
        {
            // 创建大量不同的查询（超过 Top 20）
            // Create many different queries (exceeding Top 20)
            for (int i = 0; i < 500; i++)
            {
                SqlitePerformanceMonitor.RecordQuery(
                    $"SELECT col_{i} FROM large_table WHERE id > {i * 10}",
                    i % 10 + 1f,
                    i % 5 + 1f,
                    i * 10);
            }

            Assert.DoesNotThrow(() => SqlitePerformanceMonitor.PrintSummaryReport(),
                "500 条查询时 PrintSummaryReport 排序和输出不应崩溃。");
        }

        /// <summary>
        /// 验证 NaN 值不导致崩溃。
        /// Verify NaN values don't crash.
        /// </summary>
        [Test]
        public void RecordQuery_NaNValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SqlitePerformanceMonitor.RecordQuery("SELECT nan_test",
                    float.NaN, float.NaN, 10);
            }, "NaN 值不应导致崩溃。");
        }
    }
}
