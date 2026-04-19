using System;
using System.IO;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.HostE2E
{
    /// <summary>
    /// 宿主侧基础系统 E2E 测试套件。
    /// Host-owned foundational-system E2E suite.
    /// 该套件专门服务于 step_01 BaseFlow 母包验证，通过宿主可见的反射入口巡检 AB 资源接口与 SQLite 最小读写闭环，
    /// 避免为了基础系统回归重新把依赖热更层的 Runtime.Test 程序集根引用回母包。
    /// This suite exists specifically for the step_01 BaseFlow host-package validation and inspects the AssetBundle-facing resource APIs and the minimal SQLite read-write loop through host-visible reflection entrypoints,
    /// avoiding the need to root the Runtime.Test assembly that depends on the hotfix layer back into the base package.
    /// 使用说明：Talos BaseFlow 会按 suite 名称分别执行 `asset-load` 与 `framework-integration`，
    /// 因此这里的每个入口都必须保持宿主程序集可见并显式 Preserve。
    /// Usage note: Talos BaseFlow executes `asset-load` and `framework-integration` by suite name,
    /// so each entrypoint here must stay visible from the host assembly and be explicitly preserved.
    /// </summary>
    [Preserve]
    public static class BaseFlowHostRuntimeTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";
        private const string BResourcesTypeName = "BDFramework.ResourceMgr.BResources";
        private const string SqliteConnectionTypeName = "SQLite4Unity3d.SQLiteConnection";
        private const string SqliteProbeValue = "framework-integration";

        /// <summary>
        /// 验证宿主可以通过热更资源入口联通 AB 资源系统的基础公共接口。
        /// Verify that the host can reach the foundational public APIs of the AssetBundle resource system through the hotfix resource entrypoints.
        /// 该检查覆盖资源组缓存、版控路径拼接与 Shader 查询三个稳定入口，
        /// 用最小反射调用确认母包已经具备驱动资源主链路的必要信号。
        /// This check covers asset-group caching, version-path composition, and shader lookup as stable entrypoints,
        /// confirming with minimal reflection calls that the host package has the signals needed to drive the resource mainline.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-load", order: 1, des: "验证宿主可联通 AB 资源基础接口")]
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
        [E2ETest(suite: "framework-integration", order: 1, des: "验证宿主可完成 SQLite 最小读写闭环")]
        public static void SqliteRoundTripReachable()
        {
            Debug.Log("[E2E] 测试目的=验证宿主可完成 SQLite 最小读写闭环 实现手段=反射创建 SQLiteConnection 并执行建表、写入、查询");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);
            var sqliteConnectionType = RequireLoadedType(SqliteConnectionTypeName);
            var sqliteConnectionConstructor = sqliteConnectionType.GetConstructor(new[] { typeof(string), typeof(bool) });
            if (sqliteConnectionConstructor == null)
            {
                throw new Exception("未发现 SQLiteConnection(string, bool) 构造入口");
            }

            var executeMethod = RequireInstanceMethod(
                sqliteConnectionType,
                "Execute",
                typeof(string),
                typeof(object[]));
            var executeScalarDefinition = RequireInstanceGenericMethod(
                sqliteConnectionType,
                "ExecuteScalar",
                typeof(string),
                typeof(object[]));

            var frameworkPersistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");
            var databasePath = CombinePath(
                frameworkPersistentDataPath,
                $"talos-baseflow-host-{Guid.NewGuid():N}.db");
            var normalizedDatabasePath = NormalizePathForWindowsFileApis(databasePath);
            var databaseDirectory = Path.GetDirectoryName(normalizedDatabasePath);
            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }
            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
            {
                throw new Exception($"SQLite 目录创建失败: {databaseDirectory}");
            }

            object sqliteConnection = null;
            try
            {
                if (!File.Exists(normalizedDatabasePath))
                {
                    Debug.Log($"[E2E] SQLite probe phase=precreate-file databasePath={normalizedDatabasePath}");
                    using (File.Create(normalizedDatabasePath))
                    {
                    }
                }

                Debug.Log($"[E2E] SQLite probe phase=open databasePath={normalizedDatabasePath}");
                sqliteConnection = InvokeConstructor(
                    sqliteConnectionConstructor,
                    "SQLiteConnection..ctor",
                    normalizedDatabasePath,
                    true);

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
                    executeScalarDefinition.MakeGenericMethod(typeof(int)),
                    "SQLiteConnection.ExecuteScalar<int>(count)",
                    "SELECT COUNT(*) FROM TalosBaseFlowHostSqlite;",
                    Array.Empty<object>());
                if (count != 1)
                {
                    throw new Exception($"SQLite 写入条数异常: count={count}");
                }

                Debug.Log("[E2E] SQLite probe phase=query-name");
                var loadedName = InvokeInstanceMethod(
                    sqliteConnection,
                    executeScalarDefinition.MakeGenericMethod(typeof(string)),
                    "SQLiteConnection.ExecuteScalar<string>(name)",
                    "SELECT name FROM TalosBaseFlowHostSqlite WHERE id = 1;",
                    Array.Empty<object>()) as string;
                if (!string.Equals(loadedName, SqliteProbeValue, StringComparison.Ordinal))
                {
                    throw new Exception($"SQLite 查询结果异常: name={loadedName ?? "<null>"}");
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