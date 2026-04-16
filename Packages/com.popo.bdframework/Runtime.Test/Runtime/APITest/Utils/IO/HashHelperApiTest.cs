using System;
using System.IO;
using System.Text;
using BDFramework.Hash;

namespace BDFramework.RuntimeTests.ApiTest.Utils.IO
{
    /// <summary>
    /// 哈希辅助器公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the hash helper.
    /// 该类型把字符串与文件 MD5 契约固定在 Runtime.Test 的 APITest 层内，
    /// 便于工具链、本地 Batch 与真机执行复用同一套摘要验证逻辑。
    /// This type fixes the string and file MD5 contracts inside the Runtime.Test APITest layer,
    /// allowing tooling, local Batch runs, and packaged-player executions to reuse the same digest-validation logic.
    /// </summary>
    public sealed class HashHelperApiTest
    {
        private string temporaryRootDirectory;

        /// <summary>
        /// 创建测试临时目录并输出统一日志。
        /// Create the test temporary directory and emit a unified log.
        /// </summary>
        public void SetUp(string testName)
        {
            temporaryRootDirectory = Path.Combine(Path.GetTempPath(), "bdframework-hash-helper-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRootDirectory);

            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(HashHelperApiTest) : testName,
                "验证哈希辅助器的字符串与文件 MD5 行为保持稳定。",
                "通过固定输入字符串和临时文件调用 HashHelper 公开 API，并断言输出摘要。"
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
        /// 验证 ASCII 字符串输入会输出稳定的既定 MD5 摘要。
        /// Verify that an ASCII string input produces the stable expected MD5 digest.
        /// </summary>
        public void CreateMD5ByString_WithAsciiInput_ReturnsKnownDigest()
        {
            var digest = HashHelper.CreateMD5ByString("abc");

            ApiTestAssert.AreEqual("900150983cd24fb0d6963f7d28e17f72", digest, "CreateMD5ByString 的摘要结果不匹配。");
        }

        /// <summary>
        /// 验证存在的文件会输出稳定的既定 MD5 摘要。
        /// Verify that an existing file produces the stable expected MD5 digest.
        /// </summary>
        public void CreateMD5ByFile_WithExistingFile_ReturnsKnownDigest()
        {
            var targetPath = Path.Combine(temporaryRootDirectory, "payload.txt");
            File.WriteAllText(targetPath, "abc", new UTF8Encoding(false));

            var digest = HashHelper.CreateMD5ByFile(targetPath);

            ApiTestAssert.AreEqual("900150983cd24fb0d6963f7d28e17f72", digest, "CreateMD5ByFile 的文件摘要结果不匹配。");
        }

        /// <summary>
        /// 验证缺失文件时会返回空字符串，而不是抛出异常。
        /// Verify that a missing file returns an empty string instead of throwing an exception.
        /// </summary>
        public void CreateMD5ByFile_WithMissingFile_ReturnsEmptyString()
        {
            var digest = HashHelper.CreateMD5ByFile(Path.Combine(temporaryRootDirectory, "missing.txt"));

            ApiTestAssert.AreEqual(string.Empty, digest, "缺失文件时的 MD5 结果应保持为空字符串。");
        }
    }
}