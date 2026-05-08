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
    /// E2E 测试套件目录与模块归属注册表。
    /// E2E test suite catalog and module-ownership registry.
    ///
    /// 该类提供两个核心能力：
    /// 1. 声明式目录：所有 E2E 套件的模块归属、测试层级和描述集中维护在此，
    ///    避免套件信息散落在各个测试文件和 Playwright spec 中。
    /// 2. 运行时验证：通过反射扫描 [E2ETest] 属性，确认所有已注册套件都有对应的测试方法，
    ///    且所有带 [E2ETest] 的方法都已被目录覆盖。
    ///
    /// This class provides two core capabilities:
    /// 1. Declarative catalog: module ownership, test tier, and description of all E2E suites
    ///    are maintained here centrally, avoiding suite info scattered across test files and Playwright specs.
    /// 2. Runtime verification: reflectively scan [E2ETest] attributes to confirm all registered suites
    ///    have corresponding test methods and all [E2ETest]-annotated methods are covered by the catalog.
    ///
    /// 模块归属定义 / Module ownership definitions:
    /// - sqlite: SQLite 数据存储模块 / SQLite data storage module
    /// - asset: 资源加载与版本控制模块 / Asset loading and version control module
    /// - framework: 框架核心启动与配置模块 / Framework core startup and configuration module
    /// - service-store: 服务容器与依赖注入模块 / Service container and dependency injection module
    /// - utility: 工具函数与基础设施模块 / Utility functions and infrastructure module
    /// - launch: 启动流程与宿主集成模块 / Startup flow and host integration module
    ///
    /// 测试层级定义 / Test tier definitions:
    /// - contract: API 契约与行为约束验证 / API contract and behavioral constraint verification
    /// - business: 业务逻辑与流程验证 / Business logic and flow verification
    /// - integration: 跨模块集成与管线就绪验证 / Cross-module integration and pipeline readiness verification
    /// - api: 公共 API 接口契约验证 / Public API interface contract verification
    /// - host: 宿主环境集成验证 / Host environment integration verification
    /// </summary>
    [Preserve]
    public static class E2ESuiteCatalog
    {
        /// <summary>
        /// 套件描述条目。
        /// Suite description entry.
        /// </summary>
        public struct SuiteDescriptor
        {
            /// <summary>套件名称，对应 [E2ETest(suite: ...)] 中的 suite 参数。Suite name matching [E2ETest(suite: ...)].</summary>
            public string SuiteName;
            /// <summary>所属模块。Owning module.</summary>
            public string Module;
            /// <summary>测试层级（contract/business/integration/api/host）。Test tier.</summary>
            public string Tier;
            /// <summary>
            /// 套件归属范围："framework" 表示框架测试（维护在 BDFramework.Test），"business" 表示业务测试（维护在业务侧 Assets/Code/）。
            /// Suite scope: "framework" for framework tests (maintained in BDFramework.Test), "business" for business tests (maintained in Assets/Code/).
            /// </summary>
            public string Scope;
            /// <summary>中文描述。Chinese description.</summary>
            public string Description;
            /// <summary>英文描述。English description.</summary>
            public string DescriptionEn;
        }

        /// <summary>
        /// 全部已注册套件的目录。
        /// Catalog of all registered suites.
        /// 新增套件时必须在此添加条目，确保目录与实际代码保持同步。
        /// When adding a new suite, an entry must be added here to keep the catalog in sync with actual code.
        /// </summary>
        private static readonly SuiteDescriptor[] AllSuites = new[]
        {
            // SQLite 模块 / SQLite module (framework)
            new SuiteDescriptor { SuiteName = "sqlite",              Module = "sqlite",        Tier = "business",     Scope = "framework", Description = "SQLite 基础操作",            DescriptionEn = "SQLite basic operations" },
            new SuiteDescriptor { SuiteName = "sqlite-contract",     Module = "sqlite",        Tier = "contract",     Scope = "framework", Description = "SQLite 契约验证",            DescriptionEn = "SQLite contract verification" },
            new SuiteDescriptor { SuiteName = "sqlite-business",     Module = "sqlite",        Tier = "business",     Scope = "framework", Description = "SQLite 业务逻辑",            DescriptionEn = "SQLite business logic" },
            new SuiteDescriptor { SuiteName = "sqlite-integration",  Module = "sqlite",        Tier = "integration",  Scope = "framework", Description = "SQLite 集成测试",            DescriptionEn = "SQLite integration tests" },

            // Asset 模块 — 框架套件 / Asset module — framework suites
            new SuiteDescriptor { SuiteName = "asset-load",          Module = "asset",         Tier = "integration",  Scope = "framework", Description = "资源加载全链路",              DescriptionEn = "Asset load full pipeline" },
            new SuiteDescriptor { SuiteName = "asset-traversal",     Module = "asset",         Tier = "business",     Scope = "framework", Description = "资源遍历",                   DescriptionEn = "Asset traversal" },
            new SuiteDescriptor { SuiteName = "version-controller-api", Module = "asset",      Tier = "api",          Scope = "framework", Description = "版本控制器 API 契约",         DescriptionEn = "Version controller API contract" },

            // Asset 模块 — 业务套件 / Asset module — business suites
            new SuiteDescriptor { SuiteName = "asset-business",      Module = "asset",         Tier = "business",     Scope = "business",  Description = "AssetBundle 业务",           DescriptionEn = "AssetBundle business" },
            new SuiteDescriptor { SuiteName = "version-business",    Module = "asset",         Tier = "business",     Scope = "business",  Description = "版本控制器业务",              DescriptionEn = "Version controller business" },
            new SuiteDescriptor { SuiteName = "download-prep",       Module = "asset",         Tier = "business",     Scope = "business",  Description = "下载准备",                    DescriptionEn = "Download preparation" },
            new SuiteDescriptor { SuiteName = "download-update",     Module = "asset",         Tier = "business",     Scope = "business",  Description = "下载更新",                    DescriptionEn = "Download update" },

            // Framework 模块 / Framework module (framework)
            new SuiteDescriptor { SuiteName = "framework-contract",       Module = "framework", Tier = "contract",     Scope = "framework", Description = "框架契约验证",               DescriptionEn = "Framework contract verification" },
            new SuiteDescriptor { SuiteName = "framework-core-business",  Module = "framework", Tier = "business",     Scope = "framework", Description = "框架核心业务",               DescriptionEn = "Framework core business" },
            new SuiteDescriptor { SuiteName = "framework-integration",    Module = "framework", Tier = "integration",  Scope = "framework", Description = "框架集成管线",               DescriptionEn = "Framework integration pipeline" },

            // ServiceStore 模块 / ServiceStore module (framework)
            new SuiteDescriptor { SuiteName = "service-store-api",   Module = "service-store", Tier = "api",          Scope = "framework", Description = "ServiceStore API 契约",      DescriptionEn = "ServiceStore API contract" },

            // Utility 模块 / Utility module (framework)
            new SuiteDescriptor { SuiteName = "utility-api",         Module = "utility",       Tier = "api",          Scope = "framework", Description = "工具函数 API 契约",          DescriptionEn = "Utility API contract" },
            new SuiteDescriptor { SuiteName = "object-pool-api",     Module = "utility",       Tier = "api",          Scope = "framework", Description = "对象池 API 契约",            DescriptionEn = "Object pool API contract" },
            new SuiteDescriptor { SuiteName = "logs-contract",       Module = "utility",       Tier = "contract",     Scope = "framework", Description = "日志契约验证",               DescriptionEn = "Logs contract verification" },
            new SuiteDescriptor { SuiteName = "csv-contract",        Module = "utility",       Tier = "contract",     Scope = "framework", Description = "CSV 契约验证",              DescriptionEn = "CSV contract verification" },

            // Launch 模块 / Launch module (framework)
            new SuiteDescriptor { SuiteName = "launch",              Module = "launch",        Tier = "integration",  Scope = "framework", Description = "启动流程",                    DescriptionEn = "Startup flow" },
            new SuiteDescriptor { SuiteName = "host-launch",         Module = "launch",        Tier = "host",         Scope = "framework", Description = "宿主启动",                    DescriptionEn = "Host launch" },
            new SuiteDescriptor { SuiteName = "host-asset-load",     Module = "launch",        Tier = "host",         Scope = "framework", Description = "宿主资源加载",               DescriptionEn = "Host asset load" },
            new SuiteDescriptor { SuiteName = "host-framework-integration", Module = "launch", Tier = "host",         Scope = "framework", Description = "宿主框架集成",               DescriptionEn = "Host framework integration" },

            // UI 模块 — 业务套件 / UI module — business suite
            new SuiteDescriptor { SuiteName = "window-preconfig",    Module = "ui",            Tier = "business",     Scope = "business",  Description = "预配置界面",                  DescriptionEn = "Window preconfiguration" },

            // 模块集成入口 / Module integration entry
            new SuiteDescriptor { SuiteName = "module-integration",  Module = "meta",          Tier = "integration",  Scope = "framework", Description = "框架模块集成测试入口",        DescriptionEn = "Framework module integration entry" },
            new SuiteDescriptor { SuiteName = "business-integration", Module = "meta",         Tier = "integration",  Scope = "business",  Description = "业务集成测试入口",            DescriptionEn = "Business integration entry" },
        };

        /// <summary>
        /// 获取全部已注册套件的目录列表。
        /// Get the catalog list of all registered suites.
        /// </summary>
        /// <returns>全部套件描述条目。All suite descriptors.</returns>
        public static SuiteDescriptor[] GetAllSuites()
        {
            return AllSuites;
        }

        /// <summary>
        /// 按模块获取套件列表。
        /// Get suites by module.
        /// </summary>
        /// <param name="module">模块名称。Module name.</param>
        /// <returns>属于该模块的套件列表。Suites belonging to the module.</returns>
        public static SuiteDescriptor[] GetSuitesByModule(string module)
        {
            return AllSuites.Where(s => string.Equals(s.Module, module, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>
        /// 按测试层级获取套件列表。
        /// Get suites by test tier.
        /// </summary>
        /// <param name="tier">测试层级。Test tier.</param>
        /// <returns>属于该层级的套件列表。Suites belonging to the tier.</returns>
        public static SuiteDescriptor[] GetSuitesByTier(string tier)
        {
            return AllSuites.Where(s => string.Equals(s.Tier, tier, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>
        /// 获取所有模块名称。
        /// Get all module names.
        /// </summary>
        /// <returns>去重后的模块名称列表。Distinct module names.</returns>
        public static string[] GetAllModules()
        {
            return AllSuites.Select(s => s.Module).Distinct().OrderBy(m => m).ToArray();
        }

        /// <summary>
        /// 按套件归属范围获取套件列表。
        /// Get suites by scope ("framework" or "business").
        /// </summary>
        /// <param name="scope">套件归属范围："framework" 或 "business"。Scope: "framework" or "business".</param>
        /// <returns>属于该范围的套件列表。Suites belonging to the scope.</returns>
        public static SuiteDescriptor[] GetSuitesByScope(string scope)
        {
            return AllSuites.Where(s => string.Equals(s.Scope, scope, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>
        /// 获取所有框架套件（Scope = "framework"）。
        /// Get all framework suites.
        /// </summary>
        /// <returns>框架套件列表。Framework suites.</returns>
        public static SuiteDescriptor[] GetFrameworkSuites()
        {
            return GetSuitesByScope("framework");
        }

        /// <summary>
        /// 获取所有业务套件（Scope = "business"）。
        /// Get all business suites.
        /// </summary>
        /// <returns>业务套件列表。Business suites.</returns>
        public static SuiteDescriptor[] GetBusinessSuites()
        {
            return GetSuitesByScope("business");
        }

        /// <summary>
        /// 验证目录完整性：确认所有目录中注册的套件都有对应的 [E2ETest] 方法。
        /// Verify catalog integrity: confirm all registered suites have corresponding [E2ETest] methods.
        /// 测试目的=确保 E2E 套件目录与实际代码保持同步。
        /// 实现手段=通过反射扫描所有 [E2ETest] 属性，与目录中注册的套件名称做双向比对。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "module-integration", order: 998, des: "套件目录完整性验证")]
        public static void VerifyCatalogIntegrity()
        {
            Debug.Log("[E2E] 测试目的=套件目录完整性验证 实现手段=反射扫描 [E2ETest] 属性与目录双向比对");

            // 收集运行时所有 [E2ETest] 方法对应的套件名称
            // Collect all suite names from [E2ETest] attributes at runtime
            var runtimeSuites = new HashSet<string>();
            var e2eTestAttributeType = typeof(E2ETestAttribute);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic))
                        {
                            var attr = method.GetCustomAttributes(e2eTestAttributeType, false);
                            if (attr != null && attr.Length > 0)
                            {
                                var e2eAttr = attr[0] as E2ETestAttribute;
                                if (e2eAttr != null && !string.IsNullOrEmpty(e2eAttr.Suite))
                                {
                                    runtimeSuites.Add(e2eAttr.Suite);
                                }
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 某些程序集可能无法加载所有类型，跳过
                    // Some assemblies may not load all types; skip
                }
            }

            // 收集目录中注册的套件名称
            // Collect suite names registered in the catalog
            var catalogSuites = new HashSet<string>(AllSuites.Select(s => s.SuiteName));

            // 检查目录中有但运行时没有的套件（目录过期）
            // Check suites in catalog but not at runtime (stale catalog)
            var staleCatalog = catalogSuites.Except(runtimeSuites).ToList();
            if (staleCatalog.Count > 0)
            {
                Debug.LogWarning($"[E2E] 目录中存在但运行时未发现的套件: {string.Join(", ", staleCatalog)}");
            }

            // 检查运行时有但目录中没有的套件（目录缺失）
            // Check suites at runtime but not in catalog (missing catalog entry)
            var missingCatalog = runtimeSuites.Except(catalogSuites).ToList();
            if (missingCatalog.Count > 0)
            {
                throw new Exception(
                    $"运行时发现但目录中未注册的套件: {string.Join(", ", missingCatalog)}。" +
                    $"请将这些套件添加到 E2ESuiteCatalog.AllSuites 中。");
            }

            Debug.Log($"[E2E] 套件目录完整性验证通过: 目录={catalogSuites.Count} 运行时={runtimeSuites.Count} 缺失={missingCatalog.Count} 过期={staleCatalog.Count}");
        }

        /// <summary>
        /// 输出套件目录汇总。
        /// Output suite catalog summary.
        /// </summary>
        public static void PrintCatalogSummary()
        {
            Debug.Log("[E2E] ===== E2E 套件目录汇总 =====");
            foreach (var module in GetAllModules())
            {
                var suites = GetSuitesByModule(module);
                Debug.Log($"[E2E] 模块: {module} ({suites.Length} 个套件)");
                foreach (var suite in suites)
                {
                    Debug.Log($"[E2E]   [{suite.Tier}] {suite.SuiteName} - {suite.Description}");
                }
            }
            Debug.Log($"[E2E] ===== 总计: {AllSuites.Length} 个套件 =====");
        }
    }
}
