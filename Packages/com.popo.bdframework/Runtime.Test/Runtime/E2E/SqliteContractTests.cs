using BDFramework.RuntimeTests.Contracts;
using Talos.E2E;
using UnityEngine;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// SQLite 模块可打包契约测试套件。
    /// Packaged SQLite-module contract test suite.
    /// 该套件覆盖密码回退与快速 JSON 序列化等基础契约，让 SQLite 工具链在真机侧也能持续得到细粒度回归保护。
    /// This suite covers foundational contracts such as password fallback and fast JSON serialization so the SQLite toolchain keeps fine-grained regression protection on player builds as well.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public static class SqliteContractTests
    {
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 1, des: "sqlite-password-fallback")]
        public static void SqlitePasswordFallback()
        {
            SqliteContractAssertions.VerifyPasswordFallbackReturnsFallbackWhenExplicitPasswordIsEmpty();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 2, des: "sqlite-password-explicit-priority")]
        public static void SqlitePasswordExplicitPriority()
        {
            SqliteContractAssertions.VerifyPasswordFallbackPrefersExplicitPasswordOverFallback();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 10, des: "sqlite-fastjson-int-array")]
        public static void SqliteFastJsonIntArray()
        {
            SqliteContractAssertions.VerifyIntArraySerializationRoundTrip();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 11, des: "sqlite-fastjson-long-array")]
        public static void SqliteFastJsonLongArray()
        {
            SqliteContractAssertions.VerifyLongArraySerializationRoundTrip();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 12, des: "sqlite-fastjson-float-array")]
        public static void SqliteFastJsonFloatArray()
        {
            SqliteContractAssertions.VerifyFloatArraySerializationRoundTrip();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 13, des: "sqlite-fastjson-double-array")]
        public static void SqliteFastJsonDoubleArray()
        {
            SqliteContractAssertions.VerifyDoubleArraySerializationRoundTrip();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 14, des: "sqlite-fastjson-bool-array")]
        public static void SqliteFastJsonBoolArray()
        {
            SqliteContractAssertions.VerifyBoolArraySerializationRoundTrip();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 15, des: "sqlite-fastjson-string-array")]
        public static void SqliteFastJsonStringArray()
        {
            SqliteContractAssertions.VerifyStringArraySerializationRoundTrip();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 16, des: "sqlite-fastjson-empty-arrays")]
        public static void SqliteFastJsonEmptyArrays()
        {
            SqliteContractAssertions.VerifyDeserializeEmptyArraysReturnEmpty();
        }

        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 17, des: "sqlite-fastjson-unsupported-type")]
        public static void SqliteFastJsonUnsupportedType()
        {
            SqliteContractAssertions.VerifyUnsupportedArrayTypeThrows();
        }

        /// <summary>
        /// 验证 HotPositiveIntArray 快速路径（1~5 个正整数、无减号/空白/加号）正确反序列化。
        /// Verify HotPositiveIntArray fast path (1-5 positive integers, no minus/whitespace/plus) deserializes correctly.
        /// 测试目的=验证新增的热路径 TryDeserializeHotPositiveIntArray 对典型紧凑 JSON 的正确性。
        /// 实现手段=通过 SqliteContractAssertions 逐组对比期望数组和实际反序列化结果。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 18, des: "sqlite-fastjson-hot-positive-int-fastpath")]
        public static void SqliteFastJsonHotPositiveIntFastPath()
        {
            FrameworkContractAssertions.LogTestPurposeAndMeans(
                nameof(SqliteFastJsonHotPositiveIntFastPath),
                "验证 HotPositiveIntArray 快速路径对 1~5 个正整数的紧凑 JSON 反序列化正确。",
                "通过 SqliteContractAssertions 逐组对比期望数组和 DeserializeArrayInt 结果。");
            SqliteContractAssertions.VerifyHotPositiveIntArrayFastPath();
        }

        /// <summary>
        /// 验证 CompactSmallIntArray 快速路径（1~8 个整数，含负数）正确反序列化。
        /// Verify CompactSmallIntArray fast path (1-8 integers including negatives) deserializes correctly.
        /// 测试目的=验证新增的紧凑路径 TryDeserializeCompactSmallIntArray 对含负数的中等长度数组的正确性。
        /// 实现手段=通过 SqliteContractAssertions 逐组对比含负数的期望数组和实际反序列化结果。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 19, des: "sqlite-fastjson-compact-small-int-fastpath")]
        public static void SqliteFastJsonCompactSmallIntFastPath()
        {
            FrameworkContractAssertions.LogTestPurposeAndMeans(
                nameof(SqliteFastJsonCompactSmallIntFastPath),
                "验证 CompactSmallIntArray 快速路径对含负数的 1~8 元素数组反序列化正确。",
                "通过 SqliteContractAssertions 逐组对比含负数的期望数组和 DeserializeArrayInt 结果。");
            SqliteContractAssertions.VerifyCompactSmallIntArrayFastPath();
        }

        /// <summary>
        /// 验证快速路径在遇到不支持的格式时正确回退到通用路径。
        /// Verify fast paths correctly fall back to the general path for unsupported formats.
        /// 测试目的=验证超过 8 元素、含空白、含加号等场景能正确回退。
        /// 实现手段=通过 SqliteContractAssertions 验证回退后通用路径的结果仍然正确。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [E2ETest(suite: "sqlite-contract", order: 20, des: "sqlite-fastjson-fastpath-fallback")]
        public static void SqliteFastJsonFastPathFallback()
        {
            FrameworkContractAssertions.LogTestPurposeAndMeans(
                nameof(SqliteFastJsonFastPathFallback),
                "验证快速路径在超过 8 元素、含空白、含加号时正确回退到通用路径。",
                "通过 SqliteContractAssertions 验证回退结果与期望值一致。");
            SqliteContractAssertions.VerifyFastPathFallbackToGeneralParser();
        }
    }
}