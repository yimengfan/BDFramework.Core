using System;
using BDFramework.RuntimeTests.ApiTest.Utils.IO;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 工具层 Runtime API 契约测试套件。
    /// Runtime API contract suite for the utility layer.
    /// 该套件把路径、文件与哈希辅助器的纯逻辑 API 断言迁移为可打包执行的 Talos E2E 用例，
    /// 让真机环境也能持续覆盖这些原本主要停留在 Editor 下的基础工具契约。
    /// This suite moves the pure-logic API assertions for path, file, and hash helpers into packaged Talos E2E cases,
    /// allowing device runs to keep covering these foundational utility contracts that previously lived mainly under the editor.
    /// </summary>
    public static class UtilityApiContractTests
    {
        /// <summary>
        /// 验证路径合并会移除重复分隔符。
        /// Verify that path combination removes duplicate separators.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 1, des: "path-combine-boundary-slashes")]
        public static void PathCombineBoundarySlashes()
        {
            var test = new PathApiTest();
            test.SetUp(nameof(PathCombineBoundarySlashes));
            test.Combine_WithBoundarySlashes_RemovesDuplicateSeparator();
        }

        /// <summary>
        /// 验证路径合并在空输入时直接返回另一侧。
        /// Verify that path combination returns the other side when one input is empty.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 2, des: "path-combine-empty-input")]
        public static void PathCombineEmptyInput()
        {
            var test = new PathApiTest();
            test.SetUp(nameof(PathCombineEmptyInput));
            test.Combine_WithEmptyInput_ReturnsOtherSide();
        }

        /// <summary>
        /// 验证尾部斜杠补齐只发生一次。
        /// Verify that trailing-slash completion happens only once.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 3, des: "path-add-end-symbol")]
        public static void PathAddEndSymbol()
        {
            var test = new PathApiTest();
            test.SetUp(nameof(PathAddEndSymbol));
            test.AddEndSymbol_WithMissingSlash_AppendsTrailingSlashOnce();
        }

        /// <summary>
        /// 验证 Unity 资源路径会被规范化为相对路径。
        /// Verify that Unity asset paths are normalized to relative paths.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 4, des: "path-format-unity-relative")]
        public static void PathFormatUnityRelative()
        {
            var test = new PathApiTest();
            test.SetUp(nameof(PathFormatUnityRelative));
            test.FormatPathOnUnity3d_WithAbsoluteAssetsPath_ReturnsAssetsRelativeNormalizedPath();
        }

        /// <summary>
        /// 验证写入字节时会自动创建目标目录。
        /// Verify that byte writes create the target directory automatically.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 10, des: "file-write-bytes-create-directory")]
        public static void FileWriteBytesCreatesDirectory()
        {
            var test = new FileHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(FileWriteBytesCreatesDirectory)),
                test.WriteAllBytes_WithNestedTarget_CreatesDirectoryAndPersistsBytes,
                test.TearDown);
        }

        /// <summary>
        /// 验证写入文本时会自动创建目标目录。
        /// Verify that text writes create the target directory automatically.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 11, des: "file-write-text-create-directory")]
        public static void FileWriteTextCreatesDirectory()
        {
            var test = new FileHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(FileWriteTextCreatesDirectory)),
                test.WriteAllText_WithNestedTarget_CreatesDirectoryAndPersistsText,
                test.TearDown);
        }

        /// <summary>
        /// 验证 MurmurHash3 的文件与字节重载返回一致结果。
        /// Verify that the file and byte overloads of MurmurHash3 return matching results.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 12, des: "file-murmurhash3-consistency")]
        public static void FileMurmurHash3Consistency()
        {
            var test = new FileHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(FileMurmurHash3Consistency)),
                test.GetMurmurHash3_WithFileAndBytes_ReturnsConsistentHash,
                test.TearDown);
        }

        /// <summary>
        /// 验证 MurmurHash3 缺失文件时会返回既有哨兵值。
        /// Verify that MurmurHash3 returns the existing sentinel value for a missing file.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 13, des: "file-murmurhash3-missing-file")]
        public static void FileMurmurHash3MissingFile()
        {
            var test = new FileHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(FileMurmurHash3MissingFile)),
                test.GetMurmurHash3_WithMissingFile_ReturnsNullSentinelString,
                test.TearDown);
        }

        /// <summary>
        /// 验证 MurmurHash2 的文件与字节重载返回一致结果。
        /// Verify that the file and byte overloads of MurmurHash2 return matching results.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 14, des: "file-murmurhash2-consistency")]
        public static void FileMurmurHash2Consistency()
        {
            var test = new FileHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(FileMurmurHash2Consistency)),
                test.GetMurmurHash2_WithFileAndBytes_ReturnsConsistentHash,
                test.TearDown);
        }

        /// <summary>
        /// 验证字符串 MD5 摘要保持稳定。
        /// Verify that the string MD5 digest remains stable.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 20, des: "hash-md5-string")]
        public static void HashMd5String()
        {
            var test = new HashHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(HashMd5String)),
                test.CreateMD5ByString_WithAsciiInput_ReturnsKnownDigest,
                test.TearDown);
        }

        /// <summary>
        /// 验证文件 MD5 摘要保持稳定。
        /// Verify that the file MD5 digest remains stable.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 21, des: "hash-md5-file")]
        public static void HashMd5File()
        {
            var test = new HashHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(HashMd5File)),
                test.CreateMD5ByFile_WithExistingFile_ReturnsKnownDigest,
                test.TearDown);
        }

        /// <summary>
        /// 验证缺失文件的 MD5 调用返回空字符串。
        /// Verify that the MD5 call for a missing file returns an empty string.
        /// </summary>
        [E2ETest(suite: "utility-api", order: 22, des: "hash-md5-missing-file")]
        public static void HashMd5MissingFile()
        {
            var test = new HashHelperApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(HashMd5MissingFile)),
                test.CreateMD5ByFile_WithMissingFile_ReturnsEmptyString,
                test.TearDown);
        }

        /// <summary>
        /// 按生命周期顺序执行 Runtime API 测试。
        /// Execute a runtime API test in lifecycle order.
        /// </summary>
        private static void ExecuteWithLifecycle(Action setUp, Action action, Action tearDown)
        {
            setUp();
            try
            {
                action();
            }
            finally
            {
                tearDown?.Invoke();
            }
        }
    }
}