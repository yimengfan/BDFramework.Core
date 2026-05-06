using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Text;
using UnityEngine;

/// <summary>
/// BDebug 性能剖析扩展 — 结构化分步计时、累加器、分组报告。
/// 与 BDebug 协同工作，提供比 LogWatchBegin/End 更丰富的性能分析能力。
/// 所有方法通过 ENABLE_BDEBUG 宏控制，生产包零开销。
/// BDebug performance profiling extension — structured step timing, accumulators, grouped reports.
/// Works alongside BDebug, providing richer performance analysis than LogWatchBegin/End.
/// All methods are gated by the ENABLE_BDEBUG macro — zero overhead in production builds.
/// </summary>
static public class BDebugPerformanceProfiler
{
    // ─── 配置 ───

    /// <summary>
    /// 是否启用性能剖析。运行时可动态关闭以减少开销。
    /// Whether performance profiling is enabled. Can be toggled at runtime to reduce overhead.
    /// </summary>
    static public bool IsEnabled = true;

    // ─── 分步计时（Step Timer）───

    /// <summary>
    /// 分步计时器 — 用于对同一个操作的多个阶段进行计时。
    /// 典型用法：查询流程中 Prepare → ColumnMapping → Step → CreateObj → FastSet → ReadCol。
    /// Step timer — for timing multiple phases of a single operation.
    /// Typical usage: Prepare → ColumnMapping → Step → CreateObj → FastSet → ReadCol in a query pipeline.
    /// </summary>
    private class StepTimer
    {
        public string GroupTag;
        public Stopwatch Stopwatch;
        public readonly List<StepRecord> Steps = new List<StepRecord>(8);
        public float AccumulatedMs; // 当前步骤已累积的毫秒数（用于累加模式）
    }

    /// <summary>
    /// 单步计时记录
    /// Single step timing record
    /// </summary>
    private struct StepRecord
    {
        public string StepName;
        public float TimeMs;
    }

    /// <summary>
    /// 活跃的分步计时器
    /// Active step timers
    /// </summary>
    static private readonly ConcurrentDictionary<string, StepTimer> stepTimers =
        new ConcurrentDictionary<string, StepTimer>();

    /// <summary>
    /// 开始一个分步计时会话。同一 tag 可多次 BeginStep/EndStep 循环。
    /// Begin a step timing session. The same tag can have multiple BeginStep/EndStep cycles.
    /// </summary>
    /// <param name="tag">计时器标签，需全局唯一</param>
    /// <param name="groupTag">分组标签，用于最终报告归类（可选）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void BeginStepTimer(string tag, string groupTag = "")
    {
        if (!IsEnabled) return;
        var timer = new StepTimer
        {
            GroupTag = groupTag,
            Stopwatch = Stopwatch.StartNew(),
        };
        stepTimers[tag] = timer;
    }

    /// <summary>
    /// 开始计时一个步骤。在 EndStep 之前调用。
    /// Start timing a step. Call before EndStep.
    /// </summary>
    /// <param name="tag">计时器标签</param>
    /// <param name="stepName">步骤名称（如 "Prepare", "Step", "ReadCol"）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void BeginStep(string tag, string stepName)
    {
        if (!IsEnabled) return;
        if (stepTimers.TryGetValue(tag, out var timer))
        {
            timer.AccumulatedMs = 0f;
            timer.Stopwatch.Restart();
        }
    }

    /// <summary>
    /// 结束当前步骤计时，记录该步骤的耗时。
    /// End the current step timing, recording the elapsed time for this step.
    /// </summary>
    /// <param name="tag">计时器标签</param>
    /// <param name="stepName">步骤名称（需与 BeginStep 匹配）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void EndStep(string tag, string stepName)
    {
        if (!IsEnabled) return;
        if (stepTimers.TryGetValue(tag, out var timer))
        {
            timer.Stopwatch.Stop();
            var ms = timer.Stopwatch.ElapsedTicks / 10000f;
            timer.Steps.Add(new StepRecord { StepName = stepName, TimeMs = ms });
        }
    }

    /// <summary>
    /// 将当前计时值累加到指定步骤（用于行级循环中多次累加同一步骤的耗时）。
    /// 用法：BeginStep → 操作 → AccumulateStep → 操作 → AccumulateStep → ... → EndStepTimer。
    /// Accumulate the current timing into the specified step (for row-level loops where the same
    /// step is measured multiple times).
    /// Usage: BeginStep → operation → AccumulateStep → operation → AccumulateStep → ... → EndStepTimer.
    /// </summary>
    /// <param name="tag">计时器标签</param>
    /// <param name="stepName">步骤名称</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void AccumulateStep(string tag, string stepName)
    {
        if (!IsEnabled) return;
        if (stepTimers.TryGetValue(tag, out var timer))
        {
            timer.Stopwatch.Stop();
            var ms = timer.Stopwatch.ElapsedTicks / 10000f;
            timer.AccumulatedMs += ms;

            // 查找是否已有同名步骤记录，有则累加
            // Find existing step record with the same name; if found, accumulate
            for (int i = timer.Steps.Count - 1; i >= 0; i--)
            {
                if (timer.Steps[i].StepName == stepName)
                {
                    timer.Steps[i] = new StepRecord { StepName = stepName, TimeMs = timer.Steps[i].TimeMs + ms };
                    timer.Stopwatch.Restart();
                    return;
                }
            }

            // 没有同名记录，新增
            // No existing record with this name, add new
            timer.Steps.Add(new StepRecord { StepName = stepName, TimeMs = ms });
            timer.Stopwatch.Restart();
        }
    }

    /// <summary>
    /// 结束分步计时会话，输出分步报告并释放资源。
    /// End the step timing session, output the step report, and release resources.
    /// </summary>
    /// <param name="tag">计时器标签</param>
    /// <param name="logToConsole">是否立即输出到控制台（默认 true）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void EndStepTimer(string tag, bool logToConsole = true)
    {
        if (!IsEnabled) return;
        if (stepTimers.TryRemove(tag, out var timer))
        {
            timer.Stopwatch.Stop();
            if (logToConsole && timer.Steps.Count > 0)
            {
                PrintStepReport(tag, timer);
            }
        }
    }

    /// <summary>
    /// 结束分步计时会话并返回分步数据（不输出到控制台），供调用方自行处理。
    /// End the step timing session and return step data (without console output) for caller processing.
    /// </summary>
    /// <param name="tag">计时器标签</param>
    /// <returns>分步计时结果列表；未找到时返回 null</returns>
    static public List<StepResult> EndStepTimerGetData(string tag)
    {
#if !ENABLE_BDEBUG
        return null;
#else
        if (!IsEnabled) return null;
        if (stepTimers.TryRemove(tag, out var timer))
        {
            timer.Stopwatch.Stop();
            var results = new List<StepResult>(timer.Steps.Count);
            foreach (var step in timer.Steps)
            {
                results.Add(new StepResult { StepName = step.StepName, TimeMs = step.TimeMs });
            }
            return results;
        }
        return null;
#endif
    }

    /// <summary>
    /// 分步计时结果（公开数据结构）
    /// Step timing result (public data structure)
    /// </summary>
    public struct StepResult
    {
        public string StepName;
        public float TimeMs;
    }

    /// <summary>
    /// 获取指定 tag 的分步数据快照（不结束计时器），供中间报告使用。
    /// Get a snapshot of step data for the specified tag (without ending the timer),
    /// for use in intermediate reports.
    /// </summary>
    /// <param name="tag">计时器标签</param>
    /// <returns>分步计时结果列表；未找到时返回 null</returns>
    static public List<StepResult> GetStepData(string tag)
    {
#if !ENABLE_BDEBUG
        return null;
#else
        if (!IsEnabled) return null;
        if (stepTimers.TryGetValue(tag, out var timer))
        {
            var results = new List<StepResult>(timer.Steps.Count);
            foreach (var step in timer.Steps)
            {
                results.Add(new StepResult { StepName = step.StepName, TimeMs = step.TimeMs });
            }
            return results;
        }
        return null;
#endif
    }

    /// <summary>
    /// 打印分步计时报告
    /// Print step timing report
    /// </summary>
    static private void PrintStepReport(string tag, StepTimer timer)
    {
        var totalMs = 0f;
        foreach (var step in timer.Steps)
        {
            totalMs += step.TimeMs;
        }

        var sb = ZString.CreateStringBuilder();
        sb.AppendFormat("<color=cyan>【{0}】</color> 分步计时报告 — 总耗时: <color=yellow>{1:F2}ms</color>\n", tag, totalMs);

        for (int i = 0; i < timer.Steps.Count; i++)
        {
            var step = timer.Steps[i];
            var percent = totalMs > 0 ? step.TimeMs / totalMs * 100f : 0f;
            var bar = new string('█', (int)(percent / 5f)); // 每 5% 一个方块
            sb.AppendFormat("  {0}. {1}=<color=yellow>{2:F2}ms</color> (<color=cyan>{3:F1}%</color>) {4}\n",
                i + 1, step.StepName, step.TimeMs, percent, bar);
        }

        BDebug.Log(timer.GroupTag ?? tag, sb.ToString(), Color.green);
    }

    // ─── 累加计时器（Accumulator）───

    /// <summary>
    /// 累加计时器 — 用于统计多次操作的耗时总和。
    /// 典型用法：在循环中多次调用 Accumulate，最后调用 ReportAccumulator 输出统计。
    /// Accumulator timer — for summing up elapsed time across multiple operations.
    /// Typical usage: call Accumulate multiple times in a loop, then call ReportAccumulator for stats.
    /// </summary>
    private class Accumulator
    {
        public string Tag;
        public float TotalMs;
        public int HitCount;
        public float MaxMs;
        public float MinMs = float.MaxValue;
    }

    static private readonly ConcurrentDictionary<string, Accumulator> accumulators =
        new ConcurrentDictionary<string, Accumulator>();

    /// <summary>
    /// 累加一次操作耗时到指定 tag 的累加器。
    /// Accumulate one operation's elapsed time to the specified tag's accumulator.
    /// </summary>
    /// <param name="tag">累加器标签</param>
    /// <param name="timeMs">本次操作耗时（毫秒）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void Accumulate(string tag, float timeMs)
    {
        if (!IsEnabled) return;
        var acc = accumulators.GetOrAdd(tag, _ => new Accumulator { Tag = tag });
        lock (acc)
        {
            acc.TotalMs += timeMs;
            acc.HitCount++;
            if (timeMs > acc.MaxMs) acc.MaxMs = timeMs;
            if (timeMs < acc.MinMs) acc.MinMs = timeMs;
        }
    }

    /// <summary>
    /// 输出指定 tag 的累加器统计报告，并可选择是否重置。
    /// Output the accumulator statistics report for the specified tag,
    /// optionally resetting the accumulator after reporting.
    /// </summary>
    /// <param name="tag">累加器标签</param>
    /// <param name="resetAfterReport">报告后是否重置累加器（默认 true）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void ReportAccumulator(string tag, bool resetAfterReport = true)
    {
        if (!IsEnabled) return;
        if (accumulators.TryRemove(tag, out var acc))
        {
            var avgMs = acc.HitCount > 0 ? acc.TotalMs / acc.HitCount : 0f;
            BDebug.Log(tag,
                ZString.Format(
                    "累加器报告 — 次数: <color=yellow>{0}</color>, 总耗时: <color=yellow>{1:F2}ms</color>, " +
                    "平均: <color=yellow>{2:F2}ms</color>, 最大: <color=red>{3:F2}ms</color>, 最小: <color=green>{4:F2}ms</color>",
                    acc.HitCount, acc.TotalMs, avgMs, acc.MaxMs, acc.MinMs),
                Color.green);

            if (!resetAfterReport)
            {
                // 放回字典
                accumulators[tag] = acc;
            }
        }
    }

    // ─── 分组报告（Grouped Report）───

    /// <summary>
    /// 输出一组分步计时结果的格式化报告，用于查询流水线的详细耗时展示。
    /// 典型用法：查询 200 个对象后，输出每个阶段的汇总耗时。
    /// Output a formatted report for a set of step timing results,
    /// for detailed timing display of query pipelines.
    /// Typical usage: after querying 200 objects, output the aggregated timing per phase.
    /// </summary>
    /// <param name="tag">报告标签</param>
    /// <param name="steps">分步计时结果</param>
    /// <param name="rowCount">查询行数</param>
    /// <param name="sql">SQL 语句（可选）</param>
    [Conditional("ENABLE_BDEBUG")]
    static public void PrintPipelineReport(string tag, List<StepResult> steps, int rowCount, string sql = "")
    {
        if (!IsEnabled || steps == null || steps.Count == 0) return;

        var totalMs = 0f;
        foreach (var step in steps)
        {
            totalMs += step.TimeMs;
        }

        var perRowMs = rowCount > 0 ? totalMs / rowCount : 0f;

        var sb = ZString.CreateStringBuilder();
        sb.AppendFormat("<color=cyan>【{0}】</color> 流水线耗时报告\n", tag);
        sb.AppendFormat("  行数: <color=yellow>{0}</color>, 总耗时: <color=yellow>{1:F2}ms</color>, 每行: <color=cyan>{2:F4}ms</color>\n",
            rowCount, totalMs, perRowMs);

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var percent = totalMs > 0 ? step.TimeMs / totalMs * 100f : 0f;
            var perRow = rowCount > 0 ? step.TimeMs / rowCount : 0f;
            sb.AppendFormat("  {0}. {1}=<color=yellow>{2:F2}ms</color> (<color=cyan>{3:F1}%</color>) 每行:<color=green>{4:F4}ms</color>\n",
                i + 1, step.StepName, step.TimeMs, percent, perRow);
        }

        if (!string.IsNullOrEmpty(sql))
        {
            sb.AppendFormat("  SQL: {0}\n", sql);
        }

        BDebug.Log(tag, sb.ToString(), Color.green);
    }

    // ─── 重置 ───

    /// <summary>
    /// 清空所有性能剖析数据。通常在场景切换或重新加载时调用。
    /// Clear all profiling data. Typically called during scene transitions or reloads.
    /// </summary>
    [Conditional("ENABLE_BDEBUG")]
    static public void Reset()
    {
        stepTimers.Clear();
        accumulators.Clear();
    }
}
