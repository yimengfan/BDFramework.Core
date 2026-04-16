using BDFramework.Logs;
using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest.Utils.Logs
{
    /// <summary>
    /// 日志持久化相关 API 的 Runtime 测试主体。
    /// Runtime test body for the log-persistence related APIs.
    /// 该类型固定默认持久化配置、规范化、克隆与保留策略等公开契约，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套日志持久化断言。
    /// This type fixes public contracts such as default persistence settings, normalization, cloning, and retention policy,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same log-persistence assertions.
    /// </summary>
    public sealed class PersistenceApiTest
    {
        /// <summary>
        /// 输出统一日志，记录日志持久化 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the log-persistence API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(PersistenceApiTest) : testName,
                "验证日志持久化配置、时间换算与保留策略 API 在 Runtime 场景下保持稳定。",
                "通过直接调用 PersistenceSettings、SerializedLogEntry 与底层保留策略 helper，并断言默认值、归一化与清理结果。"
            );
        }

        /// <summary>
        /// 验证 Player 默认日志持久化设置保持开启且启用加密。
        /// Verify that the default player log-persistence settings remain enabled and encrypted.
        /// </summary>
        public void PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted()
        {
            LogContractAssertions.VerifyDefaultPlayerSettingsAreEnabledAndEncrypted();
        }

        /// <summary>
        /// 验证规范化逻辑会补默认目录、最小刷新间隔和默认密码，并保留显式开关值。
        /// Verify that normalization restores the default directory, minimum flush interval, and default password while preserving explicit toggle values.
        /// </summary>
        public void PersistenceSettings_Normalize_UsesDefaultsAndMinimums()
        {
            var settings = new PersistenceSettings()
            {
                EnablePersistence = false,
                DirectoryName = "   ",
                FlushIntervalMs = 1,
                EnableEncryption = false,
                EncryptPassword = string.Empty,
            }.Normalize();

            ApiTestAssert.IsTrue(!settings.EnablePersistence, "Normalize 不应覆盖显式关闭的持久化开关。");
            ApiTestAssert.AreEqual(PersistenceSettings.DEFAULT_DIRECTORY_NAME, settings.DirectoryName,
                "空白目录名应回退到默认 playerlogs 目录。");
            ApiTestAssert.AreEqual(PersistenceSettings.MIN_FLUSH_INTERVAL_MS, settings.FlushIntervalMs,
                "过小的刷新间隔应回退到最小允许值。");
            ApiTestAssert.IsTrue(!settings.EnableEncryption, "Normalize 不应覆盖显式关闭的加密开关。");
            ApiTestAssert.AreEqual(LogCrypto.DEFAULT_PASSWORD, settings.EncryptPassword,
                "空密码应回退到默认日志密码。");
        }

        /// <summary>
        /// 验证克隆规范化会返回独立副本，并保留规范化后的值不受原实例后续修改影响。
        /// Verify that clone-normalization returns an independent copy whose normalized values are not affected by later changes to the original instance.
        /// </summary>
        public void CloneNormalized_ReturnsIndependentNormalizedCopy()
        {
            var settings = new PersistenceSettings()
            {
                EnablePersistence = true,
                DirectoryName = " logs ",
                FlushIntervalMs = 10,
                EnableEncryption = true,
                EncryptPassword = string.Empty,
            };

            var clone = settings.CloneNormalized();
            settings.DirectoryName = "mutated";
            settings.FlushIntervalMs = 999;
            settings.EncryptPassword = "changed";

            ApiTestAssert.IsTrue(!ReferenceEquals(settings, clone), "CloneNormalized 应返回独立实例而不是原对象本身。");
            ApiTestAssert.AreEqual("logs", clone.DirectoryName, "克隆结果应保留规范化后的目录名。");
            ApiTestAssert.AreEqual(PersistenceSettings.MIN_FLUSH_INTERVAL_MS, clone.FlushIntervalMs,
                "克隆结果应保留规范化后的最小刷新间隔。");
            ApiTestAssert.AreEqual(LogCrypto.DEFAULT_PASSWORD, clone.EncryptPassword,
                "克隆结果应保留规范化后的默认密码。");
        }

        /// <summary>
        /// 验证序列化日志条目的本地时间换算保持正确。
        /// Verify that the local-time conversion of serialized log entries remains correct.
        /// </summary>
        public void SerializedLogEntry_LocalTime_ConvertsFromUtcTicks()
        {
            LogContractAssertions.VerifySerializedLogEntryLocalTimeConvertsFromUtcTicks();
        }

        /// <summary>
        /// 验证日志清理策略会删除最旧文件并保留当前活跃文件。
        /// Verify that the log cleanup policy deletes the oldest files while keeping the current active file.
        /// </summary>
        public void CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile()
        {
            LogContractAssertions.VerifyCleanupOldLogFilesDeletesOldestAndKeepsActiveFile();
        }
    }
}