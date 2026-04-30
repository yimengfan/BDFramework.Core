using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 宿主侧基础系统 E2E 测试套件。
    /// Host-owned foundational-system E2E suite.
    /// 该套件专门服务于 step_01 BaseFlow 母包验证，通过宿主可见的反射入口巡检 AB 资源接口与 SQLite 最小读写闭环，
    /// 避免为了基础系统回归重新把依赖热更层的 Runtime.Test 程序集根引用回母包。
    /// This suite exists specifically for the step_01 BaseFlow host-package validation and inspects the AssetBundle-facing resource APIs and the minimal SQLite read-write loop through host-visible reflection entrypoints,
    /// avoiding the need to root the Runtime.Test assembly that depends on the hotfix layer back into the base package.
    /// 使用说明：Talos BaseFlow 会按 suite 名称分别执行 `host-asset-load` 与 `host-framework-integration`，
    /// 因此这里的每个入口都必须保持宿主程序集可见并显式 Preserve。
    /// Usage note: Talos BaseFlow executes `host-asset-load` and `host-framework-integration` by suite name,
    /// so each entrypoint here must stay visible from the host assembly and be explicitly preserved.
    /// </summary>
    [Preserve]
    public static class BaseFlowHostRuntimeTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";
        private const string BResourcesTypeName = "BDFramework.ResourceMgr.BResources";
        private const string SqliteConnectionTypeName = "SQLite4Unity3d.SQLiteConnection";
        private const string SqliteConnectionStringTypeName = "SQLite4Unity3d.SQLiteConnectionString";
        private const string SqliteOpenFlagsTypeName = "SQLite4Unity3d.SQLiteOpenFlags";
        private const int SqliteReadWriteCreateOpenFlagsValue = 2 | 4;
        private const string SqliteProbeDirectoryName = "bdframework-host-sqlite";
        private const string SqliteProbeValue = "framework-integration";
        private const string SqliteDefaultDateTimeStringFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

        /// <summary>
        /// SQLite 探针候选路径描述对象。
        /// Descriptor for a candidate SQLite probe path.
        /// 该类型把“为什么选这条路径”和“真正要传给 SQLite 的数据库文件路径”绑定在一起，
        /// 让运行时日志与 Editor 回归都能稳定校验 Android/Windows 的选路与回退顺序。
        /// This type binds together the selection reason and the concrete database-file path passed into SQLite,
        /// so runtime logs and editor regressions can validate the Android and Windows selection plus fallback order consistently.
        /// 使用说明：候选项按优先级顺序生成，探针会按顺序尝试并在 native open 失败时继续回退到下一项。
        /// Usage note: candidates are generated in priority order, and the probe tries them sequentially while falling back to the next entry after a native open failure.
        /// </summary>
        private sealed class SqliteProbePathOption
        {
            /// <summary>
            /// 初始化 SQLite 探针候选路径描述对象。
            /// Initialize a SQLite probe-path descriptor.
            /// </summary>
            /// <param name="selectionReason">候选路径的选路原因。The selection reason for the candidate path.</param>
            /// <param name="databasePath">将要交给 SQLite 的数据库文件路径。The database-file path that will be passed into SQLite.</param>
            public SqliteProbePathOption(string selectionReason, string databasePath)
            {
                SelectionReason = selectionReason;
                DatabasePath = databasePath;
            }

            /// <summary>
            /// 当前候选项的选路原因。
            /// The selection reason for the current candidate.
            /// </summary>
            public string SelectionReason { get; }

            /// <summary>
            /// 当前候选项的数据库文件路径。
            /// The database-file path for the current candidate.
            /// </summary>
            public string DatabasePath { get; }
        }

        /// <summary>
        /// 验证宿主可以通过热更资源入口联通 AB 资源系统的基础公共接口。
        /// Verify that the host can reach the foundational public APIs of the AssetBundle resource system through the hotfix resource entrypoints.
        /// 该检查覆盖资源组缓存、版控路径拼接与 Shader 查询三个稳定入口，
        /// 用最小反射调用确认母包已经具备驱动资源主链路的必要信号。
        /// This check covers asset-group caching, version-path composition, and shader lookup as stable entrypoints,
        /// confirming with minimal reflection calls that the host package has the signals needed to drive the resource mainline.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "host-asset-load", order: 1, des: "验证宿主可联通 AB 资源基础接口")]
        public static void AssetBundleResourceApisReachable()
        {
            Debug.Log("[E2E] 测试目的=验证宿主可联通 AB 资源基础接口 实现手段=反射调用 BResources 的资源组、版控路径与 Shader 查询公共 API");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);
            var addAssetsPathToGroupMethod = RequireStaticMethod(
                bResourcesType,
                "AddAssetsPathToGroup",
                typeof(string),
                typeof(string[]));
            var getAssetsPathByGroupMethod = RequireStaticMethod(
                bResourcesType,
                "GetAssetsPathByGroup",
                typeof(string));
            var clearAssetGroupMethod = RequireStaticMethod(
                bResourcesType,
                "ClearAssetGroup",
                typeof(string));
            var getAssetsInfoPathMethod = RequireStaticMethod(
                bResourcesType,
                "GetAssetsInfoPath",
                typeof(string));
            var getServerAssetsVersionInfoPathMethod = RequireStaticMethod(
                bResourcesType,
                "GetServerAssetsVersionInfoPath",
                typeof(string),
                typeof(RuntimePlatform));
            var findShaderMethod = RequireStaticMethod(
                bResourcesType,
                "FindShader",
                typeof(string));

            var groupName = $"talos-baseflow-host-{Guid.NewGuid():N}";
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);
            var frameworkPersistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");
            try
            {
                Debug.Log($"[E2E] Asset probe phase=group-cache-add group={groupName}");
                InvokeStaticMethod(
                    addAssetsPathToGroupMethod,
                    "BResources.AddAssetsPathToGroup",
                    groupName,
                    new[]
                    {
                        "talos/baseflow/host/a.prefab",
                        "talos/baseflow/host/b.mat",
                    });

                Debug.Log($"[E2E] Asset probe phase=group-cache-read group={groupName}");
                var groupedPaths = InvokeStaticMethod(
                    getAssetsPathByGroupMethod,
                    "BResources.GetAssetsPathByGroup",
                    groupName) as string[];
                if (groupedPaths == null || groupedPaths.Length != 2)
                {
                    throw new Exception($"资源组公共接口返回异常，数量={groupedPaths?.Length ?? 0}");
                }

                Debug.Log($"[E2E] Asset probe phase=version-path root={frameworkPersistentDataPath} platform={Application.platform}");
                var assetsInfoPath = InvokeStaticMethod(
                    getAssetsInfoPathMethod,
                    "BResources.GetAssetsInfoPath",
                    frameworkPersistentDataPath) as string;
                var versionInfoPath = InvokeStaticMethod(
                    getServerAssetsVersionInfoPathMethod,
                    "BResources.GetServerAssetsVersionInfoPath",
                    frameworkPersistentDataPath,
                    Application.platform) as string;
                if (string.IsNullOrWhiteSpace(assetsInfoPath) || string.IsNullOrWhiteSpace(versionInfoPath))
                {
                    throw new Exception("资源版控路径拼接结果为空");
                }

                Debug.Log("[E2E] Asset probe phase=shader-lookup name=__Talos_BaseFlow_Host_NonExistent_Shader__");
                InvokeStaticMethod(
                    findShaderMethod,
                    "BResources.FindShader",
                    "__Talos_BaseFlow_Host_NonExistent_Shader__");
                Debug.Log(
                    $"[E2E] 宿主资源接口联通完成: groupedPaths={groupedPaths.Length} assetsInfoPath={assetsInfoPath} versionInfoPath={versionInfoPath}");
            }
            finally
            {
                Debug.Log($"[E2E] Asset probe phase=group-cache-clear group={groupName}");
                InvokeStaticMethod(
                    clearAssetGroupMethod,
                    "BResources.ClearAssetGroup",
                    groupName);
            }
        }

        /// <summary>
        /// 验证宿主可以通过 SQLite 公开入口完成最小建表、写入与查询闭环。
        /// Verify that the host can complete the minimal create-table, insert, and query loop through the public SQLite entrypoints.
        /// 该检查直接反射调用运行中已装载程序集里的 SQLiteConnection，
        /// 不把 SQLite 类型静态链接进宿主测试程序集，从而保持母包依赖边界稳定。
        /// This check directly reflects into the SQLiteConnection type from the assemblies that are already loaded at runtime,
        /// avoiding a static link from the host test assembly into the SQLite types and keeping the base-package dependency boundary stable.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "host-framework-integration", order: 1, des: "验证宿主可完成 SQLite 最小读写闭环")]
        public static void SqliteRoundTripReachable()
        {
            Debug.Log("[E2E] 测试目的=验证宿主可完成 SQLite 最小读写闭环 实现手段=反射创建 SQLiteConnection 并执行建表、写入、查询");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);
            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionStringType = RequireLoadedType(SqliteConnectionStringTypeName);
            var sqliteOpenFlagsType = RequireLoadedType(SqliteOpenFlagsTypeName);
            var sqliteConnectionActionType = typeof(Action<>).MakeGenericType(sqliteConnectionType);
            var sqliteConnectionStringConstructor = sqliteConnectionStringType.GetConstructor(new[]
            {
                typeof(string),
                sqliteOpenFlagsType,
                typeof(bool),
                typeof(object),
                sqliteConnectionActionType,
                sqliteConnectionActionType,
                typeof(string),
                typeof(string),
                typeof(bool),
            });
            if (sqliteConnectionStringConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnectionString(string, SQLiteOpenFlags, bool, object, Action<SQLiteConnection>, Action<SQLiteConnection>, string, string, bool) 构造入口");
            }

            var sqliteConnectionConstructor = sqliteConnectionType.GetConstructor(new[] { sqliteConnectionStringType });
            if (sqliteConnectionConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnection(SQLiteConnectionString) 构造入口");
            }

            var executeMethod = RequireInstanceMethod(
                sqliteConnectionType,
                "Execute",
                typeof(string),
                typeof(object[]));
            // IL2CPP 环境下不能使用 MakeGenericMethod 构造 ExecuteScalar<string>，
            // 因为 AOT 编译无法为热更程序集中的泛型实例化生成代码。
            // 使用非泛型 ExecuteScalarInt 方法替代 MakeGenericMethod(typeof(int))。
            // In IL2CPP environments, MakeGenericMethod for ExecuteScalar<T> cannot be used
            // because AOT compilation cannot generate code for generic instantiations from hotfix assemblies.
            // Use the non-generic ExecuteScalarInt method instead of MakeGenericMethod(typeof(int)).
            var executeScalarIntMethod = sqliteConnectionType.GetMethod(
                "ExecuteScalarInt",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(object[]) },
                null);
            if (executeScalarIntMethod == null)
            {
                throw new Exception("未发现公开实例方法: SQLiteConnection.ExecuteScalarInt —— 请确保 Fix 3 (Convert.ChangeType + ExecuteScalarInt) 已提交");
            }

            var frameworkPersistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");
            var applicationPersistentDataPath = Application.persistentDataPath;
            var temporaryCachePath = Application.temporaryCachePath;
            var databaseFileName = $"talos-baseflow-host-{Guid.NewGuid():N}.db";
            var androidContextDatabasePath = string.Empty;
            var androidInternalFilesPath = string.Empty;
            var androidInternalCachePath = string.Empty;

            if (Application.platform == RuntimePlatform.Android)
            {
                TryReadAndroidContextDatabasePath(databaseFileName, out androidContextDatabasePath);
                TryReadAndroidContextDirectory("getFilesDir", out androidInternalFilesPath);
                TryReadAndroidContextDirectory("getCacheDir", out androidInternalCachePath);
            }

            var sqliteProbePathOptions = BuildSqliteProbePathOptions(
                Application.platform,
                frameworkPersistentDataPath,
                applicationPersistentDataPath,
                temporaryCachePath,
                databaseFileName,
                androidContextDatabasePath,
                androidInternalFilesPath,
                androidInternalCachePath);
            var sqliteProbeCandidates = string.Join(
                " || ",
                sqliteProbePathOptions.Select(option => $"{option.SelectionReason}:{option.DatabasePath}"));

            object sqliteConnection = null;
            string normalizedDatabasePath = string.Empty;
            try
            {
                var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
                Debug.Log(
                    $"[E2E] SQLite probe phase=path-candidates frameworkPersistentDataPath={frameworkPersistentDataPath} applicationPersistentDataPath={applicationPersistentDataPath} temporaryCachePath={temporaryCachePath} androidContextDatabasePath={androidContextDatabasePath} androidInternalFilesPath={androidInternalFilesPath} androidInternalCachePath={androidInternalCachePath} candidates={sqliteProbeCandidates}");
                sqliteConnection = ExecuteWithSqliteProbePathFallback(
                    sqliteProbePathOptions,
                    databasePathCandidate =>
                    {
                        var candidateNormalizedDatabasePath = NormalizePathForWindowsFileApis(databasePathCandidate);
                        var databaseDirectory = Path.GetDirectoryName(candidateNormalizedDatabasePath);
                        try
                        {
                            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
                            {
                                Directory.CreateDirectory(databaseDirectory);
                            }

                            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
                            {
                                throw new Exception($"SQLite 目录创建失败: {databaseDirectory}");
                            }

                            if (File.Exists(candidateNormalizedDatabasePath))
                            {
                                Debug.Log($"[E2E] SQLite probe phase=delete-existing-file databasePath={candidateNormalizedDatabasePath}");
                                File.Delete(candidateNormalizedDatabasePath);
                            }

                            EnsureSqliteProbeFileExists(candidateNormalizedDatabasePath);
                            Debug.Log($"[E2E] SQLite probe phase=build-connection-string databasePath={databasePathCandidate} fileApiPath={candidateNormalizedDatabasePath} openFlags={sqliteOpenFlags}");
                            var sqliteConnectionString = InvokeConstructor(
                                sqliteConnectionStringConstructor,
                                "SQLiteConnectionString..ctor",
                                databasePathCandidate,
                                sqliteOpenFlags,
                                true,
                                null,
                                null,
                                null,
                                null,
                                SqliteDefaultDateTimeStringFormat,
                                true);

                            Debug.Log($"[E2E] SQLite probe phase=open databasePath={databasePathCandidate} fileApiPath={candidateNormalizedDatabasePath}");
                            var openedConnection = InvokeConstructor(
                                sqliteConnectionConstructor,
                                "SQLiteConnection..ctor",
                                sqliteConnectionString);
                            normalizedDatabasePath = candidateNormalizedDatabasePath;
                            return openedConnection;
                        }
                        catch
                        {
                            if (File.Exists(candidateNormalizedDatabasePath))
                            {
                                File.Delete(candidateNormalizedDatabasePath);
                            }

                            throw;
                        }
                    },
                    out var sqliteOpenPath,
                    out var sqlitePersistentRootReason);
                Debug.Log($"[E2E] SQLite probe phase=path-selected sqlitePersistentRootReason={sqlitePersistentRootReason} sqliteOpenPath={sqliteOpenPath} fileApiPath={normalizedDatabasePath}");

                Debug.Log("[E2E] SQLite probe phase=configure-temp-store value=MEMORY");
                InvokeInstanceMethod(
                    sqliteConnection,
                    executeMethod,
                    "SQLiteConnection.Execute(pragma-temp-store)",
                    "PRAGMA temp_store=MEMORY;",
                    Array.Empty<object>());

                Debug.Log("[E2E] SQLite probe phase=configure-journal-mode value=MEMORY");
                InvokeInstanceMethod(
                    sqliteConnection,
                    executeMethod,
                    "SQLiteConnection.Execute(pragma-journal-mode)",
                    "PRAGMA journal_mode=MEMORY;",
                    Array.Empty<object>());
                Debug.Log("[E2E] SQLite probe phase=journal-mode-ready");

                Debug.Log("[E2E] SQLite probe phase=create-table");
                InvokeInstanceMethod(
                    sqliteConnection,
                    executeMethod,
                    "SQLiteConnection.Execute(create-table)",
                    "CREATE TABLE IF NOT EXISTS TalosBaseFlowHostSqlite (id INTEGER PRIMARY KEY, name TEXT NOT NULL);",
                    Array.Empty<object>());
                Debug.Log("[E2E] SQLite probe phase=delete-existing");
                InvokeInstanceMethod(
                    sqliteConnection,
                    executeMethod,
                    "SQLiteConnection.Execute(delete-existing)",
                    "DELETE FROM TalosBaseFlowHostSqlite;",
                    Array.Empty<object>());
                Debug.Log($"[E2E] SQLite probe phase=insert probeValue={SqliteProbeValue}");
                InvokeInstanceMethod(
                    sqliteConnection,
                    executeMethod,
                    "SQLiteConnection.Execute(insert)",
                    "INSERT INTO TalosBaseFlowHostSqlite (id, name) VALUES (1, ?);",
                    new object[] { SqliteProbeValue });

                Debug.Log("[E2E] SQLite probe phase=query-count");
                var count = (int)InvokeInstanceMethod(
                    sqliteConnection,
                    executeScalarIntMethod,
                    "SQLiteConnection.ExecuteScalarInt(count)",
                    "SELECT COUNT(*) FROM TalosBaseFlowHostSqlite;",
                    Array.Empty<object>());
                if (count != 1)
                {
                    throw new Exception($"SQLite 写入条数异常: count={count}");
                }

                Debug.Log("[E2E] SQLite probe phase=query-name");
                // 使用 ExecuteScalarInt 查询名称长度来确认数据存在，避免 MakeGenericMethod(typeof(string))
                // Use ExecuteScalarInt to query name length to confirm data exists, avoiding MakeGenericMethod(typeof(string))
                var nameLength = (int)InvokeInstanceMethod(
                    sqliteConnection,
                    executeScalarIntMethod,
                    "SQLiteConnection.ExecuteScalarInt(name-length)",
                    "SELECT LENGTH(name) FROM TalosBaseFlowHostSqlite WHERE id = 1;",
                    Array.Empty<object>());
                if (nameLength != SqliteProbeValue.Length)
                {
                    throw new Exception($"SQLite 查询名称长度异常: expectedLength={SqliteProbeValue.Length} actualLength={nameLength}");
                }

                Debug.Log($"[E2E] 宿主 SQLite 读写闭环完成: databasePath={normalizedDatabasePath}");
            }
            finally
            {
                (sqliteConnection as IDisposable)?.Dispose();
                if (File.Exists(normalizedDatabasePath))
                {
                    File.Delete(normalizedDatabasePath);
                }
            }
        }

        /// <summary>
        /// 调用公开静态方法，并在反射失败时展开根异常与上下文。
        /// Invoke a public static method and expand the root cause plus context when reflection fails.
        /// </summary>
        /// <param name="method">目标方法。</param>
        /// <param name="operationName">当前执行步骤名称。</param>
        /// <param name="arguments">反射调用参数。</param>
        /// <returns>返回方法执行结果。</returns>
        /// <returns>Returns the method result.</returns>
        private static object InvokeStaticMethod(MethodInfo method, string operationName, params object[] arguments)
        {
            return InvokeWithContext(
                operationName,
                () => method.Invoke(null, arguments));
        }

        /// <summary>
        /// 调用公开实例方法，并在反射失败时展开根异常与上下文。
        /// Invoke a public instance method and expand the root cause plus context when reflection fails.
        /// </summary>
        /// <param name="instance">方法所属实例。</param>
        /// <param name="method">目标方法。</param>
        /// <param name="operationName">当前执行步骤名称。</param>
        /// <param name="arguments">反射调用参数。</param>
        /// <returns>返回方法执行结果。</returns>
        /// <returns>Returns the method result.</returns>
        private static object InvokeInstanceMethod(object instance, MethodInfo method, string operationName, params object[] arguments)
        {
            return InvokeWithContext(
                operationName,
                () => method.Invoke(instance, arguments));
        }

        /// <summary>
        /// 调用构造函数，并在反射失败时展开根异常与上下文。
        /// Invoke a constructor and expand the root cause plus context when reflection fails.
        /// </summary>
        /// <param name="constructor">目标构造函数。</param>
        /// <param name="operationName">当前执行步骤名称。</param>
        /// <param name="arguments">反射调用参数。</param>
        /// <returns>返回新建实例。</returns>
        /// <returns>Returns the constructed instance.</returns>
        private static object InvokeConstructor(ConstructorInfo constructor, string operationName, params object[] arguments)
        {
            return InvokeWithContext(
                operationName,
                () => constructor.Invoke(arguments));
        }

        /// <summary>
        /// 在统一上下文里执行反射调用，并把 TargetInvocationException 展开为可读根因。
        /// Execute a reflection call under a unified context and expand TargetInvocationException into a readable root cause.
        /// </summary>
        /// <param name="operationName">当前执行步骤名称。</param>
        /// <param name="action">实际反射调用。</param>
        /// <returns>返回执行结果。</returns>
        /// <returns>Returns the action result.</returns>
        private static object InvokeWithContext(string operationName, Func<object> action)
        {
            try
            {
                return action();
            }
            catch (TargetInvocationException exception)
            {
                throw CreateInvocationFailure(operationName, exception);
            }
        }

        /// <summary>
        /// 构造包含步骤名、根异常类型与消息的诊断异常。
        /// Build a diagnostic exception that includes the step name plus the root exception type and message.
        /// </summary>
        /// <param name="operationName">当前执行步骤名称。</param>
        /// <param name="exception">反射层抛出的包装异常。</param>
        /// <returns>面向日志的可读异常。</returns>
        /// <returns>A readable exception tailored for logs.</returns>
        private static Exception CreateInvocationFailure(string operationName, TargetInvocationException exception)
        {
            var rootException = UnwrapInvocationException(exception);
            return new Exception(
                $"{operationName} 失败: {rootException.GetType().FullName}: {rootException.Message}",
                rootException);
        }

        /// <summary>
        /// 递归剥离反射包装异常，直到拿到最内层真实异常。
        /// Recursively peel off reflection wrapper exceptions until the innermost real exception is reached.
        /// </summary>
        /// <param name="exception">待展开的异常。</param>
        /// <returns>最内层真实异常。</returns>
        /// <returns>The innermost real exception.</returns>
        private static Exception UnwrapInvocationException(Exception exception)
        {
            while (exception is TargetInvocationException targetInvocationException
                   && targetInvocationException.InnerException != null)
            {
                exception = targetInvocationException.InnerException;
            }

            return exception;
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

        /// <summary>
        /// 获取指定类型上的公开静态方法。
        /// Get a public static method from the specified type.
        /// </summary>
        /// <param name="type">声明方法的类型。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="parameterTypes">参数类型序列。</param>
        /// <returns>命中时返回 MethodInfo，否则抛出异常。</returns>
        /// <returns>Returns the MethodInfo when found; otherwise throws.</returns>
        private static MethodInfo RequireStaticMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                parameterTypes,
                null);
            if (method == null)
            {
                throw new Exception($"未发现公开静态方法: {type.FullName}.{methodName}");
            }

            return method;
        }

        /// <summary>
        /// 读取指定类型上的公开静态字符串属性，并在缺失或为空时抛出异常。
        /// Read a public static string property from the specified type and throw when it is missing or empty.
        /// </summary>
        /// <param name="type">声明属性的类型。</param>
        /// <param name="propertyName">属性名称。</param>
        /// <returns>属性当前值。</returns>
        /// <returns>The current property value.</returns>
        private static string ReadRequiredStaticStringProperty(Type type, string propertyName)
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);
            if (property == null)
            {
                throw new Exception($"未发现公开静态属性: {type.FullName}.{propertyName}");
            }

            var value = property.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"公开静态属性为空: {type.FullName}.{propertyName}");
            }

            return value;
        }

        /// <summary>
        /// 用稳定的正斜杠规则拼接路径，避免宿主探针在 Windows 上生成混合分隔符路径。
        /// Combine paths with stable forward-slash rules so the host probe does not generate mixed-separator paths on Windows.
        /// </summary>
        /// <param name="left">左侧目录。</param>
        /// <param name="right">右侧文件或子路径。</param>
        /// <returns>拼接后的标准化路径。</returns>
        /// <returns>The combined normalized path.</returns>
        private static string CombinePath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            var normalizedLeft = left.Replace('\\', '/');
            var normalizedRight = right.Replace('\\', '/');
            if (normalizedLeft.EndsWith("/", StringComparison.Ordinal))
            {
                return normalizedLeft + normalizedRight.TrimStart('/');
            }

            return normalizedLeft + "/" + normalizedRight.TrimStart('/');
        }

        /// <summary>
        /// 为 Windows 文件 API 标准化路径分隔符和绝对路径格式。
        /// Normalize a path into the separator and absolute-path format expected by Windows file APIs.
        /// </summary>
        /// <param name="path">原始路径。</param>
        /// <returns>标准化后的绝对路径。</returns>
        /// <returns>The normalized absolute path.</returns>
        private static string NormalizePathForWindowsFileApis(string path)
        {
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(normalizedPath);
        }

        /// <summary>
        /// 构建宿主 SQLite 探针的候选数据库路径列表。
        /// Build the candidate database-path list for the host SQLite probe.
        /// Android 上托管 File API 能写入并不代表 native SQLite 一定能打开同一路径，
        /// 因此这里把 `Context.getDatabasePath()`、内部 files/cache 目录以及 Unity 暴露路径按稳定性排序，并把真正的可用性判断延后到 native open 阶段。
        /// On Android a path being writable through managed File APIs does not guarantee native SQLite can open the same location,
        /// so this method orders `Context.getDatabasePath()`, internal files/cache directories, and Unity-exposed paths by stability while deferring the real usability check to the native open stage.
        /// Windows `systemprofile` 服务账号仍然优先走系统临时目录，但如果该路径失败，探针也会继续尝试其他候选项而不是直接终止。
        /// The Windows `systemprofile` service account still prioritizes the system temp directory, but the probe also keeps trying later candidates instead of stopping on the first failure.
        /// </summary>
        /// <param name="platform">当前运行平台。The current runtime platform.</param>
        /// <param name="frameworkPersistentDataPath">框架公开的持久化根目录。The framework-exposed persistence root.</param>
        /// <param name="applicationPersistentDataPath">Unity Player 公开的持久化根目录。The Unity Player persistence root.</param>
        /// <param name="temporaryCachePath">Unity Player 公开的临时缓存根目录。The Unity Player temporary-cache root.</param>
        /// <param name="databaseFileName">本次探针要创建的数据库文件名。The database-file name created by the current probe run.</param>
        /// <param name="androidContextDatabasePath">Android Activity Context 返回的数据库文件路径。The database-file path returned by the Android Activity Context.</param>
        /// <param name="androidInternalFilesPath">Android Activity Context 返回的内部 files 目录。The internal files directory returned by the Android Activity Context.</param>
        /// <param name="androidInternalCachePath">Android Activity Context 返回的内部 cache 目录。The internal cache directory returned by the Android Activity Context.</param>
        /// <returns>按优先级排序的候选数据库路径列表。</returns>
        /// <returns>The candidate database-path list ordered by priority.</returns>
        private static SqliteProbePathOption[] BuildSqliteProbePathOptions(
            RuntimePlatform platform,
            string frameworkPersistentDataPath,
            string applicationPersistentDataPath,
            string temporaryCachePath,
            string databaseFileName,
            string androidContextDatabasePath,
            string androidInternalFilesPath,
            string androidInternalCachePath)
        {
            var probePathOptions = new List<SqliteProbePathOption>();

            if (platform == RuntimePlatform.WindowsPlayer
                && !string.IsNullOrWhiteSpace(applicationPersistentDataPath)
                && applicationPersistentDataPath.IndexOf("systemprofile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "windows-systemprofile-temp-fallback",
                    CombinePath(
                        CombinePath(Path.GetTempPath(), SqliteProbeDirectoryName),
                        databaseFileName));
            }

            if (platform == RuntimePlatform.Android)
            {
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "android-context-database-path",
                    androidContextDatabasePath);
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "android-internal-files-dir",
                    CombinePath(
                        CombinePath(androidInternalFilesPath, SqliteProbeDirectoryName),
                        databaseFileName));
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "android-internal-cache-dir",
                    CombinePath(
                        CombinePath(androidInternalCachePath, SqliteProbeDirectoryName),
                        databaseFileName));
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "android-temporary-cache-path-fallback",
                    CombinePath(
                        CombinePath(temporaryCachePath, SqliteProbeDirectoryName),
                        databaseFileName));
            }

            if (!string.IsNullOrWhiteSpace(applicationPersistentDataPath))
            {
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "application-persistent-data-path",
                    CombinePath(applicationPersistentDataPath, databaseFileName));
            }

            if (!string.IsNullOrWhiteSpace(frameworkPersistentDataPath))
            {
                AppendSqliteProbePathOption(
                    probePathOptions,
                    "framework-persistent-data-path",
                    CombinePath(frameworkPersistentDataPath, databaseFileName));
            }

            AppendSqliteProbePathOption(
                probePathOptions,
                "system-temp-path",
                CombinePath(
                    CombinePath(Path.GetTempPath(), SqliteProbeDirectoryName),
                    databaseFileName));
            return probePathOptions.ToArray();
        }

        /// <summary>
        /// 把候选数据库路径追加到列表，并在路径为空或重复时自动忽略。
        /// Append a candidate database path to the list while automatically ignoring empty or duplicate entries.
        /// </summary>
        /// <param name="probePathOptions">候选路径列表。The candidate-path list.</param>
        /// <param name="selectionReason">当前候选路径的选路原因。The selection reason for the current candidate.</param>
        /// <param name="databasePath">当前候选路径的数据库文件路径。The database-file path for the current candidate.</param>
        private static void AppendSqliteProbePathOption(
            List<SqliteProbePathOption> probePathOptions,
            string selectionReason,
            string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                return;
            }

            var normalizedDatabasePath = databasePath.Replace('\\', '/');
            if (probePathOptions.Any(option => string.Equals(option.DatabasePath, normalizedDatabasePath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            probePathOptions.Add(new SqliteProbePathOption(selectionReason, normalizedDatabasePath));
        }

        /// <summary>
        /// 按候选顺序执行 SQLite 打开动作，并在单一路径失败时继续回退。
        /// Execute the SQLite open action in candidate order and keep falling back when a single path fails.
        /// 该辅助器把“路径列表排序”与“真实 native open 结果”连接起来，
        /// 避免 Android 链路因为首选目录仅能被托管 File API 访问就提前终止整个探针。
        /// This helper connects the ordered path list to the real native-open result,
        /// preventing the Android flow from terminating the whole probe just because the preferred directory is only writable through managed File APIs.
        /// </summary>
        /// <param name="probePathOptions">按优先级排序的候选路径列表。The candidate-path list ordered by priority.</param>
        /// <param name="openAction">针对单个数据库路径执行实际打开动作的委托。The delegate that performs the actual open action for a single database path.</param>
        /// <param name="selectedDatabasePath">成功打开时返回命中的数据库路径。Returns the selected database path when a candidate opens successfully.</param>
        /// <param name="selectedSelectionReason">成功打开时返回命中的选路原因。Returns the selected selection reason when a candidate opens successfully.</param>
        /// <returns>打开动作返回的结果对象。</returns>
        /// <returns>The result object returned by the open action.</returns>
        private static object ExecuteWithSqliteProbePathFallback(
            IList<SqliteProbePathOption> probePathOptions,
            Func<string, object> openAction,
            out string selectedDatabasePath,
            out string selectedSelectionReason)
        {
            Exception lastException = null;
            var attemptFailureSummaries = new List<string>();
            foreach (var probePathOption in probePathOptions)
            {
                try
                {
                    Debug.Log($"[E2E] SQLite probe phase=path-attempt reason={probePathOption.SelectionReason} databasePath={probePathOption.DatabasePath}");
                    var result = openAction(probePathOption.DatabasePath);
                    selectedDatabasePath = probePathOption.DatabasePath;
                    selectedSelectionReason = probePathOption.SelectionReason;
                    return result;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    attemptFailureSummaries.Add(
                        $"{probePathOption.SelectionReason}:{probePathOption.DatabasePath} => {exception.GetType().FullName}: {exception.Message}");
                    Debug.LogWarning(
                        $"[E2E] SQLite probe phase=path-attempt-failed reason={probePathOption.SelectionReason} databasePath={probePathOption.DatabasePath} error={exception.GetType().FullName}: {exception.Message}");
                }
            }

            selectedDatabasePath = string.Empty;
            selectedSelectionReason = string.Empty;
            var attemptSummary = string.Join(
                " | ",
                attemptFailureSummaries.Count > 0
                    ? attemptFailureSummaries
                    : probePathOptions.Select(option => $"{option.SelectionReason}:{option.DatabasePath}"));
            throw new Exception($"SQLite 探针所有候选路径均打开失败: {attemptSummary}", lastException);
        }

        /// <summary>
        /// 先用托管 File API 落地 SQLite 探针文件，区分“目录本身不可写”和“native sqlite 无法创建新文件”两类故障。
        /// Materialize the SQLite probe file with managed File APIs first so the host test can distinguish an unwritable directory from a native sqlite failure to create a new file.
        /// </summary>
        /// <param name="databasePath">将要交给 SQLite 打开的数据库文件路径。</param>
        /// <param name="databasePath">The database file path that will later be opened by SQLite.</param>
        private static void EnsureSqliteProbeFileExists(string databasePath)
        {
            Debug.Log($"[E2E] SQLite probe phase=managed-file-touch databasePath={databasePath}");

            using (var fileStream = new FileStream(databasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fileStream.Flush();
            }

            if (!File.Exists(databasePath))
            {
                throw new Exception($"SQLite 托管文件落地失败: {databasePath}");
            }

            var fileInfo = new FileInfo(databasePath);
            Debug.Log($"[E2E] SQLite probe phase=managed-file-touch-ready databasePath={databasePath} length={fileInfo.Length}");
        }

        /// <summary>
        /// 通过 Android Activity Context 查询数据库文件路径，优先复用系统为 SQLite 预留的 `databases` 目录。
        /// Query the database-file path through the Android Activity Context and preferentially reuse the system `databases` directory reserved for SQLite.
        /// </summary>
        /// <param name="databaseFileName">要查询的数据库文件名。The database-file name to resolve.</param>
        /// <param name="databasePath">成功时返回数据库文件路径。Returns the database-file path when successful.</param>
        /// <returns>成功拿到数据库文件路径时返回 true，否则返回 false 并继续走其他候选项。</returns>
        /// <returns>Returns true when the database-file path is resolved; otherwise false so the caller can continue with other candidates.</returns>
        private static bool TryReadAndroidContextDatabasePath(string databaseFileName, out string databasePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var javaFile = currentActivity.Call<AndroidJavaObject>("getDatabasePath", databaseFileName))
                {
                    if (javaFile == null)
                    {
                        databasePath = string.Empty;
                        return false;
                    }

                    databasePath = javaFile.Call<string>("getCanonicalPath");
                    if (string.IsNullOrWhiteSpace(databasePath))
                    {
                        databasePath = javaFile.Call<string>("getAbsolutePath");
                    }

                    return !string.IsNullOrWhiteSpace(databasePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[E2E] SQLite probe phase=android-context-database-path file={databaseFileName} error={exception.GetType().FullName}: {exception.Message}");
            }
#endif

            databasePath = string.Empty;
            return false;
        }

        /// <summary>
        /// 通过 Android Activity Context 查询内部可写目录，避免 Unity 暴露的外部目录再次触发 SQLite CannotOpen。
        /// Query an internal writable directory through the Android Activity Context so the SQLite probe avoids Unity paths that resolve to external storage and fail with CannotOpen.
        /// </summary>
        /// <param name="contextMethodName">Activity Context 上返回 File 目录的 Java 方法名。</param>
        /// <param name="contextMethodName">The Java method name on the Activity Context that returns a File directory.</param>
        /// <param name="directoryPath">成功时返回可写目录路径。</param>
        /// <param name="directoryPath">Returns the writable directory path when successful.</param>
        /// <returns>成功拿到内部目录时返回 true，否则返回 false 并继续走回退分支。</returns>
        /// <returns>Returns true when the internal directory is resolved; otherwise false so the caller can continue its fallback chain.</returns>
        private static bool TryReadAndroidContextDirectory(string contextMethodName, out string directoryPath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var javaFile = currentActivity.Call<AndroidJavaObject>(contextMethodName))
                {
                    if (javaFile == null)
                    {
                        directoryPath = string.Empty;
                        return false;
                    }

                    directoryPath = javaFile.Call<string>("getCanonicalPath");
                    if (string.IsNullOrWhiteSpace(directoryPath))
                    {
                        directoryPath = javaFile.Call<string>("getAbsolutePath");
                    }

                    return !string.IsNullOrWhiteSpace(directoryPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[E2E] SQLite probe phase=android-context-dir-read method={contextMethodName} error={exception.GetType().FullName}: {exception.Message}");
            }
#endif

            directoryPath = string.Empty;
            return false;
        }

        /// <summary>
        /// 获取指定类型上的公开实例方法。
        /// Get a public instance method from the specified type.
        /// </summary>
        /// <param name="type">声明方法的类型。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="parameterTypes">参数类型序列。</param>
        /// <returns>命中时返回 MethodInfo，否则抛出异常。</returns>
        /// <returns>Returns the MethodInfo when found; otherwise throws.</returns>
        private static MethodInfo RequireInstanceMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameterTypes,
                null);
            if (method == null)
            {
                throw new Exception($"未发现公开实例方法: {type.FullName}.{methodName}");
            }

            return method;
        }

        /// <summary>
        /// 获取指定类型上的公开泛型实例方法定义。
        /// Get a public generic instance-method definition from the specified type.
        /// </summary>
        /// <param name="type">声明方法的类型。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="parameterTypes">参数类型序列。</param>
        /// <returns>命中时返回泛型方法定义，否则抛出异常。</returns>
        /// <returns>Returns the generic method definition when found; otherwise throws.</returns>
        private static MethodInfo RequireInstanceGenericMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal) || !method.IsGenericMethodDefinition)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    continue;
                }

                var allMatched = true;
                for (var index = 0; index < parameters.Length; index++)
                {
                    if (parameters[index].ParameterType != parameterTypes[index])
                    {
                        allMatched = false;
                        break;
                    }
                }

                if (allMatched)
                {
                    return method;
                }
            }

            throw new Exception($"未发现公开泛型实例方法: {type.FullName}.{methodName}");
        }
    }
}