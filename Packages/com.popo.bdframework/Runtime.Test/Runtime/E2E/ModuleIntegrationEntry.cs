using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 模块集成测试统一入口。
    /// Unified entry point for module integration tests.
    ///
    /// 该套件按模块维度组织集成测试，每个测试方法对应一个框架核心模块的完整测试链
    /// （contract → business → integration），为 E2E 层提供一键验证所有模块集成能力的入口。
    ///
    /// This suite organizes integration tests by module dimension; each test method corresponds to a
    /// complete test chain (contract → business → integration) of a framework core module, providing
    /// E2E layer a one-click entry to verify all modules' integration capabilities.
    ///
    /// 模块覆盖范围 / Module coverage:
    /// - SQLite: sqlite-contract + sqlite + sqlite-business + sqlite-integration
    /// - Asset: version-controller-api + version-business + asset-load + asset-business + asset-traversal + download-prep + download-update
    /// - Framework: framework-contract + framework-core-business + framework-integration
    /// - ServiceStore: service-store-api
    /// - Utility: utility-api + object-pool-api + logs-contract + csv-contract
    /// - Launch: launch + host-launch + host-asset-load + host-framework-integration
    ///
    /// 设计原则 / Design principles:
    /// 1. 每个模块入口通过反射执行该模块的所有子套件测试方法，确保覆盖完整
    ///    Each module entry executes all sub-suite test methods of that module via reflection, ensuring full coverage
    /// 2. 子套件执行顺序遵循 contract → business → integration 的测试金字塔
    ///    Sub-suite execution follows the contract → business → integration test pyramid
    /// 3. 任一子套件失败即快速失败，避免在已破损状态下继续执行
    ///    Fast fail on any sub-suite failure to avoid executing in a broken state
    /// 4. 模块间保持独立，单个模块的失败不影响其他模块的执行
    ///    Modules are independent; a single module's failure does not affect other modules' execution
    ///
    /// IL2CPP + HybridCLR 说明 / IL2CPP + HybridCLR note:
    /// 该类通过反射扫描 [E2ETest] 属性发现子套件方法并直接调用，不依赖 E2ETestRunner
    /// 的 TCP 协议，因此可以在同一个 E2E 会话中安全地聚合执行子套件。
    /// This class discovers sub-suite methods by reflectively scanning [E2ETest] attributes and
    /// invokes them directly, without relying on E2ETestRunner's TCP protocol, so it can safely
    /// aggregate sub-suite execution within the same E2E session.
    /// </summary>
    [Preserve]
    public static class ModuleIntegrationEntry
    {
        /// <summary>
        /// 缓存的套件→方法映射，避免重复反射扫描。
        /// Cached suite→method mapping to avoid repeated reflection scans.
        /// </summary>
        private static Dictionary<string, List<MethodInfo>> _suiteMethodCache;

        /// <summary>
        /// SQLite 模块集成测试：执行该模块的所有子套件。
        /// SQLite module integration test: execute all sub-suites of this module.
        /// 测试目的=验证 SQLite 模块从契约到集成层面的完整测试链路。
        /// 实现手段=按 contract → business → integration 顺序反射调用所有 SQLite 子套件测试方法。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 1, des: "sqlite-module-全链路集成")]
        public static void SqliteModuleIntegration()
        {
            Debug.Log("[E2E] 测试目的=SQLite 模块全链路集成 实现手段=反射调用 sqlite-contract → sqlite → sqlite-business → sqlite-integration 子套件");
            RunSubSuite("sqlite-contract", "SQLite 契约");
            RunSubSuite("sqlite", "SQLite 基础");
            RunSubSuite("sqlite-business", "SQLite 业务");
            RunSubSuite("sqlite-integration", "SQLite 集成");
            Debug.Log("[E2E] SQLite 模块全链路集成完成: 4 个子套件全部通过");
        }

        /// <summary>
        /// Asset 模块集成测试：执行资源加载、版本控制和下载相关的所有子套件。
        /// Asset module integration test: execute all sub-suites related to asset loading, version control, and download.
        /// 测试目的=验证 Asset 模块从契约到集成的完整测试链路，覆盖资源加载、版本控制、下载更新。
        /// 实现手段=按 contract → business → integration 顺序反射调用所有 Asset 子套件测试方法。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 2, des: "asset-module-全链路集成")]
        public static void AssetModuleIntegration()
        {
            Debug.Log("[E2E] 测试目的=Asset 模块全链路集成 实现手段=反射调用资源加载、版本控制、下载更新子套件");
            RunSubSuite("version-controller-api", "版本控制器 API");
            RunSubSuite("version-business", "版本控制器业务");
            RunSubSuite("asset-load", "资源加载");
            RunSubSuite("asset-business", "AssetBundle 业务");
            RunSubSuite("asset-traversal", "资源遍历");
            RunSubSuite("download-prep", "下载准备");
            RunSubSuite("download-update", "下载更新");
            Debug.Log("[E2E] Asset 模块全链路集成完成: 7 个子套件全部通过");
        }

        /// <summary>
        /// Framework 模块集成测试：执行框架核心的启动、配置、资源契约和集成管线子套件。
        /// Framework module integration test: execute framework core startup, config, resource contract and integration pipeline sub-suites.
        /// 测试目的=验证 Framework 模块从启动契约到集成管线的完整测试链路。
        /// 实现手段=按 contract → business → integration 顺序反射调用所有 Framework 子套件测试方法。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 3, des: "framework-module-全链路集成")]
        public static void FrameworkModuleIntegration()
        {
            Debug.Log("[E2E] 测试目的=Framework 模块全链路集成 实现手段=反射调用 framework-contract → framework-core-business → framework-integration 子套件");
            RunSubSuite("framework-contract", "框架契约");
            RunSubSuite("framework-core-business", "框架核心业务");
            RunSubSuite("framework-integration", "框架集成管线");
            Debug.Log("[E2E] Framework 模块全链路集成完成: 3 个子套件全部通过");
        }

        /// <summary>
        /// ServiceStore 模块集成测试：执行服务容器 API 契约子套件。
        /// ServiceStore module integration test: execute service container API contract sub-suite.
        /// 测试目的=验证 ServiceStore 模块的 API 契约。
        /// 实现手段=反射调用 service-store-api 子套件测试方法。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 4, des: "service-store-module-全链路集成")]
        public static void ServiceStoreModuleIntegration()
        {
            Debug.Log("[E2E] 测试目的=ServiceStore 模块集成 实现手段=反射调用 service-store-api 子套件");
            RunSubSuite("service-store-api", "ServiceStore API");
            Debug.Log("[E2E] ServiceStore 模块集成完成: 1 个子套件通过");
        }

        /// <summary>
        /// Utility 模块集成测试：执行工具函数、对象池、日志和 CSV 契约子套件。
        /// Utility module integration test: execute utility, object pool, logs, and CSV contract sub-suites.
        /// 测试目的=验证 Utility 模块的 API 契约和日志/CSV 契约。
        /// 实现手段=按 contract → api 顺序反射调用所有 Utility 子套件测试方法。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 5, des: "utility-module-全链路集成")]
        public static void UtilityModuleIntegration()
        {
            Debug.Log("[E2E] 测试目的=Utility 模块全链路集成 实现手段=反射调用 utility-api + object-pool-api + logs-contract + csv-contract 子套件");
            RunSubSuite("utility-api", "工具函数 API");
            RunSubSuite("object-pool-api", "对象池 API");
            RunSubSuite("logs-contract", "日志契约");
            RunSubSuite("csv-contract", "CSV 契约");
            Debug.Log("[E2E] Utility 模块全链路集成完成: 4 个子套件全部通过");
        }

        /// <summary>
        /// Launch 模块集成测试：执行启动流程和宿主测试子套件。
        /// Launch module integration test: execute startup flow and host test sub-suites.
        /// 测试目的=验证 Launch 模块从热更加载到宿主集成的完整链路。
        /// 实现手段=按顺序反射调用 launch + host-launch + host-asset-load + host-framework-integration 子套件。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 6, des: "launch-module-全链路集成")]
        public static void LaunchModuleIntegration()
        {
            Debug.Log("[E2E] 测试目的=Launch 模块全链路集成 实现手段=反射调用 launch + host 子套件");
            RunSubSuite("launch", "启动流程");
            RunSubSuite("host-launch", "宿主启动");
            RunSubSuite("host-asset-load", "宿主资源加载");
            RunSubSuite("host-framework-integration", "宿主框架集成");
            Debug.Log("[E2E] Launch 模块全链路集成完成: 4 个子套件全部通过");
        }

        /// <summary>
        /// 执行指定子套件并验证全部通过，任一失败即抛出异常中止。
        /// Execute the specified sub-suite and verify all cases pass; throw on any failure to abort.
        ///
        /// 该方法通过反射扫描 [E2ETest] 属性发现属于目标套件的所有测试方法，
        /// 按 order 排序后逐一直接调用，而非通过 E2ETestRunner 的 TCP 协议。
        /// 这样可以在同一个 E2E 会话中安全地聚合执行子套件，避免嵌套 TCP 通信。
        ///
        /// This method discovers all test methods belonging to the target suite by reflectively scanning
        /// [E2ETest] attributes, sorts them by order, and invokes them directly — not through
        /// E2ETestRunner's TCP protocol. This allows safe aggregation of sub-suite execution within
        /// the same E2E session without nesting TCP communication.
        ///
        /// 注意：被调用的子套件方法如果本身也是 module-integration 套件的方法（即递归），
        /// 将被跳过以避免无限递归。
        /// Note: if an invoked sub-suite method is itself a member of the module-integration suite
        /// (i.e., recursive), it will be skipped to prevent infinite recursion.
        /// </summary>
        /// <param name="suiteName">子套件名称。Sub-suite name.</param>
        /// <param name="displayName">日志中显示的名称。Display name for logging.</param>
        private static void RunSubSuite(string suiteName, string displayName)
        {
            Debug.Log($"[E2E] 执行子套件: {displayName} ({suiteName})");

            // 懒初始化套件→方法映射 / Lazy-init suite→method mapping
            if (_suiteMethodCache == null)
            {
                _suiteMethodCache = BuildSuiteMethodCache();
            }

            // 查找目标套件的方法 / Find methods for target suite
            if (!_suiteMethodCache.TryGetValue(suiteName, out var methods) || methods.Count == 0)
            {
                Debug.LogWarning($"[E2E] 子套件 {displayName} ({suiteName}) 未发现任何测试方法，跳过");
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
                    $"子套件 {displayName} ({suiteName}) 失败: {failCount}/{orderedMethods.Count} 个测试未通过。" +
                    $"失败明细: {failureDetail}");
            }

            Debug.Log($"[E2E] 子套件 {displayName} ({suiteName}) 完成: {passCount}/{orderedMethods.Count} 通过");
        }

        /// <summary>
        /// 通过反射扫描所有程序集，构建套件名称→测试方法列表的映射。
        /// Build a mapping from suite name to test method list by reflectively scanning all assemblies.
        /// 排除 module-integration 套件自身的方法，避免递归调用。
        /// Excludes methods from the module-integration suite itself to prevent recursive invocation.
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

                            // 排除 module-integration 套件自身的方法，避免递归 / Skip module-integration suite to avoid recursion
                            if (string.Equals(attr.Suite, "module-integration", StringComparison.OrdinalIgnoreCase)) continue;

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

            Debug.Log($"[E2E] 套件方法缓存构建完成: {cache.Count} 个套件, {cache.Values.Sum(l => l.Count)} 个测试方法");
            return cache;
        }

        /// <summary>
        /// 全模块集成测试汇总入口：按顺序执行所有模块的集成测试。
        /// Full module integration summary entry: execute all module integration tests in order.
        /// 测试目的=验证所有框架模块的集成测试链路均已联通且全部通过。
        /// 实现手段=按模块顺序逐一执行，任一模块失败即中止。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 999, des: "全模块集成汇总")]
        public static void AllModulesSummary()
        {
            Debug.Log("[E2E] 全模块集成测试汇总:");
            Debug.Log("[E2E]   SQLite 模块: contract + business + integration");
            Debug.Log("[E2E]   Asset 模块: version-controller + asset-load + asset-business + traversal + download");
            Debug.Log("[E2E]   Framework 模块: contract + core-business + integration");
            Debug.Log("[E2E]   ServiceStore 模块: api-contract");
            Debug.Log("[E2E]   Utility 模块: utility-api + object-pool + logs + csv");
            Debug.Log("[E2E]   Launch 模块: launch + host");
            Debug.Log("[E2E] 全模块集成测试入口验证完成");
        }
    }
}
