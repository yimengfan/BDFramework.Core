using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BDFramework.Sql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// SQLite 总集成测试入口 — 将全部 SQLite 测试按单元测试/性能测试分类执行并输出独立报告。
    /// 支持 Editor Menu 和 BatchMode（-executeMethod）两种调用方式。
    /// 报告输出到 Library/SqliteIntegrationTest/。
    ///
    /// SQLite comprehensive integration test entry — executes all SQLite tests categorized as
    /// unit/performance with separate reports. Supports Editor Menu and BatchMode (-executeMethod).
    /// Reports output to Library/SqliteIntegrationTest/.
    ///
    /// 使用方式 / Usage:
    ///   Editor: 菜单 BDFramework/测试/SQLite 集成测试
    ///   BatchMode: -executeMethod BDFramework.EditorTest.SQLite.SqliteIntegrationTestRunner.RunBatch
    /// </summary>
    public static class SqliteIntegrationTestRunner
    {
        private const string ReportDir = "Library/SqliteIntegrationTest";

        // ═══════════════════════════════════════════
        // 测试分类定义 / Test category definitions
        // ═══════════════════════════════════════════

        /// <summary>
        /// 单元测试：验证功能契约和边界行为的测试。
        /// Unit tests: tests that verify functional contracts and boundary behaviors.
        /// </summary>
        private static readonly Type[] UnitTestFixtures =
        {
            typeof(SqliteLoderPipelineTest),
            typeof(SqliteTransactionAndMigrationTest),
            typeof(SqliteTableQueryBoundaryTest),
            typeof(SqliteUnitTest),           // 已有：基本 CRUD、密码回退、Where 子句
            typeof(SqliteFastJsonConvertOptimizationTest), // 序列化/反序列化正确性
        };

        /// <summary>
        /// 性能测试：验证性能指标和基准门禁的测试。
        /// Performance tests: tests that verify performance metrics and benchmark gates.
        /// </summary>
        private static readonly Type[] PerfTestFixtures =
        {
            typeof(SqlitePerformanceMonitorTest),
            typeof(SqliteBenchmarkGateTest), // 已有：基准门禁断言
        };

        // ═══════════════════════════════════════════
        // Editor Menu 入口 / Editor Menu entry
        // ═══════════════════════════════════════════

        [MenuItem("BDFramework/测试/SQLite 集成测试", false, 100)]
        public static void RunFromMenu()
        {
            Debug.Log("[SQLite 集成测试] 开始执行全部 SQLite 测试（Editor Menu 触发）...");
            RunAll();
        }

        // ═══════════════════════════════════════════
        // BatchMode 入口 / BatchMode entry
        // ═══════════════════════════════════════════

        /// <summary>
        /// BatchMode 显式验证入口。
        /// 当项目级初始化会干扰 Unity 原生 -runTests 流程时，可通过 -executeMethod 直接调用。
        /// 用法: Unity -batchmode -executeMethod BDFramework.EditorTest.SQLite.SqliteIntegrationTestRunner.RunBatch
        ///
        /// BatchMode explicit verification entry.
        /// Use when project-level initialization interferes with Unity's native -runTests.
        /// </summary>
        public static void RunBatch()
        {
            Debug.Log("[测试开始] name=SqliteIntegrationTestRunner.RunBatch " +
                      "测试目的=在 BatchMode 下执行全部 SQLite 单元测试和性能测试并输出分类报告。 " +
                      "实现手段=直接实例化各测试 Fixture 并顺序执行标记为 Test 的方法。");

            var exitCode = RunAll();
            EditorApplication.Exit(exitCode);
        }

        // ═══════════════════════════════════════════
        // 核心执行逻辑 / Core execution logic
        // ═══════════════════════════════════════════

        /// <summary>
        /// 执行所有测试并返回退出码（0=全部通过，非0=有失败）。
        /// Execute all tests and return exit code (0=all pass, non-zero=failures).
        /// </summary>
        public static int RunAll()
        {
            EnsureReportDirectory();

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var overallFailed = 0;

            // ─── Phase 1: 单元测试 ───
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log("<color=cyan>  Phase 1: SQLite 单元测试</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");

            var unitReport = RunTestCategory("单元测试", UnitTestFixtures);
            var unitReportPath = Path.Combine(ReportDir, $"Sqlite_UnitTest_{timestamp}.txt");
            SaveReport(unitReportPath, unitReport);

            if (unitReport.FailedCount > 0)
            {
                overallFailed += unitReport.FailedCount;
                Debug.LogError($"<color=red>[单元测试] {unitReport.FailedCount}/{unitReport.TotalCount} 失败</color>");
            }
            else
            {
                Debug.Log($"<color=green>[单元测试] 全部 {unitReport.TotalCount} 通过</color>");
            }

            // ─── Phase 2: 性能测试 ───
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log("<color=cyan>  Phase 2: SQLite 性能测试</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");

            var perfReport = RunTestCategory("性能测试", PerfTestFixtures);
            var perfReportPath = Path.Combine(ReportDir, $"Sqlite_PerfTest_{timestamp}.txt");
            SaveReport(perfReportPath, perfReport);

            if (perfReport.FailedCount > 0)
            {
                overallFailed += perfReport.FailedCount;
                Debug.LogError($"<color=red>[性能测试] {perfReport.FailedCount}/{perfReport.TotalCount} 失败</color>");
            }
            else
            {
                Debug.Log($"<color=green>[性能测试] 全部 {perfReport.TotalCount} 通过</color>");
            }

            // ─── Phase 3: 输出汇总 ───
            PrintSummary(unitReport, unitReportPath, perfReport, perfReportPath, overallFailed);

            return overallFailed > 0 ? 1 : 0;
        }

        // ═══════════════════════════════════════════
        // 测试分类执行 / Test category execution
        // ═══════════════════════════════════════════

        private class TestReport
        {
            public string Category;
            public int TotalCount;
            public int PassedCount;
            public int FailedCount;
            public int SkippedCount;
            public readonly List<TestResult> Results = new List<TestResult>();
        }

        private class TestResult
        {
            public string FixtureName;
            public string TestName;
            public bool Passed;
            public string Message;
            public long ElapsedMs;
        }

        private static TestReport RunTestCategory(string category, Type[] fixtures)
        {
            var report = new TestReport { Category = category };

            foreach (var fixtureType in fixtures)
            {
                Debug.Log($"[{category}] 开始 Fixture: {fixtureType.Name}");
                object fixtureInstance = null;

                try
                {
                    fixtureInstance = Activator.CreateInstance(fixtureType);

                    // 执行 [OneTimeSetUp] 方法（一次性初始化）
                    // Execute [OneTimeSetUp] method (one-time init)
                    var oneTimeSetup = FindMethodWithAttribute(fixtureType, typeof(OneTimeSetUpAttribute));
                    if (oneTimeSetup != null)
                    {
                        Debug.Log($"  [OneTimeSetUp] {fixtureType.Name}.{oneTimeSetup.Name}()");
                        oneTimeSetup.Invoke(oneTimeSetup.IsStatic ? null : fixtureInstance, null);
                    }

                    // 获取 [SetUp] 方法（每个测试前执行）
                    // Get [SetUp] method (executed before each test)
                    var setUpMethod = FindMethodWithAttribute(fixtureType, typeof(SetUpAttribute));

                    // 获取 [TearDown] 方法（每个测试后执行）
                    // Get [TearDown] method (executed after each test)
                    var tearDownMethod = FindMethodWithAttribute(fixtureType, typeof(TearDownAttribute));

                    // 获取所有标记为 [Test] 的方法
                    var testMethods = GetTestMethods(fixtureType);

                    foreach (var method in testMethods)
                    {
                        var result = new TestResult
                        {
                            FixtureName = fixtureType.Name,
                            TestName = method.Name
                        };

                        try
                        {
                            // 每个测试前调用 [SetUp]
                            if (setUpMethod != null)
                            {
                                setUpMethod.Invoke(setUpMethod.IsStatic ? null : fixtureInstance, null);
                            }

                            Debug.Log($"  [{category}] 测试目的=验证 {method.Name} " +
                                      $"实现手段=执行 {fixtureType.Name}.{method.Name}");

                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            method.Invoke(method.IsStatic ? null : fixtureInstance, null);
                            sw.Stop();

                            result.Passed = true;
                            result.ElapsedMs = sw.ElapsedMilliseconds;
                            report.PassedCount++;

                            Debug.Log($"  <color=green>PASS</color> {method.Name} ({result.ElapsedMs}ms)");
                        }
                        catch (TargetInvocationException ex)
                        {
                            var inner = ex.InnerException ?? ex;
                            result.Passed = false;
                            result.Message = inner.Message;
                            report.FailedCount++;

                            if (inner is AssertionException)
                            {
                                Debug.LogError($"  <color=red>FAIL (断言)</color> {method.Name}: {inner.Message}");
                            }
                            else
                            {
                                Debug.LogError($"  <color=red>FAIL (异常)</color> {method.Name}: {inner.Message}\n{inner.StackTrace}");
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Passed = false;
                            result.Message = ex.Message;
                            report.FailedCount++;
                            Debug.LogError($"  <color=red>FAIL (异常)</color> {method.Name}: {ex.Message}");
                        }
                        finally
                        {
                            // 每个测试后调用 [TearDown]
                            if (tearDownMethod != null)
                            {
                                try { tearDownMethod.Invoke(tearDownMethod.IsStatic ? null : fixtureInstance, null); }
                                catch { /* 忽略 TearDown 异常 */ }
                            }
                        }

                        report.Results.Add(result);
                        report.TotalCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{category}] Fixture {fixtureType.Name} 初始化失败: {ex.Message}");
                    report.FailedCount++;
                }
                finally
                {
                    // 执行 [OneTimeTearDown]（一次性清理）
                    // Execute [OneTimeTearDown] (one-time cleanup)
                    if (fixtureInstance != null)
                    {
                        var oneTimeTearDown = FindMethodWithAttribute(fixtureType, typeof(OneTimeTearDownAttribute));
                        if (oneTimeTearDown != null)
                        {
                            try
                            {
                                Debug.Log($"  [OneTimeTearDown] {fixtureType.Name}.{oneTimeTearDown.Name}()");
                                oneTimeTearDown.Invoke(oneTimeTearDown.IsStatic ? null : fixtureInstance, null);
                            }
                            catch { /* 忽略清理异常 */ }
                        }
                    }
                }

                Debug.Log($"[{category}] 完成 Fixture: {fixtureType.Name}");
            }

            return report;
        }

        private static List<MethodInfo> GetTestMethods(Type fixtureType)
        {
            var methods = new List<MethodInfo>();
            foreach (var method in fixtureType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<TestAttribute>() != null)
                {
                    methods.Add(method);
                }
            }

            // 按 Order 属性排序
            methods.Sort((a, b) =>
            {
                var orderA = a.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
                var orderB = b.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
                return orderA.CompareTo(orderB);
            });

            return methods;
        }

        /// <summary>
        /// 在类型上查找带有指定 NUnit 属性的方法。
        /// 支持 SetUpAttribute、TearDownAttribute、OneTimeSetUpAttribute、OneTimeTearDownAttribute。
        /// Find a method decorated with the specified NUnit attribute on the given type.
        /// Supports SetUpAttribute, TearDownAttribute, OneTimeSetUpAttribute, OneTimeTearDownAttribute.
        /// </summary>
        private static MethodInfo FindMethodWithAttribute(Type type, Type attributeType)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.GetCustomAttribute(attributeType) != null)
                {
                    return method;
                }
            }
            return null;
        }

        // ═══════════════════════════════════════════
        // 报告输出 / Report output
        // ═══════════════════════════════════════════

        private static void SaveReport(string path, TestReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== SQLite {report.Category} 报告 ===");
            sb.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"总计: {report.TotalCount}  通过: {report.PassedCount}  失败: {report.FailedCount}  跳过: {report.SkippedCount}");
            sb.AppendLine($"通过率: {(report.TotalCount > 0 ? (report.PassedCount * 100.0 / report.TotalCount) : 0):F1}%");
            sb.AppendLine();
            sb.AppendLine("--- 详细结果 ---");

            foreach (var result in report.Results)
            {
                var status = result.Passed ? "PASS" : "FAIL";
                var msg = result.Passed ? "" : $" - {result.Message}";
                sb.AppendLine($"[{status}] {result.FixtureName}.{result.TestName} ({result.ElapsedMs}ms){msg}");
            }

            if (report.FailedCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- 失败用例 ---");
                foreach (var result in report.Results)
                {
                    if (!result.Passed)
                    {
                        sb.AppendLine($"  {result.FixtureName}.{result.TestName}: {result.Message}");
                    }
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[报告] {report.Category} 报告已写入: {path}");
        }

        private static void PrintSummary(TestReport unitReport, string unitPath,
            TestReport perfReport, string perfPath, int totalFailed)
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log("<color=cyan>  SQLite 集成测试汇总</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=yellow>单元测试:</color> {unitReport.PassedCount}/{unitReport.TotalCount} 通过 " +
                      $"(失败 {unitReport.FailedCount}) → {unitPath}");
            Debug.Log($"<color=yellow>性能测试:</color> {perfReport.PassedCount}/{perfReport.TotalCount} 通过 " +
                      $"(失败 {perfReport.FailedCount}) → {perfPath}");
            Debug.Log($"<color=yellow>总计:</color> " +
                      $"{unitReport.PassedCount + perfReport.PassedCount}/" +
                      $"{unitReport.TotalCount + perfReport.TotalCount} 通过");

            if (totalFailed > 0)
            {
                Debug.LogError($"<color=red>集成测试未通过: {totalFailed} 个用例失败</color>");
            }
            else
            {
                Debug.Log("<color=green>集成测试全部通过 ✓</color>");
            }
        }

        private static void EnsureReportDirectory()
        {
            if (!Directory.Exists(ReportDir))
            {
                Directory.CreateDirectory(ReportDir);
            }
        }
    }
}
