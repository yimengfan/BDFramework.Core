using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Talos.E2E;
using UnityEngine;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架启动后总集成测试套件。
    /// Framework post-start integration test suite.
    /// 该套件面向真机启动后的 Talos E2E 场景，顺序巡检启动、配置、资源、SQLite 与日志持久化主链路，
    /// 用最少的外部前置条件确认 BDFramework 在真实运行态下的基础能力已经联通。
    /// This suite targets Talos E2E runs after player startup and inspects the startup, configuration, resource, SQLite, and log-persistence mainlines in order,
    /// confirming with minimal external prerequisites that BDFramework's foundational capabilities are wired together in the real runtime state.
    ///
    /// IL2CPP + HybridCLR 静态字段可见性说明：
    /// BDFramework.Test 是 AOT 程序集，直接引用 BDFramework.Core（热更程序集）。
    /// IL2CPP 会为 BDFramework.Core 中的类型生成独立的原生代码副本和静态字段存储，
    /// 而 HybridCLR 在运行时加载 BDFramework.Core 时会创建另一套独立的静态字段。
    /// 因此，所有对热更程序集的静态属性、静态方法和实例方法的访问都必须通过
    /// AppDomain 枚举 + 反射来进行，以确保读到 HybridCLR 解释器中的实际值。
    ///
    /// IL2CPP + HybridCLR static field visibility note:
    /// BDFramework.Test is an AOT assembly that directly references BDFramework.Core (hotfix assembly).
    /// IL2CPP generates its own native code copy and static field storage for types in BDFramework.Core,
    /// while HybridCLR creates a separate set of static fields when it loads BDFramework.Core at runtime.
    /// Therefore, all accesses to static properties, static methods, and instance methods in the hotfix assembly
    /// must go through AppDomain enumeration + reflection to ensure reading the actual values from the HybridCLR interpreter.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public static class FrameworkIntegrationTests
    {
        private const string IntegrationDatabaseName = "TalosFrameworkIntegration.db";
        private const int PlayerLogWaitTimeoutMs = 2000;

        /// <summary>
        /// 热更框架程序集名称，用于 AppDomain 枚举查找。
        /// Hotfix framework assembly name used for AppDomain enumeration lookup.
        /// </summary>
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";

        /// <summary>
        /// 验证框架在启动完成后已经进入可执行的运行上下文。
        /// Verify that the framework reaches an executable runtime context after startup completes.
        /// 真机模式下要求运行标记、版本号和托管类型发现均已就绪；编辑器仅保留无场景副作用的轻量检查。
        /// In player mode, this requires the runtime flag, framework version, and hosted-type discovery to be ready; the editor keeps only the light checks without scene-side side effects.
        ///
        /// IL2CPP 环境下必须通过反射读取热更程序集中的 BApplication.IsPlaying 和 ScriptLoder.GetAppDomainHostingTypes()，
        /// 而不是直接访问 AOT 编译副本的静态字段和方法。
        /// In IL2CPP environments, BApplication.IsPlaying and ScriptLoder.GetAppDomainHostingTypes() must be read
        /// through reflection from the hotfix assembly, not through direct access to AOT-compiled copy's static fields and methods.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "framework-integration", order: 1, des: "startup-context-ready")]
        public static void StartupPipelineReady()
        {
            if (!Application.isEditor)
            {
                // Player 构建模式下，必须通过反射读取热更程序集中的 BApplication.IsPlaying，
                // 因为 AOT 编译副本与 HybridCLR 解释器维护的静态字段是独立的。
                // In player builds, BApplication.IsPlaying must be read through reflection from the hotfix assembly,
                // because the AOT-compiled copy and the HybridCLR interpreter maintain independent static fields.
                var isPlaying = ReadHotfixStaticProperty<bool>(
                    "BDFramework.Core.Tools.BApplication", "IsPlaying", false);
                if (!isPlaying)
                {
                    throw new Exception("BApplication.IsPlaying is false after player startup (reflection read).");
                }
            }

            var frameworkVersion = BDFramework.BDLauncher.FrameworkVersion;
            if (string.IsNullOrWhiteSpace(frameworkVersion))
            {
                throw new Exception("Framework version is empty.");
            }

            // 通过反射调用热更程序集中的 ScriptLoder.GetAppDomainHostingTypes()，
            // 确保 HybridCLR 解释器中的托管类型列表能被正确读取。
            // Call ScriptLoder.GetAppDomainHostingTypes() through reflection from the hotfix assembly,
            // ensuring the hosted type list maintained by the HybridCLR interpreter is read correctly.
            var hostingTypes = InvokeHotfixStaticMethod<System.Collections.Generic.IEnumerable<Type>>(
                "BDFramework.ScriptLoder", "GetAppDomainHostingTypes", null)?.ToList();
            if (hostingTypes == null || hostingTypes.Count == 0)
            {
                throw new Exception("No hosted types were discovered for the framework runtime.");
            }

            var isPlayingValue = Application.isEditor
                ? true
                : ReadHotfixStaticProperty<bool>("BDFramework.Core.Tools.BApplication", "IsPlaying", false);
            Debug.Log(
                $"[E2E] Framework startup context ready. playing={isPlayingValue} version={frameworkVersion} hostedTypes={hostingTypes.Count}");
        }

        /// <summary>
        /// 验证框架配置入口在启动后可解析并返回基础配置对象。
        /// Verify that the framework configuration entry can resolve and return the base configuration object after startup.
        /// 该检查直接走 `GameConfigManager` 的公共入口，确认配置文本来源回退、处理器注册与对象反序列化已经协同可用。
        /// This check uses the public `GameConfigManager` entry so configuration-source fallback, processor registration, and object deserialization are proven to work together.
        ///
        /// IL2CPP 环境下必须通过反射访问热更程序集中的 GameConfigManager.Inst 和 GameBaseConfigProcessor.Config，
        /// 因为这些类型在 AOT 编译副本中可能拥有独立的静态字段。
        /// In IL2CPP environments, GameConfigManager.Inst and GameBaseConfigProcessor.Config must be accessed
        /// through reflection from the hotfix assembly, because these types may have independent static fields in the AOT-compiled copy.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "framework-integration", order: 2, des: "config-pipeline-ready")]
        public static void ConfigPipelineReady()
        {
            // 通过反射从热更程序集中获取 GameConfigManager.Inst.GetConfig<Config>()
            // Access GameConfigManager.Inst.GetConfig<Config>() through reflection from the hotfix assembly
            // Inst 是在 ManagerBase<T,V> 基类上声明的静态属性，IL2CPP 环境下 GetProperty 不会自动搜索基类，
            // 因此需要用 FlattenHierarchy 或手动遍历基类链来查找。
            // Inst is a static property declared on the ManagerBase<T,V> base class; GetProperty does not
            // automatically search base classes in IL2CPP, so we use FlattenHierarchy or walk the base chain.
            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var gameConfigManagerType = RequireType(hotfixAssembly, "BDFramework.Configure.GameConfigManager");
            var instProperty = gameConfigManagerType.GetProperty("Inst", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProperty == null)
            {
                // 回退：沿基类链手动查找 / Fallback: walk the base-class chain manually
                var currentType = gameConfigManagerType;
                while (currentType != null && instProperty == null)
                {
                    instProperty = currentType.GetProperty("Inst", BindingFlags.Public | BindingFlags.Static);
                    currentType = currentType.BaseType;
                }
            }
            if (instProperty == null)
            {
                throw new Exception("GameConfigManager.Inst property not found in hotfix assembly (searched base chain).");
            }
            var inst = instProperty.GetValue(null);
            if (inst == null)
            {
                throw new Exception("GameConfigManager.Inst is null in hotfix assembly.");
            }

            var configProcessorType = RequireType(hotfixAssembly, "BDFramework.Configure.GameBaseConfigProcessor");
            var configNestedType = configProcessorType.GetNestedType("Config");
            if (configNestedType == null)
            {
                throw new Exception("GameBaseConfigProcessor.Config nested type not found in hotfix assembly.");
            }

            var getConfigMethod = gameConfigManagerType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Instance);
            if (getConfigMethod == null)
            {
                throw new Exception("GameConfigManager.GetConfig method not found in hotfix assembly.");
            }
            var genericGetConfigMethod = getConfigMethod.MakeGenericMethod(configNestedType);
            var config = genericGetConfigMethod.Invoke(inst, null);
            if (config == null)
            {
                throw new Exception("GameBaseConfigProcessor.Config could not be resolved from hotfix assembly.");
            }

            // 通过反射读取 config.ClientVersionNum
            // Read config.ClientVersionNum through reflection
            var clientVersionNumProperty = configNestedType.GetProperty("ClientVersionNum", BindingFlags.Public | BindingFlags.Instance);
            var clientVersionNum = clientVersionNumProperty != null ? clientVersionNumProperty.GetValue(config) : "<unknown>";
            Debug.Log($"[E2E] Config pipeline ready. clientVersion={clientVersionNum}");
        }

        /// <summary>
        /// 验证资源主链路的基础公共接口在启动后可联通。
        /// Verify that the foundational public interfaces of the resource mainline are wired after startup.
        /// 该检查不依赖真实资源内容，只验证路径拼接、资源组缓存和常见查询入口可用，
        /// 这样能在不同真机包体上保持稳定，同时尽早发现资源系统初始化缺口。
        /// This check does not depend on concrete asset content and instead verifies path composition, asset-group caching, and common lookup entrypoints,
        /// keeping it stable across different player packages while still exposing resource-system initialization gaps early.
        ///
        /// IL2CPP 环境下必须通过反射访问热更程序集中的 BApplication 和 BResources 类型，
        /// 因为这些类型在 AOT 编译副本中拥有独立的静态字段。
        /// In IL2CPP environments, BApplication and BResources must be accessed through reflection
        /// from the hotfix assembly, because these types have independent static fields in the AOT-compiled copy.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "framework-integration", order: 3, des: "resource-pipeline-ready")]
        public static void ResourcePipelineReady()
        {
            // 通过反射读取热更程序集中的 BApplication 属性
            // Read BApplication properties through reflection from the hotfix assembly
            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, "BDFramework.Core.Tools.BApplication");

            var isPlayingValue = Application.isEditor
                ? true
                : ReadHotfixStaticProperty<bool>("BDFramework.Core.Tools.BApplication", "IsPlaying", false);

            var platform = isPlayingValue
                ? ReadHotfixStaticProperty<RuntimePlatform>("BDFramework.Core.Tools.BApplication", "RuntimePlatform", Application.platform)
                : Application.platform;
            var persistentDataPath = ReadHotfixStaticProperty<string>("BDFramework.Core.Tools.BApplication", "persistentDataPath", Application.persistentDataPath);

            // 通过反射调用热更程序集中的 BResources 静态方法
            // Call BResources static methods through reflection from the hotfix assembly
            var bResourcesType = RequireType(hotfixAssembly, "BDFramework.ResourceMgr.BResources");

            var getAssetsInfoPathMethod = bResourcesType.GetMethod("GetAssetsInfoPath", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var getServerAssetsVersionInfoPathMethod = bResourcesType.GetMethod("GetServerAssetsVersionInfoPath", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(RuntimePlatform) }, null);
            var addAssetsPathToGroupMethod = bResourcesType.GetMethod("AddAssetsPathToGroup", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string[]) }, null);
            var getAssetsPathByGroupMethod = bResourcesType.GetMethod("GetAssetsPathByGroup", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var clearAssetGroupMethod = bResourcesType.GetMethod("ClearAssetGroup", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var findShaderMethod = bResourcesType.GetMethod("FindShader", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);

            if (getAssetsInfoPathMethod == null || getServerAssetsVersionInfoPathMethod == null ||
                addAssetsPathToGroupMethod == null || getAssetsPathByGroupMethod == null ||
                clearAssetGroupMethod == null || findShaderMethod == null)
            {
                throw new Exception("One or more BResources methods not found in hotfix assembly.");
            }

            var assetsInfoPath = getAssetsInfoPathMethod.Invoke(null, new object[] { persistentDataPath }) as string;
            var versionInfoPath = getServerAssetsVersionInfoPathMethod.Invoke(null, new object[] { persistentDataPath, platform }) as string;
            if (string.IsNullOrWhiteSpace(assetsInfoPath) || string.IsNullOrWhiteSpace(versionInfoPath))
            {
                throw new Exception("Resource version paths were not composed correctly.");
            }

            var groupName = $"framework-integration-{Guid.NewGuid():N}";
            try
            {
                addAssetsPathToGroupMethod.Invoke(null, new object[] { groupName, new[] { "integration/a.prefab", "integration/b.mat" } });
                var groupedPaths = getAssetsPathByGroupMethod.Invoke(null, new object[] { groupName }) as string[];
                if (groupedPaths == null || groupedPaths.Length != 2)
                {
                    throw new Exception($"Unexpected grouped path count: {groupedPaths?.Length ?? 0}.");
                }

                var shader = findShaderMethod.Invoke(null, new object[] { "__Talos_E2E_FrameworkIntegration_NonExistent_Shader__" });
                Debug.Log(
                    $"[E2E] Resource pipeline ready. assetsInfoPath={assetsInfoPath} versionInfoPath={versionInfoPath} shader={(shader != null ? "found" : "null")}");
            }
            finally
            {
                clearAssetGroupMethod.Invoke(null, new object[] { groupName });
            }
        }

        /// <summary>
        /// 验证运行时 SQLite 能在真机环境里完成最小的建表、写入和查询闭环。
        /// Verify that runtime SQLite can complete the minimal create-table, write, and query loop in the player environment.
        /// 该检查使用独立的临时数据库文件，避免与框架默认连接池或业务数据库发生冲突。
        /// This check uses an isolated temporary database file so it does not conflict with the framework's default connection pool or business databases.
        ///
        /// IL2CPP 环境下不能使用泛型方法（如 CreateTable&lt;T&gt;、Table&lt;T&gt;、Insert），
        /// 因为 AOT 编译无法为热更程序集中的泛型实例化生成代码。
        /// 改用原始 SQL 语句完成建表、写入和查询闭环。
        /// In IL2CPP environments, generic methods (such as CreateTable&lt;T&gt;, Table&lt;T&gt;, Insert) cannot be used
        /// because AOT compilation cannot generate code for generic instantiations from hotfix assemblies.
        /// Instead, raw SQL statements are used to complete the create-table, insert, and query loop.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "framework-integration", order: 4, des: "sqlite-pipeline-ready")]
        public static void SqlitePipelineReady()
        {
            // 通过反射读取热更程序集中的 BApplication.persistentDataPath
            // Read BApplication.persistentDataPath through reflection from the hotfix assembly
            var persistentDataPath = ReadHotfixStaticProperty<string>(
                "BDFramework.Core.Tools.BApplication", "persistentDataPath", Application.persistentDataPath);
            var databasePath = IPath.Combine(persistentDataPath, IntegrationDatabaseName);
            var databaseDirectory = Path.GetDirectoryName(databasePath);

            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            // 通过反射创建 SQLiteConnection 和 SQLiteConnectionString 实例
            // Create SQLiteConnection and SQLiteConnectionString instances through reflection
            // SQLiteConnectionString 只暴露 2 参数、6 参数和 9 参数构造函数，
            // 3 参数版本不存在（9 参数构造函数虽有默认参数但 GetConstructor 需精确 Type[] 匹配）。
            // 使用 9 参数构造函数（与 BaseFlowHostRuntimeTests 相同的模式）。
            // SQLiteConnectionString only exposes 2-param, 6-param, and 9-param constructors;
            // the 3-param version does not exist as a standalone overload (the 9-param ctor has default
            // parameters but GetConstructor requires an exact Type[] match).
            // Use the 9-param constructor (same pattern as BaseFlowHostRuntimeTests).
            var sqliteConnectionType = RequireLoadedType("SQLite4Unity3d.SQLiteConnection");
            var sqliteConnectionStringType = RequireLoadedType("SQLite4Unity3d.SQLiteConnectionString");
            var sqliteOpenFlagsType = RequireLoadedType("SQLite4Unity3d.SQLiteOpenFlags");

            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, 2 | 4); // ReadWrite | Create
            var sqliteConnectionActionType = typeof(Action<>).MakeGenericType(sqliteConnectionType);
            var connectionStringCtor = sqliteConnectionStringType.GetConstructor(new[]
            {
                typeof(string), sqliteOpenFlagsType, typeof(bool), typeof(object),
                sqliteConnectionActionType, sqliteConnectionActionType,
                typeof(string), typeof(string), typeof(bool),
            });
            if (connectionStringCtor == null)
            {
                throw new Exception("SQLiteConnectionString(string, SQLiteOpenFlags, bool, object, Action<SQLiteConnection>, Action<SQLiteConnection>, string, string, bool) constructor not found.");
            }
            var connectionString = connectionStringCtor.Invoke(new object[]
            {
                databasePath, sqliteOpenFlags, true, null, null, null, null,
                "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff", true,
            });

            var connectionCtor = sqliteConnectionType.GetConstructor(new[] { sqliteConnectionStringType });
            if (connectionCtor == null)
            {
                throw new Exception("SQLiteConnection(SQLiteConnectionString) constructor not found.");
            }

            object connection = null;
            try
            {
                connection = connectionCtor.Invoke(new object[] { connectionString });

                // 使用原始 SQL 而非泛型方法，避免 IL2CPP AOT 泛型实例化错误。
                // Use raw SQL instead of generic methods to avoid IL2CPP AOT generic instantiation errors.
                var executeMethod = sqliteConnectionType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(object[]) }, null);
                if (executeMethod == null)
                {
                    throw new Exception("SQLiteConnection.Execute method not found.");
                }

                // 建表 / Create table
                executeMethod.Invoke(connection, new object[] { "CREATE TABLE IF NOT EXISTS TalosFrameworkIntegration (id INTEGER PRIMARY KEY, name TEXT NOT NULL);", Array.Empty<object>() });
                // 删除旧数据 / Delete old data
                executeMethod.Invoke(connection, new object[] { "DELETE FROM TalosFrameworkIntegration;", Array.Empty<object>() });
                // 插入 / Insert
                executeMethod.Invoke(connection, new object[] { "INSERT INTO TalosFrameworkIntegration (id, name) VALUES (1, ?);", new object[] { "framework-integration" } });

                // 使用非泛型 ExecuteScalarInt 方法查询计数（已在 Fix 3 中添加）
                // Use non-generic ExecuteScalarInt method for count query (added in Fix 3)
                var executeScalarIntMethod = sqliteConnectionType.GetMethod("ExecuteScalarInt", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(object[]) }, null);
                if (executeScalarIntMethod == null)
                {
                    throw new Exception("SQLiteConnection.ExecuteScalarInt method not found. Ensure Fix 3 (Convert.ChangeType + ExecuteScalarInt) is applied.");
                }
                var count = (int)executeScalarIntMethod.Invoke(connection, new object[] { "SELECT COUNT(*) FROM TalosFrameworkIntegration;", Array.Empty<object>() });
                if (count != 1)
                {
                    throw new Exception($"SQLite integration count mismatch: count={count}");
                }

                // 使用非泛型 ExecuteScalarInt 查询名称长度来确认数据存在
                // Use non-generic ExecuteScalarInt to query name length to confirm data exists
                var nameLength = (int)executeScalarIntMethod.Invoke(connection, new object[] { "SELECT LENGTH(name) FROM TalosFrameworkIntegration WHERE id = 1;", Array.Empty<object>() });
                if (nameLength != "framework-integration".Length)
                {
                    throw new Exception($"SQLite integration name length mismatch: expectedLength={("framework-integration".Length)} actualLength={nameLength}");
                }

                Debug.Log($"[E2E] SQLite pipeline ready. databasePath={databasePath}");
            }
            finally
            {
                (connection as IDisposable)?.Dispose();
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }

        /// <summary>
        /// 验证真机日志持久化链路在启动后已经可以落地文件。
        /// Verify that the player log-persistence pipeline can materialize a file after startup.
        /// 编辑器模式下该能力由条件编译主动关闭，因此这里只在真机环境做严格断言，
        /// 以免把编辑器专用运行方式误判为持久化故障。
        /// This capability is intentionally compiled out in the editor, so the strict assertion only runs in player mode,
        /// preventing editor-only execution behavior from being misclassified as a persistence failure.
        ///
        /// IL2CPP 环境下必须通过反射访问热更程序集中的 BDebug 静态属性和方法，
        /// 因为 BDebug 在 AOT 编译副本中拥有独立的静态字段。
        /// In IL2CPP environments, BDebug static properties and methods must be accessed through reflection
        /// from the hotfix assembly, because BDebug has independent static fields in the AOT-compiled copy.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "framework-integration", order: 5, des: "logging-pipeline-ready")]
        public static void LoggingPipelineReady()
        {
            if (Application.isEditor)
            {
                Debug.Log("[E2E] Editor mode skips player log persistence verification.");
                return;
            }

            // 通过反射读取热更程序集中的 BDebug.PlayerLogRootPath
            // Read BDebug.PlayerLogRootPath through reflection from the hotfix assembly
            // BDebug 位于全局命名空间，不是 BDFramework.Logs 命名空间。
            // BDebug is in the global namespace, not in the BDFramework.Logs namespace.
            var logRootPath = ReadHotfixStaticProperty<string>(
                "BDebug", "PlayerLogRootPath", "");
            if (string.IsNullOrWhiteSpace(logRootPath))
            {
                throw new Exception("Player log root path is empty.");
            }

            Debug.Log($"[E2E] FrameworkIntegration logging probe. root={logRootPath}");

            // 通过反射调用热更程序集中的 BDebug.FlushPlayerLogs()
            // Call BDebug.FlushPlayerLogs() through reflection from the hotfix assembly
            // BDebug 位于全局命名空间。
            // BDebug is in the global namespace.
            InvokeHotfixStaticMethod("BDebug", "FlushPlayerLogs", null);

            // 等待日志文件落盘 / Wait for log file to be written
            WaitFor(
                () =>
                {
                    if (!Directory.Exists(logRootPath)) return false;
                    var currentLogFilePath = ReadHotfixStaticProperty<string>(
                        "BDebug", "CurrentPlayerLogFilePath", "");
                    return !string.IsNullOrWhiteSpace(currentLogFilePath) && File.Exists(currentLogFilePath);
                },
                PlayerLogWaitTimeoutMs,
                $"Player log persistence did not create a log file under {logRootPath}.");

            var currentLogFile = ReadHotfixStaticProperty<string>(
                "BDebug", "CurrentPlayerLogFilePath", "");
            Debug.Log($"[E2E] Logging pipeline ready. file={currentLogFile}");
        }

        /// <summary>
        /// 在限定时间内轮询某个条件，直到条件满足或超时失败。
        /// Poll a condition within a bounded time window until it succeeds or times out.
        /// 该辅助方法用于真机异步日志写盘等非瞬时行为，避免把短暂的线程调度延迟误判为集成故障。
        /// This helper is used for non-instant player behaviors such as asynchronous log flushing so short scheduling delays are not mistaken for integration failures.
        /// </summary>
        /// <param name="predicate">需要等待命中的条件。</param>
        /// <param name="predicate">The condition that must become true.</param>
        /// <param name="timeoutMs">超时时间，单位毫秒。</param>
        /// <param name="timeoutMs">The timeout window in milliseconds.</param>
        /// <param name="failureMessage">超时后抛出的失败信息。</param>
        /// <param name="failureMessage">The failure message thrown after timeout.</param>
        private static void WaitFor(Func<bool> predicate, int timeoutMs, string failureMessage)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (predicate())
                {
                    return;
                }

                Thread.Sleep(50);
            }

            if (predicate())
            {
                return;
            }

            throw new Exception(failureMessage);
        }

        #region 热更程序集反射辅助方法 / Hotfix Assembly Reflection Helpers

        /// <summary>
        /// 通过 AppDomain 枚举从热更程序集中读取静态属性值。
        /// Read a static property value from the hotfix assembly through AppDomain enumeration.
        /// 在 IL2CPP + HybridCLR 环境中，AOT 编译副本与 HybridCLR 解释器维护独立的静态字段，
        /// 因此必须通过反射从热更程序集的运行时类型中读取实际值。
        /// In IL2CPP + HybridCLR environments, the AOT-compiled copy and the HybridCLR interpreter
        /// maintain independent static fields, so the actual value must be read through reflection
        /// from the hotfix assembly's runtime type.
        /// </summary>
        /// <typeparam name="T">属性值的期望类型。Expected type of the property value.</typeparam>
        /// <param name="typeName">类型的全限定名。Fully qualified type name.</param>
        /// <param name="propertyName">静态属性名。Static property name.</param>
        /// <param name="defaultValue">反射查找失败时的回退默认值。Fallback default value when reflection lookup fails.</param>
        /// <returns>热更程序集中静态属性的当前值。Current value of the static property from the hotfix assembly.</returns>
        private static T ReadHotfixStaticProperty<T>(string typeName, string propertyName, T defaultValue)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                {
                    var value = property.GetValue(null);
                    return value is T typedValue ? typedValue : defaultValue;
                }
            }

            Debug.LogWarning($"[E2E] 未在任何已加载程序集中找到 {typeName}.{propertyName}，使用默认值 {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// 通过 AppDomain 枚举从热更程序集中调用静态方法。
        /// Invoke a static method from the hotfix assembly through AppDomain enumeration.
        /// </summary>
        /// <typeparam name="T">方法返回值的期望类型。Expected type of the method return value.</typeparam>
        /// <param name="typeName">类型的全限定名。Fully qualified type name.</param>
        /// <param name="methodName">静态方法名。Static method name.</param>
        /// <param name="args">方法参数。Method arguments.</param>
        /// <returns>热更程序集中静态方法的返回值。Return value of the static method from the hotfix assembly.</returns>
        private static T InvokeHotfixStaticMethod<T>(string typeName, string methodName, object[] args)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var result = method.Invoke(null, args);
                    return result is T typedResult ? typedResult : default;
                }
            }

            Debug.LogWarning($"[E2E] 未在任何已加载程序集中找到 {typeName}.{methodName}()");
            return default;
        }

        /// <summary>
        /// 通过 AppDomain 枚举从热更程序集中调用无返回值的静态方法。
        /// Invoke a void static method from the hotfix assembly through AppDomain enumeration.
        /// </summary>
        /// <param name="typeName">类型的全限定名。Fully qualified type name.</param>
        /// <param name="methodName">静态方法名。Static method name.</param>
        /// <param name="args">方法参数。Method arguments.</param>
        private static void InvokeHotfixStaticMethod(string typeName, string methodName, object[] args)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, args);
                    return;
                }
            }

            Debug.LogWarning($"[E2E] 未在任何已加载程序集中找到 {typeName}.{methodName}()");
        }

        /// <summary>
        /// 从当前 AppDomain 中查找指定短名称的已装载程序集。
        /// Find a loaded assembly with the specified short name from the current AppDomain.
        /// </summary>
        /// <param name="assemblyName">目标程序集短名称。</param>
        /// <param name="assemblyName">Target short assembly name.</param>
        /// <returns>命中时返回程序集，否则抛出异常。</returns>
        /// <returns>Returns the assembly when found; otherwise throws.</returns>
        private static Assembly RequireLoadedAssembly(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            throw new Exception($"未发现已装载程序集: {assemblyName}");
        }

        /// <summary>
        /// 从指定程序集里获取目标类型。
        /// Get the target type from the specified assembly.
        /// </summary>
        /// <param name="assembly">已装载程序集。</param>
        /// <param name="typeName">目标类型全名。</param>
        /// <returns>命中时返回类型，否则抛出异常。</returns>
        /// <returns>Returns the type when found; otherwise throws.</returns>
        private static Type RequireType(Assembly assembly, string typeName)
        {
            var type = assembly.GetType(typeName);
            if (type == null)
            {
                throw new Exception($"未发现类型: {typeName}");
            }

            return type;
        }

        /// <summary>
        /// 在当前 AppDomain 中查找指定全名的已装载类型。
        /// Find a loaded type with the specified full name in the current AppDomain.
        /// </summary>
        /// <param name="typeName">目标类型全名。</param>
        /// <returns>命中时返回类型，否则抛出异常。</returns>
        /// <returns>Returns the type when found; otherwise throws.</returns>
        private static Type RequireLoadedType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new Exception($"未发现已装载类型: {typeName}");
        }

        #endregion
    }
}