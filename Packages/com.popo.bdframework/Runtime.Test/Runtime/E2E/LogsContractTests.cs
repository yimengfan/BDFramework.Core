using BDFramework.RuntimeTests.Contracts;
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
            LogContractAssertions.VerifyEncryptAndDecryptRoundTrip();
        }

        [E2ETest(suite: "logs-contract", order: 2, des: "log-crypto-wrong-password")]
        public static void LogCryptoWrongPassword()
        {
            LogContractAssertions.VerifyDecryptWithWrongPasswordThrows();
        }

        [E2ETest(suite: "logs-contract", order: 3, des: "log-reader-export")]
        public static void LogReaderExport()
        {
            LogContractAssertions.VerifyLogReaderReadAllAndExportWorksForPlainAndEncryptedRecords();
        }

        [E2ETest(suite: "logs-contract", order: 4, des: "log-default-settings")]
        public static void LogDefaultSettings()
        {
            LogContractAssertions.VerifyDefaultPlayerSettingsAreEnabledAndEncrypted();
        }

        [E2ETest(suite: "logs-contract", order: 5, des: "log-local-time-conversion")]
        public static void LogLocalTimeConversion()
        {
            LogContractAssertions.VerifySerializedLogEntryLocalTimeConvertsFromUtcTicks();
        }

        [E2ETest(suite: "logs-contract", order: 6, des: "log-retention-policy")]
        public static void LogRetentionPolicy()
        {
            LogContractAssertions.VerifyCleanupOldLogFilesDeletesOldestAndKeepsActiveFile();
        }
    }
}