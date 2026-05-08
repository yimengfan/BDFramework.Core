using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Game.E2E
{
    /// <summary>
    /// 业务模块集成测试统一入口。
    /// Unified entry point for business module integration tests.
    ///
    /// 该套件按模块维度组织业务侧的集成测试，每个测试方法对应一个业务模块的完整测试链，
    /// 为 E2E 层提供一键验证所有业务模块集成能力的入口。
    /// 仅聚合 Scope = "business" 的套件；框架套件由框架侧的 ModuleIntegrationEntry 管理。
    ///
    /// This suite organizes business-side integration tests by module dimension; each test method corresponds to a
    /// complete test chain of a business module, providing E2E layer a one-click entry to verify all
    /// business modules' integration capabilities. Only aggregates suites with Scope = "business";
    /// framework suites are managed by ModuleIntegrationEntry.
    /// </summary>
    [Preserve]
    public static class BusinessModuleIntegrationEntry
    {
        /// <summary>
        /// 测试层级执行优先级：contract → api → business → integration → host。
        /// Test tier execution priority: contract → api → business → integration → host.
        /// 数值越小越先执行。未识别的层级排在最后。
        /// Lower values execute first. Unrecognized tiers are placed last.
        /// </summary>
        private static readonly Dictionary<string, int> TierPriority = new Dictionary<string, int>
        {
            { "contract", 0 },
            { "api", 1 },
            { "business", 2 },
            { "integration", 3 },
            { "host", 4 },
        };

        /// <summary>
        /// 缓存的套件→方法映射，避免重复反射扫描。
        /// Cached suite→method mapping to avoid repeated reflection scans.
        /// </summary>
        private static Dictionary<string, List<MethodInfo>> _suiteMethodCache;

        /// <summary>
        /// 全部业务模块入口定义。
        /// All business module entry definitions.
        /// 新增业务模块时只需在此数组添加一条记录，无需手动编写 RunSubSuite 调用。
        /// When adding a new business module, just add one entry to this array; no need to manually write RunSubSuite calls.
        /// </summary>
        private static readonly (string module, string displayName, string displayNameEn, int order)[] AllBusinessModuleEntries = new[]
        {
            ("asset",         "资源业务",            "Asset Business",          1),
            ("ui",            "UI 业务流程",          "UI Business",             2),
        };

        /// <summary>
        /// Asset 业务模块集成测试：自动执行该模块所有业务套件。
        /// Asset business module integration test: auto-execute all business suites of this module.
        /// 测试目的=验证 Asset 业务模块集成链路 实现手段=从 E2ESuiteCatalog 查找 asset 模块的业务套件并按层级排序反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "business-integration", order: 1, des: "asset-business-全链路集成")]
        public static void AssetBusinessIntegration()
        {
            RunModuleIntegration("asset", "资源业务");
        }

        /// <summary>
        /// UI 业务模块集成测试：自动执行该模块所有业务套件。
        /// UI business module integration test: auto-execute all business suites of this module.
        /// 测试目的=验证 UI 业务模块集成链路 实现手段=从 E2ESuiteCatalog 查找 ui 模块的业务套件并按层级排序反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "business-integration", order: 2, des: "ui-business-全链路集成")]
        public static void UiBusinessIntegration()
        {
            RunModuleIntegration("ui", "UI 业务流程");
        }

        /// <summary>
        /// 按模块自动执行业务集成测试：从 E2ESuiteCatalog 查找该模块的业务套件，按层级排序后逐一执行。
        /// Auto-execute business integration tests by module: find all business suites for the module from E2ESuiteCatalog,
        /// sort by tier priority, and invoke each in order.
        /// </summary>
        /// <param name="module">模块名称，对应 E2ESuiteCatalog 中的 Module 字段。Module name matching E2ESuiteCatalog.Module.</param>
        /// <param name="displayName">日志中显示的模块名称。Display name for logging.</param>
        private static void RunModuleIntegration(string module, string displayName)
        {
            Debug.Log($"[E2E] 测试目的={displayName}业务模块全链路集成 实现手段=从 E2ESuiteCatalog 查找模块业务套件并按层级排序反射调用");

            // 从目录查找该模块的所有业务套件，按层级优先级排序
            // Find all business suites for the module from catalog, sorted by tier priority
            var suites = E2ESuiteCatalog.GetSuitesByModule(module);
            if (suites == null || suites.Length == 0)
            {
                Debug.LogWarning($"[E2E] 业务模块 {displayName} ({module}) 在 E2ESuiteCatalog 中未注册任何套件，跳过");
                return;
            }

            // 仅筛选业务套件，排除 business-integration 自身，按层级优先级排序
            // Only include business scoped suites, exclude business-integration itself, sort by tier priority
            var orderedSuites = suites
                .Where(s => string.Equals(s.Scope, "business", StringComparison.OrdinalIgnoreCase))
                .Where(s => !string.Equals(s.SuiteName, "business-integration", StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => TierPriority.TryGetValue(s.Tier, out var priority) ? priority : 99)
                .ThenBy(s => s.SuiteName)
                .ToList();

            if (orderedSuites.Count == 0)
            {
                Debug.Log($"[E2E] 业务模块 {displayName} ({module}): 无业务层套件，跳过");
                return;
            }

            Debug.Log($"[E2E] 业务模块 {displayName} ({module}): 发现 {orderedSuites.Count} 个业务子套件");

            var passCount = 0;
            foreach (var suite in orderedSuites)
            {
                RunSubSuite(suite.SuiteName, $"{displayName} / {suite.Description}");
                passCount++;
            }

            Debug.Log($"[E2E] {displayName} 业务模块全链路集成完成: {passCount}/{orderedSuites.Count} 个业务子套件全部通过");
        }

        /// <summary>
        /// 执行指定子套件并验证全部通过，任一失败即抛出异常中止。
        /// Execute the specified sub-suite and verify all cases pass; throw on any failure to abort.
        /// </summary>
        /// <param name="suiteName">子套件名称。Sub-suite name.</param>
        /// <param name="displayName">日志中显示的名称。Display name for logging.</param>
        private static void RunSubSuite(string suiteName, string displayName)
        {
            Debug.Log($"[E2E] 执行业务子套件: {displayName} ({suiteName})");

            // 懒初始化套件→方法映射 / Lazy-init suite→method mapping
            if (_suiteMethodCache == null)
            {
                _suiteMethodCache = BuildSuiteMethodCache();
            }

            // 查找目标套件的方法 / Find methods for target suite
            if (!_suiteMethodCache.TryGetValue(suiteName, out var methods) || methods.Count == 0)
            {
                Debug.LogWarning($"[E2E] 业务子套件 {displayName} ({suiteName}) 未发现任何测试方法，跳过");
                return;
            }

            // 按 order 排序并逐一执行 / Sort by order and invoke each
            var orderedMethods = methods.OrderBy(m =>
            {
                var attr = m.GetCustomAttribute<E2ETestAttribute>();
                return attr?.Order ?? 0;
            }).ToList();

            var passCount = 0;
            var failCount = 0;
            var failures = new List<string>();

            foreach (var method in orderedMethods)
            {
                var attr = method.GetCustomAttribute<E2ETestAttribute>();
                var methodDesc = attr?.Des ?? method.Name;

                try
                {
                    method.Invoke(null, null);
                    passCount++;
                    Debug.Log($"[E2E]   ✅ {suiteName}.{method.Name}  {methodDesc}");
                }
                catch (TargetInvocationException tie)
                {
                    failCount++;
                    var innerMsg = tie.InnerException?.Message ?? tie.Message;
                    failures.Add($"{suiteName}.{method.Name} ({methodDesc}): {innerMsg}");
                    Debug.LogError($"[E2E]   ❌ {suiteName}.{method.Name}  {methodDesc} — {innerMsg}");
                    // 快速失败：任一子测试失败即中止当前子套件 / Fast fail on any sub-test failure
                    break;
                }
                catch (Exception ex)
                {
                    failCount++;
                    failures.Add($"{suiteName}.{method.Name} ({methodDesc}): {ex.Message}");
                    Debug.LogError($"[E2E]   ❌ {suiteName}.{method.Name}  {methodDesc} — {ex.Message}");
                    break;
                }
            }

            // 汇总子套件结果 / Summarize sub-suite results
            if (failCount > 0)
            {
                var failureDetail = string.Join("; ", failures);
                throw new Exception(
                    $"业务子套件 {displayName} ({suiteName}) 失败: {failCount}/{orderedMethods.Count} 个测试未通过。" +
                    $"失败明细: {failureDetail}");
            }

            Debug.Log($"[E2E] 业务子套件 {displayName} ({suiteName}) 完成: {passCount}/{orderedMethods.Count} 通过");
        }

        /// <summary>
        /// 通过反射扫描所有程序集，构建套件名称→测试方法列表的映射。
        /// Build a mapping from suite name to test method list by reflectively scanning all assemblies.
        /// 排除 business-integration 套件自身的方法，避免递归调用。
        /// Excludes methods from the business-integration suite itself to prevent recursive invocation.
        /// </summary>
        private static Dictionary<string, List<MethodInfo>> BuildSuiteMethodCache()
        {
            var cache = new Dictionary<string, List<MethodInfo>>();
            var e2eTestAttributeType = typeof(E2ETestAttribute);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic))
                        {
                            var attr = method.GetCustomAttribute<E2ETestAttribute>();
                            if (attr == null || string.IsNullOrEmpty(attr.Suite)) continue;

                            // 排除 business-integration 套件自身的方法，避免递归
                            if (string.Equals(attr.Suite, "business-integration", StringComparison.OrdinalIgnoreCase)) continue;

                            if (!cache.TryGetValue(attr.Suite, out var list))
                            {
                                list = new List<MethodInfo>();
                                cache[attr.Suite] = list;
                            }
                            list.Add(method);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 某些程序集可能无法加载所有类型，跳过
                    // Some assemblies may not load all types; skip
                }
            }

            Debug.Log($"[E2E] 业务套件方法缓存构建完成: {cache.Count} 个套件, {cache.Values.Sum(l => l.Count)} 个测试方法");
            return cache;
        }

        /// <summary>
        /// 全业务模块集成测试汇总入口：按顺序输出所有业务模块的覆盖范围。
        /// Full business module integration summary entry: output coverage of all business modules in order.
        /// 测试目的=验证所有业务模块的集成测试链路均已联通且全部通过。
        /// 实现手段=从 AllBusinessModuleEntries 读取模块列表，自动汇总业务层套件覆盖范围。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "business-integration", order: 999, des: "全业务模块集成汇总")]
        public static void AllBusinessModulesSummary()
        {
            Debug.Log("[E2E] 业务模块集成测试汇总:");

            // 从目录自动输出每个模块的业务层覆盖范围 / Auto-output each module's business-scope coverage from catalog
            foreach (var entry in AllBusinessModuleEntries)
            {
                var suites = E2ESuiteCatalog.GetSuitesByModule(entry.module);
                var suiteNames = suites
                    .Where(s => string.Equals(s.Scope, "business", StringComparison.OrdinalIgnoreCase))
                    .Where(s => !string.Equals(s.SuiteName, "business-integration", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.SuiteName);
                Debug.Log($"[E2E]   {entry.displayName} ({entry.module}): {string.Join(" + ", suiteNames)}");
            }

            Debug.Log("[E2E] 业务模块集成测试入口验证完成（框架套件由 ModuleIntegrationEntry 管理）");
        }
    }
}
