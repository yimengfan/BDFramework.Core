using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BDFramework.Test.SqliteBenchmark
{
    /// <summary>
    /// 基准测试报告数据结构 — 存储所有测试结果。
    /// Benchmark report data structure — stores all test results.
    /// </summary>
    public class SqliteBenchmarkReport
    {
        // FastJsonConvert 正确性 / FastJsonConvert correctness
        public int FastJsonCorrectnessPass;
        public int FastJsonCorrectnessFail;
        public int KnownBugs;

        // 批量导入 / Batch import
        public long InsertRowByRowMs;
        public int InsertRowByRowGC;
        public float InsertRowByRowMemKB;
        public long InsertBatchMs;
        public int InsertBatchGC;
        public float InsertBatchMemKB;
        public float InsertSpeedup;

        // PRAGMA 优化 / PRAGMA optimization
        public long QueryDefaultMs;
        public int QueryDefaultGC;
        public long QueryPragmaMs;
        public int QueryPragmaGC;
        public float QueryPragmaSpeedup;
        public long QueryFullTableDefaultMs;
        public int QueryFullTableDefaultRows;
        public long QueryFullTablePragmaMs;

        // Prepared Statement 缓存 / Prepared Statement cache
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

        // 真实数据模拟 / Real data simulation
        public int RealTableCount;
        public long RealDataInsertMs;
        public long RealDataQueryMs;
        public int RealDataQueryRows;
        public long RealDataFullScanMs;

        // 真实 Schema 瓶颈分析 / Real Schema bottleneck analysis
        public Dictionary<string, StepTimingResult> RealSchemaStepTimings;

        /// <summary>
        /// 输出格式化的汇总报告到日志。
        /// Output formatted summary report to log.
        /// </summary>
        public string FormatReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║            SQLite 优化性能基准测试 — 结果报告              ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine($"║  FastJsonConvert 正确性: {FastJsonCorrectnessPass}通过 / {FastJsonCorrectnessFail}失败 / {KnownBugs}已知Bug".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── 批量导入性能 ──".PadRight(60) + "║");
            sb.AppendLine($"║  逐行Insert:   {InsertRowByRowMs}ms  GC:{InsertRowByRowGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  批量InsertAll: {InsertBatchMs}ms  GC:{InsertBatchGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {InsertSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── PRAGMA 只读优化 ──".PadRight(60) + "║");
            sb.AppendLine($"║  默认配置查询: {QueryDefaultMs}ms  GC:{QueryDefaultGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  PRAGMA优化查询: {QueryPragmaMs}ms  GC:{QueryPragmaGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {QueryPragmaSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine($"║  全表扫描(默认): {QueryFullTableDefaultMs}ms ({QueryFullTableDefaultRows}行)".PadRight(60) + "║");
            sb.AppendLine($"║  全表扫描(PRAGMA): {QueryFullTablePragmaMs}ms".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── Prepared Statement 缓存 ──".PadRight(60) + "║");
            sb.AppendLine($"║  无缓存查询: {QueryNoCacheMs}ms  GC:{QueryNoCacheGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  缓存查询: {QueryWithCacheMs}ms  GC:{QueryWithCacheGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  直接API缓存: {QueryDirectCacheMs}ms  GC:{QueryDirectCacheGC}次".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {PreparedStatementSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── FastJson Span vs string.Split ──".PadRight(60) + "║");
            sb.AppendLine($"║  Span解析: {FastJsonSpanMs}ms  GC:{FastJsonSpanGC}次  内存:{FastJsonSpanMemKB:F1}KB".PadRight(60) + "║");
            sb.AppendLine($"║  string.Split: {FastJsonLegacyMs}ms  GC:{FastJsonLegacyGC}次  内存:{FastJsonLegacyMemKB:F1}KB".PadRight(60) + "║");
            sb.AppendLine($"║  加速比: {FastJsonSpeedup:F2}x".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── 真实数据模拟 ──".PadRight(60) + "║");
            sb.AppendLine($"║  真实表数量: {RealTableCount}个xlsx".PadRight(60) + "║");
            sb.AppendLine($"║  大数据导入: {RealDataInsertMs}ms".PadRight(60) + "║");
            sb.AppendLine($"║  大数据条件查询: {RealDataQueryMs}ms ({RealDataQueryRows}行)".PadRight(60) + "║");
            sb.AppendLine($"║  大数据全表扫描: {RealDataFullScanMs}ms".PadRight(60) + "║");
            sb.AppendLine("║                                                            ║");
            sb.AppendLine("║  ── 真实 Schema 瓶颈分析 ──".PadRight(60) + "║");
            if (RealSchemaStepTimings != null && RealSchemaStepTimings.Count > 0)
            {
                foreach (var kv in RealSchemaStepTimings)
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
            return sb.ToString();
        }
    }

    /// <summary>
    /// 单次查询的 6 步计时结果。
    /// 6-step timing result for a single query.
    /// </summary>
    public class StepTimingResult
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
        /// ReadCol 占总耗时百分比。
        /// ReadCol as percentage of total time.
        /// </summary>
        public float ReadColPercent => TotalMs > 0 ? ReadColMs / TotalMs * 100f : 0f;

        /// <summary>
        /// FastSet 占总耗时百分比。
        /// FastSet as percentage of total time.
        /// </summary>
        public float FastSetPercent => TotalMs > 0 ? FastSetMs / TotalMs * 100f : 0f;

        /// <summary>
        /// 格式化为单行日志字符串。
        /// Format as a single-line log string.
        /// </summary>
        public string ToLogString(string label)
        {
            return $"【{label}】{RowCount}行, {ScalarFields}标量+{ArrayFields}数组字段\n" +
                   $"  Prepare={PrepareMs:F2}ms  ColMap={ColumnMappingMs:F2}ms  " +
                   $"Step={StepMs:F2}ms  CreateObj={CreateObjMs:F2}ms  " +
                   $"FastSet={FastSetMs:F2}ms  " +
                   $"ReadCol={ReadColMs:F2}ms\n" +
                   $"  总计={TotalMs:F2}ms  " +
                   $"FastSet占比={FastSetPercent:F1}%  " +
                   $"ReadCol占比={ReadColPercent:F1}%";
        }
    }
}
