using BDFramework.RuntimeTests.ApiTest.Utils.Logs;
using NUnit.Framework;

namespace BDFramework.EditorTest.Logs
{
    /// <summary>
    /// Runtime 日志加解密与读取导出 API 测试的编辑器包装器。
    /// Editor wrapper for the runtime log encryption, reading, and export API tests.
    /// 该类把 Runtime.Test/Runtime/APITest 下的日志 API 断言接入 NUnit，
    /// 让 Editor 验证与真机 Talos 套件共享同一套日志契约实现。
    /// This class plugs the log API assertions under Runtime.Test/Runtime/APITest into NUnit,
    /// ensuring the editor verification and packaged Talos suites share the same log-contract implementation.
    /// </summary>
    public class LogCryptoAndReaderTests
    {
        private readonly LogCryptoAndReaderApiTest runtimeTest = new LogCryptoAndReaderApiTest();

        /// <summary>
        /// 在每个测试入口输出统一日志。
        /// Emit a unified log at the start of each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            runtimeTest.SetUp(LogsApiTestLog.ResolveCurrentTestName(nameof(LogCryptoAndReaderTests)));
        }

        /// <summary>
        /// 验证日志加密与解密可以稳定完成往返。
        /// Verify that log encryption and decryption complete a stable round trip.
        /// </summary>
        [Test]
        public void EncryptAndDecrypt_RoundTrip_Succeeds()
        {
            runtimeTest.EncryptAndDecrypt_RoundTrip_Succeeds();
        }

        /// <summary>
        /// 验证错误密码会触发明确的解密失败契约。
        /// Verify that a wrong password triggers the explicit decryption-failure contract.
        /// </summary>
        [Test]
        public void Decrypt_WithWrongPassword_Throws()
        {
            runtimeTest.Decrypt_WithWrongPassword_Throws();
        }

        /// <summary>
        /// 验证明文与密文日志都可以被读取并导出文本。
        /// Verify that both plain and encrypted logs can be read and exported as text.
        /// </summary>
        [Test]
        public void LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords()
        {
            runtimeTest.LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords();
        }
    }
}

