using BDFramework.RuntimeTests.Contracts;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// SQLite 模块可打包契约测试套件。
    /// Packaged SQLite-module contract test suite.
    /// 该套件覆盖密码回退与快速 JSON 序列化等基础契约，让 SQLite 工具链在真机侧也能持续得到细粒度回归保护。
    /// This suite covers foundational contracts such as password fallback and fast JSON serialization so the SQLite toolchain keeps fine-grained regression protection on player builds as well.
    /// </summary>
    public static class SqliteContractTests
    {
        [E2ETest(suite: "sqlite-contract", order: 1, des: "sqlite-password-fallback")]
        public static void SqlitePasswordFallback()
        {
            SqliteContractAssertions.VerifyPasswordFallbackReturnsFallbackWhenExplicitPasswordIsEmpty();
        }

        [E2ETest(suite: "sqlite-contract", order: 2, des: "sqlite-password-explicit-priority")]
        public static void SqlitePasswordExplicitPriority()
        {
            SqliteContractAssertions.VerifyPasswordFallbackPrefersExplicitPasswordOverFallback();
        }

        [E2ETest(suite: "sqlite-contract", order: 10, des: "sqlite-fastjson-int-array")]
        public static void SqliteFastJsonIntArray()
        {
            SqliteContractAssertions.VerifyIntArraySerializationRoundTrip();
        }

        [E2ETest(suite: "sqlite-contract", order: 11, des: "sqlite-fastjson-long-array")]
        public static void SqliteFastJsonLongArray()
        {
            SqliteContractAssertions.VerifyLongArraySerializationRoundTrip();
        }

        [E2ETest(suite: "sqlite-contract", order: 12, des: "sqlite-fastjson-float-array")]
        public static void SqliteFastJsonFloatArray()
        {
            SqliteContractAssertions.VerifyFloatArraySerializationRoundTrip();
        }

        [E2ETest(suite: "sqlite-contract", order: 13, des: "sqlite-fastjson-double-array")]
        public static void SqliteFastJsonDoubleArray()
        {
            SqliteContractAssertions.VerifyDoubleArraySerializationRoundTrip();
        }

        [E2ETest(suite: "sqlite-contract", order: 14, des: "sqlite-fastjson-bool-array")]
        public static void SqliteFastJsonBoolArray()
        {
            SqliteContractAssertions.VerifyBoolArraySerializationRoundTrip();
        }

        [E2ETest(suite: "sqlite-contract", order: 15, des: "sqlite-fastjson-string-array")]
        public static void SqliteFastJsonStringArray()
        {
            SqliteContractAssertions.VerifyStringArraySerializationRoundTrip();
        }

        [E2ETest(suite: "sqlite-contract", order: 16, des: "sqlite-fastjson-empty-arrays")]
        public static void SqliteFastJsonEmptyArrays()
        {
            SqliteContractAssertions.VerifyDeserializeEmptyArraysReturnEmpty();
        }

        [E2ETest(suite: "sqlite-contract", order: 17, des: "sqlite-fastjson-unsupported-type")]
        public static void SqliteFastJsonUnsupportedType()
        {
            SqliteContractAssertions.VerifyUnsupportedArrayTypeThrows();
        }
    }
}