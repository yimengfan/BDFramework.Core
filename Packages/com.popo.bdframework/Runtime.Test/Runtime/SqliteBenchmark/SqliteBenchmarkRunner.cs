using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AssetsManager.Sql;
using BDFramework.Sql;
using Cysharp.Text;
using SQLite4Unity3d;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Sqlite3Statement 在 SQLite.cs 中是 using 别名（= System.IntPtr），
// 不能跨文件可见，在此定义同名类型别名供基准测试使用。
// Sqlite3Statement is a using alias (= System.IntPtr) in SQLite.cs,
// not visible across files; define a local type alias for benchmark use.
using Sqlite3Statement = System.IntPtr;

namespace BDFramework.Test.SqliteBenchmark
{
    /// <summary>
    /// SQLite 性能基准测试运行器 — Runtime 兼容，支持真机和 Editor 运行。
    /// 使用 BDebugPerformanceProfiler 输出结构化流水线报告，
    /// 支持 6 步分步计时（Prepare → ColumnMapping → Step → CreateObj → FastSet → ReadCol）。
    ///
    /// SQLite performance benchmark runner — Runtime compatible, supports device and Editor.
    /// Uses BDebugPerformanceProfiler for structured pipeline reports,
    /// supports 6-step timing (Prepare → ColumnMapping → Step → CreateObj → FastSet → ReadCol).
    /// </summary>
    static public class SqliteBenchmarkRunner
    {
        /// <summary>
        /// 基准测试数据库临时目录。
        /// Benchmark database temporary directory.
        /// </summary>
        static public readonly string BenchmarkDbDir = Path.Combine(Path.GetTempPath(), "BDFramework_SqliteBenchmark");

        /// <summary>
        /// 运行全部基准测试（测试 1~7），输出结构化报告。
        /// 测试目的=验证 SQLite 查询管线各步骤性能，识别瓶颈。
        /// 实现手段=使用 BDebugPerformanceProfiler 分步计时 + SqlitePerformanceMonitor 汇总。
        /// Run all benchmark tests (1-7), output structured report.
        /// Test purpose=verify SQLite query pipeline step performance, identify bottlenecks.
        /// Method=use BDebugPerformanceProfiler step timing + SqlitePerformanceMonitor aggregation.
        /// </summary>
        /// <param name="customDbDir">自定义数据库目录，null 则使用临时目录 / Custom DB directory, null for temp dir</param>
        /// <param name="realTableDir">
        /// 真实表数据目录（xlsx 文件），null 则跳过测试 7。
        /// 真机运行时传入 persistentDataPath 下的子目录即可。
        /// Real table data directory (xlsx files), null to skip test 7.
        /// On device, pass a subdirectory under persistentDataPath.
        /// </param>
        static public SqliteBenchmarkReport RunAll(string customDbDir = null, string realTableDir = null)
        {
            var dbDir = customDbDir ?? BenchmarkDbDir;

            Debug.Log("<color=cyan>═══════════════════════════════════════════════</color>");
            Debug.Log("<color=cyan>   SQLite 性能基准测试 — 开始</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════════════</color>");

            // 关闭 TableQueryForILRuntime 的编辑器 SQL 日志，避免日志开销干扰基准计时
            // Disable TableQueryForILRuntime editor SQL logs to avoid log overhead affecting benchmark timing
            TableQueryForILRuntime.EnableEditorSqlLog = false;

            var report = new SqliteBenchmarkReport();
            var totalSw = Stopwatch.StartNew();

            try
            {
                PrepareBenchmarkDirectory(dbDir);

                // ─── 测试 1: FastJsonConvert 正确性验证 ───
                Test_FastJsonConvert_Correctness(report);

                // ─── 测试 2: 批量 InsertAll vs 逐行 Insert ───
                Test_InsertAll_Vs_RowByRow(report, dbDir);

                // ─── 测试 3: PRAGMA 只读优化 vs 默认配置 ───
                Test_PragmaOptimization_Vs_Default(report, dbDir);

                // ─── 测试 4: 真实 Schema 瓶颈分析（6步分步计时）───
                Test_RealSchema_BottleneckAnalysis(report, dbDir);

                // ─── 测试 5: Prepared Statement 缓存 vs 无缓存 ───
                Test_PreparedStatementCache_Vs_NoCache(report, dbDir);

                // ─── 测试 6: GC 压力测量（FastJson Span vs string.Split）───
                Test_GC_Pressure(report);

                // ─── 测试 7: 真实表数据导入测试 ───
                if (!string.IsNullOrEmpty(realTableDir))
                {
                    Test_RealTableDataImport(report, dbDir, realTableDir);
                }
                else
                {
                    Debug.Log("<color=yellow>── 测试 7: 真实表数据导入 — 跳过（未提供 realTableDir）──</color>");
                }

                // ─── 汇总报告 ──
                Debug.Log(report.FormatReport());
            }
            catch (Exception e)
            {
                Debug.LogError($"基准测试异常: {e}\n{e.StackTrace}");
            }
            finally
            {
                totalSw.Stop();
                Debug.Log($"<color=cyan>基准测试总耗时: {totalSw.ElapsedMilliseconds}ms</color>");
                TableQueryForILRuntime.EnableEditorSqlLog = true;
                CleanupBenchmarkDirectory(dbDir);
            }

            return report;
        }

        /// <summary>
        /// 仅运行真实 Schema 瓶颈分析测试 — 输出 200 行对象的每步耗时。
        /// 测试目的=量化查询 200 行时各步骤（SQL、反序列化等）的时间分布。
        /// 实现手段=使用 BDebugPerformanceProfiler + Stopwatch 分步计时。
        /// Run only real Schema bottleneck analysis — output per-step timing for 200-row queries.
        /// Test purpose=quantify time distribution across query steps (SQL, deserialization, etc.) for 200 rows.
        /// Method=use BDebugPerformanceProfiler + Stopwatch step timing.
        /// </summary>
        static public Dictionary<string, StepTimingResult> RunBottleneckAnalysis(string customDbDir = null)
        {
            var dbDir = customDbDir ?? BenchmarkDbDir;
            var report = new SqliteBenchmarkReport();

            try
            {
                PrepareBenchmarkDirectory(dbDir);
                Test_RealSchema_BottleneckAnalysis(report, dbDir);
            }
            catch (Exception e)
            {
                Debug.LogError($"瓶颈分析异常: {e}\n{e.StackTrace}");
            }
            finally
            {
                CleanupBenchmarkDirectory(dbDir);
            }

            return report.RealSchemaStepTimings;
        }

        #region 准备 & 清理

        static void PrepareBenchmarkDirectory(string dbDir)
        {
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
        }

        static void CleanupBenchmarkDirectory(string dbDir)
        {
            try
            {
                if (Directory.Exists(dbDir))
                {
                    Directory.Delete(dbDir, true);
                }
            }
            catch
            {
                // 忽略清理失败 / Ignore cleanup failure
            }
        }

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
                // Some PRAGMAs may fail in read-only mode, ignore
            }
        }

        #endregion

        #region 测试 1: FastJsonConvert 正确性验证

        static void Test_FastJsonConvert_Correctness(SqliteBenchmarkReport report)
        {
            Debug.Log("<color=yellow>── 测试 1: FastJsonConvert 正确性验证 ──</color>");
            int passCount = 0;
            int failCount = 0;

            // int[]
            {
                var original = new int[] {1, 2, 3, 100, -5, 0, 999999};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayInt(json);
                if (SqliteBenchmarkDataGenerator.ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ int[] 正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ int[] 不匹配!");
                }
            }

            // float[]
            {
                var original = new float[] {1.1f, 2.5f, -3.14f, 0f, 999.999f};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayFloat(json);
                if (SqliteBenchmarkDataGenerator.ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ float[] 正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ float[] 不匹配!");
                }
            }

            // double[]
            {
                var original = new double[] {1.12345678d, 2.0d, -3.14d, 0d};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayDouble(json);
                if (SqliteBenchmarkDataGenerator.ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ double[] 正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ double[] 不匹配!");
                }
            }

            // string[]
            {
                var original = new string[] {"hello", "world", "", "with,comma", "spaces here"};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayString(json);
                if (SqliteBenchmarkDataGenerator.ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ string[] 正确(简单): {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ string[] 不匹配(简单)!");
                }
            }

            // bool[]
            {
                var original = new bool[] {true, false, true, false, true};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayBool(json);
                if (SqliteBenchmarkDataGenerator.ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ bool[] 正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ bool[] 不匹配!");
                }
            }

            // long[]
            {
                var original = new long[] {1L, 9999999999L, -123456789L, 0L};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayLong(json);
                if (SqliteBenchmarkDataGenerator.ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ long[] 正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ long[] 不匹配!");
                }
            }

            // 空数组
            {
                var intArr = SqliteFastJsonConvert.DeserializeArrayInt("[]");
                if (intArr.Length == 0)
                {
                    passCount++;
                    Debug.Log($"  ✅ 空数组正确");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ 空数组不匹配!");
                }
            }

            // null/空字符串
            {
                var intArr = SqliteFastJsonConvert.DeserializeArrayInt(null);
                var strArr = SqliteFastJsonConvert.DeserializeArrayString("");
                if (intArr.Length == 0 && strArr.Length == 0)
                {
                    passCount++;
                    Debug.Log($"  ✅ null/空字符串正确");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ null/空字符串不匹配!");
                }
            }

            Debug.Log($"<color=yellow>FastJsonConvert 正确性: {passCount}通过 / {failCount}失败</color>");
            report.FastJsonCorrectnessPass = passCount;
            report.FastJsonCorrectnessFail = failCount;
        }

        #endregion

        #region 测试 2: 批量 InsertAll vs 逐行 Insert

        static void Test_InsertAll_Vs_RowByRow(SqliteBenchmarkReport report, string dbDir)
        {
            Debug.Log("<color=yellow>── 测试 2: 批量 InsertAll vs 逐行 Insert ──</color>");

            const int rowCount = 5000;
            var rows = SqliteBenchmarkDataGenerator.GenerateBenchmarkRows(rowCount);

            // ── 方式 A: 逐行 Insert（无事务） ──
            {
                var dbPath = Path.Combine(dbDir, "insert_rowbyrow.db");
                if (File.Exists(dbPath)) File.Delete(dbPath);

                using (var conn = new SQLiteConnection(dbPath))
                {
                    conn.CreateTable<BenchmarkRow>();
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    foreach (var row in rows)
                    {
                        conn.Insert(row);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.InsertRowByRowMs = sw.ElapsedMilliseconds;
                    report.InsertRowByRowGC = gc0After - gc0;
                    Debug.Log($"  逐行Insert: {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }

                if (File.Exists(dbPath)) File.Delete(dbPath);
            }

            // ── 方式 B: InsertAll with transaction ──
            {
                var dbPath = Path.Combine(dbDir, "insert_batch.db");
                if (File.Exists(dbPath)) File.Delete(dbPath);

                using (var conn = new SQLiteConnection(dbPath))
                {
                    conn.CreateTable<BenchmarkRow>();
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    conn.InsertAll(rows, typeof(BenchmarkRow), runInTransaction: true);

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.InsertBatchMs = sw.ElapsedMilliseconds;
                    report.InsertBatchGC = gc0After - gc0;
                    Debug.Log($"  批量InsertAll: {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }

                if (File.Exists(dbPath)) File.Delete(dbPath);
            }

            var speedup = report.InsertRowByRowMs > 0
                ? (float)report.InsertRowByRowMs / Math.Max(1, report.InsertBatchMs)
                : 0;
            Debug.Log($"  <color=green>批量Insert加速比: {speedup:F2}x</color>");
            report.InsertSpeedup = speedup;
        }

        #endregion

        #region 测试 3: PRAGMA 只读优化 vs 默认配置

        static void Test_PragmaOptimization_Vs_Default(SqliteBenchmarkReport report, string dbDir)
        {
            Debug.Log("<color=yellow>── 测试 3: PRAGMA 只读优化 vs 默认配置 ──</color>");

            var readOnlyDbPath = Path.Combine(dbDir, "benchmark_readonly.db");
            const int rowCount = 20000;
            var rows = SqliteBenchmarkDataGenerator.GenerateSimpleRows(rowCount);

            // 创建只读数据库
            {
                if (File.Exists(readOnlyDbPath)) File.Delete(readOnlyDbPath);
                using (var conn = new SQLiteConnection(readOnlyDbPath))
                {
                    conn.CreateTable<SimpleRow>();
                    conn.InsertAll(rows, typeof(SimpleRow), runInTransaction: true);
                }
            }

            const int queryIterations = 100;

            // ── 方式 A: 默认配置 ──
            {
                using (var conn = new SQLiteConnection(readOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", 5000);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.QueryDefaultMs = sw.ElapsedMilliseconds;
                    report.QueryDefaultGC = gc0After - gc0;
                    Debug.Log($"  默认配置查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 B: PRAGMA 优化 ──
            {
                using (var conn = new SQLiteConnection(readOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", 5000);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.QueryPragmaMs = sw.ElapsedMilliseconds;
                    report.QueryPragmaGC = gc0After - gc0;
                    Debug.Log($"  PRAGMA优化查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            var speedup = report.QueryDefaultMs > 0
                ? (float)report.QueryDefaultMs / Math.Max(1, report.QueryPragmaMs)
                : 0;
            Debug.Log($"  <color=green>PRAGMA优化加速比: {speedup:F2}x</color>");
            report.QueryPragmaSpeedup = speedup;

            // ── 全表扫描对比 / Full table scan comparison ──
            {
                // 默认配置全表扫描
                using (var conn = new SQLiteConnection(readOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    var sw = Stopwatch.StartNew();
                    var allRows = conn.Query<SimpleRow>("SELECT * FROM SimpleRow");
                    sw.Stop();
                    report.QueryFullTableDefaultMs = sw.ElapsedMilliseconds;
                    report.QueryFullTableDefaultRows = allRows.Count;
                    Debug.Log($"  默认配置全表扫描({allRows.Count}行): {sw.ElapsedMilliseconds}ms");
                }

                // PRAGMA优化全表扫描
                using (var conn = new SQLiteConnection(readOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var allRows = conn.Query<SimpleRow>("SELECT * FROM SimpleRow");
                    sw.Stop();
                    report.QueryFullTablePragmaMs = sw.ElapsedMilliseconds;
                    Debug.Log($"  PRAGMA优化全表扫描({allRows.Count}行): {sw.ElapsedMilliseconds}ms");
                }
            }

            if (File.Exists(readOnlyDbPath)) File.Delete(readOnlyDbPath);
        }

        #endregion

        #region 测试 4: 真实 Schema 瓶颈分析（6步分步计时）

        /// <summary>
        /// 使用匹配真实游戏 Schema 的 POCO 类，
        /// 对比纯标量表、少量数组表、密集数组表的 6 步分步计时，
        /// 识别 ReadCol → FastJsonConvert 路径是否为瓶颈。
        /// Uses POCO classes matching real game schemas to compare 6-step timing
        /// across scalar-only, few-array, and array-heavy tables,
        /// identifying whether the ReadCol → FastJsonConvert path is the bottleneck.
        /// </summary>
        static void Test_RealSchema_BottleneckAnalysis(SqliteBenchmarkReport report, string dbDir)
        {
            Debug.Log("<color=yellow>── 测试 4: 真实 Schema 瓶颈分析（6步分步计时）──</color>");

            var dbPath = Path.Combine(dbDir, "real_schema.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            SqlitePerformanceMonitor.Reset();

            using (var conn = new SQLiteConnection(dbPath))
            {
                // ── 创建表 ──
                conn.CreateTable<HeroSkillParameterRow>();
                conn.CreateTable<ItemRow>();
                conn.CreateTable<GoodsBaseRow>();
                conn.CreateTable<ScalarOnlyRow>();

                // ── 插入数据 ──
                var heroRows = SqliteBenchmarkDataGenerator.GenerateHeroSkillParameterRows(295);
                var itemRows = SqliteBenchmarkDataGenerator.GenerateItemRows(337);
                var goodsRows = SqliteBenchmarkDataGenerator.GenerateGoodsBaseRows(170);
                var scalarRows = SqliteBenchmarkDataGenerator.GenerateScalarOnlyRows(295);

                conn.InsertAll(heroRows, typeof(HeroSkillParameterRow), runInTransaction: true);
                conn.InsertAll(itemRows, typeof(ItemRow), runInTransaction: true);
                conn.InsertAll(goodsRows, typeof(GoodsBaseRow), runInTransaction: true);
                conn.InsertAll(scalarRows, typeof(ScalarOnlyRow), runInTransaction: true);

                Debug.Log($"  数据插入完成: HeroSkillParameter={heroRows.Count}行, " +
                          $"Item={itemRows.Count}行, GoodsBase={goodsRows.Count}行, ScalarOnly={scalarRows.Count}行");
            }

            // ── PRAGMA 优化后查询 + 6 步分步计时 ──
            const int queryRepetitions = 5;
            var stepTimings = new Dictionary<string, StepTimingResult>();

            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);

                // ── 1. 纯标量表（对照组） ──
                {
                    var timing = MeasureQuerySteps<ScalarOnlyRow>(conn, queryRepetitions);
                    stepTimings["ScalarOnly(17标量0数组)"] = timing;
                    Debug.Log($"  <color=cyan>{timing.ToLogString("纯标量表(对照组)")}</color>");
                }

                // ── 2. Item 表（15标量+2数组） ──
                {
                    var timing = MeasureQuerySteps<ItemRow>(conn, queryRepetitions);
                    stepTimings["Item(15标量+2数组)"] = timing;
                    Debug.Log($"  <color=cyan>{timing.ToLogString("Item表(15标量+2数组)")}</color>");
                }

                // ── 3. GoodsBase 表（18标量+7数组） ──
                {
                    var timing = MeasureQuerySteps<GoodsBaseRow>(conn, queryRepetitions);
                    stepTimings["GoodsBase(18标量+7数组)"] = timing;
                    Debug.Log($"  <color=cyan>{timing.ToLogString("GoodsBase表(18标量+7数组)")}</color>");
                }

                // ── 4. HeroSkillParameter 表（12标量+21数组）—— 最极端 ──
                {
                    var timing = MeasureQuerySteps<HeroSkillParameterRow>(conn, queryRepetitions);
                    stepTimings["HeroSkillParameter(12标量+21数组)"] = timing;
                    Debug.Log($"  <color=cyan>{timing.ToLogString("HeroSkillParameter表(12标量+21数组)")}</color>");
                }
            }

            // ── 瓶颈分析报告 ──
            PrintBottleneckReport(stepTimings);
            report.RealSchemaStepTimings = stepTimings;

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        /// <summary>
        /// 对指定 POCO 类型的全表查询执行多次，取 6 步计时的平均值。
        /// 使用 BDebugPerformanceProfiler 记录 Prepare/ColumnMapping，
        /// 使用 Stopwatch 累加行级计时（Step/CreateObj/FastSet/ReadCol）。
        /// Executes full-table queries for the given POCO type multiple times,
        /// returning the average of 6-step timing measurements.
        /// Uses BDebugPerformanceProfiler for Prepare/ColumnMapping,
        /// uses Stopwatch for row-level timing (Step/CreateObj/FastSet/ReadCol).
        /// </summary>
        static StepTimingResult MeasureQuerySteps<T>(SQLiteConnection conn, int repetitions)
            where T : new()
        {
            var result = new StepTimingResult();
            var map = conn.GetMapping(typeof(T));
            var tableName = map.TableName;
            result.TableName = tableName;

            // 预热：首次查询建立 column mapping 缓存
            // Warmup: first query establishes column mapping cache
            var warmupResults = conn.Query<T>("SELECT * FROM " + tableName);
            result.RowCount = warmupResults.Count;

            SqlitePerformanceMonitor.Reset();

            // ── 手动分步计时 ──
            // Manual step-by-step timing
            float totalPrepareMs = 0f;
            float totalColumnMappingMs = 0f;
            float totalStepMs = 0f;
            float totalCreateObjMs = 0f;
            float totalFastSetMs = 0f;
            float totalReadColMs = 0f;

            var sw = new Stopwatch();

            for (int i = 0; i < repetitions; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 使用 BDebugPerformanceProfiler 记录 Prepare + ColumnMapping
                // Use BDebugPerformanceProfiler for Prepare + ColumnMapping
                var perfTag = ZString.Format("Bench_{0}_{1}", tableName, i);
                BDebugPerformanceProfiler.BeginStepTimer(perfTag, "Benchmark");

                BDebugPerformanceProfiler.BeginStep(perfTag, "Prepare");
                var cmd = conn.CreateCommand("SELECT * FROM " + tableName);

                sw.Restart();
                var stmt = SQLiteCommandHelper.Prepare(cmd);
                sw.Stop();
                totalPrepareMs += sw.ElapsedTicks / 10000f;
                BDebugPerformanceProfiler.EndStep(perfTag, "Prepare");

                BDebugPerformanceProfiler.BeginStep(perfTag, "ColumnMapping");
                try
                {
                    sw.Restart();
                    var cols = new TableMapping.Column[SQLite3.ColumnCount(stmt)];
                    var fastColumnSetters = new Action<object, Sqlite3Statement, int>[cols.Length];
                    int scalarCount = 0, arrayCount = 0;
                    for (int ci = 0; ci < cols.Length; ci++)
                    {
                        var name = SQLite3.ColumnName16(stmt, ci);
                        cols[ci] = map.FindColumn(name);
                        if (cols[ci] != null)
                        {
                            fastColumnSetters[ci] = FastColumnSetterHelper.GetFastSetter<T>(conn, cols[ci]);
                            var propType = cols[ci].PropertyInfo.PropertyType;
                            if (propType.IsArray || (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>)))
                                arrayCount++;
                            else
                                scalarCount++;
                        }
                    }
                    sw.Stop();
                    totalColumnMappingMs += sw.ElapsedTicks / 10000f;
                    BDebugPerformanceProfiler.EndStep(perfTag, "ColumnMapping");
                    result.ScalarFields = scalarCount;
                    result.ArrayFields = arrayCount;

                    // ── 行级计时累加器（热循环） ──
                    // Row-level timing accumulators (hot loop)
                    float stepTimeMs = 0f;
                    float createObjTimeMs = 0f;
                    float fastSetTimeMs = 0f;
                    float readColTimeMs = 0f;

                    sw.Restart();
                    var hasRow = SQLite3.Step(stmt) == SQLite3.Result.Row;
                    sw.Stop();
                    stepTimeMs += sw.ElapsedTicks / 10000f;

                    while (hasRow)
                    {
                        // Step 4: Create Object
                        sw.Restart();
                        var obj = Activator.CreateInstance(typeof(T));
                        sw.Stop();
                        createObjTimeMs += sw.ElapsedTicks / 10000f;

                        // Step 5: Set Fields
                        for (int ci = 0; ci < cols.Length; ci++)
                        {
                            if (cols[ci] == null) continue;

                            if (fastColumnSetters[ci] != null)
                            {
                                sw.Restart();
                                fastColumnSetters[ci].Invoke(obj, stmt, ci);
                                sw.Stop();
                                fastSetTimeMs += sw.ElapsedTicks / 10000f;
                            }
                            else
                            {
                                sw.Restart();
                                var colType = SQLite3.ColumnType(stmt, ci);
                                var val = SQLiteCommandHelper.ReadCol(cmd, stmt, ci, colType, cols[ci].ColumnType);
                                cols[ci].SetValue(obj, val);
                                sw.Stop();
                                readColTimeMs += sw.ElapsedTicks / 10000f;
                            }
                        }

                        sw.Restart();
                        hasRow = SQLite3.Step(stmt) == SQLite3.Result.Row;
                        sw.Stop();
                        stepTimeMs += sw.ElapsedTicks / 10000f;
                    }

                    totalStepMs += stepTimeMs;
                    totalCreateObjMs += createObjTimeMs;
                    totalFastSetMs += fastSetTimeMs;
                    totalReadColMs += readColTimeMs;
                }
                finally
                {
                    SQLite3.Finalize(stmt);
                }

                // 从 BDebugPerformanceProfiler 提取 Prepare/ColumnMapping 数据并输出流水线报告
                // Extract Prepare/ColumnMapping from BDebugPerformanceProfiler and output pipeline report
                var stepData = BDebugPerformanceProfiler.EndStepTimerGetData(perfTag);
                if (stepData != null)
                {
                    stepData.Add(new BDebugPerformanceProfiler.StepResult { StepName = "Step", TimeMs = totalStepMs / repetitions });
                    stepData.Add(new BDebugPerformanceProfiler.StepResult { StepName = "CreateObj", TimeMs = totalCreateObjMs / repetitions });
                    stepData.Add(new BDebugPerformanceProfiler.StepResult { StepName = "FastSet", TimeMs = totalFastSetMs / repetitions });
                    stepData.Add(new BDebugPerformanceProfiler.StepResult { StepName = "ReadCol", TimeMs = totalReadColMs / repetitions });
                    BDebugPerformanceProfiler.PrintPipelineReport(perfTag, stepData, result.RowCount, "SELECT * FROM " + tableName);
                }
            }

            result.PrepareMs = totalPrepareMs / repetitions;
            result.ColumnMappingMs = totalColumnMappingMs / repetitions;
            result.StepMs = totalStepMs / repetitions;
            result.CreateObjMs = totalCreateObjMs / repetitions;
            result.FastSetMs = totalFastSetMs / repetitions;
            result.ReadColMs = totalReadColMs / repetitions;

            return result;
        }

        /// <summary>
        /// 打印瓶颈分析报告。
        /// Print bottleneck analysis report.
        /// </summary>
        static void PrintBottleneckReport(Dictionary<string, StepTimingResult> timings)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║         真实游戏 Schema 性能瓶颈分析 — 6 步分步计时报告          ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            sb.AppendLine($"║ {"表名",-35} {"行数",5} {"Prepare",9} {"ColMap",9} {"Step",9} {"CreateObj",9} {"FastSet",9} {"ReadCol",9} {"总计",9} {"RC%",6} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            foreach (var kv in timings)
            {
                var r = kv.Value;
                sb.AppendLine($"║ {kv.Key,-35} {r.RowCount,5} {r.PrepareMs,8:F2}ms {r.ColumnMappingMs,8:F2}ms {r.StepMs,8:F2}ms {r.CreateObjMs,8:F2}ms {r.FastSetMs,8:F2}ms {r.ReadColMs,8:F2}ms {r.TotalMs,8:F2}ms {r.ReadColPercent,5:F1}% ║");
            }

            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            var heroTiming = timings.Values.FirstOrDefault(t => t.ArrayFields >= 20);
            if (heroTiming != null && heroTiming.ReadColPercent > 50f)
            {
                sb.AppendLine($"║                                                                    ║");
                sb.AppendLine($"║  🔴 瓶颈识别: ReadCol(反射+FastJsonConvert) 在数组密集型表中     ║");
                sb.AppendLine($"║     占比 {heroTiming.ReadColPercent:F1}%，是主要性能瓶颈。                      ║");
                sb.AppendLine($"║                                                                    ║");
                sb.AppendLine($"║  💡 优化方案:                                                      ║");
                sb.AppendLine($"║     • 为 int[] 添加 FastColumnSetter 支持，避免反射路径             ║");
                sb.AppendLine($"║     • 直接调用 DeserializeArrayInt，跳过类型分发                   ║");
                sb.AppendLine($"║     • 预期加速: ReadCol 时间减少 30-50%                           ║");
            }
            else if (heroTiming != null)
            {
                sb.AppendLine($"║                                                                    ║");
                sb.AppendLine($"║  🟢 ReadCol 不是主要瓶颈 (占比 {heroTiming.ReadColPercent:F1}%)                         ║");
            }

            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════╝");

            Debug.Log(sb.ToString());
        }

        #endregion

        #region 测试 5: Prepared Statement 缓存 vs 无缓存

        /// <summary>
        /// 对比无缓存查询、TableQueryForILRuntime 缓存查询、连接级直接缓存查询的性能。
        /// Compare no-cache query, TableQueryForILRuntime cached query, and connection-level direct cache query.
        /// </summary>
        static void Test_PreparedStatementCache_Vs_NoCache(SqliteBenchmarkReport report, string dbDir)
        {
            Debug.Log("<color=yellow>── 测试 5: Prepared Statement 缓存 vs 无缓存 ──</color>");

            const int rowCount = 10000;
            var rows = SqliteBenchmarkDataGenerator.GenerateSimpleRows(rowCount);

            var dbPath = Path.Combine(dbDir, "pstmt_cache.db");
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
                    report.QueryNoCacheMs = sw.ElapsedMilliseconds;
                    report.QueryNoCacheGC = gc0After - gc0;
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
                    report.QueryWithCacheMs = sw.ElapsedMilliseconds;
                    report.QueryWithCacheGC = gc0After - gc0;
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
                    report.QueryDirectCacheMs = sw.ElapsedMilliseconds;
                    report.QueryDirectCacheGC = gc0After - gc0;
                    Debug.Log($"  直接缓存API({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            var speedup = report.QueryNoCacheMs > 0
                ? (float)report.QueryNoCacheMs / Math.Max(1, report.QueryWithCacheMs)
                : 0;
            Debug.Log($"  <color=green>PreparedStmt缓存加速比: {speedup:F2}x</color>");
            report.PreparedStatementSpeedup = speedup;

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        #endregion

        #region 测试 6: GC 压力测量（FastJson Span vs string.Split）

        /// <summary>
        /// 对比 FastJsonConvert Span 解析和传统 string.Split 解析的 GC 压力。
        /// Compare GC pressure between FastJsonConvert Span parsing and legacy string.Split parsing.
        /// </summary>
        static void Test_GC_Pressure(SqliteBenchmarkReport report)
        {
            Debug.Log("<color=yellow>── 测试 6: GC 压力测量（FastJson Span vs string.Split）──</color>");

            // Span 方式
            {
                var testJson = "[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]";
                const int iterations = 10000;

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
                report.FastJsonSpanMs = sw.ElapsedMilliseconds;
                report.FastJsonSpanGC = gc0After - gc0Before;
                report.FastJsonSpanMemKB = (memAfter - memBefore) / 1024f;

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
                report.FastJsonLegacyMs = sw.ElapsedMilliseconds;
                report.FastJsonLegacyGC = gc0After - gc0Before;
                report.FastJsonLegacyMemKB = (memAfter - memBefore) / 1024f;

                Debug.Log($"  string.Split({iterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0Before}, 内存增量:{(memAfter - memBefore) / 1024f:F1}KB");
            }

            var speedup = report.FastJsonLegacyMs > 0
                ? (float)report.FastJsonLegacyMs / Math.Max(1, report.FastJsonSpanMs)
                : 0;
            Debug.Log($"  <color=green>Span解析加速比: {speedup:F2}x, GC减少: {report.FastJsonLegacyGC - report.FastJsonSpanGC}次</color>");
            report.FastJsonSpeedup = speedup;
        }

        /// <summary>
        /// 模拟旧版 string.Split 反序列化，用于对比 GC 压力。
        /// Simulate legacy string.Split deserialization for GC pressure comparison.
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

        #region 测试 7: 真实表数据导入测试

        /// <summary>
        /// 使用大数据量模拟真实表导入场景，测试批量导入和查询性能。
        /// realTableDir 为可选的真实 xlsx 目录路径，为空则跳过 xlsx 扫描只做模拟。
        /// Uses large data volumes to simulate real table import, testing batch import and query performance.
        /// realTableDir is an optional real xlsx directory path; empty to skip xlsx scanning and only simulate.
        /// </summary>
        static void Test_RealTableDataImport(SqliteBenchmarkReport report, string dbDir, string realTableDir)
        {
            Debug.Log("<color=yellow>── 测试 7: 真实表数据导入测试 ──</color>");

            // 扫描真实 xlsx 文件（如果目录存在）
            // Scan real xlsx files (if directory exists)
            if (Directory.Exists(realTableDir))
            {
                var xlsxFiles = Directory.GetFiles(realTableDir, "*.xlsx")
                    .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 跳过临时文件
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .ToArray();

                Debug.Log($"  发现 {xlsxFiles.Length} 个 xlsx 文件");
                report.RealTableCount = xlsxFiles.Length;

                // 列出最大的5个文件
                for (int i = 0; i < Math.Min(5, xlsxFiles.Length); i++)
                {
                    var fi = new FileInfo(xlsxFiles[i]);
                    Debug.Log($"    Top{i + 1}: {Path.GetFileName(xlsxFiles[i])} ({fi.Length / 1024}KB)");
                }
            }
            else
            {
                Debug.LogWarning($"  真实表数据目录不存在: {realTableDir}, 仅执行模拟测试");
                report.RealTableCount = 0;
            }

            // 使用一个简化的真实数据模拟：创建含大数据行的表并测试查询性能
            // 由于 xlsx 无法直接在 SQLite 中使用，我们用模拟大数据来验证
            var dbPath = Path.Combine(dbDir, "realdata_sim.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            const int simRows = 50000; // 模拟大型表
            var simRowsList = SqliteBenchmarkDataGenerator.GenerateSimpleRows(simRows);

            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.CreateTable<SimpleRow>();
                var sw = Stopwatch.StartNew();
                conn.InsertAll(simRowsList, typeof(SimpleRow), runInTransaction: true);
                sw.Stop();
                report.RealDataInsertMs = sw.ElapsedMilliseconds;
                Debug.Log($"  模拟大数据导入({simRows}行): {sw.ElapsedMilliseconds}ms");
            }

            // 使用 PRAGMA 优化后查询
            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);
                var sw = Stopwatch.StartNew();
                var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ? AND Value2 < ?", 25000, 500.0f);
                sw.Stop();
                report.RealDataQueryMs = sw.ElapsedMilliseconds;
                report.RealDataQueryRows = results.Count;
                Debug.Log($"  大数据条件查询: {sw.ElapsedMilliseconds}ms, 结果:{results.Count}行");
            }

            // 全表扫描
            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);
                var sw = Stopwatch.StartNew();
                var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow");
                sw.Stop();
                report.RealDataFullScanMs = sw.ElapsedMilliseconds;
                Debug.Log($"  大数据全表扫描({simRows}行): {sw.ElapsedMilliseconds}ms");
            }

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        #endregion

        #region 测试 5: Prepared Statement 缓存 vs 无缓存

        /// <summary>
        /// 对比三种查询模式：无缓存（每次创建新连接）、LruTableQuery 缓存、
        /// 直接 API 调用（绕过 TableQuery 层），测量 Prepared Statement 缓存的加速效果。
        /// 测试目的=量化 Prepared Statement 缓存对查询性能和 GC 的影响。
        /// 实现手段=使用 Stopwatch 计时 + GC.CollectionCount 测量 GC 次数。
        /// Compare three query modes: no cache (new connection each time), LruTableQuery cache,
        /// and direct API call (bypassing TableQuery layer), measuring Prepared Statement cache speedup.
        /// Test purpose=quantify the impact of Prepared Statement caching on query performance and GC.
        /// Method=use Stopwatch timing + GC.CollectionCount for GC measurement.
        /// </summary>
        static void Test_PreparedStatementCache_Vs_NoCache(SqliteBenchmarkReport report, string dbDir)
        {
            Debug.Log("<color=yellow>── 测试 5: Prepared Statement 缓存 vs 无缓存 ──</color>");

            var dbPath = Path.Combine(dbDir, "stmt_cache.db");
            const int rowCount = 5000;
            var rows = SqliteBenchmarkDataGenerator.GenerateSimpleRows(rowCount);

            // 创建测试数据库
            if (File.Exists(dbPath)) File.Delete(dbPath);
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.CreateTable<SimpleRow>();
                conn.InsertAll(rows, typeof(SimpleRow), runInTransaction: true);
            }

            const int queryIterations = 200;

            // ── 方式 A: 无缓存 — 每次创建新命令 ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        // 每次创建新命令，模拟无缓存场景
                        var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", i % 1000);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.QueryNoCacheMs = sw.ElapsedMilliseconds;
                    report.QueryNoCacheGC = gc0After - gc0;
                    Debug.Log($"  无缓存查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 B: 缓存 — 复用同一命令，仅更新参数 ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    // 创建一次命令并缓存
                    var cmd = conn.CreateCommand("SELECT * FROM SimpleRow WHERE Value1 > ?", 0);
                    for (int i = 0; i < queryIterations; i++)
                    {
                        // 更新参数值，复用 Prepared Statement
                        cmd.BindParameter(1, i % 1000);
                        var results = cmd.ExecuteQuery<SimpleRow>();
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.QueryWithCacheMs = sw.ElapsedMilliseconds;
                    report.QueryWithCacheGC = gc0After - gc0;
                    Debug.Log($"  缓存查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 C: 直接 API — 使用最低层 API 调用 ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    var cmd = conn.CreateCommand("SELECT * FROM SimpleRow WHERE Value1 > ?", 0);
                    for (int i = 0; i < queryIterations; i++)
                    {
                        cmd.BindParameter(1, i % 1000);
                        // 直接执行底层 Step，跳过 TableQuery 反射层
                        var stmt = SQLiteCommandHelper.Prepare(cmd);
                        try
                        {
                            while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                            {
                                // 仅读取第一列，测量纯 SQLite 层开销
                                var val = SQLite3.ColumnInt(stmt, 0);
                            }
                        }
                        finally
                        {
                            SQLite3.Reset(stmt);
                        }
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.QueryDirectCacheMs = sw.ElapsedMilliseconds;
                    report.QueryDirectCacheGC = gc0After - gc0;
                    Debug.Log($"  直接API缓存查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            var speedup = report.QueryNoCacheMs > 0
                ? (float)report.QueryNoCacheMs / Math.Max(1, report.QueryWithCacheMs)
                : 0;
            Debug.Log($"  <color=green>Prepared Statement 缓存加速比: {speedup:F2}x</color>");
            report.PreparedStatementSpeedup = speedup;

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        #endregion

        #region 测试 6: GC 压力测量（FastJson Span vs string.Split）

        /// <summary>
        /// 对比 FastJsonConvert 的 Span 解析与传统 string.Split 解析的 GC 压力和性能。
        /// 测试目的=量化 Span 解析对 GC 次数和内存分配的优化效果。
        /// 实现手段=使用 Stopwatch 计时 + GC.CollectionCount 测量 GC + 内存估算。
        /// Compare GC pressure and performance between FastJsonConvert Span parsing
        /// and traditional string.Split parsing.
        /// Test purpose=quantify Span parsing optimization for GC count and memory allocation.
        /// Method=use Stopwatch timing + GC.CollectionCount for GC + memory estimation.
        /// </summary>
        static void Test_GC_Pressure(SqliteBenchmarkReport report)
        {
            Debug.Log("<color=yellow>── 测试 6: GC 压力测量（FastJson Span vs string.Split）──</color>");

            // 生成测试 JSON 数据 — 模拟典型 int[] 配置数据
            const int arrayCount = 1000;
            const int elementCount = 10;
            var rng = new Random(42);
            var testData = new string[arrayCount];
            var sb = new StringBuilder();
            for (int i = 0; i < arrayCount; i++)
            {
                sb.Clear();
                sb.Append("[");
                for (int j = 0; j < elementCount; j++)
                {
                    sb.Append(rng.Next(0, 10000));
                    if (j < elementCount - 1) sb.Append(",");
                }
                sb.Append("]");
                testData[i] = sb.ToString();
            }

            const int iterations = 100;

            // ── 方式 A: FastJsonConvert Span 解析 ──
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var sw = Stopwatch.StartNew();
                var gc0 = GC.CollectionCount(0);
                long memBefore = GC.GetTotalMemory(true);

                for (int iter = 0; iter < iterations; iter++)
                {
                    for (int i = 0; i < arrayCount; i++)
                    {
                        var result = SqliteFastJsonConvert.DeserializeArrayInt(testData[i]);
                    }
                }

                sw.Stop();
                long memAfter = GC.GetTotalMemory(false);
                var gc0After = GC.CollectionCount(0);
                report.FastJsonSpanMs = sw.ElapsedMilliseconds;
                report.FastJsonSpanGC = gc0After - gc0;
                report.FastJsonSpanMemKB = Math.Max(0, memAfter - memBefore) / 1024f;
                Debug.Log($"  Span解析({iterations}x{arrayCount}次): {sw.ElapsedMilliseconds}ms, " +
                          $"GC Gen0:{gc0After - gc0}, 内存:{report.FastJsonSpanMemKB:F1}KB");
            }

            // ── 方式 B: 传统 string.Split 解析 ──
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var sw = Stopwatch.StartNew();
                var gc0 = GC.CollectionCount(0);
                long memBefore = GC.GetTotalMemory(true);

                for (int iter = 0; iter < iterations; iter++)
                {
                    for (int i = 0; i < arrayCount; i++)
                    {
                        // 模拟传统 string.Split 解析：产生大量临时字符串分配
                        var json = testData[i];
                        if (string.IsNullOrEmpty(json)) continue;
                        var trimmed = json.Trim('[', ']');
                        var parts = trimmed.Split(',');
                        var result = new int[parts.Length];
                        for (int j = 0; j < parts.Length; j++)
                        {
                            int.TryParse(parts[j].Trim(), out result[j]);
                        }
                    }
                }

                sw.Stop();
                long memAfter = GC.GetTotalMemory(false);
                var gc0After = GC.CollectionCount(0);
                report.FastJsonLegacyMs = sw.ElapsedMilliseconds;
                report.FastJsonLegacyGC = gc0After - gc0;
                report.FastJsonLegacyMemKB = Math.Max(0, memAfter - memBefore) / 1024f;
                Debug.Log($"  string.Split解析({iterations}x{arrayCount}次): {sw.ElapsedMilliseconds}ms, " +
                          $"GC Gen0:{gc0After - gc0}, 内存:{report.FastJsonLegacyMemKB:F1}KB");
            }

            var speedup = report.FastJsonLegacyMs > 0
                ? (float)report.FastJsonLegacyMs / Math.Max(1, report.FastJsonSpanMs)
                : 0;
            Debug.Log($"  <color=green>Span解析加速比: {speedup:F2}x</color>");
            report.FastJsonSpeedup = speedup;
        }

        #endregion

        #region 测试 7: 真实表数据导入测试

        /// <summary>
        /// 使用真实游戏 Schema 的混合表结构，模拟 xlsx 数据导入流程，
        /// 测量大量数据写入和查询的完整性能。
        /// 测试目的=验证真实游戏数据量下 SQLite 导入和查询的端到端性能。
        /// 实现手段=使用 BDFramework 数据生成器创建混合 POCO 数据 + Stopwatch 分阶段计时。
        /// Use real game Schema mixed table structures to simulate xlsx data import flow,
        /// measuring full performance of bulk data write and query.
        /// Test purpose=verify SQLite end-to-end performance under real game data volume.
        /// Method=use BDFramework data generator for mixed POCO data + Stopwatch phased timing.
        /// </summary>
        static void Test_RealTableDataImport(SqliteBenchmarkReport report, string dbDir, string realTableDir)
        {
            Debug.Log("<color=yellow>── 测试 7: 真实表数据导入测试 ──</color>");

            var dbPath = Path.Combine(dbDir, "real_data_import.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            // 生成各表数据 — 模拟真实 xlsx 导入数据量
            var heroRows = SqliteBenchmarkDataGenerator.GenerateHeroSkillParameterRows(295);
            var itemRows = SqliteBenchmarkDataGenerator.GenerateItemRows(337);
            var goodsRows = SqliteBenchmarkDataGenerator.GenerateGoodsBaseRows(170);
            var scalarRows = SqliteBenchmarkDataGenerator.GenerateScalarOnlyRows(295);
            var benchmarkRows = SqliteBenchmarkDataGenerator.GenerateBenchmarkRows(500);

            report.RealTableCount = 5; // 5个模拟 xlsx 表

            using (var conn = new SQLiteConnection(dbPath))
            {
                // ── 阶段 1: 大数据导入 ──
                {
                    var sw = Stopwatch.StartNew();

                    conn.CreateTable<HeroSkillParameterRow>();
                    conn.CreateTable<ItemRow>();
                    conn.CreateTable<GoodsBaseRow>();
                    conn.CreateTable<ScalarOnlyRow>();
                    conn.CreateTable<BenchmarkRow>();

                    conn.InsertAll(heroRows, typeof(HeroSkillParameterRow), runInTransaction: true);
                    conn.InsertAll(itemRows, typeof(ItemRow), runInTransaction: true);
                    conn.InsertAll(goodsRows, typeof(GoodsBaseRow), runInTransaction: true);
                    conn.InsertAll(scalarRows, typeof(ScalarOnlyRow), runInTransaction: true);
                    conn.InsertAll(benchmarkRows, typeof(BenchmarkRow), runInTransaction: true);

                    sw.Stop();
                    report.RealDataInsertMs = sw.ElapsedMilliseconds;
                    Debug.Log($"  大数据导入(5表, 共{heroRows.Count + itemRows.Count + goodsRows.Count + scalarRows.Count + benchmarkRows.Count}行): {sw.ElapsedMilliseconds}ms");
                }

                // ── 阶段 2: 条件查询 ──
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();

                    var heroResult = conn.Query<HeroSkillParameterRow>(
                        "SELECT * FROM HeroSkillParameter WHERE SkillType > ?", 3);
                    var itemResult = conn.Query<ItemRow>(
                        "SELECT * FROM Item WHERE Quality >= ?", 4);

                    sw.Stop();
                    report.RealDataQueryMs = sw.ElapsedMilliseconds;
                    report.RealDataQueryRows = heroResult.Count + itemResult.Count;
                    Debug.Log($"  条件查询: {sw.ElapsedMilliseconds}ms, 返回{heroResult.Count + itemResult.Count}行");
                }

                // ── 阶段 3: 全表扫描 ──
                {
                    var sw = Stopwatch.StartNew();

                    var allHero = conn.Query<HeroSkillParameterRow>("SELECT * FROM HeroSkillParameter");
                    var allItem = conn.Query<ItemRow>("SELECT * FROM Item");
                    var allGoods = conn.Query<GoodsBaseRow>("SELECT * FROM GoodsBase");
                    var allScalar = conn.Query<ScalarOnlyRow>("SELECT * FROM ScalarOnlyRow");
                    var allBench = conn.Query<BenchmarkRow>("SELECT * FROM BenchmarkRow");

                    sw.Stop();
                    report.RealDataFullScanMs = sw.ElapsedMilliseconds;
                    Debug.Log($"  全表扫描(5表, 共{allHero.Count + allItem.Count + allGoods.Count + allScalar.Count + allBench.Count}行): {sw.ElapsedMilliseconds}ms");
                }
            }

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        #endregion

        #region 内部辅助类

        /// <summary>
        /// SQLiteCommand 辅助类 — 暴露内部 Prepare/ReadCol 方法供基准测试使用。
        /// SQLiteCommand helper — exposes internal Prepare/ReadCol methods for benchmark use.
        /// </summary>
        static class SQLiteCommandHelper
        {
            static readonly System.Reflection.MethodInfo PrepareMethod =
                typeof(SQLiteCommand).GetMethod("Prepare",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            static readonly System.Reflection.MethodInfo ReadColMethod =
                typeof(SQLiteCommand).GetMethod("ReadCol",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            public static Sqlite3Statement Prepare(SQLiteCommand cmd)
            {
                return (Sqlite3Statement)PrepareMethod.Invoke(cmd, null);
            }

            public static object ReadCol(SQLiteCommand cmd, Sqlite3Statement stmt, int index, SQLite3.ColType colType, Type clrType)
            {
                return ReadColMethod.Invoke(cmd, new object[] { stmt, index, colType, clrType });
            }
        }

        /// <summary>
        /// FastColumnSetter 辅助类 — 通过反射访问 internal GetFastSetter 方法。
        /// 框架基础设施代码使用反射，在注释中说明原因：FastColumnSetter.GetFastSetter 是 internal，
        /// 无法从测试程序集直接调用，只能通过反射访问。
        /// FastColumnSetter helper — accesses internal GetFastSetter method via reflection.
        /// Framework infrastructure code uses reflection with documented reason:
        /// FastColumnSetter.GetFastSetter is internal and cannot be called directly
        /// from the test assembly, requiring reflection access.
        /// </summary>
        static class FastColumnSetterHelper
        {
            static readonly Type FastColumnSetterType =
                typeof(SQLiteConnection).Assembly.GetType("SQLite4Unity3d.FastColumnSetter");

            static readonly System.Reflection.MethodInfo GetFastSetterMethod =
                FastColumnSetterType?.GetMethod("GetFastSetter",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            /// <summary>
            /// 通过反射调用 FastColumnSetter.GetFastSetter&lt;T&gt;，返回 null 表示无 fast setter。
            /// Invokes FastColumnSetter.GetFastSetter&lt;T&gt; via reflection; returns null if no fast setter available.
            /// </summary>
            public static Action<object, Sqlite3Statement, int> GetFastSetter<T>(SQLiteConnection conn, TableMapping.Column column)
            {
                if (GetFastSetterMethod == null) return null;
                var genericMethod = GetFastSetterMethod.MakeGenericMethod(typeof(T));
                return genericMethod.Invoke(null, new object[] { conn, column }) as Action<object, Sqlite3Statement, int>;
            }
        }

        #endregion
    }
}
