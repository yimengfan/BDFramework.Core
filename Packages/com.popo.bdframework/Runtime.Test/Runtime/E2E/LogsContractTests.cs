using BDFramework.RuntimeTests.ApiTest.Utils.Logs;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 日志模块可打包契约测试套件。
    /// Packaged log-module contract test suite.
    /// 该套件覆盖日志模块的纯逻辑契约，让加解密、读取导出和保留策略可以在真机与 BatchMode 下统一验证。
    /// This suite covers the pure-logic contracts of the log module so encryption, reading and export, and retention policy can be validated consistently in player builds and BatchMode.
    /// </summary>
    public static class LogsContractTests
    {
        [E2ETest(suite: "logs-contract", order: 1, des: "log-crypto-roundtrip")]
        public static void LogCryptoRoundTrip()
        {
            var test = new LogCryptoAndReaderApiTest();
            test.SetUp(nameof(LogCryptoRoundTrip));
            test.EncryptAndDecrypt_RoundTrip_Succeeds();
        }

        [E2ETest(suite: "logs-contract", order: 2, des: "log-crypto-wrong-password")]
        public static void LogCryptoWrongPassword()
        {
            var test = new LogCryptoAndReaderApiTest();
            test.SetUp(nameof(LogCryptoWrongPassword));
            test.Decrypt_WithWrongPassword_Throws();
        }

        [E2ETest(suite: "logs-contract", order: 3, des: "log-reader-export")]
        public static void LogReaderExport()
        {
            var test = new LogCryptoAndReaderApiTest();
            test.SetUp(nameof(LogReaderExport));
            test.LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords();
        }

        [E2ETest(suite: "logs-contract", order: 4, des: "log-default-settings")]
        public static void LogDefaultSettings()
        {
            var test = new PersistenceApiTest();
            test.SetUp(nameof(LogDefaultSettings));
            test.PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted();
        }

        [E2ETest(suite: "logs-contract", order: 5, des: "log-settings-normalize")]
        public static void LogSettingsNormalize()
        {
            var test = new PersistenceApiTest();
            test.SetUp(nameof(LogSettingsNormalize));
            test.PersistenceSettings_Normalize_UsesDefaultsAndMinimums();
        }

        [E2ETest(suite: "logs-contract", order: 6, des: "log-clone-normalized")]
        public static void LogCloneNormalized()
        {
            var test = new PersistenceApiTest();
            test.SetUp(nameof(LogCloneNormalized));
            test.CloneNormalized_ReturnsIndependentNormalizedCopy();
        }

        [E2ETest(suite: "logs-contract", order: 7, des: "log-local-time-conversion")]
        public static void LogLocalTimeConversion()
        {
            var test = new PersistenceApiTest();
            test.SetUp(nameof(LogLocalTimeConversion));
            test.SerializedLogEntry_LocalTime_ConvertsFromUtcTicks();
        }

        [E2ETest(suite: "logs-contract", order: 8, des: "log-retention-policy")]
        public static void LogRetentionPolicy()
        {
            var test = new PersistenceApiTest();
            test.SetUp(nameof(LogRetentionPolicy));
            test.CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile();
        }
    }
}