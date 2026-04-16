using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest.Utils.Logs
{
    /// <summary>
    /// 日志加解密与读取导出 API 的 Runtime 测试主体。
    /// Runtime test body for the log encryption, reading, and export APIs.
    /// 该类型把日志模块的高价值纯逻辑回归收口到 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套日志 API 断言实现。
    /// This type centralizes the high-value pure-logic regressions of the log module into the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same log API assertion implementation.
    /// </summary>
    public sealed class LogCryptoAndReaderApiTest
    {
        /// <summary>
        /// 输出统一日志，记录日志加解密与读取导出 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the log encryption, reading, and export API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(LogCryptoAndReaderApiTest) : testName,
                "验证日志加解密、读取与文本导出 API 在 Runtime 场景下保持稳定。",
                "通过直接调用 LogCrypto、LogReader 与 BDebug 导出入口，并断言往返、异常与导出结果。"
            );
        }

        /// <summary>
        /// 验证日志加密与解密可以稳定完成往返。
        /// Verify that log encryption and decryption complete a stable round trip.
        /// </summary>
        public void EncryptAndDecrypt_RoundTrip_Succeeds()
        {
            LogContractAssertions.VerifyEncryptAndDecryptRoundTrip();
        }

        /// <summary>
        /// 验证错误密码会触发明确的解密失败契约。
        /// Verify that a wrong password triggers the explicit decryption-failure contract.
        /// </summary>
        public void Decrypt_WithWrongPassword_Throws()
        {
            LogContractAssertions.VerifyDecryptWithWrongPasswordThrows();
        }

        /// <summary>
        /// 验证明文与密文日志都可以被读取并导出文本。
        /// Verify that both plain and encrypted logs can be read and exported as text.
        /// </summary>
        public void LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords()
        {
            LogContractAssertions.VerifyLogReaderReadAllAndExportWorksForPlainAndEncryptedRecords();
        }
    }
}