using System;
using System.IO;
using System.Text;

namespace BDFramework.RuntimeTests.ApiTest.Utils.IO
{
    /// <summary>
    /// 文件工具公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the file helper.
    /// 该类型把目录创建、文件写入与 MurmurHash 契约固定在 Runtime.Test 的 APITest 层内，
    /// 让 Editor NUnit 包装与真机 Talos E2E 复用同一套断言实现。
    /// This type fixes the directory-creation, file-write, and MurmurHash contracts inside the Runtime.Test APITest layer,
    /// allowing editor NUnit wrappers and packaged Talos E2E runs to reuse the same assertion implementation.
    /// </summary>
    public sealed class FileHelperApiTest
    {
        private string temporaryRootDirectory;

        /// <summary>
        /// 创建测试临时目录并输出统一日志。
        /// Create the test temporary directory and emit a unified log.
        /// </summary>
        public void SetUp(string testName)
        {
            temporaryRootDirectory = Path.Combine(Path.GetTempPath(), "bdframework-file-helper-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRootDirectory);

            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(FileHelperApiTest) : testName,
                "验证文件工具的目录创建、写入与哈希行为保持稳定。",
                "通过临时目录构造真实文件并直接调用 FileHelper 公开 API，断言文件内容与哈希结果。"
            );
        }

        /// <summary>
        /// 清理测试创建的临时目录，避免磁盘副作用污染后续验证。
        /// Clean up the temporary directory created by the test so disk side effects do not leak into later verification.
        /// </summary>
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(temporaryRootDirectory) && Directory.Exists(temporaryRootDirectory))
            {
                Directory.Delete(temporaryRootDirectory, true);
            }

            temporaryRootDirectory = null;
        }

        /// <summary>
        /// 验证写入字节时会自动创建目标目录并保留原始字节内容。
        /// Verify that writing bytes automatically creates the target directory and preserves the original byte content.
        /// </summary>
        public void WriteAllBytes_WithNestedTarget_CreatesDirectoryAndPersistsBytes()
        {
            var targetPath = Path.Combine(temporaryRootDirectory, "nested", "payload.bytes");
            var expected = new byte[] { 1, 2, 3, 4, 5 };

            FileHelper.WriteAllBytes(targetPath, expected);

            ApiTestAssert.IsTrue(File.Exists(targetPath), "WriteAllBytes 应在目标路径上生成文件。");
            ApiTestAssert.SequenceEqual(expected, File.ReadAllBytes(targetPath), "WriteAllBytes 写入后的字节内容不匹配。");
        }

        /// <summary>
        /// 验证写入文本时会自动创建目标目录并保留原始文本内容。
        /// Verify that writing text automatically creates the target directory and preserves the original text content.
        /// </summary>
        public void WriteAllText_WithNestedTarget_CreatesDirectoryAndPersistsText()
        {
            var targetPath = Path.Combine(temporaryRootDirectory, "nested", "payload.txt");
            const string Expected = "file-helper-text";

            FileHelper.WriteAllText(targetPath, Expected);

            ApiTestAssert.IsTrue(File.Exists(targetPath), "WriteAllText 应在目标路径上生成文件。");
            ApiTestAssert.AreEqual(Expected, File.ReadAllText(targetPath), "WriteAllText 写入后的文本内容不匹配。");
        }

        /// <summary>
        /// 验证 MurmurHash3 的文件版本与字节版本对同一内容会返回一致结果。
        /// Verify that the file and byte overloads of MurmurHash3 return the same result for identical content.
        /// </summary>
        public void GetMurmurHash3_WithFileAndBytes_ReturnsConsistentHash()
        {
            var targetPath = Path.Combine(temporaryRootDirectory, "nested", "payload.bytes");
            var bytes = Encoding.UTF8.GetBytes("hash-helper-3");
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllBytes(targetPath, bytes);

            var fileHash = FileHelper.GetMurmurHash3(targetPath);
            var bytesHash = FileHelper.GetMurmurHash3(bytes);

            ApiTestAssert.AreEqual(bytesHash, fileHash, "MurmurHash3 的文件与字节结果应保持一致。");
        }

        /// <summary>
        /// 验证缺失文件计算 MurmurHash3 时，会返回既有的字符串哨兵值而不是抛出异常。
        /// Verify that calculating MurmurHash3 for a missing file returns the existing string sentinel instead of throwing.
        /// </summary>
        public void GetMurmurHash3_WithMissingFile_ReturnsNullSentinelString()
        {
            var missingPath = Path.Combine(temporaryRootDirectory, "missing.bytes");

            var hash = FileHelper.GetMurmurHash3(missingPath);

            ApiTestAssert.AreEqual("null", hash, "缺失文件的 MurmurHash3 结果应保持为 null 哨兵字符串。");
        }

        /// <summary>
        /// 验证 MurmurHash2 的文件版本与字节版本对同一内容会返回一致结果。
        /// Verify that the file and byte overloads of MurmurHash2 return the same result for identical content.
        /// </summary>
        public void GetMurmurHash2_WithFileAndBytes_ReturnsConsistentHash()
        {
            var targetPath = Path.Combine(temporaryRootDirectory, "nested", "payload.bytes");
            var bytes = Encoding.UTF8.GetBytes("hash-helper-2");
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllBytes(targetPath, bytes);

            var fileHash = FileHelper.GetMurmurHash2(targetPath);
            var bytesHash = FileHelper.GetMurmurHash2(bytes);

            ApiTestAssert.AreEqual(bytesHash, fileHash, "MurmurHash2 的文件与字节结果应保持一致。");
        }
    }
}