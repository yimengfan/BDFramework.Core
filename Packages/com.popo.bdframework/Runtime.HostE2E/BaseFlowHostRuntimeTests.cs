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
            try
            {
                addAssetsPathToGroupMethod.Invoke(
                    null,
                    new object[]
                    {
                        groupName,
                        new[]
                        {
                            "talos/baseflow/host/a.prefab",
                            "talos/baseflow/host/b.mat",
                        },
                    });

                var groupedPaths = getAssetsPathByGroupMethod.Invoke(null, new object[] { groupName }) as string[];
                if (groupedPaths == null || groupedPaths.Length != 2)
                {
                    throw new Exception($"资源组公共接口返回异常，数量={groupedPaths?.Length ?? 0}");
                }

                var assetsInfoPath = getAssetsInfoPathMethod.Invoke(
                    null,
                    new object[] { Application.persistentDataPath }) as string;
                var versionInfoPath = getServerAssetsVersionInfoPathMethod.Invoke(
                    null,
                    new object[] { Application.persistentDataPath, Application.platform }) as string;
                if (string.IsNullOrWhiteSpace(assetsInfoPath) || string.IsNullOrWhiteSpace(versionInfoPath))
                {
                    throw new Exception("资源版控路径拼接结果为空");
                }

                findShaderMethod.Invoke(null, new object[] { "__Talos_BaseFlow_Host_NonExistent_Shader__" });
                Debug.Log(
                    $"[E2E] 宿主资源接口联通完成: groupedPaths={groupedPaths.Length} assetsInfoPath={assetsInfoPath} versionInfoPath={versionInfoPath}");
            }
            finally
            {
                clearAssetGroupMethod.Invoke(null, new object[] { groupName });
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

            var databasePath = Path.Combine(
                Application.persistentDataPath,
                $"talos-baseflow-host-{Guid.NewGuid():N}.db");
            var databaseDirectory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            object sqliteConnection = null;
            try
            {
                sqliteConnection = sqliteConnectionConstructor.Invoke(new object[] { databasePath, true });

                executeMethod.Invoke(
                    sqliteConnection,
                    new object[]
                    {
                        "CREATE TABLE IF NOT EXISTS TalosBaseFlowHostSqlite (id INTEGER PRIMARY KEY, name TEXT NOT NULL);",
                        Array.Empty<object>(),
                    });
                executeMethod.Invoke(
                    sqliteConnection,
                    new object[]
                    {
                        "DELETE FROM TalosBaseFlowHostSqlite;",
                        Array.Empty<object>(),
                    });
                executeMethod.Invoke(
                    sqliteConnection,
                    new object[]
                    {
                        "INSERT INTO TalosBaseFlowHostSqlite (id, name) VALUES (1, ?);",
                        new object[] { SqliteProbeValue },
                    });

                var count = (int)executeScalarDefinition
                    .MakeGenericMethod(typeof(int))
                    .Invoke(
                        sqliteConnection,
                        new object[]
                        {
                            "SELECT COUNT(*) FROM TalosBaseFlowHostSqlite;",
                            Array.Empty<object>(),
                        });
                if (count != 1)
                {
                    throw new Exception($"SQLite 写入条数异常: count={count}");
                }

                var loadedName = executeScalarDefinition
                    .MakeGenericMethod(typeof(string))
                    .Invoke(
                        sqliteConnection,
                        new object[]
                        {
                            "SELECT name FROM TalosBaseFlowHostSqlite WHERE id = 1;",
                            Array.Empty<object>(),
                        }) as string;
                if (!string.Equals(loadedName, SqliteProbeValue, StringComparison.Ordinal))
                {
                    throw new Exception($"SQLite 查询结果异常: name={loadedName ?? "<null>"}");
                }

                Debug.Log($"[E2E] 宿主 SQLite 读写闭环完成: databasePath={databasePath}");
            }
            finally
            {
                (sqliteConnection as IDisposable)?.Dispose();
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
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