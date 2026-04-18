using System;
using System.IO;
using System.Linq;
using System.Threading;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.Logs;
using BDFramework.ResourceMgr;
using SQLite4Unity3d;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架启动后总集成测试套件。
    /// Framework post-start integration test suite.
    /// 该套件面向真机启动后的 Talos E2E 场景，顺序巡检启动、配置、资源、SQLite 与日志持久化主链路，
    /// 用最少的外部前置条件确认 BDFramework 在真实运行态下的基础能力已经联通。
    /// This suite targets Talos E2E runs after player startup and inspects the startup, configuration, resource, SQLite, and log-persistence mainlines in order,
    /// confirming with minimal external prerequisites that BDFramework's foundational capabilities are wired together in the real runtime state.
    /// </summary>
    [Preserve]
    public static class FrameworkIntegrationTests
    {
        private const string IntegrationDatabaseName = "TalosFrameworkIntegration.db";
        private const int PlayerLogWaitTimeoutMs = 2000;

        /// <summary>
        /// 真机 SQLite 集成检查使用的最小数据行。
        /// Minimal data row used by the player SQLite integration check.
        /// 该模型只覆盖建表、插入和查询这三类核心行为，避免把业务表结构耦合进框架总巡检。
        /// This model only covers the core create-table, insert, and query behaviors so business table shapes do not leak into the framework-wide smoke pass.
        /// </summary>
        private sealed class IntegrationSqliteRow
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        /// <summary>
        /// 验证框架在启动完成后已经进入可执行的运行上下文。
        /// Verify that the framework reaches an executable runtime context after startup completes.
        /// 真机模式下要求运行标记、版本号和托管类型发现均已就绪；编辑器仅保留无场景副作用的轻量检查。
        /// In player mode, this requires the runtime flag, framework version, and hosted-type discovery to be ready; the editor keeps only the light checks without scene-side side effects.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-integration", order: 1, des: "startup-context-ready")]
        public static void StartupPipelineReady()
        {
            if (!Application.isEditor && !BApplication.IsPlaying)
            {
                throw new Exception("BApplication.IsPlaying is false after player startup.");
            }

            var frameworkVersion = BDLauncher.FrameworkVersion;
            if (string.IsNullOrWhiteSpace(frameworkVersion))
            {
                throw new Exception("Framework version is empty.");
            }

            var hostingTypes = ScriptLoder.GetAppDomainHostingTypes()?.ToList();
            if (hostingTypes == null || hostingTypes.Count == 0)
            {
                throw new Exception("No hosted types were discovered for the framework runtime.");
            }

            Debug.Log(
                $"[E2E] Framework startup context ready. playing={BApplication.IsPlaying} version={frameworkVersion} hostedTypes={hostingTypes.Count}");
        }

        /// <summary>
        /// 验证框架配置入口在启动后可解析并返回基础配置对象。
        /// Verify that the framework configuration entry can resolve and return the base configuration object after startup.
        /// 该检查直接走 `GameConfigManager` 的公共入口，确认配置文本来源回退、处理器注册与对象反序列化已经协同可用。
        /// This check uses the public `GameConfigManager` entry so configuration-source fallback, processor registration, and object deserialization are proven to work together.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-integration", order: 2, des: "config-pipeline-ready")]
        public static void ConfigPipelineReady()
        {
            var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            if (config == null)
            {
                throw new Exception("GameBaseConfigProcessor.Config could not be resolved.");
            }

            Debug.Log($"[E2E] Config pipeline ready. clientVersion={config.ClientVersionNum}");
        }

        /// <summary>
        /// 验证资源主链路的基础公共接口在启动后可联通。
        /// Verify that the foundational public interfaces of the resource mainline are wired after startup.
        /// 该检查不依赖真实资源内容，只验证路径拼接、资源组缓存和常见查询入口可用，
        /// 这样能在不同真机包体上保持稳定，同时尽早发现资源系统初始化缺口。
        /// This check does not depend on concrete asset content and instead verifies path composition, asset-group caching, and common lookup entrypoints,
        /// keeping it stable across different player packages while still exposing resource-system initialization gaps early.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-integration", order: 3, des: "resource-pipeline-ready")]
        public static void ResourcePipelineReady()
        {
            var platform = BApplication.IsPlaying ? BApplication.RuntimePlatform : Application.platform;
            var assetsInfoPath = BResources.GetAssetsInfoPath(BApplication.persistentDataPath, platform);
            var versionInfoPath = BResources.GetServerAssetsVersionInfoPath(BApplication.persistentDataPath, platform);
            if (string.IsNullOrWhiteSpace(assetsInfoPath) || string.IsNullOrWhiteSpace(versionInfoPath))
            {
                throw new Exception("Resource version paths were not composed correctly.");
            }

            var groupName = $"framework-integration-{Guid.NewGuid():N}";
            try
            {
                BResources.AddAssetsPathToGroup(groupName, "integration/a.prefab", "integration/b.mat");
                var groupedPaths = BResources.GetAssetsPathByGroup(groupName);
                if (groupedPaths == null || groupedPaths.Length != 2)
                {
                    throw new Exception($"Unexpected grouped path count: {groupedPaths?.Length ?? 0}.");
                }

                var shader = BResources.FindShader("__Talos_E2E_FrameworkIntegration_NonExistent_Shader__");
                Debug.Log(
                    $"[E2E] Resource pipeline ready. assetsInfoPath={assetsInfoPath} versionInfoPath={versionInfoPath} shader={(shader != null ? shader.name : "null")}");
            }
            finally
            {
                BResources.ClearAssetGroup(groupName);
            }
        }

        /// <summary>
        /// 验证运行时 SQLite 能在真机环境里完成最小的建表、写入和查询闭环。
        /// Verify that runtime SQLite can complete the minimal create-table, write, and query loop in the player environment.
        /// 该检查使用独立的临时数据库文件，避免与框架默认连接池或业务数据库发生冲突。
        /// This check uses an isolated temporary database file so it does not conflict with the framework's default connection pool or business databases.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-integration", order: 4, des: "sqlite-pipeline-ready")]
        public static void SqlitePipelineReady()
        {
            var databasePath = GetIntegrationDatabasePath();
            SQLiteConnection connection = null;
            var databaseDirectory = Path.GetDirectoryName(databasePath);

            if (!string.IsNullOrEmpty(databaseDirectory) && !Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            try
            {
                var connectionString = new SQLiteConnectionString(
                    databasePath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create,
                    true);
                connection = new SQLiteConnection(connectionString);
                connection.CreateTable<IntegrationSqliteRow>();
                connection.Insert(new IntegrationSqliteRow()
                {
                    Id = 1,
                    Name = "framework-integration",
                });

                var loadedRow = connection.Table<IntegrationSqliteRow>().FirstOrDefault(row => row.Id == 1);
                if (loadedRow == null || loadedRow.Name != "framework-integration")
                {
                    throw new Exception("SQLite integration row could not be read back from the database.");
                }

                Debug.Log($"[E2E] SQLite pipeline ready. databasePath={databasePath}");
            }
            finally
            {
                connection?.Dispose();
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
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-integration", order: 5, des: "logging-pipeline-ready")]
        public static void LoggingPipelineReady()
        {
            if (Application.isEditor)
            {
                Debug.Log("[E2E] Editor mode skips player log persistence verification.");
                return;
            }

            var logRootPath = BDebug.PlayerLogRootPath;
            if (string.IsNullOrWhiteSpace(logRootPath))
            {
                throw new Exception("Player log root path is empty.");
            }

            Debug.Log($"[E2E] FrameworkIntegration logging probe. root={logRootPath}");
            BDebug.FlushPlayerLogs();
            WaitFor(
                () => Directory.Exists(logRootPath)
                      && !string.IsNullOrWhiteSpace(BDebug.CurrentPlayerLogFilePath)
                      && File.Exists(BDebug.CurrentPlayerLogFilePath),
                PlayerLogWaitTimeoutMs,
                $"Player log persistence did not create a log file under {logRootPath}.");

            Debug.Log($"[E2E] Logging pipeline ready. file={BDebug.CurrentPlayerLogFilePath}");
        }

        /// <summary>
        /// 获取框架总集成测试使用的临时数据库路径。
        /// Get the temporary database path used by the framework-wide integration suite.
        /// </summary>
        /// <returns>位于框架持久化目录下的独立测试数据库路径。</returns>
        /// <returns>An isolated test database path under the framework persistence directory.</returns>
        private static string GetIntegrationDatabasePath()
        {
            return IPath.Combine(BApplication.persistentDataPath, IntegrationDatabaseName);
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
    }
}