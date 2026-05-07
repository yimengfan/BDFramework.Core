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

                // ─── 测试 8: 查询+反序列化 端到端基准 ───
                Test_QueryDeserialize_EndToEnd(report, dbDir);

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

            // ── 方式 A: 无缓存 — 每次创建新连接和新命令，模拟完全无缓存场景 ──
            // ── Mode A: No cache — create new connection and command each time, simulating fully uncached scenario ──
            {
                var sw = Stopwatch.StartNew();
                var gc0 = GC.CollectionCount(0);

                for (int i = 0; i < queryIterations; i++)
                {
                    // 每次创建新连接，强制重新 Prepare + ColumnMapping + FastSetter 构建
                    // New connection each time forces re-Prepare + re-ColumnMapping + re-FastSetter build
                    using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                    {
                        ApplyBenchmarkPragmas(conn);
                        var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", i % 1000);
                    }
                }

                sw.Stop();
                var gc0After = GC.CollectionCount(0);
                report.QueryNoCacheMs = sw.ElapsedMilliseconds;
                report.QueryNoCacheGC = gc0After - gc0;
                Debug.Log($"  无缓存查询(新连接, {queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
            }

            // ── 方式 B: 缓存 — 复用同一连接，Prepared Statement + ColumnMapping + FastSetter 全部命中缓存 ──
            // ── Mode B: Cached — reuse same connection, PS + ColumnMapping + FastSetter all cache hits ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        // 复用同一连接，所有缓存（PS、ColumnMapping、FastSetter）命中
                        // Reuse same connection — all caches (PS, ColumnMapping, FastSetter) hit
                        var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", i % 1000);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    report.QueryWithCacheMs = sw.ElapsedMilliseconds;
                    report.QueryWithCacheGC = gc0After - gc0;
                    Debug.Log($"  缓存查询(热连接, {queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 C: 直接 API — 使用 SQLite3 原生 API 绕过 SQLiteCommand 层 ──
            {
                using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    // 直接使用 SQLite3 原生 API，绕过 SQLiteCommand 的参数绑定和反射层
                    var sql = "SELECT * FROM SimpleRow WHERE Value1 > ?";
                    var stmt = SQLite3.Prepare2(conn.Handle, sql);
                    try
                    {
                        for (int i = 0; i < queryIterations; i++)
                        {
                            // 直接绑定 int 参数，复用 prepared statement
                            SQLite3.BindInt(stmt, 1, i % 1000);
                            while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                            {
                                // 仅读取第一列，测量纯 SQLite 层开销
                                var val = SQLite3.ColumnInt(stmt, 0);
                            }
                            SQLite3.Reset(stmt);
                        }
                    }
                    finally
                    {
                        SQLite3.Finalize(stmt);
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
            var rng = new System.Random(42);
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

        #region 测试 8: 查询+反序列化 端到端基准

        /// <summary>
        /// 用户典型场景基准测试：查询+反序列化。
        /// 测量 ExecuteQuery&lt;T&gt; 路径的端到端性能和每步耗时：
        ///   - 构建 FastColumnSetter 委托，标量走快速委托，数组走类型化 DeserializeArray*
        /// 同时测量冷查询（首次，含 ColumnMapping + PS 缓存未命中）和热查询（后续，PS 缓存命中）的差异。
        ///
        /// End-to-end benchmark for the user's typical scenario: query + deserialize.
        /// Measures end-to-end query + deserialize performance on the given POCO type.
        ///
        /// 测试目的=量化用户典型查询+反序列化场景中各步骤的耗时分布。
        /// 实现手段=使用 Stopwatch 分步计时 + GC.CollectionCount 测量 + 手动分步计时器。
        /// </summary>
        static public void Test_QueryDeserialize_EndToEnd(SqliteBenchmarkReport report, string dbDir)
        {
            Debug.Log("<color=yellow>── 测试 8: 查询+反序列化 端到端基准 ──</color>");

            var dbPath = Path.Combine(dbDir, "e2e_query.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            // ── 创建测试数据库 ──
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.CreateTable<HeroSkillParameterRow>();
                conn.CreateTable<ItemRow>();
                conn.CreateTable<GoodsBaseRow>();
                conn.CreateTable<ScalarOnlyRow>();

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

            var e2eResults = new List<E2EQueryResult>();

            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);

                // ── 测试每种 POCO 类型 ──
                MeasureE2EForTable<ScalarOnlyRow>(conn, "ScalarOnly(17标量0数组)", e2eResults);
                MeasureE2EForTable<ItemRow>(conn, "Item(15标量+2数组)", e2eResults);
                MeasureE2EForTable<GoodsBaseRow>(conn, "GoodsBase(18标量+7数组)", e2eResults);
                MeasureE2EForTable<HeroSkillParameterRow>(conn, "HeroSkillParameter(12标量+21数组)", e2eResults);
            }

            // ── 打印对比报告 ──
            PrintE2EReport(e2eResults);
            report.E2EQueryResults = e2eResults;

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        /// <summary>
        /// 对指定 POCO 类型测量端到端查询+反序列化性能。
        /// Measures end-to-end query + deserialize performance on the given POCO type.
        /// </summary>
        static void MeasureE2EForTable<T>(SQLiteConnection conn, string label, List<E2EQueryResult> results) where T : new()
        {
            var map = conn.GetMapping(typeof(T));
            var tableName = map.TableName;
            var sql = $"SELECT * FROM {tableName}";

            Debug.Log($"  ── {label} ({tableName}) ──");

            // 冷查询（首次 — 含 ColumnMapping + Prepare）
            // Cold query (first call — includes ColumnMapping + Prepare)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var gc0 = GC.CollectionCount(0);
            var sw = Stopwatch.StartNew();
            var coldResult = conn.Query<T>(sql);
            sw.Stop();
            var gc0After = GC.CollectionCount(0);
            var coldMs = sw.ElapsedTicks / 10000f;
            var coldGC = gc0After - gc0;
            var rowCount = coldResult.Count;
            Debug.Log($"    冷查询: {coldMs:F2}ms, {rowCount}行, GC Gen0:{coldGC}");

            // 热查询（后续 — PS 缓存命中，ColumnMapping 缓存命中）
            // Warm query (subsequent — PS cache hit, ColumnMapping cache hit)
            const int warmIterations = 5;
            float warmTotalMs = 0f;
            int warmGC = 0;
            for (int i = 0; i < warmIterations; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                gc0 = GC.CollectionCount(0);
                sw.Restart();
                var warmResult = conn.Query<T>(sql);
                sw.Stop();
                gc0After = GC.CollectionCount(0);
                warmTotalMs += sw.ElapsedTicks / 10000f;
                warmGC += gc0After - gc0;
            }
            var warmAvgMs = warmTotalMs / warmIterations;
            Debug.Log($"    热查询(avg {warmIterations}次): {warmAvgMs:F2}ms, GC Gen0:{warmGC}");

            // 分步计时
            // Step-by-step timing
            var steps = MeasureQuerySteps<T>(conn, 3);

            // ── 汇总结果 ──
            var e2eResult = new E2EQueryResult
            {
                Label = label,
                TableName = tableName,
                RowCount = rowCount,
                ScalarFields = steps.ScalarFields,
                ArrayFields = steps.ArrayFields,
                ColdMs = coldMs,
                WarmAvgMs = warmAvgMs,
                ColdGC = coldGC,
                WarmGC = warmGC,
                PrepareMs = steps.PrepareMs,
                ColumnMappingMs = steps.ColumnMappingMs,
                StepMs = steps.StepMs,
                CreateObjMs = steps.CreateObjMs,
                FastSetMs = steps.FastSetMs,
                ReadColMs = steps.ReadColMs,
            };
            results.Add(e2eResult);
        }

        /// <summary>
        /// 打印查询+反序列化端到端对比报告。
        /// <summary>
        /// 打印查询+反序列化端到端报告。
        /// Print query+deserialize end-to-end report.
        /// </summary>
        static void PrintE2EReport(List<E2EQueryResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                        查询+反序列化 端到端基准                                                    ║");
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║                                                                                                    ║");
            sb.AppendLine("║  说明：                                                                                             ║");
            sb.AppendLine("║  • 路径 = ExecuteQuery<T>：构建 FastSetter 委托，标量走快速委托，数组走类型化 DeserializeArray*        ║");
            sb.AppendLine("║  • 冷查询 = 首次调用（含 Prepare + ColumnMapping），热查询 = 后续调用（缓存命中）                      ║");
            sb.AppendLine("║                                                                                                    ║");
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            // ── 端到端总耗时 ──
            sb.AppendLine("║  ── 端到端总耗时 ──".PadRight(100) + "║");
            sb.AppendLine($"║  {"表名",-35} {"行数",5} {"冷查询",9} {"热查询",9} {"冷/热",7}".PadRight(100) + "║");
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            foreach (var r in results)
            {
                var coldWarmRatio = r.WarmAvgMs > 0 ? r.ColdMs / r.WarmAvgMs : 0;
                sb.AppendLine($"║  {r.Label,-35} {r.RowCount,5} {r.ColdMs,8:F2}ms {r.WarmAvgMs,8:F2}ms {coldWarmRatio,6:F2}x".PadRight(100) + "║");
            }

            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            // ── 分步耗时 ──
            sb.AppendLine("║  ── 分步耗时（热查询平均）──".PadRight(100) + "║");
            sb.AppendLine($"║  {"表名",-35} {"Prepare",9} {"ColMap",9} {"Step",9} {"CreateObj",9} {"FastSet",9} {"ReadCol",9} {"总计",9}".PadRight(100) + "║");
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            foreach (var r in results)
            {
                var total = r.PrepareMs + r.ColumnMappingMs + r.StepMs + r.CreateObjMs + r.FastSetMs + r.ReadColMs;
                sb.AppendLine($"║  {r.Label,-35} {r.PrepareMs,8:F2}ms {r.ColumnMappingMs,8:F2}ms {r.StepMs,8:F2}ms {r.CreateObjMs,8:F2}ms {r.FastSetMs,8:F2}ms {r.ReadColMs,8:F2}ms {total,8:F2}ms".PadRight(100) + "║");
            }

            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            // ── 分步占比分析 ──
            sb.AppendLine("║  ── 分步占比分析（热查询）──".PadRight(100) + "║");
            sb.AppendLine($"║  {"表名",-35} {"Prepare",8} {"ColMap",8} {"Step",8} {"CreateObj",8} {"FastSet",8} {"ReadCol",8} {"瓶颈",12}".PadRight(100) + "║");
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            foreach (var r in results)
            {
                var total = r.PrepareMs + r.ColumnMappingMs + r.StepMs + r.CreateObjMs + r.FastSetMs + r.ReadColMs;
                var prepPct = total > 0 ? r.PrepareMs / total * 100f : 0f;
                var cmPct = total > 0 ? r.ColumnMappingMs / total * 100f : 0f;
                var stepPct = total > 0 ? r.StepMs / total * 100f : 0f;
                var coPct = total > 0 ? r.CreateObjMs / total * 100f : 0f;
                var fsPct = total > 0 ? r.FastSetMs / total * 100f : 0f;
                var rcPct = total > 0 ? r.ReadColMs / total * 100f : 0f;
                var bottleneck = rcPct > 50f ? "🔴ReadCol" :
                                 stepPct > 50f ? "🟡Step" :
                                 rcPct > stepPct ? "🟠RC>Step" : "🟢均衡";
                sb.AppendLine($"║  {r.Label,-35} {prepPct,7:F1}% {cmPct,7:F1}% {stepPct,7:F1}% {coPct,7:F1}% {fsPct,7:F1}% {rcPct,7:F1}% {bottleneck,12}".PadRight(100) + "║");
            }

            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            // ── GC 压力 ──
            sb.AppendLine("║  ── GC 压力 ──".PadRight(100) + "║");
            sb.AppendLine($"║  {"表名",-35} {"冷查询GC",9} {"热查询GC",9}".PadRight(100) + "║");
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

            foreach (var r in results)
            {
                sb.AppendLine($"║  {r.Label,-35} {r.ColdGC,8}次 {r.WarmGC,8}次".PadRight(100) + "║");
            }

            // ── 结论 ──
            sb.AppendLine("╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  ── 结论 ──".PadRight(100) + "║");

            var maxArrayResult = results.OrderByDescending(r => r.ArrayFields).FirstOrDefault();
            if (maxArrayResult != null)
            {
                var total = maxArrayResult.PrepareMs + maxArrayResult.ColumnMappingMs + maxArrayResult.StepMs + maxArrayResult.CreateObjMs + maxArrayResult.FastSetMs + maxArrayResult.ReadColMs;
                var rcPct = total > 0 ? maxArrayResult.ReadColMs / total * 100f : 0f;

                if (rcPct > 50f)
                {
                    sb.AppendLine($"║  🔴 在数组密集型表({maxArrayResult.Label})中，ReadCol 占比 {rcPct:F1}%".PadRight(100) + "║");
                    sb.AppendLine($"║     原因: FastJsonConvert 反序列化数组字段耗时较高".PadRight(100) + "║");
                }
                else if (rcPct > 20f)
                {
                    sb.AppendLine($"║  🟡 在数组密集型表({maxArrayResult.Label})中，ReadCol 占比 {rcPct:F1}%".PadRight(100) + "║");
                    sb.AppendLine($"║     FastSetter 已覆盖大部分字段".PadRight(100) + "║");
                }
                else
                {
                    sb.AppendLine($"║  🟢 ReadCol 不是主要瓶颈 (占比 {rcPct:F1}%)，FastSetter 已覆盖大部分字段".PadRight(100) + "║");
                }
            }

            var scalarResult = results.FirstOrDefault(r => r.ArrayFields == 0);
            if (scalarResult != null)
            {
                var totalS = scalarResult.PrepareMs + scalarResult.ColumnMappingMs + scalarResult.StepMs + scalarResult.CreateObjMs + scalarResult.FastSetMs + scalarResult.ReadColMs;
                var rcPctS = totalS > 0 ? scalarResult.ReadColMs / totalS * 100f : 0f;
                sb.AppendLine($"║  📊 纯标量表({scalarResult.Label}): ReadCol占比={rcPctS:F1}% — 标量字段瓶颈较小".PadRight(100) + "║");
            }

            sb.AppendLine("║                                                                                                    ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════════════════════════════════════════════╝");

            Debug.Log(sb.ToString());
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
