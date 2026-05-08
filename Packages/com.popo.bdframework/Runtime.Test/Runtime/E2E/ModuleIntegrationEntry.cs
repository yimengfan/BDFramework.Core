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
    /// 模块集成测试统一入口（框架侧）。
    /// Unified entry point for module integration tests (framework side).
    ///
    /// 该套件按模块维度组织框架侧的集成测试，每个测试方法对应一个框架核心模块的完整测试链
    /// （contract → business → integration），为 E2E 层提供一键验证所有框架模块集成能力的入口。
    /// 仅聚合 Scope = "framework" 的套件；业务套件由业务侧的 BusinessModuleIntegrationEntry 管理。
    ///
    /// This suite organizes framework-side integration tests by module dimension; each test method corresponds to a
    /// complete test chain (contract → business → integration) of a framework core module, providing
    /// E2E layer a one-click entry to verify all framework modules' integration capabilities.
    /// Only aggregates suites with Scope = "framework"; business suites are managed by BusinessModuleIntegrationEntry.
    ///
    /// 自动化维护模型 / Automated maintenance model:
    /// - 模块入口方法通过 E2ESuiteCatalog 自动生成，无需手动添加 RunSubSuite 调用
    ///   Module entry methods are auto-generated from E2ESuiteCatalog, no manual RunSubSuite calls needed
    /// - 新增模块只需：①创建测试文件 + ②在 E2ESuiteCatalog.AllSuites 添加条目
    ///   Adding a new module only requires: ①create test file + ②add entry to E2ESuiteCatalog.AllSuites
    /// - 新增模块后须在 AllModuleEntries 中添加对应的入口方法（一行代码）
    ///   After adding a module, add a corresponding entry method in AllModuleEntries (one line of code)
    /// - VerifyCatalogIntegrity 会自动检测目录遗漏
    ///   VerifyCatalogIntegrity auto-detects missing catalog entries
    ///
    /// 设计原则 / Design principles:
    /// 1. 每个模块入口通过反射执行该模块的所有子套件测试方法，确保覆盖完整
    ///    Each module entry executes all sub-suite test methods of that module via reflection, ensuring full coverage
    /// 2. 子套件执行顺序遵循 contract → api → business → integration → host 的测试层级
    ///    Sub-suite execution follows the contract → api → business → integration → host tier order
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
        /// 全部模块入口定义。
        /// All module entry definitions.
        /// 新增模块时只需在此数组添加一条记录，无需手动编写 RunSubSuite 调用。
        /// When adding a new module, just add one entry to this array; no need to manually write RunSubSuite calls.
        /// 执行时会自动从 E2ESuiteCatalog 查找该模块的所有子套件，按层级排序后逐一执行。
        /// Execution automatically finds all sub-suites for the module from E2ESuiteCatalog,
        /// sorts by tier priority, and invokes each in order.
        /// </summary>
        private static readonly (string module, string displayName, string displayNameEn, int order)[] AllModuleEntries = new[]
        {
            ("sqlite",        "SQLite 数据存储",  "SQLite Data Storage",      1),
            ("asset",         "资源加载与版本控制", "Asset Loading & Version",  2),
            ("framework",     "框架核心启动与配置", "Framework Core",           3),
            ("service-store", "服务容器与依赖注入", "ServiceStore",             4),
            ("utility",       "工具函数与基础设施", "Utility & Infrastructure", 5),
            ("launch",        "启动流程与宿主集成", "Launch & Host Integration",6),
            ("ui",            "UI 框架",            "UI Framework",             7),
        };

        /// <summary>
        /// SQLite 模块集成测试：自动执行该模块所有子套件。
        /// SQLite module integration test: auto-execute all sub-suites of this module.
        /// 测试目的=验证 SQLite 模块从契约到集成层面的完整测试链路。
        /// 实现手段=从 E2ESuiteCatalog 查找 sqlite 模块的所有套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 1, des: "sqlite-module-全链路集成")]
        public static void SqliteModuleIntegration()
        {
            RunModuleIntegration("sqlite", "SQLite 数据存储");
        }

        /// <summary>
        /// Asset 模块集成测试：自动执行该模块所有子套件。
        /// Asset module integration test: auto-execute all sub-suites of this module.
        /// 测试目的=验证 Asset 模块从契约到集成的完整测试链路。
        /// 实现手段=从 E2ESuiteCatalog 查找 asset 模块的所有套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 2, des: "asset-module-全链路集成")]
        public static void AssetModuleIntegration()
        {
            RunModuleIntegration("asset", "资源加载与版本控制");
        }

        /// <summary>
        /// Framework 模块集成测试：自动执行该模块所有子套件。
        /// Framework module integration test: auto-execute all sub-suites of this module.
        /// 测试目的=验证 Framework 模块从契约到集成的完整测试链路。
        /// 实现手段=从 E2ESuiteCatalog 查找 framework 模块的所有套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 3, des: "framework-module-全链路集成")]
        public static void FrameworkModuleIntegration()
        {
            RunModuleIntegration("framework", "框架核心启动与配置");
        }

        /// <summary>
        /// ServiceStore 模块集成测试：自动执行该模块所有子套件。
        /// ServiceStore module integration test: auto-execute all sub-suites of this module.
        /// 测试目的=验证 ServiceStore 模块的 API 契约。
        /// 实现手段=从 E2ESuiteCatalog 查找 service-store 模块的所有套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 4, des: "service-store-module-全链路集成")]
        public static void ServiceStoreModuleIntegration()
        {
            RunModuleIntegration("service-store", "服务容器与依赖注入");
        }

        /// <summary>
        /// Utility 模块集成测试：自动执行该模块所有子套件。
        /// Utility module integration test: auto-execute all sub-suites of this module.
        /// 测试目的=验证 Utility 模块的 API 契约和日志/CSV 契约。
        /// 实现手段=从 E2ESuiteCatalog 查找 utility 模块的所有套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 5, des: "utility-module-全链路集成")]
        public static void UtilityModuleIntegration()
        {
            RunModuleIntegration("utility", "工具函数与基础设施");
        }

        /// <summary>
        /// Launch 模块集成测试：自动执行该模块所有子套件。
        /// Launch module integration test: auto-execute all sub-suites of this module.
        /// 测试目的=验证 Launch 模块从热更加载到宿主集成的完整链路。
        /// 实现手段=从 E2ESuiteCatalog 查找 launch 模块的所有套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 6, des: "launch-module-全链路集成")]
        public static void LaunchModuleIntegration()
        {
            RunModuleIntegration("launch", "启动流程与宿主集成");
        }

        /// <summary>
        /// UI 模块集成测试：自动执行该模块所有框架套件（当前无框架套件，window-preconfig 属于业务）。
        /// UI module integration test: auto-execute all framework suites of this module (currently none; window-preconfig is business).
        /// 测试目的=验证 UI 模块的框架层集成链路。
        /// 实现手段=从 E2ESuiteCatalog 查找 ui 模块的框架套件，按层级排序后反射调用。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 7, des: "ui-module-全链路集成")]
        public static void UiModuleIntegration()
        {
            RunModuleIntegration("ui", "UI 框架");
        }

        /// <summary>
        /// 按模块自动执行集成测试：从 E2ESuiteCatalog 查找该模块的框架套件，按层级排序后逐一执行。
        /// Auto-execute integration tests by module: find all framework suites for the module from E2ESuiteCatalog,
        /// sort by tier priority, and invoke each in order.
        ///
        /// 仅聚合 Scope = "framework" 的套件，业务套件由业务侧的 BusinessModuleIntegrationEntry 处理。
        /// Only aggregates suites with Scope = "framework"; business suites are handled by BusinessModuleIntegrationEntry.
        ///
        /// 该方法消除了手动维护 RunSubSuite 调用的需求——新增子套件只需在 E2ESuiteCatalog
        /// 中添加条目，模块入口会自动发现并执行它。
        /// This method eliminates the need to manually maintain RunSubSuite calls — adding a new sub-suite
        /// only requires an entry in E2ESuiteCatalog, and the module entry will automatically discover and execute it.
        /// </summary>
        /// <param name="module">模块名称，对应 E2ESuiteCatalog 中的 Module 字段。Module name matching E2ESuiteCatalog.Module.</param>
        /// <param name="displayName">日志中显示的模块名称。Display name for logging.</param>
        private static void RunModuleIntegration(string module, string displayName)
        {
            Debug.Log($"[E2E] 测试目的={displayName}模块全链路集成 实现手段=从 E2ESuiteCatalog 查找模块框架套件并按层级排序反射调用");

            // 从目录查找该模块的所有框架套件，按层级优先级排序
            // Find all framework suites for the module from catalog, sorted by tier priority
            var suites = E2ESuiteCatalog.GetSuitesByModule(module);
            if (suites == null || suites.Length == 0)
            {
                Debug.LogWarning($"[E2E] 模块 {displayName} ({module}) 在 E2ESuiteCatalog 中未注册任何套件，跳过");
                return;
            }

            // 仅筛选框架套件，排除 module-integration 自身，按层级优先级排序
            // Only include framework scoped suites, exclude module-integration itself, sort by tier priority
            var orderedSuites = suites
                .Where(s => string.Equals(s.Scope, "framework", StringComparison.OrdinalIgnoreCase))
                .Where(s => !string.Equals(s.SuiteName, "module-integration", StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => TierPriority.TryGetValue(s.Tier, out var priority) ? priority : 99)
                .ThenBy(s => s.SuiteName)
                .ToList();

            if (orderedSuites.Count == 0)
            {
                Debug.Log($"[E2E] 模块 {displayName} ({module}): 无框架层套件，跳过（业务套件由 BusinessModuleIntegrationEntry 处理）");
                return;
            }

            Debug.Log($"[E2E] 模块 {displayName} ({module}): 发现 {orderedSuites.Count} 个子套件");

            var passCount = 0;
            foreach (var suite in orderedSuites)
            {
                RunSubSuite(suite.SuiteName, $"{displayName} / {suite.Description}");
                passCount++;
            }

            Debug.Log($"[E2E] {displayName} 模块全链路集成完成: {passCount}/{orderedSuites.Count} 个子套件全部通过");
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
        /// 全模块集成测试汇总入口：按顺序输出所有框架模块的覆盖范围。
        /// Full module integration summary entry: output coverage of all framework modules in order.
        /// 测试目的=验证所有框架模块的集成测试链路均已联通且全部通过。
        /// 实现手段=从 AllModuleEntries 读取模块列表，自动汇总框架层套件覆盖范围。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 999, des: "全模块集成汇总")]
        public static void AllModulesSummary()
        {
            Debug.Log("[E2E] 框架模块集成测试汇总:");

            // 从目录自动输出每个模块的框架层覆盖范围 / Auto-output each module's framework-scope coverage from catalog
            foreach (var entry in AllModuleEntries)
            {
                var suites = E2ESuiteCatalog.GetSuitesByModule(entry.module);
                var suiteNames = suites
                    .Where(s => string.Equals(s.Scope, "framework", StringComparison.OrdinalIgnoreCase))
                    .Where(s => !string.Equals(s.SuiteName, "module-integration", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.SuiteName);
                Debug.Log($"[E2E]   {entry.displayName} ({entry.module}): {string.Join(" + ", suiteNames)}");
            }

            Debug.Log("[E2E] 框架模块集成测试入口验证完成（业务套件由 BusinessModuleIntegrationEntry 管理）");
        }
    }
}
