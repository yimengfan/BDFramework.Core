using BDFramework.Logs;
using BDFramework.RuntimeTests.ApiTest.Utils.Logs;
using NUnit.Framework;

namespace BDFramework.EditorTest.Logs
{
    /// <summary>
    /// Runtime 日志持久化 API 测试的编辑器包装器。
    /// Editor wrapper for the runtime log-persistence API tests.
    /// 该类把 Runtime.Test/Runtime/APITest 下的日志持久化断言接入 NUnit，
    /// 仅保留编辑器专属路径断言在 Editor 侧执行。
    /// This class plugs the log-persistence assertions under Runtime.Test/Runtime/APITest into NUnit,
    /// while keeping only the editor-specific path assertion on the editor side.
    /// </summary>
    public class PersistenceAndBDebugTests
    {
        private readonly PersistenceApiTest runtimeTest = new PersistenceApiTest();

        /// <summary>
        /// 在每个测试入口输出统一日志。
        /// Emit a unified log at the start of each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            runtimeTest.SetUp(LogsApiTestLog.ResolveCurrentTestName(nameof(PersistenceAndBDebugTests)));
        }

        /// <summary>
        /// 验证 Player 默认日志持久化设置保持开启且启用加密。
        /// Verify that the default player log-persistence settings remain enabled and encrypted.
        /// </summary>
        [Test]
        public void PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted()
        {
            runtimeTest.PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted();
        }

        /// <summary>
        /// 验证规范化逻辑会补默认目录、最小刷新间隔和默认密码，并保留显式开关值。
        /// Verify that normalization restores the default directory, minimum flush interval, and default password while preserving explicit toggle values.
        /// </summary>
        [Test]
        public void PersistenceSettings_Normalize_UsesDefaultsAndMinimums()
        {
            runtimeTest.PersistenceSettings_Normalize_UsesDefaultsAndMinimums();
        }

        /// <summary>
        /// 验证克隆规范化会返回独立副本，并保留规范化后的值不受原实例后续修改影响。
        /// Verify that clone-normalization returns an independent copy whose normalized values are not affected by later changes to the original instance.
        /// </summary>
        [Test]
        public void CloneNormalized_ReturnsIndependentNormalizedCopy()
        {
            runtimeTest.CloneNormalized_ReturnsIndependentNormalizedCopy();
        }

        /// <summary>
        /// 验证序列化日志条目的本地时间换算保持正确。
        /// Verify that the local-time conversion of serialized log entries remains correct.
        /// </summary>
        [Test]
        public void SerializedLogEntry_LocalTime_ConvertsFromUtcTicks()
        {
            runtimeTest.SerializedLogEntry_LocalTime_ConvertsFromUtcTicks();
        }

        /// <summary>
        /// 验证日志清理策略会删除最旧文件并保留当前活跃文件。
        /// Verify that the log cleanup policy deletes the oldest files while keeping the current active file.
        /// </summary>
        [Test]
        public void CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile()
        {
            runtimeTest.CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile();
        }

        /// <summary>
        /// 验证编辑器环境不会错误暴露 Player 侧日志落盘路径。
        /// Verify that the editor environment does not incorrectly expose player-side log output paths.
        /// </summary>
        [Test]
        public void BDebug_PlayerLogPaths_AreEmptyInEditor()
        {
            Assert.That(BDebug.PlayerLogRootPath, Is.EqualTo(string.Empty));
            Assert.That(BDebug.CurrentPlayerLogFilePath, Is.EqualTo(string.Empty));
        }
    }
}

