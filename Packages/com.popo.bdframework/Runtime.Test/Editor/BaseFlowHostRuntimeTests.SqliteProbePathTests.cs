using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// BaseFlow 宿主 SQLite 探针路径选路与回退逻辑测试。
    /// Tests for the BaseFlow host SQLite probe path-selection and fallback logic.
    /// 这些测试只锁定纯逻辑契约：Android 候选顺序、Windows `systemprofile` 降级优先级，以及首选路径失败后的继续回退行为，
    /// 避免 TeamCity Android 链路再次退化成“单一路径假设”。
    /// These tests lock pure-logic contracts only: Android candidate ordering, the Windows `systemprofile` degradation priority, and the continue-on-failure fallback behavior after the first path fails,
    /// preventing the TeamCity Android flow from regressing back into a single-path assumption.
    /// </summary>
    public class BaseFlowHostRuntimeTestsSqliteProbePathTests
    {
        private const int SqliteReadWriteCreateOpenFlagsValue = 2 | 4;
        private const string SqliteDefaultDateTimeStringFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

        /// <summary>
        /// 候选路径摘要。
        /// Candidate-path summary.
        /// 该轻量对象只用于把反射读出的私有候选项投影成稳定断言数据，
        /// 避免 EditorTest 对宿主程序集的私有嵌套类型形成编译期依赖。
        /// This lightweight object only projects the reflected private candidate entries into stable assertion data,
        /// avoiding a compile-time dependency from EditorTest onto the host assembly's private nested type.
        /// </summary>
        private sealed class ProbePathSummary
        {
            /// <summary>
            /// 初始化候选路径摘要。
            /// Initialize a candidate-path summary.
            /// </summary>
            /// <param name="selectionReason">选路原因。The selection reason.</param>
            /// <param name="databasePath">数据库路径。The database path.</param>
            public ProbePathSummary(string selectionReason, string databasePath)
            {
                SelectionReason = selectionReason;
                DatabasePath = databasePath;
            }

            /// <summary>
            /// 当前候选项的选路原因。
            /// The selection reason of the current candidate.
            /// </summary>
            public string SelectionReason { get; }

            /// <summary>
            /// 当前候选项的数据库路径。
            /// The database path of the current candidate.
            /// </summary>
            public string DatabasePath { get; }
        }

        /// <summary>
        /// 每个测试开始前输出统一中文日志，方便 batchmode 与 Unity Test Runner 快速定位当前验证目标。
        /// Emit a unified Chinese log before each test so batchmode and Unity Test Runner can quickly identify the current verification target.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 BaseFlow 宿主 SQLite 探针在 Editor NUnit 下保持稳定的路径候选顺序与失败回退契约。 实现手段=通过反射调用宿主测试程序集的私有路径构建与回退执行辅助器并断言候选顺序和回退结果。");
        }

        /// <summary>
        /// 解析宿主 BaseFlow SQLite 探针类型。
        /// Resolve the host BaseFlow SQLite probe type.
        /// </summary>
        /// <returns>已加载的宿主探针类型。The loaded host probe type.</returns>
        private static Type GetBaseFlowHostRuntimeTestsType()
        {
            var runtimeType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("BDFramework.Test.E2E.BaseFlowHostRuntimeTests"))
                .FirstOrDefault(type => type != null);

            Assert.That(runtimeType, Is.Not.Null, "应能在当前 AppDomain 中解析到 BDFramework.Test.E2E.BaseFlowHostRuntimeTests 类型。");
            return runtimeType!;
        }

        /// <summary>
        /// 解析当前 AppDomain 中已装载的目标类型。
        /// Resolve a loaded target type from the current AppDomain.
        /// </summary>
        /// <param name="typeName">目标类型全名。The full target type name.</param>
        /// <returns>已装载的目标类型。The loaded target type.</returns>
        private static Type GetLoadedType(string typeName)
        {
            var loadedType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);

            Assert.That(loadedType, Is.Not.Null, $"应能在当前 AppDomain 中解析到 {typeName} 类型。");
            return loadedType!;
        }

        /// <summary>
        /// 通过反射调用宿主的 SQLite 候选路径构建辅助器。
        /// Invoke the host SQLite candidate-path builder through reflection.
        /// </summary>
        /// <param name="platform">测试平台。The test platform.</param>
        /// <param name="frameworkPersistentDataPath">框架持久化目录。The framework persistence path.</param>
        /// <param name="applicationPersistentDataPath">Unity 持久化目录。The Unity persistence path.</param>
        /// <param name="temporaryCachePath">Unity 临时缓存目录。The Unity temporary-cache path.</param>
        /// <param name="databaseFileName">数据库文件名。The database file name.</param>
        /// <param name="androidContextDatabasePath">Android Context 数据库路径。The Android Context database path.</param>
        /// <param name="androidInternalFilesPath">Android 内部 files 目录。The Android internal files directory.</param>
        /// <param name="androidInternalCachePath">Android 内部 cache 目录。The Android internal cache directory.</param>
        /// <returns>反射返回的私有候选路径数组。The private candidate-path array returned through reflection.</returns>
        private static Array InvokeBuildSqliteProbePathOptions(
            RuntimePlatform platform,
            string frameworkPersistentDataPath,
            string applicationPersistentDataPath,
            string temporaryCachePath,
            string databaseFileName,
            string androidContextDatabasePath,
            string androidInternalFilesPath,
            string androidInternalCachePath)
        {
            var hostType = GetBaseFlowHostRuntimeTestsType();
            var method = hostType.GetMethod("BuildSqliteProbePathOptions", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "应能找到 BuildSqliteProbePathOptions 私有静态方法。");
            var result = method!.Invoke(
                null,
                new object[]
                {
                    platform,
                    frameworkPersistentDataPath,
                    applicationPersistentDataPath,
                    temporaryCachePath,
                    databaseFileName,
                    androidContextDatabasePath,
                    androidInternalFilesPath,
                    androidInternalCachePath,
                }) as Array;
            Assert.That(result, Is.Not.Null, "候选路径构建结果应为非空数组。");
            return result!;
        }

        /// <summary>
        /// 通过反射读取候选路径数组的摘要信息。
        /// Read summary information from the candidate-path array through reflection.
        /// </summary>
        /// <param name="pathOptions">私有候选路径数组。The private candidate-path array.</param>
        /// <returns>便于断言的候选路径摘要列表。A candidate-path summary list that is easy to assert.</returns>
        private static IReadOnlyList<ProbePathSummary> SummarizeProbePathOptions(Array pathOptions)
        {
            var summaries = new List<ProbePathSummary>();

            foreach (var pathOption in pathOptions)
            {
                Assert.That(pathOption, Is.Not.Null, "候选路径项不应为空。");
                var optionType = pathOption!.GetType();
                var selectionReasonProperty = optionType.GetProperty("SelectionReason", BindingFlags.Public | BindingFlags.Instance);
                var databasePathProperty = optionType.GetProperty("DatabasePath", BindingFlags.Public | BindingFlags.Instance);

                Assert.That(selectionReasonProperty, Is.Not.Null, "候选路径项应暴露 SelectionReason 属性。");
                Assert.That(databasePathProperty, Is.Not.Null, "候选路径项应暴露 DatabasePath 属性。");

                summaries.Add(
                    new ProbePathSummary(
                        selectionReasonProperty!.GetValue(pathOption) as string ?? string.Empty,
                        databasePathProperty!.GetValue(pathOption) as string ?? string.Empty));
            }

            return summaries;
        }

        /// <summary>
        /// 通过反射调用宿主的 SQLite 路径回退执行辅助器。
        /// Invoke the host SQLite fallback executor through reflection.
        /// </summary>
        /// <param name="pathOptions">私有候选路径数组。The private candidate-path array.</param>
        /// <param name="openAction">测试用打开动作。The test open action.</param>
        /// <returns>回退执行后的选中原因、路径与返回值。The selected reason, selected path, and return value after fallback execution.</returns>
        private static (string SelectionReason, string DatabasePath, object Result) InvokeExecuteWithSqliteProbePathFallback(
            Array pathOptions,
            Func<string, object> openAction)
        {
            var hostType = GetBaseFlowHostRuntimeTestsType();
            var method = hostType.GetMethod("ExecuteWithSqliteProbePathFallback", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "应能找到 ExecuteWithSqliteProbePathFallback 私有静态方法。");
            var arguments = new object[]
            {
                pathOptions,
                openAction,
                null!,
                null!,
            };
            var result = method!.Invoke(null, arguments);
            return (
                arguments[3] as string ?? string.Empty,
                arguments[2] as string ?? string.Empty,
                result!);
        }

        /// <summary>
        /// 通过反射调用宿主的 SQLite 路径回退执行辅助器，并返回最终抛出的根异常。
        /// Invoke the host SQLite fallback executor through reflection and return the final root exception.
        /// </summary>
        /// <param name="pathOptions">私有候选路径数组。The private candidate-path array.</param>
        /// <param name="openAction">测试用打开动作。The test open action.</param>
        /// <returns>宿主回退执行器抛出的根异常。The root exception thrown by the host fallback executor.</returns>
        private static Exception CaptureExecuteWithSqliteProbePathFallbackFailure(
            Array pathOptions,
            Func<string, object> openAction)
        {
            var hostType = GetBaseFlowHostRuntimeTestsType();
            var method = hostType.GetMethod("ExecuteWithSqliteProbePathFallback", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "应能找到 ExecuteWithSqliteProbePathFallback 私有静态方法。");
            try
            {
                method!.Invoke(
                    null,
                    new object[]
                    {
                        pathOptions,
                        openAction,
                        null!,
                        null!,
                    });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                return exception.InnerException;
            }

            Assert.Fail("应当捕获到宿主 SQLite 回退执行器抛出的异常。");
            return new Exception("unreachable");
        }

        /// <summary>
        /// 通过与宿主探针一致的反射参数创建 SQLiteConnectionString。
        /// Create a SQLiteConnectionString with the same reflection arguments used by the host probe.
        /// </summary>
        /// <param name="databasePath">数据库路径。The database path.</param>
        /// <returns>创建出的 SQLiteConnectionString 实例。The constructed SQLiteConnectionString instance.</returns>
        private static object InvokeReflectedSqliteConnectionStringConstructor(string databasePath)
        {
            var sqliteConnectionType = GetLoadedType("SQLite4Unity3d.SQLiteConnection");
            var sqliteConnectionStringType = GetLoadedType("SQLite4Unity3d.SQLiteConnectionString");
            var sqliteOpenFlagsType = GetLoadedType("SQLite4Unity3d.SQLiteOpenFlags");
            var sqliteConnectionActionType = typeof(Action<>).MakeGenericType(sqliteConnectionType);
            var constructor = sqliteConnectionStringType.GetConstructor(new[]
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

            Assert.That(constructor, Is.Not.Null,
                "应能找到 SQLiteConnectionString(string, SQLiteOpenFlags, bool, object, Action<SQLiteConnection>, Action<SQLiteConnection>, string, string, bool) 构造函数。");
            var sqliteOpenFlags = Enum.ToObject(sqliteOpenFlagsType, SqliteReadWriteCreateOpenFlagsValue);
            return constructor!.Invoke(new object[]
            {
                databasePath,
                sqliteOpenFlags,
                true,
                null!,
                null!,
                null!,
                null!,
                SqliteDefaultDateTimeStringFormat,
                true,
            });
        }

        /// <summary>
        /// 验证 Android 候选顺序优先走 `Context.getDatabasePath()`，随后才是内部 files/cache 与 Unity 回退路径。
        /// Verify that the Android candidate order prefers `Context.getDatabasePath()` first, followed by internal files/cache and only then Unity fallback paths.
        /// </summary>
        [Test]
        public void BuildSqliteProbePathOptions_PrefersAndroidDatabaseFilesAndCacheBeforeUnityFallbacks()
        {
            var summaries = SummarizeProbePathOptions(
                InvokeBuildSqliteProbePathOptions(
                    RuntimePlatform.Android,
                    "/framework/persistent",
                    "/storage/emulated/0/Android/data/com.talos/files",
                    "/storage/emulated/0/Android/data/com.talos/cache",
                    "probe.db",
                    "/data/user/0/com.talos/databases/probe.db",
                    "/data/user/0/com.talos/files",
                    "/data/user/0/com.talos/cache"));

            Assert.That(summaries.Select(summary => summary.SelectionReason).ToArray(),
                Is.EqualTo(new[]
                {
                    "android-context-database-path",
                    "android-internal-files-dir",
                    "android-internal-cache-dir",
                    "android-temporary-cache-path-fallback",
                    "application-persistent-data-path",
                    "framework-persistent-data-path",
                    "system-temp-path",
                }),
                "Android 候选顺序应先尝试 Context 数据库路径与内部目录，再回退到 Unity 暴露路径和系统临时目录。");
            Assert.That(summaries[0].DatabasePath, Is.EqualTo("/data/user/0/com.talos/databases/probe.db"),
                "Android 首选候选项应直接使用 Context.getDatabasePath 返回的数据库文件路径。");
            Assert.That(summaries[1].DatabasePath, Is.EqualTo("/data/user/0/com.talos/files/bdframework-host-sqlite/probe.db"),
                "Android 内部 files 候选项应落到宿主 SQLite 专用子目录下。");
            Assert.That(summaries[2].DatabasePath, Is.EqualTo("/data/user/0/com.talos/cache/bdframework-host-sqlite/probe.db"),
                "Android 内部 cache 候选项应排在内部 files 之后，避免再次把 cache 当成唯一选项。");
        }

        /// <summary>
        /// 验证 Windows `systemprofile` 服务账号会把系统临时目录放到首位，但仍保留后续候选项以便继续回退。
        /// Verify that the Windows `systemprofile` service account moves the system temp directory to the first slot while still preserving later candidates for fallback.
        /// </summary>
        [Test]
        public void BuildSqliteProbePathOptions_PutsWindowsSystemProfileTempFallbackFirst()
        {
            var summaries = SummarizeProbePathOptions(
                InvokeBuildSqliteProbePathOptions(
                    RuntimePlatform.WindowsPlayer,
                    "C:/framework/persistent",
                    "C:/Windows/System32/config/systemprofile/AppData/LocalLow/Company/Game",
                    "C:/unity/cache",
                    "probe.db",
                    string.Empty,
                    string.Empty,
                    string.Empty));

            Assert.That(summaries[0].SelectionReason, Is.EqualTo("windows-systemprofile-temp-fallback"),
                "Windows systemprofile 环境下应优先尝试系统临时目录回退路径。");
            Assert.That(summaries[0].DatabasePath.Replace('\\', '/'), Does.EndWith("/bdframework-host-sqlite/probe.db"),
                "Windows systemprofile 临时回退路径应落到宿主 SQLite 专用子目录下。");
            Assert.That(summaries.Select(summary => summary.SelectionReason), Does.Contain("application-persistent-data-path"),
                "即使系统临时目录被提前到首位，也应保留 Unity 持久化目录作为后续候选项。");
        }

        /// <summary>
        /// 验证首选候选路径失败后，探针会继续回退到下一条路径而不是直接终止。
        /// Verify that the probe falls back to the next candidate after the preferred path fails instead of terminating immediately.
        /// </summary>
        [Test]
        public void ExecuteWithSqliteProbePathFallback_RetriesNextCandidateAfterFailure()
        {
            var pathOptions = InvokeBuildSqliteProbePathOptions(
                RuntimePlatform.Android,
                "/framework/persistent",
                "/storage/emulated/0/Android/data/com.talos/files",
                "/storage/emulated/0/Android/data/com.talos/cache",
                "probe.db",
                "/data/user/0/com.talos/databases/probe.db",
                "/data/user/0/com.talos/files",
                "/data/user/0/com.talos/cache");
            var attemptedPaths = new List<string>();

            var executionResult = InvokeExecuteWithSqliteProbePathFallback(
                pathOptions,
                databasePath =>
                {
                    attemptedPaths.Add(databasePath);
                    if (attemptedPaths.Count == 1)
                    {
                        throw new InvalidOperationException("simulate first path cannot open");
                    }

                    return databasePath;
                });

            Assert.That(attemptedPaths, Is.EqualTo(new[]
            {
                "/data/user/0/com.talos/databases/probe.db",
                "/data/user/0/com.talos/files/bdframework-host-sqlite/probe.db",
            }), "首选 Android 数据库路径失败后，应立即回退到内部 files 候选项。 ");
            Assert.That(executionResult.SelectionReason, Is.EqualTo("android-internal-files-dir"),
                "第二条成功候选项应把选路原因回传给调用方，便于运行时日志定位实际命中的路径。");
            Assert.That(executionResult.DatabasePath, Is.EqualTo("/data/user/0/com.talos/files/bdframework-host-sqlite/probe.db"),
                "成功命中的数据库路径应与第二条候选项一致。");
            Assert.That(executionResult.Result as string, Is.EqualTo("/data/user/0/com.talos/files/bdframework-host-sqlite/probe.db"),
                "回退执行器应把成功路径的打开结果原样返回给调用方。");
        }

        /// <summary>
        /// 验证全部候选都失败时，最终异常会包含逐候选的路径与根异常摘要，便于 TeamCity 直接定位失败原因。
        /// Verify that when all candidates fail, the final exception includes per-candidate path and root-exception summaries so TeamCity can diagnose the failure directly.
        /// </summary>
        [Test]
        public void ExecuteWithSqliteProbePathFallback_IncludesCandidateFailureDetailsWhenAllCandidatesFail()
        {
            var pathOptions = InvokeBuildSqliteProbePathOptions(
                RuntimePlatform.Android,
                "/framework/persistent",
                "/storage/emulated/0/Android/data/com.talos/files",
                "/storage/emulated/0/Android/data/com.talos/cache",
                "probe.db",
                "/data/user/0/com.talos/databases/probe.db",
                "/data/user/0/com.talos/files",
                "/data/user/0/com.talos/cache");

            var exception = CaptureExecuteWithSqliteProbePathFallbackFailure(
                pathOptions,
                databasePath => throw new InvalidOperationException($"cannot open {databasePath}"));

            Assert.That(exception.Message,
                Does.Contain("android-context-database-path:/data/user/0/com.talos/databases/probe.db => System.InvalidOperationException: cannot open /data/user/0/com.talos/databases/probe.db"),
                "全部候选都失败时，异常消息应包含首个候选路径及其根异常摘要。");
            Assert.That(exception.Message,
                Does.Contain("android-internal-files-dir:/data/user/0/com.talos/files/bdframework-host-sqlite/probe.db => System.InvalidOperationException: cannot open /data/user/0/com.talos/files/bdframework-host-sqlite/probe.db"),
                "异常消息应继续包含后续候选的失败摘要，避免运行日志只剩路径列表而缺少具体错误原因。");
        }

        /// <summary>
        /// 验证宿主采用的反射构造参数会显式传入空 key 与默认时间格式，而不是依赖 `Type.Missing` 占位。
        /// Verify that the host reflection constructor arguments pass an explicit null key and default date-time format instead of relying on `Type.Missing` placeholders.
        /// </summary>
        [Test]
        public void ReflectedSqliteConnectionStringConstructor_UsesExplicitDefaults()
        {
            var connectionString = InvokeReflectedSqliteConnectionStringConstructor("/tmp/baseflow-host-probe.db");
            var connectionStringType = connectionString.GetType();

            Assert.That(connectionStringType.GetProperty("DatabasePath")!.GetValue(connectionString) as string,
                Is.EqualTo("/tmp/baseflow-host-probe.db"),
                "反射构造得到的 SQLiteConnectionString 应保留调用方传入的数据库路径。");
            Assert.That(connectionStringType.GetProperty("Key")!.GetValue(connectionString),
                Is.Null,
                "宿主探针的反射构造应显式传入 null key，避免把 `Type.Missing` 误当成加密键对象。");
            Assert.That(connectionStringType.GetProperty("DateTimeStringFormat")!.GetValue(connectionString) as string,
                Is.EqualTo(SqliteDefaultDateTimeStringFormat),
                "宿主探针的反射构造应显式传入 SQLite 默认时间格式，保持与公开构造的默认行为一致。");
            Assert.That(connectionStringType.GetProperty("StoreTimeSpanAsTicks")!.GetValue(connectionString),
                Is.EqualTo(true),
                "宿主探针的反射构造应显式保留 TimeSpan ticks 存储默认值。");
        }
    }
}