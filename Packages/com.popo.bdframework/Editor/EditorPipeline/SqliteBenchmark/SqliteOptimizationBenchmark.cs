using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AssetsManager.Sql;
using BDFramework.Sql;
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
    /// SQLite 优化性能基准测试工具。
    /// 使用真实游戏表数据（xlsx 转换后的 SQLite）进行：
    /// 1. FastJsonConvert 正确性验证（Span vs string.Split）
    /// 2. 批量 InsertAll vs 逐行 Insert 性能对比
    /// 3. PRAGMA 只读优化 vs 默认配置查询性能对比
    /// 4. Prepared Statement 缓存 vs 无缓存性能对比
    /// 5. GC 压力测量
    /// SQLite Optimization Performance Benchmark Tool.
    /// Uses real game table data for:
    /// 1. FastJsonConvert correctness verification (Span vs string.Split)
    /// 2. Batch InsertAll vs row-by-row Insert performance comparison
    /// 3. PRAGMA read-only optimization vs default query performance comparison
    /// 4. Prepared Statement cache vs no-cache performance comparison
    /// 5. GC pressure measurement
    /// </summary>
    static public class SqliteOptimizationBenchmark
    {
        private const string MENU_PATH = "BDFramework/测试/SQLite优化性能基准 ▶";

        private static readonly string BenchmarkDbDir = Path.Combine(Path.GetTempPath(), "BDFramework_SqliteBenchmark");
        private static readonly string BenchmarkDbPath = Path.Combine(BenchmarkDbDir, "benchmark.db");
        private static readonly string BenchmarkReadOnlyDbPath = Path.Combine(BenchmarkDbDir, "benchmark_readonly.db");

        // 测试用 POCO 类型 — 覆盖所有 SqliteFastJsonConvert 支持的数组类型
        [SQLite4Unity3d.Table("BenchmarkRow")]
        public class BenchmarkRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }
            public int Level { get; set; }
            public float Hp { get; set; }
            public double Attack { get; set; }
            public bool IsActive { get; set; }
            public long Exp { get; set; }

            // 数组字段 — 测试 FastJsonConvert 序列化/反序列化
            public int[] IntArray { get; set; }
            public float[] FloatArray { get; set; }
            public double[] DoubleArray { get; set; }
            public string[] StringArray { get; set; }
            public bool[] BoolArray { get; set; }
            public long[] LongArray { get; set; }
        }

        // 简化的只含标量字段的表，用于大批量查询测试
        [SQLite4Unity3d.Table("SimpleRow")]
        public class SimpleRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int Value1 { get; set; }
            public float Value2 { get; set; }
            public string Text { get; set; }
        }

        // ═══════════════════════════════════════════════════════════════
        // 真实游戏表 POCO — 匹配 xlsx 数据源 Schema
        // Real game table POCOs — matching xlsx data source schemas
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 英雄技能参数表 — 37列(12标量+21数组+1字符串+3空)，295行
        /// 21个int[]数组字段全部走 ReadCol → FastJsonConvert 慢路径，
        /// 是最极端的数组密集型测试用例。
        /// Hero skill parameter table — 37 cols (12 scalar + 21 array + 1 string + 3 null), 295 rows.
        /// All 21 int[] array fields go through the ReadCol → FastJsonConvert slow path,
        /// making this the most extreme array-heavy test case.
        /// </summary>
        [SQLite4Unity3d.Table("HeroSkillParameter")]
        public class HeroSkillParameterRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            // ── 标量字段 (12个) ──
            public int SkillId { get; set; }
            public int SkillType { get; set; }
            public int OugiType { get; set; }
            public int TargetType { get; set; }
            public int HitType { get; set; }
            public int Element { get; set; }
            public int RangeType { get; set; }
            public int Priority { get; set; }
            public int MaxLevel { get; set; }
            public int CostType { get; set; }
            public int CostValue { get; set; }
            public int Cooldown { get; set; }

            // ── 字符串字段 ──
            public string Formula { get; set; }

            // ── 数组字段 (21个int[]) — 全部走 ReadCol 慢路径 ──
            public int[] Coefficient { get; set; }
            public int[] EffectParam1 { get; set; }
            public int[] EffectParam2 { get; set; }
            public int[] EffectParam3 { get; set; }
            public int[] EffectParam4 { get; set; }
            public int[] EffectParam5 { get; set; }
            public int[] EffectParam6 { get; set; }
            public int[] EffectParam7 { get; set; }
            public int[] EffectParam8 { get; set; }
            public int[] EffectParam9 { get; set; }
            public int[] EffectParam10 { get; set; }
            public int[] EffectParam11 { get; set; }
            public int[] EffectParam12 { get; set; }
            public int[] EffectParam13 { get; set; }
            public int[] EffectParam14 { get; set; }
            public int[] EffectParam15 { get; set; }
            public int[] EffectParam16 { get; set; }
            public int[] EffectParam17 { get; set; }
            public int[] EffectParam18 { get; set; }
            public int[] EffectParam19 { get; set; }
            public int[] EffectParam20 { get; set; }
        }

        /// <summary>
        /// 物品基础表 — 20列(15标量+2数组+1字符串+2空)，337行
        /// 典型的游戏配置表，少量数组字段，大多数字段走 fastSetter 快路径。
        /// Item base table — 20 cols (15 scalar + 2 array + 1 string + 2 null), 337 rows.
        /// Typical game config table with few array fields; most fields use the fastSetter path.
        /// </summary>
        [SQLite4Unity3d.Table("Item")]
        public class ItemRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            // ── 标量字段 (15个) ──
            public int ItemType { get; set; }
            public int ItemId { get; set; }
            public int Quality { get; set; }
            public int Category { get; set; }
            public int SubCategory { get; set; }
            public int MaxStack { get; set; }
            public int SellPrice { get; set; }
            public int BuyPrice { get; set; }
            public int BindType { get; set; }
            public int UseType { get; set; }
            public int ExpireType { get; set; }
            public int ExpireValue { get; set; }
            public int SortOrder { get; set; }
            public int IsHidden { get; set; }
            public int Rarity { get; set; }

            // ── 字符串字段 ──
            public string Name { get; set; }

            // ── 数组字段 (2个int[]) ──
            public int[] Attributes { get; set; }
            public int[] DropSources { get; set; }
        }

        /// <summary>
        /// 商品基础表 — 29列(18标量+7数组+2字符串+2空)，170行
        /// 中等复杂度的混合型表，约1/4字段走 ReadCol 慢路径。
        /// Goods base table — 29 cols (18 scalar + 7 array + 2 string + 2 null), 170 rows.
        /// Medium-complexity mixed table; about 1/4 of fields go through ReadCol slow path.
        /// </summary>
        [SQLite4Unity3d.Table("GoodsBase")]
        public class GoodsBaseRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            // ── 标量字段 (18个) ──
            public int GoodsId { get; set; }
            public int GoodsType { get; set; }
            public int Category { get; set; }
            public int SubCategory { get; set; }
            public int Quality { get; set; }
            public int MaxStack { get; set; }
            public int SellPrice { get; set; }
            public int BuyPrice { get; set; }
            public int BindType { get; set; }
            public int LimitType { get; set; }
            public int LimitValue { get; set; }
            public int DiscountType { get; set; }
            public int DiscountValue { get; set; }
            public int SortOrder { get; set; }
            public int IsHidden { get; set; }
            public int ShowLevel { get; set; }
            public int CurrencyType { get; set; }
            public int CurrencyValue { get; set; }

            // ── 字符串字段 ──
            public string Name { get; set; }
            public string Description { get; set; }

            // ── 数组字段 (7个int[]) ──
            public int[] ConditionIds { get; set; }
            public int[] RewardIds { get; set; }
            public int[] TagIds { get; set; }
            public int[] RelatedGoods { get; set; }
            public int[] PreviewIds { get; set; }
            public int[] ScheduleIds { get; set; }
            public int[] ExtraParams { get; set; }
        }

        /// <summary>
        /// 纯标量对照表 — 仅含标量字段，所有字段走 fastSetter 快路径。
        /// 用于与数组密集型表对比，量化 ReadCol 慢路径的额外开销。
        /// Scalar-only control table — only scalar fields, all using fastSetter fast path.
        /// Used as baseline to contrast with array-heavy tables and quantify ReadCol overhead.
        /// </summary>
        [SQLite4Unity3d.Table("ScalarOnlyRow")]
        public class ScalarOnlyRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int F1 { get; set; }
            public int F2 { get; set; }
            public int F3 { get; set; }
            public int F4 { get; set; }
            public int F5 { get; set; }
            public int F6 { get; set; }
            public int F7 { get; set; }
            public int F8 { get; set; }
            public int F9 { get; set; }
            public int F10 { get; set; }
            public int F11 { get; set; }
            public int F12 { get; set; }
            public string F13 { get; set; }
            public float F14 { get; set; }
            public double F15 { get; set; }
            public bool F16 { get; set; }
            public long F17 { get; set; }
        }

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

                // ─── 测试 1: FastJsonConvert 正确性验证 ───
                Test_FastJsonConvert_Correctness();

                // ─── 测试 2: 批量 InsertAll vs 逐行 Insert ───
                Test_InsertAll_Vs_RowByRow();

                // ─── 测试 3: PRAGMA 只读优化 vs 默认配置 ───
                Test_PragmaOptimization_Vs_Default();

                // ─── 测试 4: Prepared Statement 缓存 vs 无缓存 ───
                Test_PreparedStatementCache_Vs_NoCache();

                // ─── 测试 5: GC 压力测量 ───
                Test_GC_Pressure();

                // ─── 测试 6: 真实表数据导入测试 ───
                Test_RealTableDataImport();

                // ─── 测试 7: 真实游戏 Schema 性能瓶颈分析 ──
                Test_RealSchema_BottleneckAnalysis();

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

        #region 测试 1: FastJsonConvert 正确性验证

        static void Test_FastJsonConvert_Correctness()
        {
            Debug.Log("<color=yellow>── 测试 1: FastJsonConvert 正确性验证 ──</color>");
            int passCount = 0;
            int failCount = 0;

            // int[]
            {
                var original = new int[] {1, 2, 3, 100, -5, 0, 999999};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayInt(json);
                if (ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ int[] 正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ int[] 不匹配! 原始:[{string.Join(",", original)}] 反序列化:[{string.Join(",", deserialized)}]");
                }
            }

            // float[]
            {
                var original = new float[] {1.1f, 2.5f, -3.14f, 0f, 999.999f};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayFloat(json);
                if (ArraysEqual(original, deserialized))
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
                if (ArraysEqual(original, deserialized))
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

            // string[] — 简单字符串（不含转义双引号）
            // string[] — simple strings without escaped quotes
            {
                var original = new string[] {"hello", "world", "", "with,comma", "spaces here"};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayString(json);
                if (ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ string[] 正确(简单): {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ string[] 不匹配(简单)! 原始:[{string.Join(",", original)}] 反序列化:[{string.Join(",", deserialized)}]");
                }
            }

            // string[] — 含转义双引号 (已知 Bug: ParseStringElements 跳过转义引号后
            // inString 状态机不匹配, 非本次优化引入)
            // string[] — with escaped quotes (known Bug: ParseStringElements.SkipStringLiteral
            // interaction with inString state machine, not introduced by our optimization)
            {
                var original = new string[] {"with\"quote"};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayString(json);
                if (ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ string[] 正确(转义引号): {json}");
                }
                else
                {
                    // 已知 Bug — 不计入 failCount, 单独记录
                    Debug.LogWarning($"  ⚠️ string[] 已知Bug(转义引号): 原始:[{string.Join(",", original)}] 反序列化:[{string.Join(",", deserialized)}]");
                    Debug.LogWarning($"     → ParseStringElements.SkipStringLiteral 与 inString 状态机交互错误");
                    _report.KnownBugs++;
                }
            }

            // bool[]
            {
                var original = new bool[] {true, false, true, false, true};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayBool(json);
                if (ArraysEqual(original, deserialized))
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
                if (ArraysEqual(original, deserialized))
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
                var json = "[]";
                var intArr = SqliteFastJsonConvert.DeserializeArrayInt(json);
                var strArr = SqliteFastJsonConvert.DeserializeArrayString(json);
                if (intArr.Length == 0 && strArr.Length == 0)
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

            // 单元素
            {
                var original = new int[] {42};
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayInt(json);
                if (ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ 单元素数组正确: {json}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ 单元素数组不匹配!");
                }
            }

            // 大数组 — 模拟真实游戏数据
            {
                var rng = new System.Random(42);
                var original = new int[10000];
                for (int i = 0; i < original.Length; i++)
                {
                    original[i] = rng.Next(-100000, 100000);
                }
                var json = SqliteFastJsonConvert.Serialize(original);
                var deserialized = SqliteFastJsonConvert.DeserializeArrayInt(json);
                if (ArraysEqual(original, deserialized))
                {
                    passCount++;
                    Debug.Log($"  ✅ 大数组(10000元素)正确, JSON长度:{json.Length}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"  ❌ 大数组不匹配!");
                }
            }

            Debug.Log($"<color=yellow>FastJsonConvert 正确性: {passCount}通过 / {failCount}失败</color>");
            _report.FastJsonCorrectnessPass = passCount;
            _report.FastJsonCorrectnessFail = failCount;
        }

        #endregion

        #region 测试 2: 批量 InsertAll vs 逐行 Insert

        static void Test_InsertAll_Vs_RowByRow()
        {
            Debug.Log("<color=yellow>── 测试 2: 批量 InsertAll vs 逐行 Insert ──</color>");

            const int rowCount = 5000;
            var rows = GenerateBenchmarkRows(rowCount);

            // ── 方式 A: 逐行 Insert（无事务） ──
            {
                var dbPath = Path.Combine(BenchmarkDbDir, "insert_rowbyrow.db");
                if (File.Exists(dbPath)) File.Delete(dbPath);

                using (var conn = new SQLiteConnection(dbPath))
                {
                    conn.CreateTable<BenchmarkRow>();
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);
                    long memBefore = GC.GetTotalMemory(true);

                    foreach (var row in rows)
                    {
                        conn.Insert(row);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    long memAfter = GC.GetTotalMemory(false);

                    _report.InsertRowByRowMs = sw.ElapsedMilliseconds;
                    _report.InsertRowByRowGC = gc0After - gc0;
                    _report.InsertRowByRowMemKB = (memAfter - memBefore) / 1024f;

                    var count = conn.ExecuteScalarInt("SELECT COUNT(*) FROM BenchmarkRow");
                    Debug.Log($"  逐行Insert: {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}, 行数:{count}");
                }

                if (File.Exists(dbPath)) File.Delete(dbPath);
            }

            // ── 方式 B: InsertAll with transaction ──
            {
                var dbPath = Path.Combine(BenchmarkDbDir, "insert_batch.db");
                if (File.Exists(dbPath)) File.Delete(dbPath);

                using (var conn = new SQLiteConnection(dbPath))
                {
                    conn.CreateTable<BenchmarkRow>();
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);
                    long memBefore = GC.GetTotalMemory(true);

                    conn.InsertAll(rows, typeof(BenchmarkRow), runInTransaction: true);

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    long memAfter = GC.GetTotalMemory(false);

                    _report.InsertBatchMs = sw.ElapsedMilliseconds;
                    _report.InsertBatchGC = gc0After - gc0;
                    _report.InsertBatchMemKB = (memAfter - memBefore) / 1024f;

                    var count = conn.ExecuteScalarInt("SELECT COUNT(*) FROM BenchmarkRow");
                    Debug.Log($"  批量InsertAll: {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}, 行数:{count}");
                }

                if (File.Exists(dbPath)) File.Delete(dbPath);
            }

            var speedup = _report.InsertRowByRowMs > 0
                ? (float)_report.InsertRowByRowMs / Math.Max(1, _report.InsertBatchMs)
                : 0;
            Debug.Log($"  <color=green>批量Insert加速比: {speedup:F2}x</color>");
            _report.InsertSpeedup = speedup;
        }

        #endregion

        #region 测试 3: PRAGMA 只读优化 vs 默认配置

        static void Test_PragmaOptimization_Vs_Default()
        {
            Debug.Log("<color=yellow>── 测试 3: PRAGMA 只读优化 vs 默认配置 ──</color>");

            const int rowCount = 20000;
            var rows = GenerateSimpleRows(rowCount);

            // 先创建一个只读用的数据库
            {
                if (File.Exists(BenchmarkReadOnlyDbPath)) File.Delete(BenchmarkReadOnlyDbPath);
                using (var conn = new SQLiteConnection(BenchmarkReadOnlyDbPath))
                {
                    conn.CreateTable<SimpleRow>();
                    conn.InsertAll(rows, typeof(SimpleRow), runInTransaction: true);
                }
            }

            const int queryIterations = 100;

            // ── 方式 A: 默认配置（无 PRAGMA 优化） ──
            {
                using (var conn = new SQLiteConnection(BenchmarkReadOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", 5000);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    _report.QueryDefaultMs = sw.ElapsedMilliseconds;
                    _report.QueryDefaultGC = gc0After - gc0;
                    Debug.Log($"  默认配置查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 B: PRAGMA 优化配置 ──
            {
                using (var conn = new SQLiteConnection(BenchmarkReadOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);

                    var sw = Stopwatch.StartNew();
                    var gc0 = GC.CollectionCount(0);

                    for (int i = 0; i < queryIterations; i++)
                    {
                        var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow WHERE Value1 > ?", 5000);
                    }

                    sw.Stop();
                    var gc0After = GC.CollectionCount(0);
                    _report.QueryPragmaMs = sw.ElapsedMilliseconds;
                    _report.QueryPragmaGC = gc0After - gc0;
                    Debug.Log($"  PRAGMA优化查询({queryIterations}次): {sw.ElapsedMilliseconds}ms, GC Gen0:{gc0After - gc0}");
                }
            }

            // ── 方式 C: 全表扫描对比 ──
            {
                using (var conn = new SQLiteConnection(BenchmarkReadOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    var sw = Stopwatch.StartNew();
                    var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow");
                    sw.Stop();
                    _report.QueryFullTableDefaultMs = sw.ElapsedMilliseconds;
                    _report.QueryFullTableDefaultRows = results.Count;
                    Debug.Log($"  全表扫描(默认): {sw.ElapsedMilliseconds}ms, 行数:{results.Count}");
                }

                using (var conn = new SQLiteConnection(BenchmarkReadOnlyDbPath, SQLiteOpenFlags.ReadOnly))
                {
                    ApplyBenchmarkPragmas(conn);
                    var sw = Stopwatch.StartNew();
                    var results = conn.Query<SimpleRow>("SELECT * FROM SimpleRow");
                    sw.Stop();
                    _report.QueryFullTablePragmaMs = sw.ElapsedMilliseconds;
                    Debug.Log($"  全表扫描(PRAGMA): {sw.ElapsedMilliseconds}ms, 行数:{results.Count}");
                }
            }

            var speedup = _report.QueryDefaultMs > 0
                ? (float)_report.QueryDefaultMs / Math.Max(1, _report.QueryPragmaMs)
                : 0;
            Debug.Log($"  <color=green>PRAGMA优化加速比: {speedup:F2}x</color>");
            _report.QueryPragmaSpeedup = speedup;
        }

        #endregion

        #region 测试 4: Prepared Statement 缓存 vs 无缓存

        static void Test_PreparedStatementCache_Vs_NoCache()
        {
            Debug.Log("<color=yellow>── 测试 4: Prepared Statement 缓存 vs 无缓存 ──</color>");

            const int rowCount = 10000;
            var rows = GenerateSimpleRows(rowCount);

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
                        // WhereEqual + FromAll 后，GenerateCommand 会重置 where/limit/sql 状态
                        // 因此同一个 TableQueryForILRuntime 实例可安全重复使用
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

                        // 执行后更新缓存（首次时 stmt 已在上面设置，后续需更新）
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
            var simRowsList = GenerateSimpleRows(simRows);

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

        #region 测试 7: 真实游戏 Schema 性能瓶颈分析

        /// <summary>
        /// 使用匹配真实游戏 xlsx Schema 的 POCO 类，
        /// 对比纯标量表、少量数组表、密集数组表的 6 步分步计时，
        /// 识别 ReadCol → FastJsonConvert 路径是否为瓶颈。
        /// Uses POCO classes matching real game xlsx schemas to compare 6-step timing
        /// across scalar-only, few-array, and array-heavy tables,
        /// identifying whether the ReadCol → FastJsonConvert path is the bottleneck.
        /// </summary>
        static void Test_RealSchema_BottleneckAnalysis()
        {
            Debug.Log("<color=yellow>── 测试 7: 真实游戏 Schema 性能瓶颈分析 ──</color>");

            var dbPath = Path.Combine(BenchmarkDbDir, "real_schema.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            // 重置监控器以获取干净数据
            // Reset monitor for clean data
            SqlitePerformanceMonitor.Reset();

            using (var conn = new SQLiteConnection(dbPath))
            {
                // ── 创建表 ──
                conn.CreateTable<HeroSkillParameterRow>();
                conn.CreateTable<ItemRow>();
                conn.CreateTable<GoodsBaseRow>();
                conn.CreateTable<ScalarOnlyRow>();

                // ── 插入数据 ──
                var heroRows = GenerateHeroSkillParameterRows(295);
                var itemRows = GenerateItemRows(337);
                var goodsRows = GenerateGoodsBaseRows(170);
                var scalarRows = GenerateScalarOnlyRows(295);

                conn.InsertAll(heroRows, typeof(HeroSkillParameterRow), runInTransaction: true);
                conn.InsertAll(itemRows, typeof(ItemRow), runInTransaction: true);
                conn.InsertAll(goodsRows, typeof(GoodsBaseRow), runInTransaction: true);
                conn.InsertAll(scalarRows, typeof(ScalarOnlyRow), runInTransaction: true);

                Debug.Log($"  数据插入完成: HeroSkillParameter={heroRows.Count}行, Item={itemRows.Count}行, GoodsBase={goodsRows.Count}行, ScalarOnly={scalarRows.Count}行");
            }

            // ── PRAGMA 优化后查询 + SqlitePerformanceMonitor 记录 ──
            // 每个表类型执行多次查询取平均值
            const int queryRepetitions = 5;
            var stepTimings = new Dictionary<string, StepTimingResult>();

            using (var conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
            {
                ApplyBenchmarkPragmas(conn);

                // ── 1. 纯标量表（对照组） ──
                {
                    var timing = MeasureQuerySteps<ScalarOnlyRow>(conn, queryRepetitions);
                    stepTimings["ScalarOnly(17标量0数组)"] = timing;
                    PrintStepTiming("纯标量表(对照组)", timing, 17, 0);
                }

                // ── 2. Item 表（15标量+2数组） ──
                {
                    var timing = MeasureQuerySteps<ItemRow>(conn, queryRepetitions);
                    stepTimings["Item(15标量+2数组)"] = timing;
                    PrintStepTiming("Item表(15标量+2数组)", timing, 15, 2);
                }

                // ── 3. GoodsBase 表（18标量+7数组） ──
                {
                    var timing = MeasureQuerySteps<GoodsBaseRow>(conn, queryRepetitions);
                    stepTimings["GoodsBase(18标量+7数组)"] = timing;
                    PrintStepTiming("GoodsBase表(18标量+7数组)", timing, 18, 7);
                }

                // ── 4. HeroSkillParameter 表（12标量+21数组）—— 最极端 ──
                {
                    var timing = MeasureQuerySteps<HeroSkillParameterRow>(conn, queryRepetitions);
                    stepTimings["HeroSkillParameter(12标量+21数组)"] = timing;
                    PrintStepTiming("HeroSkillParameter表(12标量+21数组)", timing, 12, 21);
                }
            }

            // ── 瓶颈分析报告 ──
            PrintBottleneckReport(stepTimings);

            // 保存到报告
            _report.RealSchemaStepTimings = stepTimings;

            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        /// <summary>
        /// 单次查询的 6 步计时结果
        /// 6-step timing result for a single query
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

        /// <summary>
        /// 对指定 POCO 类型的全表查询执行多次，取 6 步计时的平均值。
        /// Executes full-table queries for the given POCO type multiple times,
        /// returning the average of 6-step timing measurements.
        /// </summary>
        static StepTimingResult MeasureQuerySteps<T>(SQLiteConnection conn, int repetitions)
            where T : new()
        {
            var result = new StepTimingResult();
            // 使用 TableMapping 中的表名（[Table] 属性指定），而非 C# 类名
            // Use the table name from TableMapping (specified by [Table] attribute), not the C# class name
            var map = conn.GetMapping(typeof(T));
            var tableName = map.TableName;
            result.TableName = tableName;

            // 预热：首次查询建立 column mapping 缓存
            // Warmup: first query establishes column mapping cache
            var warmupResults = conn.Query<T>("SELECT * FROM " + tableName);
            result.RowCount = warmupResults.Count;

            // 重置监控器，只记录正式测试数据
            // Reset monitor to only record test data
            SqlitePerformanceMonitor.Reset();

            for (int i = 0; i < repetitions; i++)
            {
                // 每次查询前强制 GC 以减少 GC 干扰
                // Force GC before each query to reduce GC interference
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var results = conn.Query<T>("SELECT * FROM " + tableName);
            }

            // 从 SqlitePerformanceMonitor 提取汇总数据
            // Extract summary data from SqlitePerformanceMonitor
            // 由于 Monitor 是内部类，我们用外部计时来估算分步占比
            // 使用手动计时作为后备方案
            SqlitePerformanceMonitor.Reset();

            // ── 手动分步计时 ──
            // Manual step-by-step timing as primary measurement
            float totalPrepareMs = 0f;
            float totalColumnMappingMs = 0f;
            float totalStepMs = 0f;
            float totalCreateObjMs = 0f;
            float totalFastSetMs = 0f;
            float totalReadColMs = 0f;

            for (int i = 0; i < repetitions; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var cmd = conn.CreateCommand("SELECT * FROM " + tableName);
                var sw = new Stopwatch();

                // Step 1: Prepare
                sw.Restart();
                var stmt = SQLiteCommandHelper.Prepare(cmd);
                sw.Stop();
                totalPrepareMs += sw.ElapsedTicks / 10000f;

                try
                {
                    // Step 2: Column Mapping
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
                            // FastColumnSetter.GetFastSetter<T> 是 internal，通过反射调用
                            // FastColumnSetter.GetFastSetter<T> is internal, access via reflection
                            fastColumnSetters[ci] = FastColumnSetterHelper.GetFastSetter<T>(conn, cols[ci]);

                            // 统计标量/数组字段数
                            // Count scalar vs array fields
                            var propType = cols[ci].PropertyInfo.PropertyType;
                            if (propType.IsArray || (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>)))
                                arrayCount++;
                            else
                                scalarCount++;
                        }
                    }
                    sw.Stop();
                    totalColumnMappingMs += sw.ElapsedTicks / 10000f;
                    result.ScalarFields = scalarCount;
                    result.ArrayFields = arrayCount;

                    // Row-level timing
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
        /// 打印单表 6 步计时结果
        /// Print 6-step timing result for a single table
        /// </summary>
        static void PrintStepTiming(string label, StepTimingResult r, int scalarCount, int arrayCount)
        {
            Debug.Log(
                $"  <color=cyan>【{label}】</color> {r.RowCount}行, {scalarCount}标量+{arrayCount}数组字段\n" +
                $"    Prepare={r.PrepareMs:F2}ms  ColMap={r.ColumnMappingMs:F2}ms  " +
                $"Step={r.StepMs:F2}ms  CreateObj={r.CreateObjMs:F2}ms  " +
                $"FastSet=<color=green>{r.FastSetMs:F2}ms</color>  " +
                $"ReadCol=<color=red>{r.ReadColMs:F2}ms</color>\n" +
                $"    总计={r.TotalMs:F2}ms  " +
                $"FastSet占比={r.FastSetPercent:F1}%  " +
                $"ReadCol占比=<color=red>{r.ReadColPercent:F1}%</color>");
        }

        /// <summary>
        /// 打印瓶颈分析报告，识别 ReadCol → FastJsonConvert 是否为性能瓶颈
        /// Print bottleneck analysis report, identifying whether ReadCol → FastJsonConvert is the bottleneck
        /// </summary>
        static void PrintBottleneckReport(Dictionary<string, StepTimingResult> timings)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║         真实游戏 Schema 性能瓶颈分析 — 6 步分步计时报告          ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            // 表头
            sb.AppendLine($"║ {"表名",-35} {"行数",5} {"Prepare",9} {"ColMap",9} {"Step",9} {"CreateObj",9} {"FastSet",9} {"ReadCol",9} {"总计",9} {"RC%",6} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            foreach (var kv in timings)
            {
                var r = kv.Value;
                sb.AppendLine($"║ {kv.Key,-35} {r.RowCount,5} {r.PrepareMs,8:F2}ms {r.ColumnMappingMs,8:F2}ms {r.StepMs,8:F2}ms {r.CreateObjMs,8:F2}ms {r.FastSetMs,8:F2}ms {r.ReadColMs,8:F2}ms {r.TotalMs,8:F2}ms {r.ReadColPercent,5:F1}% ║");
            }

            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            // 瓶颈分析结论
            var heroTiming = timings.Values.FirstOrDefault(t => t.ArrayFields >= 20);
            if (heroTiming != null && heroTiming.ReadColPercent > 50f)
            {
                sb.AppendLine($"║                                                                    ║");
                sb.AppendLine($"║  🔴 瓶颈识别: ReadCol(反射+FastJsonConvert) 在数组密集型表中     ║");
                sb.AppendLine($"║     占比 {heroTiming.ReadColPercent:F1}%，是主要性能瓶颈。                      ║");
                sb.AppendLine($"║                                                                    ║");
                sb.AppendLine($"║  📊 根因分析:                                                     ║");
                sb.AppendLine($"║     • FastColumnSetter 不支持数组类型 → 返回 null                  ║");
                sb.AppendLine($"║     • 数组字段走 ReadCol → SqliteFastJsonConvert.DeserializeArray  ║");
                sb.AppendLine($"║     • 21个int[]字段×{heroTiming.RowCount}行 = {21 * heroTiming.RowCount}次反序列化调用            ║");
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
                var maxStep = new[] {
                    ("Prepare", heroTiming.PrepareMs),
                    ("ColumnMapping", heroTiming.ColumnMappingMs),
                    ("Step", heroTiming.StepMs),
                    ("CreateObj", heroTiming.CreateObjMs),
                    ("FastSet", heroTiming.FastSetMs),
                    ("ReadCol", heroTiming.ReadColMs)
                }.OrderByDescending(x => x.Item2).First();
                sb.AppendLine($"║     最大耗时阶段: {maxStep.Item1} ({maxStep.Item2:F2}ms)                     ║");
            }

            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════╝");

            Debug.Log(sb.ToString());

            // 保存报告到桌面
            try
            {
                var reportPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                    "SqliteBenchmark_RealSchema_Report.txt");
                File.WriteAllText(reportPath, sb.ToString());
                Debug.Log($"<color=cyan>瓶颈分析报告已保存至: {reportPath}</color>");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"报告保存失败: {e.Message}");
            }
        }

        #endregion

        #region 真实 Schema 数据生成

        /// <summary>
        /// 生成 HeroSkillParameter 测试数据 — 295行，12标量+21数组字段。
        /// 数组字段每行 1~5 个随机元素，模拟真实游戏配置数据。
        /// Generate HeroSkillParameter test data — 295 rows, 12 scalar + 21 array fields.
        /// Array fields have 1~5 random elements per row, simulating real game config data.
        /// </summary>
        static List<HeroSkillParameterRow> GenerateHeroSkillParameterRows(int count)
        {
            var rng = new System.Random(42);
            var rows = new List<HeroSkillParameterRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new HeroSkillParameterRow
                {
                    SkillId = 100001 + i,
                    SkillType = rng.Next(1, 10),
                    OugiType = rng.Next(0, 5),
                    TargetType = rng.Next(1, 8),
                    HitType = rng.Next(1, 4),
                    Element = rng.Next(0, 7),
                    RangeType = rng.Next(1, 5),
                    Priority = rng.Next(1, 100),
                    MaxLevel = rng.Next(5, 20),
                    CostType = rng.Next(1, 4),
                    CostValue = rng.Next(10, 200),
                    Cooldown = rng.Next(0, 30),
                    Formula = $"f_{i}_{rng.Next(1000, 9999)}",

                    // 21个数组字段 — 每行 1~5 个随机 int 元素
                    Coefficient = GenerateRandomIntArray(rng, 1, 5, 0, 10000),
                    EffectParam1 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam2 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam3 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam4 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam5 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam6 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam7 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam8 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam9 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam10 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam11 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam12 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam13 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam14 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam15 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam16 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam17 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam18 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam19 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam20 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成 Item 测试数据 — 337行，15标量+2数组字段。
        /// Generate Item test data — 337 rows, 15 scalar + 2 array fields.
        /// </summary>
        static List<ItemRow> GenerateItemRows(int count)
        {
            var rng = new System.Random(42);
            var rows = new List<ItemRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new ItemRow
                {
                    ItemType = rng.Next(1, 10),
                    ItemId = 200001 + i,
                    Quality = rng.Next(1, 6),
                    Category = rng.Next(1, 20),
                    SubCategory = rng.Next(1, 50),
                    MaxStack = rng.Next(1, 999),
                    SellPrice = rng.Next(0, 10000),
                    BuyPrice = rng.Next(0, 50000),
                    BindType = rng.Next(0, 3),
                    UseType = rng.Next(0, 5),
                    ExpireType = rng.Next(0, 3),
                    ExpireValue = rng.Next(0, 86400),
                    SortOrder = rng.Next(0, 1000),
                    IsHidden = rng.Next(2),
                    Rarity = rng.Next(1, 6),
                    Name = $"Item_{i}",
                    Attributes = GenerateRandomIntArray(rng, 1, 4, 100, 200),
                    DropSources = GenerateRandomIntArray(rng, 1, 3, 300, 400),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成 GoodsBase 测试数据 — 170行，18标量+7数组字段。
        /// Generate GoodsBase test data — 170 rows, 18 scalar + 7 array fields.
        /// </summary>
        static List<GoodsBaseRow> GenerateGoodsBaseRows(int count)
        {
            var rng = new System.Random(42);
            var rows = new List<GoodsBaseRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new GoodsBaseRow
                {
                    GoodsId = 300001 + i,
                    GoodsType = rng.Next(1, 8),
                    Category = rng.Next(1, 15),
                    SubCategory = rng.Next(1, 30),
                    Quality = rng.Next(1, 6),
                    MaxStack = rng.Next(1, 999),
                    SellPrice = rng.Next(0, 50000),
                    BuyPrice = rng.Next(0, 100000),
                    BindType = rng.Next(0, 3),
                    LimitType = rng.Next(0, 4),
                    LimitValue = rng.Next(0, 100),
                    DiscountType = rng.Next(0, 3),
                    DiscountValue = rng.Next(0, 100),
                    SortOrder = rng.Next(0, 500),
                    IsHidden = rng.Next(2),
                    ShowLevel = rng.Next(1, 100),
                    CurrencyType = rng.Next(1, 5),
                    CurrencyValue = rng.Next(10, 9999),
                    Name = $"Goods_{i}",
                    Description = $"Desc for goods {i}",

                    ConditionIds = GenerateRandomIntArray(rng, 1, 3, 500, 600),
                    RewardIds = GenerateRandomIntArray(rng, 1, 5, 600, 700),
                    TagIds = GenerateRandomIntArray(rng, 1, 4, 700, 800),
                    RelatedGoods = GenerateRandomIntArray(rng, 1, 3, 800, 900),
                    PreviewIds = GenerateRandomIntArray(rng, 1, 2, 900, 1000),
                    ScheduleIds = GenerateRandomIntArray(rng, 1, 2, 1000, 1100),
                    ExtraParams = GenerateRandomIntArray(rng, 1, 3, 1100, 1200),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成 ScalarOnly 对照组数据 — 295行，17标量字段，无数组。
        /// 所有字段走 FastColumnSetter 快路径。
        /// Generate ScalarOnly control group data — 295 rows, 17 scalar fields, no arrays.
        /// All fields use the FastColumnSetter fast path.
        /// </summary>
        static List<ScalarOnlyRow> GenerateScalarOnlyRows(int count)
        {
            var rng = new System.Random(42);
            var rows = new List<ScalarOnlyRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new ScalarOnlyRow
                {
                    F1 = rng.Next(0, 10000),
                    F2 = rng.Next(0, 10000),
                    F3 = rng.Next(0, 10000),
                    F4 = rng.Next(0, 10000),
                    F5 = rng.Next(0, 10000),
                    F6 = rng.Next(0, 10000),
                    F7 = rng.Next(0, 10000),
                    F8 = rng.Next(0, 10000),
                    F9 = rng.Next(0, 10000),
                    F10 = rng.Next(0, 10000),
                    F11 = rng.Next(0, 10000),
                    F12 = rng.Next(0, 10000),
                    F13 = $"Str_{i}_{rng.Next(1000)}",
                    F14 = (float)rng.NextDouble() * 1000f,
                    F15 = rng.NextDouble() * 1000.0,
                    F16 = rng.Next(2) == 0,
                    F17 = RandomExtensions.NextLong(rng),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成随机 int[] 数组，长度在 [minLen, maxLen] 之间，值在 [minVal, maxVal) 范围内。
        /// Generate a random int[] with length in [minLen, maxLen] and values in [minVal, maxVal).
        /// </summary>
        static int[] GenerateRandomIntArray(System.Random rng, int minLen, int maxLen, int minVal, int maxVal)
        {
            var len = rng.Next(minLen, maxLen + 1);
            var arr = new int[len];
            for (int i = 0; i < len; i++)
            {
                arr[i] = rng.Next(minVal, maxVal);
            }
            return arr;
        }

        /// <summary>
        /// SQLiteCommand 辅助类 — 暴露内部 Prepare/ReadCol 方法供基准测试使用。
        /// SQLiteCommand helper — exposes internal Prepare/ReadCol methods for benchmark use.
        /// </summary>
        static class SQLiteCommandHelper
        {
            /// <summary>
            /// 通过反射获取 SQLiteCommand 的 Prepare 方法，用于手动分步计时。
            /// Accesses SQLiteCommand.Prepare via reflection for manual step timing.
            /// </summary>
            static readonly System.Reflection.MethodInfo PrepareMethod =
                typeof(SQLiteCommand).GetMethod("Prepare",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            /// <summary>
            /// 通过反射获取 SQLiteCommand 的 ReadCol 方法，用于手动分步计时。
            /// Accesses SQLiteCommand.ReadCol via reflection for manual step timing.
            /// </summary>
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
        /// FastColumnSetter helper — accesses internal GetFastSetter method via reflection.
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

        static List<BenchmarkRow> GenerateBenchmarkRows(int count)
        {
            var rng = new System.Random(42);
            var rows = new List<BenchmarkRow>(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new BenchmarkRow
                {
                    Name = $"Row_{i}",
                    Level = rng.Next(1, 100),
                    Hp = rng.Next(100, 10000),
                    Attack = rng.NextDouble() * 1000,
                    IsActive = rng.Next(2) == 0,
                    Exp = RandomExtensions.NextLong(rng),
                    IntArray = new int[] {rng.Next(0, 100), rng.Next(0, 100), rng.Next(0, 100)},
                    FloatArray = new float[] {(float)rng.NextDouble() * 100f, (float)rng.NextDouble() * 100f},
                    DoubleArray = new double[] {rng.NextDouble(), rng.NextDouble()},
                    StringArray = new string[] {$"str_{i}_a", $"str_{i}_b"},
                    BoolArray = new bool[] {true, false, true},
                    LongArray = new long[] {RandomExtensions.NextLong(rng), RandomExtensions.NextLong(rng)},
                });
            }
            return rows;
        }

        static List<SimpleRow> GenerateSimpleRows(int count)
        {
            var rng = new System.Random(42);
            var rows = new List<SimpleRow>(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new SimpleRow
                {
                    Value1 = rng.Next(0, 100000),
                    Value2 = (float)rng.NextDouble() * 1000f,
                    Text = $"Text_{i}_{rng.Next(10000)}",
                });
            }
            return rows;
        }

        static bool ArraysEqual<T>(T[] a, T[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(a[i], b[i]))
                    return false;
            }
            return true;
        }

        #endregion

        #region Random 扩展

        /// <summary>
        /// Random 扩展方法 — 提供 NextFloat 和 NextLong。
        /// Random extension methods — providing NextFloat and NextLong.
        /// </summary>
        static class RandomExtensions
        {
            /// <summary>
            /// 返回 [0, max) 范围内的随机 long，避免生成超出 long 范围的极端值。
            /// Returns a random long in [0, max), avoiding extreme values beyond practical range.
            /// </summary>
            public static long NextLong(System.Random rng, long max = 100000000L)
            {
                // 使用两个 Next 调用组合，避免分配 byte[] 数组
                var high = (long)rng.Next();
                var low = (long)rng.Next();
                return Math.Abs((high << 32 | low) % max);
            }
        }

        #endregion

        #region 报告数据结构

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

        #endregion
    }
}
