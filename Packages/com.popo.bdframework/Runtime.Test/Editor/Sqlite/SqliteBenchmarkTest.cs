using System;
using System.IO;
using System.Text;
using BDFramework.Test.SqliteBenchmark;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// SQLite 性能基准测试的 NUnit 包装 — 在 Unity Test Runner 中运行并断言关键指标。
    /// 测试逻辑委托给 Runtime 兼容的 SqliteBenchmarkRunner，本类仅提供 NUnit 集成和断言门禁。
    /// 常规情况下通过 Unity Test Runner 执行；BatchMode 下可通过 RunBatchVerification() 显式验证。
    ///
    /// NUnit wrapper for SQLite performance benchmarks — runs in Unity Test Runner with key metric assertions.
    /// Test logic is delegated to the Runtime-compatible SqliteBenchmarkRunner; this class provides NUnit integration
    /// and assertion gates only. Normally runs via Unity Test Runner; BatchMode via RunBatchVerification().
    /// </summary>
    [TestFixture]
    public class SqliteBenchmarkTest
    {
        /// <summary>
        /// 基准测试报告 — 所有测试方法共享，由 RunAllBenchmarkTests 一次性填充。
        /// Benchmark report — shared across test methods, populated once by RunAllBenchmarkTests.
        /// </summary>
        private static SqliteBenchmarkReport _report;

        /// <summary>
        /// 报告输出路径。
        /// Report output path.
        /// </summary>
        private static string _reportPath;

        /// <summary>
        /// BatchMode 显式验证入口。
        /// 当项目级初始化会干扰 Unity 原生 -runTests 流程时，可通过 -executeMethod 直接调用。
        /// BatchMode explicit verification entry.
        /// When project-level initialization interferes with Unity's native -runTests, call via -executeMethod.
        /// </summary>
        public static void RunBatchVerification()
        {
            SqliteBenchmarkBatchVerification.RunBatchVerification();
        }

        [SetUp]
        public void LogCurrentTestPurpose()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} " +
                      "测试目的=验证 SQLite 性能基准测试关键指标符合预期门禁。 " +
                      "实现手段=委托 SqliteBenchmarkRunner.RunAll 并断言报告关键字段。");
        }

        /// <summary>
        /// 运行全部基准测试（一次性），生成报告并保存到文件。
        /// Run all benchmark tests (once), generate report and save to file.
        /// </summary>
        [OneTimeSetUp]
        public void RunAllBenchmarkTests()
        {
            Debug.Log("[SqliteBenchmarkTest] 开始运行全部基准测试...");
            _report = SqliteBenchmarkRunner.RunAll();
            _reportPath = Path.Combine(Application.persistentDataPath,
                $"SqliteBenchmark_NUnit_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            _report.SaveToFile(_reportPath);
            Debug.Log($"[SqliteBenchmarkTest] 基准测试完成，报告已写入: {_reportPath}");
        }

        /// <summary>
        /// 验证报告对象非空 — 确保基准测试成功执行并返回了结果。
        /// Verify report object is not null — ensures benchmark ran successfully and returned results.
        /// </summary>
        [Test]
        public void Report_IsNotNull()
        {
            Assert.IsNotNull(_report, "基准测试报告不应为空，RunAll 应返回有效报告。");
        }

        /// <summary>
        /// 验证 FastJsonConvert 正确性测试无失败 — 所有数组类型序列化/反序列化应正确。
        /// Verify FastJsonConvert correctness tests have no failures — all array type serialization/deserialization should be correct.
        /// </summary>
        [Test]
        public void FastJsonConvert_NoCorrectnessFailures()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.AreEqual(0, _report.FastJsonCorrectnessFail,
                $"FastJsonConvert 正确性测试有 {_report.FastJsonCorrectnessFail} 个失败，预期 0。");
        }

        /// <summary>
        /// 验证 FastJsonConvert 至少有一个正确性测试通过 — 确认测试确实执行了。
        /// Verify at least one FastJsonConvert correctness test passed — confirms tests actually ran.
        /// </summary>
        [Test]
        public void FastJsonConvert_AtLeastOnePass()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.Greater(_report.FastJsonCorrectnessPass, 0,
                "FastJsonConvert 正确性测试应至少有 1 个通过，确认测试已执行。");
        }

        /// <summary>
        /// 验证批量 InsertAll 比逐行 Insert 有加速效果（加速比 > 1.0x）。
        /// Verify batch InsertAll is faster than row-by-row Insert (speedup > 1.0x).
        /// </summary>
        [Test]
        public void InsertBatch_IsFasterThanRowByRow()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.Greater(_report.InsertSpeedup, 1.0f,
                $"批量 Insert 加速比 {_report.InsertSpeedup:F2}x 应大于 1.0x。" +
                $"逐行={_report.InsertRowByRowMs}ms, 批量={_report.InsertBatchMs}ms");
        }

        /// <summary>
        /// 验证 PRAGMA 优化查询比默认配置有加速效果（加速比 > 1.0x）。
        /// Verify PRAGMA-optimized query is faster than default config (speedup > 1.0x).
        /// </summary>
        [Test]
        public void PragmaOptimization_IsFasterThanDefault()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.Greater(_report.QueryPragmaSpeedup, 1.0f,
                $"PRAGMA 优化加速比 {_report.QueryPragmaSpeedup:F2}x 应大于 1.0x。" +
                $"默认={_report.QueryDefaultMs}ms, PRAGMA={_report.QueryPragmaMs}ms");
        }

        /// <summary>
        /// 验证 Prepared Statement 缓存查询比无缓存查询有加速效果（加速比 > 1.0x）。
        /// Verify Prepared Statement cached query is faster than no-cache query (speedup > 1.0x).
        /// </summary>
        [Test]
        public void PreparedStatementCache_IsFasterThanNoCache()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.Greater(_report.PreparedStatementSpeedup, 1.0f,
                $"Prepared Statement 缓存加速比 {_report.PreparedStatementSpeedup:F2}x 应大于 1.0x。" +
                $"无缓存={_report.QueryNoCacheMs}ms, 缓存={_report.QueryWithCacheMs}ms");
        }

        /// <summary>
        /// 验证 FastJson Span 解析比传统 string.Split 有加速效果（加速比 > 1.0x）。
        /// Verify FastJson Span parsing is faster than traditional string.Split (speedup > 1.0x).
        /// </summary>
        [Test]
        public void FastJsonSpan_IsFasterThanStringSplit()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.Greater(_report.FastJsonSpeedup, 1.0f,
                $"FastJson Span 加速比 {_report.FastJsonSpeedup:F2}x 应大于 1.0x。" +
                $"Span={_report.FastJsonSpanMs}ms, string.Split={_report.FastJsonLegacyMs}ms");
        }

        /// <summary>
        /// 验证真实 Schema 瓶颈分析结果非空 — 至少有一组 StepTimingResult。
        /// Verify real Schema bottleneck analysis results are not empty — at least one StepTimingResult.
        /// </summary>
        [Test]
        public void RealSchemaBottleneck_HasResults()
        {
            Assert.IsNotNull(_report, "报告为空，无法断言。");
            Assert.IsNotNull(_report.RealSchemaStepTimings, "真实 Schema 瓶颈分析结果不应为空。");
            Assert.Greater(_report.RealSchemaStepTimings.Count, 0,
                "真实 Schema 瓶颈分析应至少有 1 组计时结果。");
        }

        /// <summary>
        /// 验证报告文件已成功写入磁盘。
        /// Verify report file was successfully written to disk.
        /// </summary>
        [Test]
        public void ReportFile_ExistsOnDisk()
        {
            Assert.IsNotNull(_reportPath, "报告路径不应为空。");
            Assert.IsTrue(File.Exists(_reportPath),
                $"报告文件应存在于磁盘: {_reportPath}");
        }

        /// <summary>
        /// 清理：输出报告摘要。
        /// Cleanup: output report summary.
        /// </summary>
        [OneTimeTearDown]
        public void TearDown()
        {
            if (_report != null)
            {
                Debug.Log($"[SqliteBenchmarkTest] 报告摘要: " +
                          $"FastJson={_report.FastJsonCorrectnessPass}通过/{_report.FastJsonCorrectnessFail}失败, " +
                          $"Insert加速={_report.InsertSpeedup:F2}x, " +
                          $"PRAGMA加速={_report.QueryPragmaSpeedup:F2}x, " +
                          $"PS缓存加速={_report.PreparedStatementSpeedup:F2}x, " +
                          $"Span加速={_report.FastJsonSpeedup:F2}x");
            }

            Debug.Log($"[SqliteBenchmarkTest] 报告文件: {_reportPath}");
        }
    }

    /// <summary>
    /// SQLite 基准测试的 BatchMode 显式验证入口。
    /// 用于在 Unity Test Runner 被项目初始化流程干扰时，仍能稳定验证基准测试关键指标。
    ///
    /// BatchMode explicit verification entry for SQLite benchmarks.
    /// Used when Unity Test Runner is interfered with by project initialization,
    /// ensuring stable verification of benchmark key metrics.
    /// </summary>
    public static class SqliteBenchmarkBatchVerification
    {
        /// <summary>
        /// 顺序执行基准测试断言，写出批验证报告，并用退出码反馈结果。
        /// Sequentially execute benchmark assertions, write batch verification report, and exit with code.
        /// </summary>
        public static void RunBatchVerification()
        {
            Debug.Log("[测试开始] name=SqliteBenchmarkBatchVerification " +
                      "测试目的=验证 SQLite 性能基准测试关键指标在 BatchMode 下符合预期门禁。 " +
                      "实现手段=委托 SqliteBenchmarkRunner.RunAll 并断言报告关键字段，写出批验证报告。");

            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "SqliteBenchmarkBatchVerification.txt");

            SqliteBenchmarkReport report = null;

            try
            {
                // Phase 1: 运行全部基准测试
                report = SqliteBenchmarkRunner.RunAll();
                var reportFilePath = report.SaveToFile();
                reportBuilder.AppendLine($"BenchmarkReport: {reportFilePath}");
            }
            catch (Exception ex)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL RunAll {ex}");
            }

            // Phase 2: 断言关键指标
            var checks = new (string Name, Action<SqliteBenchmarkReport> Check)[]
            {
                ("ReportNotNull", r => { if (r == null) throw new Exception("报告为空"); }),
                ("FastJsonNoFailures", r => { if (r.FastJsonCorrectnessFail != 0) throw new Exception($"FastJson有{r.FastJsonCorrectnessFail}个失败"); }),
                ("FastJsonAtLeastOnePass", r => { if (r.FastJsonCorrectnessPass <= 0) throw new Exception("FastJson无通过用例"); }),
                ("InsertBatchFaster", r => { if (r.InsertSpeedup <= 1.0f) throw new Exception($"Insert加速比{r.InsertSpeedup:F2}x<=1.0x"); }),
                ("PragmaFaster", r => { if (r.QueryPragmaSpeedup <= 1.0f) throw new Exception($"PRAGMA加速比{r.QueryPragmaSpeedup:F2}x<=1.0x"); }),
                ("PSCacheFaster", r => { if (r.PreparedStatementSpeedup <= 1.0f) throw new Exception($"PS缓存加速比{r.PreparedStatementSpeedup:F2}x<=1.0x"); }),
                ("SpanFaster", r => { if (r.FastJsonSpeedup <= 1.0f) throw new Exception($"Span加速比{r.FastJsonSpeedup:F2}x<=1.0x"); }),
                ("BottleneckHasResults", r => { if (r.RealSchemaStepTimings == null || r.RealSchemaStepTimings.Count == 0) throw new Exception("瓶颈分析无结果"); }),
            };

            for (var i = 0; i < checks.Length; i++)
            {
                var check = checks[i];
                try
                {
                    check.Check(report);
                    reportBuilder.AppendLine($"PASS {check.Name}");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    reportBuilder.AppendLine($"FAIL {check.Name} {ex.Message}");
                }
            }

            // Phase 3: 持久化报告并通过显式退出码反馈整体结果
            var totalChecks = checks.Length;
            reportBuilder.Insert(0,
                $"Summary: total={totalChecks} passed={totalChecks - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                Debug.LogError($"Sqlite benchmark batch verification failed. Report: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"Sqlite benchmark batch verification passed. Report: {outputPath}");
            EditorApplication.Exit(0);
        }
    }
}
