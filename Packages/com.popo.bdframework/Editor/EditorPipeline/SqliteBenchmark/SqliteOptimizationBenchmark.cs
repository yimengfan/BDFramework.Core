using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AssetsManager.Sql;
using BDFramework.Sql;
using BDFramework.Test.SqliteBenchmark;
using SQLite4Unity3d;

// Sqlite3Statement 在 SQLite.cs 中是 using 别名（= System.IntPtr），
// 不能跨文件可见，在此定义同名类型别名供基准测试使用。
// Sqlite3Statement is a using alias (= System.IntPtr) in SQLite.cs,
// not visible across files; define a local type alias for benchmark use.
using Sqlite3Statement = System.IntPtr;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.SqliteBenchmark
{
    /// <summary>
    /// SQLite 优化性能基准测试工具（Editor 专属部分）。
    /// 测试 1~3 和 7 委托给 Runtime 兼容的 SqliteBenchmarkRunner，
    /// 此类保留 Editor 专属测试：Prepared Statement 缓存（4）、GC 压力（5）、真实表数据导入（6）。
    /// SQLite Optimization Performance Benchmark Tool (Editor-only portion).
    /// Tests 1-3 and 7 delegate to the Runtime-compatible SqliteBenchmarkRunner.
    /// This class retains Editor-only tests: Prepared Statement cache (4), GC pressure (5), Real table data import (6).
    /// </summary>
    static public class SqliteOptimizationBenchmark
    {
        private const string MENU_PATH = "BDFramework/测试/SQLite优化性能基准 ▶";

        private static readonly string BenchmarkDbDir = Path.Combine(Path.GetTempPath(), "BDFramework_SqliteBenchmark");
        private static readonly string BenchmarkDbPath = Path.Combine(BenchmarkDbDir, "benchmark.db");
        private static readonly string BenchmarkReadOnlyDbPath = Path.Combine(BenchmarkDbDir, "benchmark_readonly.db");

        // POCO 类型（BenchmarkRow, SimpleRow 等）已迁移至 Runtime.Test/Runtime/SqliteBenchmark/SqliteBenchmarkPoco.cs，
        // 通过 BDFramework.Test.SqliteBenchmark 命名空间引用，支持真机运行。
        // POCO types migrated to Runtime.Test/Runtime/SqliteBenchmark/SqliteBenchmarkPoco.cs,
        // referenced via BDFramework.Test.SqliteBenchmark namespace, supporting device execution.

        [MenuItem(MENU_PATH)]
        static public void RunBenchmark()
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════════════</color>");
            Debug.Log("<color=cyan>   SQLite 优化性能基准测试 — 开始</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════════════</color>");

            // 关闭 TableQueryForILRuntime 的编辑器 SQL 日志，避免日志开销干扰基准计时
            // Disable TableQueryForILRuntime editor SQL logs to avoid log overhead affecting benchmark timing
            TableQueryForILRuntime.EnableEditorSqlLog = false;

            var totalSw = Stopwatch.StartNew();

            try
            {
                PrepareBenchmarkDirectory();

                // ─── 测试 1~3 + 7: 委托给 Runtime 兼容的 SqliteBenchmarkRunner ───
                // Runtime runner covers: FastJson correctness, InsertAll vs RowByRow,
                // PRAGMA optimization, Real Schema bottleneck analysis.
                var runtimeReport = SqliteBenchmarkRunner.RunAll(BenchmarkDbDir);
                CopyRuntimeReport(runtimeReport);

                // ─── 测试 4: Prepared Statement 缓存 vs 无缓存（Editor 专属）───
                Test_PreparedStatementCache_Vs_NoCache();

                // ─── 测试 5: GC 压力测量（Editor 专属）───
                Test_GC_Pressure();

                // ─── 测试 6: 真实表数据导入测试（Editor 专属）───
                Test_RealTableDataImport();

                // ─── 汇总报告 ──
                PrintFinalReport();
            }
            catch (Exception e)
            {
                Debug.LogError($"基准测试异常: {e}\n{e.StackTrace}");
            }
            finally
            {
                totalSw.Stop();
                Debug.Log($"<color=cyan>基准测试总耗时: {totalSw.ElapsedMilliseconds}ms</color>");
                // 恢复编辑器 SQL 日志
                // Restore editor SQL log
                TableQueryForILRuntime.EnableEditorSqlLog = true;
                CleanupBenchmarkDirectory();
            }
        }

        /// <summary>
        /// 将 Runtime 报告数据复制到 Editor 报告结构，保持 Editor 汇总报告格式兼容。
        /// Copy Runtime report data to Editor report structure for consistent summary format.
        /// </summary>
        static void CopyRuntimeReport(SqliteBenchmarkReport runtimeReport)
        {
            if (runtimeReport == null) return;
            _report.FastJsonCorrectnessPass = runtimeReport.FastJsonCorrectnessPass;
            _report.FastJsonCorrectnessFail = runtimeReport.FastJsonCorrectnessFail;
            _report.KnownBugs = runtimeReport.KnownBugs;
            _report.InsertRowByRowMs = runtimeReport.InsertRowByRowMs;
            _report.InsertRowByRowGC = runtimeReport.InsertRowByRowGC;
            _report.InsertRowByRowMemKB = runtimeReport.InsertRowByRowMemKB;
            _report.InsertBatchMs = runtimeReport.InsertBatchMs;
            _report.InsertBatchGC = runtimeReport.InsertBatchGC;
            _report.InsertBatchMemKB = runtimeReport.InsertBatchMemKB;
            _report.InsertSpeedup = runtimeReport.InsertSpeedup;
            _report.QueryDefaultMs = runtimeReport.QueryDefaultMs;
            _report.QueryDefaultGC = runtimeReport.QueryDefaultGC;
            _report.QueryPragmaMs = runtimeReport.QueryPragmaMs;
            _report.QueryPragmaGC = runtimeReport.QueryPragmaGC;
            _report.QueryPragmaSpeedup = runtimeReport.QueryPragmaSpeedup;
            _report.QueryFullTableDefaultMs = runtimeReport.QueryFullTableDefaultMs;
            _report.QueryFullTableDefaultRows = runtimeReport.QueryFullTableDefaultRows;
            _report.QueryFullTablePragmaMs = runtimeReport.QueryFullTablePragmaMs;

            // 转换 Runtime StepTimingResult 到 Editor StepTimingResult
            // Convert Runtime StepTimingResult to Editor StepTimingResult
            if (runtimeReport.RealSchemaStepTimings != null)
            {
                _report.RealSchemaStepTimings = new Dictionary<string, StepTimingResult>();
                foreach (var kv in runtimeReport.RealSchemaStepTimings)
                {
                    var r = kv.Value;
                    _report.RealSchemaStepTimings[kv.Key] = new StepTimingResult
                    {
                        TableName = r.TableName,
                        RowCount = r.RowCount,
                        ScalarFields = r.ScalarFields,
                        ArrayFields = r.ArrayFields,
                        PrepareMs = r.PrepareMs,
                        ColumnMappingMs = r.ColumnMappingMs,
                        StepMs = r.StepMs,
                        CreateObjMs = r.CreateObjMs,
                        FastSetMs = r.FastSetMs,
                        ReadColMs = r.ReadColMs,
                    };
                }
            }
        }

        #region 准备 & 清理

        static void PrepareBenchmarkDirectory()
        {
            if (!Directory.Exists(BenchmarkDbDir))
            {
                Directory.CreateDirectory(BenchmarkDbDir);
            }

            // 清理旧文件
            if (File.Exists(BenchmarkDbPath)) File.Delete(BenchmarkDbPath);
            if (File.Exists(BenchmarkReadOnlyDbPath)) File.Delete(BenchmarkReadOnlyDbPath);
        }

        static void CleanupBenchmarkDirectory()
        {
            try
            {
                if (Directory.Exists(BenchmarkDbDir))
                {
                    Directory.Delete(BenchmarkDbDir, true);
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        #endregion

        #region 测试 4: Prepared Statement 缓存 vs 无缓存

        static void Test_PreparedStatementCache_Vs_NoCache()
        {
            Debug.Log("<color=yellow>── 测试 4: Prepared Statement 缓存 vs 无缓存 ──</color>");

            const int rowCount = 10000;
            var rows = SqliteBenchmarkDataGenerator.GenerateSimpleRows(rowCount);

            var dbPath = Path.Combine(BenchmarkDbDir, "pstmt_cache.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.CreateTable<SimpleRow>();
                conn.InsertAll(rows, typeof(SimpleRow), runInTransaction: true);
            }

            const int queryIterations = 200;
            var sql = "SELECT * FROM SimpleRow WHERE Id = ?";

            // ── 方式 A: 无缓存（每次 Prepare + Finalize） ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        var results = conn.Query<SimpleRow>(sql, (i % rowCount) + 1);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    _report.QueryNoCacheMs = sw.ElapsedMilliseconds;
                    _report.QueryNoCacheGC = gc0After - gc0;
                    Debug.Log($"  无缓存查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 B: 使用 TableQueryForILRuntime 缓存 ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var tq = new TableQueryForILRuntime(conn);
                    tq.EnableSqlCahce(triggerCacheNum: 1); // 第1次即触发缓存

                    // 预热：让 SQL 执行计数达到缓存阈值
                    for (int i = 0; i < 2; i++)
                    {
                        tq.WhereEqual("Id", 1).FromAll(typeof(SimpleRow));
                    }

                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        var results = tq.WhereEqual("Id", (i % rowCount) + 1).FromAll(typeof(SimpleRow));
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    _report.QueryWithCacheMs = sw.ElapsedMilliseconds;
                    _report.QueryWithCacheGC = gc0After - gc0;
                    Debug.Log($"  缓存查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 C: 直接使用连接级缓存 API ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);

                    // 预热：首次执行并缓存
                    var warmupCmd = conn.CreateCommand(sql, 1);
                    warmupCmd.ExecuteQuery<SimpleRow>();
                    var warmupStmt = warmupCmd.GetPreparedStatement();
                    if (warmupStmt != IntPtr.Zero)
                    {
                        conn.SetPreparedStatement(sql, warmupStmt);
                    }

                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        var cachedStmt = conn.GetPreparedStatement(sql);
                        var cmd = conn.CreateCommand(sql, (i % rowCount) + 1);
                        if (cachedStmt != IntPtr.Zero)
                        {
                            cmd.SetPreparedStatement(cachedStmt);
                        }
                        cmd.ExecuteQuery<SimpleRow>();

                        // 执行后更新缓存
                        var newStmt = cmd.GetPreparedStatement();
                        if (newStmt != IntPtr.Zero && newStmt != cachedStmt)
                        {
                            conn.SetPreparedStatement(sql, newStmt);
                        }
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    _report.QueryDirectCacheMs = sw.ElapsedMilliseconds;
                    _report.QueryDirectCacheGC = gc0After - gc0;
                    Debug.Log($"  直接缓存API({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            var speedup = _report.QueryNoCacheMs > 0
                ? (float)_report.QueryNoCacheMs / Math.Max(1, _report.QueryWithCacheMs)
                : 0;
            Debug.Log($"  <color=green>PreparedStmt缓存加速比: {speedup:F2}x</color>");
            _report.PreparedStatementSpeedup = speedup;

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        #endregion

        #region 测试 5: GC 压力测量

        static void Test_GC_Pressure()
        {
            Debug.Log("<color=yellow>── 测试 5: GC 压力测量 ──</color>");

            // FastJsonConvert 旧方式 (string.Split) vs 新方式 (Span)
            {
                var testJson = "[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]";
                const int iterations = 10000;

                // Span 方式
                GC.Collect();
                var gc0Before = GC.CollectionCount(0);
                long memBefore = GC.GetTotalMemory(true);
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < iterations; i++)
                {
                    SqliteFastJsonConvert.DeserializeArrayInt(testJson);
                }

                sw.Stop();
                var gc0After = GC.CollectionCount(0);
                long memAfter = GC.GetTotalMemory(false);
                _report.FastJsonSpanMs = sw.ElapsedMilliseconds;
                _report.FastJsonSpanGC = gc0After - gc0Before;
                _report.FastJsonSpanMemKB = (memAfter - memBefore) / 1024f;

                Debug.Log($"  Span解析({iterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0Before}, 内存增量:{(memAfter - memBefore) / 1024f:F1}KB");
            }

            // string.Split 旧方式模拟
            {
                var testJson = "[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]";
                const int iterations = 10000;

                GC.Collect();
                var gc0Before = GC.CollectionCount(0);
                long memBefore = GC.GetTotalMemory(true);
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < iterations; i++)
                {
                    LegacyStringSplitDeserialize(testJson);
                }

                sw.Stop();
                var gc0After = GC.CollectionCount(0);
                long memAfter = GC.GetTotalMemory(false);
                _report.FastJsonLegacyMs = sw.ElapsedMilliseconds;
                _report.FastJsonLegacyGC = gc0After - gc0Before;
                _report.FastJsonLegacyMemKB = (memAfter - memBefore) / 1024f;

                Debug.Log($"  string.Split({iterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0Before}, 内存增量:{(memAfter - memBefore) / 1024f:F1}KB");
            }

            var speedup = _report.FastJsonLegacyMs > 0
                ? (float)_report.FastJsonLegacyMs / Math.Max(1, _report.FastJsonSpanMs)
                : 0;
            Debug.Log($"  <color=green>Span解析加速比: {speedup:F2}x, GC减少: {_report.FastJsonLegacyGC - _report.FastJsonSpanGC}次</color>");
            _report.FastJsonSpeedup = speedup;
        }

        /// <summary>
        /// 模拟旧版 string.Split 反序列化，用于对比 GC 压力
        /// Simulate legacy string.Split deserialization for GC pressure comparison
        /// </summary>
        static int[] LegacyStringSplitDeserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<int>();
            var trimmed = json.Trim('[', ']');
            var parts = trimmed.Split(',');
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int.TryParse(parts[i].Trim(), out result[i]);
            }
            return result;
        }

        #endregion

        #region 测试 6: 真实表数据导入测试

        static void Test_RealTableDataImport()
        {
            Debug.Log("<color=yellow>── 测试 6: 真实表数据导入测试 ──</color>");

            var tableDir = "/Users/naipaopao/Documents/DarkDuck/TheCity/__Arts__/Table";
            if (!Directory.Exists(tableDir))
            {
                Debug.LogWarning($"  真实表数据目录不存在: {tableDir}, 跳过此测试");
                return;
            }

            var xlsxFiles = Directory.GetFiles(tableDir, "*.xlsx")
                .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 跳过临时文件
                .OrderByDescending(f => new FileInfo(f).Length)
                .ToArray();

            Debug.Log($"  发现 {xlsxFiles.Length} 个 xlsx 文件");
            _report.RealTableCount = xlsxFiles.Length;

            // 列出最大的5个文件
            for (int i = 0; i < Math.Min(5, xlsxFiles.Length); i++)
            {
                var fi = new FileInfo(xlsxFiles[i]);
                Debug.Log($"    Top{i + 1}: {Path.GetFileName(xlsxFiles[i])} ({fi.Length / 1024}KB)");
            }

            // 使用一个简化的真实数据模拟：创建含大数据行的表并测试查询性能
            // 由于 xlsx 无法直接在 SQLite 中使用，我们用模拟大数据来验证
            var dbPath = Path.Combine(BenchmarkDbDir, "realdata_sim.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            const int simRows = 50000; // 模拟大型表
            var simRowsList = SqliteBenchmarkDataGenerator.GenerateSimpleRows(simRows);

            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.CreateTable<SimpleRow>();
                var sw = Stopwatch.StartNew();
                conn.InsertAll(simRowsList, typeof(SimpleRow), runInTransaction: true);
                sw.Stop();
                _report.RealDataInsertMs = sw.ElapsedMilliseconds;
                Debug.Log($"  模拟大数据导入({simRows}行): {sw.ElapsedMilliseconds}ms");
            }

            // 使用 PRAGMA 优化后查询
            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);
                var sw = Stopwatch.StartNew();
                var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ? AND Value2 < ?", 25000, 500.0f);
                sw.Stop();
                _report.RealDataQueryMs = sw.ElapsedMilliseconds;
                _report.RealDataQueryRows = results.Count;
                Debug.Log($"  大数据条件查询: {sw.ElapsedMilliseconds}ms, 结果:{results.Count}行");
            }

            // 全表扫描
            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);
                var sw = Stopwatch.StartNew();
                var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow");
                sw.Stop();
                _report.RealDataFullScanMs = sw.ElapsedMilliseconds;
                Debug.Log($"  大数据全表扫描({simRows}行): {sw.ElapsedMilliseconds}ms");
            }

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        #endregion

        #region 汇总报告

        static BenchmarkReport _report = new BenchmarkReport();

        static void PrintFinalReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║            SQLite 优化性能基准测试 — 结果报告              ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine($"║  FastJsonConvert 正确性: {_report.FastJsonCorrectnessPass}通过 / {_report.FastJsonCorrectnessFail}失败 / {_report.KnownBugs}已知Bug".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── 批量导入性能 ──".PadRight(60) + "║");
            sb.AppendLine($"║  逐行Insert:   {_report.InsertRowByRowMs}ms  GC:{_report.InsertRowByRowGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  批量InsertAll: {_report.InsertBatchMs}ms  GC:{_report.InsertBatchGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {_report.InsertSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── PRAGMA 只读优化 ──".PadRight(60) + "║");
            sb.AppendLine($"║  默认配置查询: {_report.QueryDefaultMs}ms  GC:{_report.QueryDefaultGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  PRAGMA优化查询: {_report.QueryPragmaMs}ms  GC:{_report.QueryPragmaGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {_report.QueryPragmaSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine($"║  全表扫描(默认): {_report.QueryFullTableDefaultMs}ms ({_report.QueryFullTableDefaultRows}行)".PadRight(60) + "║");
            sb.AppendLine($"║  全表扫描(PRAGMA): {_report.QueryFullTablePragmaMs}ms".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── Prepared Statement 缓存 ──".PadRight(60) + "║");
            sb.AppendLine($"║  无缓存查询: {_report.QueryNoCacheMs}ms  GC:{_report.QueryNoCacheGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  缓存查询: {_report.QueryWithCacheMs}ms  GC:{_report.QueryWithCacheGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  直接API缓存: {_report.QueryDirectCacheMs}ms  GC:{_report.QueryDirectCacheGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {_report.PreparedStatementSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── FastJson Span vs string.Split ──".PadRight(60) + "║");
            sb.AppendLine($"║  Span解析: {_report.FastJsonSpanMs}ms  GC:{_report.FastJsonSpanGC}次  内存:{_report.FastJsonSpanMemKB:F1}KB".PadRight(60) + "║");
            sb.AppendLine($"║  string.Split: {_report.FastJsonLegacyMs}ms  GC:{_report.FastJsonLegacyGC}次  内存:{_report.FastJsonLegacyMemKB:F1}KB".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {_report.FastJsonSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── 真实数据模拟 ──".PadRight(60) + "║");
            sb.AppendLine($"║  真实表数量: {_report.RealTableCount}个xlsx".PadRight(60) + "║");
            sb.AppendLine($"║  大数据导入: {_report.RealDataInsertMs}ms".PadRight(60) + "║");
            sb.AppendLine($"║  大数据条件查询: {_report.RealDataQueryMs}ms ({_report.RealDataQueryRows}行)".PadRight(60) + "║");
            sb.AppendLine($"║  大数据全表扫描: {_report.RealDataFullScanMs}ms".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── 真实 Schema 瓶颈分析 ──".PadRight(60) + "║");
            if (_report.RealSchemaStepTimings != null && _report.RealSchemaStepTimings.Count > 0)
            {
                foreach (var kv in _report.RealSchemaStepTimings)
                {
                    var r = kv.Value;
                    var bottleneck = r.ReadColPercent > 50f ? "ReadCol瓶颈" :
                                     r.StepMs > r.TotalMs * 0.5f ? "Step瓶颈" : "均衡";
                    sb.AppendLine($"║  {kv.Key}".PadRight(60) + "║");
                    sb.AppendLine($"║    总计={r.TotalMs:F2}ms  FastSet={r.FastSetPercent:F1}%  ReadCol={r.ReadColPercent:F1}%  [{bottleneck}]".PadRight(60) + "║");
                }
            }
            else
            {
                sb.AppendLine("║  (未执行)".PadRight(60) + "║");
            }
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");

            Debug.Log(sb.ToString());

            // 同时输出到文件
            var reportPath = Path.Combine(BenchmarkDbDir, "benchmark_report.txt");
            try
            {
                // 报告可能在 CleanupBenchmarkDirectory 之前被删除，改用持久目录
                reportPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "SqliteBenchmark_Report.txt");
                File.WriteAllText(reportPath, sb.ToString());
                Debug.Log($"<color=cyan>报告已保存至: {reportPath}</color>");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"报告保存失败: {e.Message}");
            }
        }

        #endregion

        #region Editor 专属辅助

        static void ApplyBenchmarkPragmas(SQLiteConnection conn)
        {
            try
            {
                conn.Execute("PRAGMA cache_size=-20000");
                conn.Execute("PRAGMA mmap_size=268435456");
                conn.Execute("PRAGMA journal_mode=OFF");
                conn.Execute("PRAGMA synchronous=OFF");
                conn.Execute("PRAGMA temp_store=MEMORY");
                conn.Execute("PRAGMA locking_mode=NORMAL");
            }
            catch
            {
                // 某些 PRAGMA 在只读模式下可能失败，忽略
            }
        }

        #endregion

        #region 报告数据结构

        /// <summary>
        /// Editor 报告数据结构，包含所有测试结果。
        /// Tests 1-3 和 7 的数据由 Runtime runner 填充，Tests 4-6 由 Editor 本地填充。
        /// Editor report structure with all test results.
        /// Test 1-3 and 7 data filled by Runtime runner; Tests 4-6 filled by Editor locally.
        /// </summary>
        class BenchmarkReport
        {
            // FastJsonConvert 正确性
            public int FastJsonCorrectnessPass;
            public int FastJsonCorrectnessFail;
            public int KnownBugs;

            // 批量导入
            public long InsertRowByRowMs;
            public int InsertRowByRowGC;
            public float InsertRowByRowMemKB;
            public long InsertBatchMs;
            public int InsertBatchGC;
            public float InsertBatchMemKB;
            public float InsertSpeedup;

            // PRAGMA 优化
            public long QueryDefaultMs;
            public int QueryDefaultGC;
            public long QueryPragmaMs;
            public int QueryPragmaGC;
            public float QueryPragmaSpeedup;
            public long QueryFullTableDefaultMs;
            public int QueryFullTableDefaultRows;
            public long QueryFullTablePragmaMs;

            // Prepared Statement 缓存
            public long QueryNoCacheMs;
            public int QueryNoCacheGC;
            public long QueryWithCacheMs;
            public int QueryWithCacheGC;
            public long QueryDirectCacheMs;
            public int QueryDirectCacheGC;
            public float PreparedStatementSpeedup;

            // FastJson Span vs Legacy
            public long FastJsonSpanMs;
            public int FastJsonSpanGC;
            public float FastJsonSpanMemKB;
            public long FastJsonLegacyMs;
            public int FastJsonLegacyGC;
            public float FastJsonLegacyMemKB;
            public float FastJsonSpeedup;

            // 真实数据模拟
            public int RealTableCount;
            public long RealDataInsertMs;
            public long RealDataQueryMs;
            public int RealDataQueryRows;
            public long RealDataFullScanMs;

            // 真实 Schema 瓶颈分析（测试 7）
            // Real Schema bottleneck analysis (Test 7)
            public Dictionary<string, StepTimingResult> RealSchemaStepTimings;
        }

        /// <summary>
        /// Editor 本地的 6 步计时结果类，用于汇总报告。
        /// 数据从 Runtime StepTimingResult 转换而来（见 CopyRuntimeReport）。
        /// Editor-local 6-step timing result class for summary report.
        /// Data converted from Runtime StepTimingResult (see CopyRuntimeReport).
        /// </summary>
        class StepTimingResult
        {
            public string TableName;
            public int RowCount;
            public int ScalarFields;
            public int ArrayFields;
            public float PrepareMs;
            public float ColumnMappingMs;
            public float StepMs;
            public float CreateObjMs;
            public float FastSetMs;
            public float ReadColMs;
            public float TotalMs => PrepareMs + ColumnMappingMs + StepMs + CreateObjMs + FastSetMs + ReadColMs;

            /// <summary>
            /// ReadCol 占总耗时百分比
            /// ReadCol as percentage of total time
            /// </summary>
            public float ReadColPercent => TotalMs > 0 ? ReadColMs / TotalMs * 100f : 0f;

            /// <summary>
            /// FastSet 占总耗时百分比
            /// FastSet as percentage of total time
            /// </summary>
            public float FastSetPercent => TotalMs > 0 ? FastSetMs / TotalMs * 100f : 0f;
        }

        #endregion
    }
}
