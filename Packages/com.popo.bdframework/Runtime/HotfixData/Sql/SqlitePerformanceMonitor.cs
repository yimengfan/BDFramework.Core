using System;
using System.Collections.Generic;
using System.Diagnostics;
using BDFramework.Core.Tools;
using Cysharp.Text;
using UnityEngine;

namespace BDFramework.Sql
{
    /// <summary>
    /// SQLite 性能监控器。
    /// 提供 SQL 查询耗时统计、慢查询告警、GC 压力追踪和启动阶段全链路计时。
    /// 所有数据采集通过 ENABLE_BDEBUG 宏控制，生产包零开销。
    /// SQLite performance monitor.
    /// Provides SQL query timing statistics, slow-query alerts, GC pressure tracking,
    /// and full-pipeline timing for the startup phase.
    /// All data collection is gated by the ENABLE_BDEBUG macro — zero overhead in production builds.
    /// </summary>
    static public class SqlitePerformanceMonitor
    {
        static readonly string Tag = "SqlPerf";

        // ─── 配置 ───

        /// <summary>
        /// 慢查询阈值（毫秒）。超过此阈值的查询将被记录并告警。
        /// Slow-query threshold in milliseconds. Queries exceeding this will be logged and alerted.
        /// </summary>
        public static float SlowQueryThresholdMs = 10f;

        /// <summary>
        /// 是否启用监控。运行时可动态关闭以减少开销。
        /// Whether monitoring is enabled. Can be toggled at runtime to reduce overhead.
        /// </summary>
        public static bool IsEnabled = true;

        /// <summary>
        /// 是否在每次慢查询时立即打印详细日志。
        /// Setting to false batches stats into periodic summaries instead.
        /// Whether to print detailed logs immediately on each slow query.
        /// Setting to false batches stats into periodic summaries instead.
        /// </summary>
        public static bool VerboseLog = true;

        // ─── 统计数据 ───

        /// <summary>
        /// SQL 查询统计：key=SQL 文本, value=统计条目
        /// SQL query statistics: key=SQL text, value=stats entry
        /// </summary>
        private static readonly Dictionary<string, QueryStats> queryStatsMap =
            new Dictionary<string, QueryStats>();

        /// <summary>
        /// 所有查询的总耗时（毫秒）
        /// Total elapsed time across all queries (ms)
        /// </summary>
        private static float totalQueryTimeMs;

        /// <summary>
        /// 所有查询的总次数
        /// Total number of query invocations
        /// </summary>
        private static int totalQueryCount;

        /// <summary>
        /// 所有查询反序列化的总行数
        /// Total number of rows deserialized across all queries
        /// </summary>
        private static long totalRowsDeserialized;

        /// <summary>
        /// GC 发生次数（在 SQL 查询期间触发 GC 的计数）
        /// Number of GC collections that occurred during SQL queries
        /// </summary>
        private static int gcCountDuringQueries;

        // ─── 启动阶段计时 ───

        private static Stopwatch startupSw;
        private static long startupGc0Before;
        private static long startupGc1Before;
        private static long startupGc2Before;

        /// <summary>
        /// 开始记录 SQLite 启动阶段计时（从 Init 调用到首次查询完成）
        /// Begin recording SQLite startup phase timing (from Init call to first query completion)
        /// </summary>
        [Conditional("ENABLE_BDEBUG")]
        static public void BeginStartupPhase()
        {
            startupSw = Stopwatch.StartNew();
            startupGc0Before = GC.CollectionCount(0);
            startupGc1Before = GC.CollectionCount(1);
            startupGc2Before = GC.CollectionCount(2);
            BDebug.Log(Tag, "启动阶段计时开始", Color.cyan);
        }

        /// <summary>
        /// 结束启动阶段计时并输出报告
        /// End startup phase timing and output report
        /// </summary>
        [Conditional("ENABLE_BDEBUG")]
        static public void EndStartupPhase()
        {
            if (startupSw == null) return;
            startupSw.Stop();

            var totalMs = startupSw.ElapsedTicks / 10000f;
            var gc0Delta = GC.CollectionCount(0) - startupGc0Before;
            var gc1Delta = GC.CollectionCount(1) - startupGc1Before;
            var gc2Delta = GC.CollectionCount(2) - startupGc2Before;

            BDebug.Log(Tag,
                ZString.Format(
                    "启动阶段完成 — 总耗时: <color=yellow>{0:F2}ms</color>, " +
                    "查询次数: <color=yellow>{1}</color>, 查询总耗时: <color=yellow>{2:F2}ms</color>, " +
                    "反序列化行数: <color=yellow>{3}</color>, " +
                    "GC(Gen0/1/2): <color=red>{4}/{5}/{6}</color>",
                    totalMs, totalQueryCount, totalQueryTimeMs, totalRowsDeserialized,
                    gc0Delta, gc1Delta, gc2Delta),
                Color.green);

            // 打印 PRAGMA 设置确认
            BDebug.Log(Tag,
                ZString.Format(
                    "PRAGMA 状态: mmap_size={0}, cache_size={1}, page_size={2}",
                    currentMmapSize, currentCacheSize, currentPageSize),
                Color.cyan);
        }

        // ─── 查询计时 ───

        /// <summary>
        /// 记录一次 SQL 查询的耗时。
        /// Record the elapsed time of a single SQL query.
        /// </summary>
        /// <param name="sql">SQL 语句文本</param>
        /// <param name="searchTimeMs">SQL 执行耗时（毫秒）</param>
        /// <param name="deserializeTimeMs">反序列化耗时（毫秒）</param>
        /// <param name="rowCount">返回的行数</param>
        [Conditional("ENABLE_BDEBUG")]
        static public void RecordQuery(string sql, float searchTimeMs, float deserializeTimeMs, int rowCount)
        {
            RecordQuery(sql, searchTimeMs, deserializeTimeMs, rowCount,
                0f, 0f, 0f, 0f, 0f, 0f);
        }

        /// <summary>
        /// 记录一次 SQL 查询的耗时（含细粒度分步计时）。
        /// Record the elapsed time of a single SQL query with fine-grained step timing.
        /// </summary>
        /// <param name="sql">SQL 语句文本</param>
        /// <param name="searchTimeMs">SQL 执行耗时（毫秒，= prepare + columnMapping）</param>
        /// <param name="deserializeTimeMs">反序列化耗时（毫秒，= step + createObj + fastSet + readCol）</param>
        /// <param name="rowCount">返回的行数</param>
        /// <param name="prepareTimeMs">Prepare 阶段耗时（编译 SQL 或复用缓存语句 + 绑定参数）</param>
        /// <param name="columnMappingTimeMs">列映射阶段耗时（构建 fastColumnSetter 委托）</param>
        /// <param name="stepTimeMs">SQLite3.Step 总耗时（所有行的 Step 累加）</param>
        /// <param name="createObjTimeMs">对象实例化总耗时（Activator/ScriptLoder）</param>
        /// <param name="fastSetTimeMs">fastColumnSetter 赋值总耗时</param>
        /// <param name="readColTimeMs">ReadCol 反射赋值总耗时（含 FastJsonConvert）</param>
        [Conditional("ENABLE_BDEBUG")]
        static public void RecordQuery(string sql, float searchTimeMs, float deserializeTimeMs, int rowCount,
            float prepareTimeMs, float columnMappingTimeMs, float stepTimeMs,
            float createObjTimeMs, float fastSetTimeMs, float readColTimeMs)
        {
            if (!IsEnabled) return;

            var totalMs = searchTimeMs + deserializeTimeMs;
            totalQueryTimeMs += totalMs;
            totalQueryCount++;
            totalRowsDeserialized += rowCount;

            // 查询级统计
            QueryStats stats;
            if (!queryStatsMap.TryGetValue(sql, out stats))
            {
                stats = new QueryStats { Sql = sql };
                queryStatsMap[sql] = stats;
            }

            stats.HitCount++;
            stats.TotalTimeMs += totalMs;
            stats.TotalSearchTimeMs += searchTimeMs;
            stats.TotalDeserializeTimeMs += deserializeTimeMs;
            stats.TotalRows += rowCount;
            if (totalMs > stats.MaxTimeMs) stats.MaxTimeMs = totalMs;
            if (totalMs < stats.MinTimeMs || stats.MinTimeMs == 0) stats.MinTimeMs = totalMs;

            // 细粒度计时累加
            stats.TotalPrepareTimeMs += prepareTimeMs;
            stats.TotalColumnMappingTimeMs += columnMappingTimeMs;
            stats.TotalStepTimeMs += stepTimeMs;
            stats.TotalCreateObjTimeMs += createObjTimeMs;
            stats.TotalFastSetTimeMs += fastSetTimeMs;
            stats.TotalReadColTimeMs += readColTimeMs;

            // 慢查询告警（含分步耗时）
            if (totalMs > SlowQueryThresholdMs && VerboseLog)
            {
                var hasDetail = prepareTimeMs + columnMappingTimeMs + stepTimeMs + createObjTimeMs + fastSetTimeMs + readColTimeMs > 0;
                if (hasDetail)
                {
                    BDebug.LogError(Tag,
                        ZString.Format(
                            "慢查询告警 — 耗时: <color=yellow>{0:F2}ms</color>, 行数: <color=red>{1}</color>, " +
                            "累计执行: <color=yellow>{2}</color>次\n" +
                            "  分步计时: Prepare=<color=cyan>{3:F2}ms</color> " +
                            "ColumnMapping=<color=cyan>{4:F2}ms</color> " +
                            "Step=<color=cyan>{5:F2}ms</color> " +
                            "CreateObj=<color=cyan>{6:F2}ms</color> " +
                            "FastSet=<color=cyan>{7:F2}ms</color> " +
                            "ReadCol=<color=cyan>{8:F2}ms</color>\n" +
                            "SQL: {9}",
                            totalMs, rowCount, stats.HitCount,
                            prepareTimeMs, columnMappingTimeMs, stepTimeMs,
                            createObjTimeMs, fastSetTimeMs, readColTimeMs,
                            sql));
                }
                else
                {
                    BDebug.LogError(Tag,
                        ZString.Format(
                            "慢查询告警 — 耗时: <color=yellow>{0:F2}ms</color> (SQL:<color=yellow>{1:F2}ms</color> " +
                            "反序列化:<color=yellow>{2:F2}ms</color>), 行数: <color=red>{3}</color>, " +
                            "累计执行: <color=yellow>{4}</color>次\nSQL: {5}",
                            totalMs, searchTimeMs, deserializeTimeMs, rowCount,
                            stats.HitCount, sql));
                }
            }
        }

        /// <summary>
        /// 记录一次仅执行（无结果集）的 SQL 耗时，如 INSERT/UPDATE/DELETE。
        /// Record the elapsed time of a non-query SQL execution (INSERT/UPDATE/DELETE).
        /// </summary>
        [Conditional("ENABLE_BDEBUG")]
        static public void RecordExecute(string sql, float timeMs)
        {
            if (!IsEnabled) return;
            totalQueryTimeMs += timeMs;
            totalQueryCount++;

            QueryStats stats;
            if (!queryStatsMap.TryGetValue(sql, out stats))
            {
                stats = new QueryStats { Sql = sql };
                queryStatsMap[sql] = stats;
            }

            stats.HitCount++;
            stats.TotalTimeMs += timeMs;
            if (timeMs > stats.MaxTimeMs) stats.MaxTimeMs = timeMs;
        }

        // ─── PRAGMA 配置追踪 ───

        private static long currentMmapSize;
        private static int currentCacheSize;
        private static int currentPageSize;

        /// <summary>
        /// 记录当前 PRAGMA 配置值，用于启动报告。
        /// Record current PRAGMA configuration values for the startup report.
        /// </summary>
        [Conditional("ENABLE_BDEBUG")]
        static public void RecordPragmaConfig(long mmapSize, int cacheSize, int pageSize)
        {
            currentMmapSize = mmapSize;
            currentCacheSize = cacheSize;
            currentPageSize = pageSize;
        }

        // ─── 周期性报告 ───

        /// <summary>
        /// 输出当前所有 SQL 查询统计的汇总报告。
        /// Output a summary report of all current SQL query statistics.
        /// </summary>
        [Conditional("ENABLE_BDEBUG")]
        static public void PrintSummaryReport()
        {
            if (totalQueryCount == 0) return;

            var gc0 = GC.CollectionCount(0);
            var gc1 = GC.CollectionCount(1);
            var gc2 = GC.CollectionCount(2);

            BDebug.Log(Tag,
                ZString.Format(
                    "=== SQL 性能汇总 ===\n" +
                    "总查询次数: <color=yellow>{0}</color>, 总耗时: <color=yellow>{1:F2}ms</color>, " +
                    "总行数: <color=yellow>{2}</color>\n" +
                    "GC(Gen0/1/2): <color=red>{3}/{4}/{5}</color>",
                    totalQueryCount, totalQueryTimeMs, totalRowsDeserialized, gc0, gc1, gc2),
                Color.green);

            // 按总耗时排序，输出 Top N 慢查询
            var sorted = new List<QueryStats>(queryStatsMap.Values);
            sorted.Sort((a, b) => b.TotalTimeMs.CompareTo(a.TotalTimeMs));

            var topN = Math.Min(sorted.Count, 20);
            for (int i = 0; i < topN; i++)
            {
                var s = sorted[i];
                var avgMs = s.HitCount > 0 ? s.TotalTimeMs / s.HitCount : 0;
                var hasDetail = s.TotalPrepareTimeMs + s.TotalStepTimeMs + s.TotalFastSetTimeMs + s.TotalReadColTimeMs > 0;
                if (hasDetail)
                {
                    BDebug.Log(Tag,
                        ZString.Format(
                            "  #{0} — 累计耗时: <color=yellow>{1:F2}ms</color>, " +
                            "次数: <color=yellow>{2}</color>, 平均: <color=yellow>{3:F2}ms</color>, " +
                            "最大: <color=red>{4:F2}ms</color>, 行数: <color=yellow>{5}</color>\n" +
                            "    分步平均: Prepare=<color=cyan>{6:F2}ms</color> " +
                            "ColMap=<color=cyan>{7:F2}ms</color> " +
                            "Step=<color=cyan>{8:F2}ms</color> " +
                            "CreateObj=<color=cyan>{9:F2}ms</color> " +
                            "FastSet=<color=cyan>{10:F2}ms</color> " +
                            "ReadCol=<color=cyan>{11:F2}ms</color>\n" +
                            "  SQL: {12}",
                            i + 1, s.TotalTimeMs, s.HitCount, avgMs, s.MaxTimeMs, s.TotalRows,
                            s.TotalPrepareTimeMs / s.HitCount,
                            s.TotalColumnMappingTimeMs / s.HitCount,
                            s.TotalStepTimeMs / s.HitCount,
                            s.TotalCreateObjTimeMs / s.HitCount,
                            s.TotalFastSetTimeMs / s.HitCount,
                            s.TotalReadColTimeMs / s.HitCount,
                            s.Sql));
                }
                else
                {
                    BDebug.Log(Tag,
                        ZString.Format(
                            "  #{0} — 累计耗时: <color=yellow>{1:F2}ms</color>, " +
                            "次数: <color=yellow>{2}</color>, 平均: <color=yellow>{3:F2}ms</color>, " +
                            "最大: <color=red>{4:F2}ms</color>, 行数: <color=yellow>{5}</color>\n" +
                            "  SQL: {6}",
                            i + 1, s.TotalTimeMs, s.HitCount, avgMs, s.MaxTimeMs, s.TotalRows, s.Sql));
                }
            }
        }

        /// <summary>
        /// 清空所有统计数据。通常在场景切换或重新加载时调用。
        /// Clear all statistics data. Typically called during scene transitions or reloads.
        /// </summary>
        static public void Reset()
        {
            queryStatsMap.Clear();
            totalQueryTimeMs = 0;
            totalQueryCount = 0;
            totalRowsDeserialized = 0;
            gcCountDuringQueries = 0;
            startupSw = null;
        }

        // ─── 数据结构 ───

        /// <summary>
        /// 单条 SQL 的执行统计
        /// Execution statistics for a single SQL statement
        /// </summary>
        public class QueryStats
        {
            public string Sql;
            public int HitCount;
            public float TotalTimeMs;
            public float TotalSearchTimeMs;
            public float TotalDeserializeTimeMs;
            public float MaxTimeMs;
            public float MinTimeMs;
            public long TotalRows;

            // ─── 细粒度分步计时（累加值）───
            // Fine-grained step timing (accumulated values)
            public float TotalPrepareTimeMs;         // Prepare 阶段耗时
            public float TotalColumnMappingTimeMs;    // 列映射阶段耗时
            public float TotalStepTimeMs;            // SQLite3.Step 总耗时
            public float TotalCreateObjTimeMs;       // 对象实例化总耗时
            public float TotalFastSetTimeMs;         // fastColumnSetter 赋值总耗时
            public float TotalReadColTimeMs;         // ReadCol 反射赋值总耗时（含 FastJsonConvert）
        }

        // ─── GC 压力专用 ───

        /// <summary>
        /// 强制触发一次 GC 并记录前后状态，用于测试 GC 对查询性能的影响。
        /// Force a GC collection and record before/after state, for testing GC impact on query performance.
        /// </summary>
        [Conditional("ENABLE_BDEBUG")]
        static public void ForceGCAndReport()
        {
            var gc0Before = GC.CollectionCount(0);
            var memBefore = GC.GetTotalMemory(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var gc0After = GC.CollectionCount(0);
            var memAfter = GC.GetTotalMemory(true);

            BDebug.Log(Tag,
                ZString.Format(
                    "强制GC报告 — Gen0回收: <color=yellow>{0}</color>次, " +
                    "内存释放: <color=yellow>{1:F2}MB</color> → <color=yellow>{2:F2}MB</color> (减少<color=red>{3:F2}MB</color>)",
                    gc0After - gc0Before,
                    memBefore / 1048576f, memAfter / 1048576f,
                    (memBefore - memAfter) / 1048576f),
                Color.cyan);
        }
    }
}