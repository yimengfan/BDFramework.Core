using System;
using AssetsManager.Sql;
using BDFramework.Sql;

namespace BDFramework.RuntimeTests.Contracts
{
    /// <summary>
    /// SQLite 模块可打包契约断言集合。
    /// Packaged SQLite-module contract assertion collection.
    /// 该类型覆盖密码回退、快速 JSON 序列化和类型分发等基础契约，
    /// 让 SQLite 核心工具链在真机环境里也能持续得到细粒度回归保护。
    /// This type covers foundational contracts such as password fallback, fast JSON serialization, and type dispatch,
    /// keeping the SQLite core toolchain protected by fine-grained regression checks in player builds as well.
    /// </summary>
    public static class SqliteContractAssertions
    {
        /// <summary>
        /// 验证当显式密码为空时会回退到 PasswordFallback。
        /// Verify that an empty explicit password falls back to PasswordFallback.
        /// </summary>
        public static void VerifyPasswordFallbackReturnsFallbackWhenExplicitPasswordIsEmpty()
        {
            var oldPassword = SqliteLoder.password;
            var oldPasswordFallback = SqliteLoder.PasswordFallback;
            SqliteLoder.password = string.Empty;
            SqliteLoder.PasswordFallback = () => "fallback-password";

            try
            {
                FrameworkContractAssertions.EnsureEqual(
                    "fallback-password",
                    SqliteLoder.Password,
                    "未显式设置密码时，应返回 PasswordFallback 提供的默认密码。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldPasswordFallback;
            }
        }

        /// <summary>
        /// 验证显式密码会优先于回退密码。
        /// Verify that the explicit password takes precedence over the fallback password.
        /// </summary>
        public static void VerifyPasswordFallbackPrefersExplicitPasswordOverFallback()
        {
            var oldPassword = SqliteLoder.password;
            var oldPasswordFallback = SqliteLoder.PasswordFallback;
            SqliteLoder.password = "explicit-password";
            SqliteLoder.PasswordFallback = () => "fallback-password";

            try
            {
                FrameworkContractAssertions.EnsureEqual(
                    "explicit-password",
                    SqliteLoder.Password,
                    "显式密码存在时，应优先返回显式密码而不是回退密码。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldPasswordFallback;
            }
        }

        /// <summary>
        /// 验证 int 数组的序列化与反序列化契约。
        /// Verify the serialization and deserialization contract for int arrays.
        /// </summary>
        public static void VerifyIntArraySerializationRoundTrip()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            VerifyExactJsonAndRoundTrip(source, "[1,2,3,4,5]", SqliteFastJsonConvert.DeserializeArrayInt);
        }

        /// <summary>
        /// 验证 long 数组的序列化与反序列化契约。
        /// Verify the serialization and deserialization contract for long arrays.
        /// </summary>
        public static void VerifyLongArraySerializationRoundTrip()
        {
            var source = new long[] { 10000000000, 20000000000, 30000000000, 40000000000, 50000000000 };
            VerifyExactJsonAndRoundTrip(source, "[10000000000,20000000000,30000000000,40000000000,50000000000]", SqliteFastJsonConvert.DeserializeArrayLong);
        }

        /// <summary>
        /// 验证 float 数组的序列化与反序列化契约。
        /// Verify the serialization and deserialization contract for float arrays.
        /// </summary>
        public static void VerifyFloatArraySerializationRoundTrip()
        {
            var source = new[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f };
            VerifyExactJsonAndRoundTrip(source, "[1.1,2.2,3.3,4.4,5.5]", SqliteFastJsonConvert.DeserializeArrayFloat);
        }

        /// <summary>
        /// 验证 double 数组的序列化与反序列化契约。
        /// Verify the serialization and deserialization contract for double arrays.
        /// </summary>
        public static void VerifyDoubleArraySerializationRoundTrip()
        {
            var source = new[] { 1.11d, 2.22d, 3.33d, 4.44d, 5.55d };
            VerifyExactJsonAndRoundTrip(source, "[1.11,2.22,3.33,4.44,5.55]", SqliteFastJsonConvert.DeserializeArrayDouble);
        }

        /// <summary>
        /// 验证 bool 数组的序列化与反序列化契约。
        /// Verify the serialization and deserialization contract for bool arrays.
        /// </summary>
        public static void VerifyBoolArraySerializationRoundTrip()
        {
            var source = new[] { true, false, true };
            VerifyExactJsonAndRoundTrip(source, "[true,false,true]", SqliteFastJsonConvert.DeserializeArrayBool);
        }

        /// <summary>
        /// 验证 string 数组的序列化与反序列化契约。
        /// Verify the serialization and deserialization contract for string arrays.
        /// </summary>
        public static void VerifyStringArraySerializationRoundTrip()
        {
            var source = new[] { "Hello", "World", "!" };
            VerifyExactJsonAndRoundTrip(source, "[\"Hello\",\"World\",\"!\"]", SqliteFastJsonConvert.DeserializeArrayString);
        }

        /// <summary>
        /// 验证空 JSON 数组会返回空结果。
        /// Verify that an empty JSON array returns an empty result.
        /// </summary>
        public static void VerifyDeserializeEmptyArraysReturnEmpty()
        {
            FrameworkContractAssertions.EnsureEqual(0, SqliteFastJsonConvert.DeserializeArrayInt("[]").Length, "空 int 数组反序列化结果应为空。");
            FrameworkContractAssertions.EnsureEqual(0, SqliteFastJsonConvert.DeserializeArrayLong("[]").Length, "空 long 数组反序列化结果应为空。");
            FrameworkContractAssertions.EnsureEqual(0, SqliteFastJsonConvert.DeserializeArrayFloat("[]").Length, "空 float 数组反序列化结果应为空。");
            FrameworkContractAssertions.EnsureEqual(0, SqliteFastJsonConvert.DeserializeArrayDouble("[]").Length, "空 double 数组反序列化结果应为空。");
            FrameworkContractAssertions.EnsureEqual(0, SqliteFastJsonConvert.DeserializeArrayBool("[]").Length, "空 bool 数组反序列化结果应为空。");
            FrameworkContractAssertions.EnsureEqual(0, SqliteFastJsonConvert.DeserializeArrayString("[]").Length, "空 string 数组反序列化结果应为空。");
        }

        /// <summary>
        /// 验证不支持的数组类型会抛出显式异常。
        /// Verify that unsupported array types throw explicit exceptions.
        /// </summary>
        public static void VerifyUnsupportedArrayTypeThrows()
        {
            try
            {
                SqliteFastJsonConvert.Serialize(new short[] { 1, 2, 3 });
                throw new Exception("不支持的数组序列化应抛出异常。");
            }
            catch (Exception ex)
            {
                FrameworkContractAssertions.EnsureTrue(ex.Message.Contains("不支持类型"), "不支持数组序列化的异常消息应显式说明类型不受支持。");
            }

            try
            {
                SqliteFastJsonConvert.DeserializeArray(typeof(short[]), "[1,2,3]");
                throw new Exception("不支持的数组反序列化应抛出异常。");
            }
            catch (Exception ex)
            {
                FrameworkContractAssertions.EnsureTrue(ex.Message.Contains("不支持类型"), "不支持数组反序列化的异常消息应显式说明类型不受支持。");
            }
        }

        private static void VerifyExactJsonAndRoundTrip(int[] source, string expectedJson, Func<string, int[]> deserializer)
        {
            var json = SqliteFastJsonConvert.Serialize(source);
            FrameworkContractAssertions.EnsureEqual(expectedJson, json, "int 数组序列化 JSON 不匹配。");
            EnsureIntArrayEqual(source, deserializer(json), "int 数组反序列化结果不匹配。");
            EnsureIntArrayEqual(source, (int[])SqliteFastJsonConvert.DeserializeArray(typeof(int[]), json), "int 数组类型分发结果不匹配。");
        }

        private static void VerifyExactJsonAndRoundTrip(long[] source, string expectedJson, Func<string, long[]> deserializer)
        {
            var json = SqliteFastJsonConvert.Serialize(source);
            FrameworkContractAssertions.EnsureEqual(expectedJson, json, "long 数组序列化 JSON 不匹配。");
            EnsureLongArrayEqual(source, deserializer(json), "long 数组反序列化结果不匹配。");
            EnsureLongArrayEqual(source, (long[])SqliteFastJsonConvert.DeserializeArray(typeof(long[]), json), "long 数组类型分发结果不匹配。");
        }

        private static void VerifyExactJsonAndRoundTrip(float[] source, string expectedJson, Func<string, float[]> deserializer)
        {
            var json = SqliteFastJsonConvert.Serialize(source);
            FrameworkContractAssertions.EnsureEqual(expectedJson, json, "float 数组序列化 JSON 不匹配。");
            EnsureFloatArrayEqual(source, deserializer(json), "float 数组反序列化结果不匹配。");
            EnsureFloatArrayEqual(source, (float[])SqliteFastJsonConvert.DeserializeArray(typeof(float[]), json), "float 数组类型分发结果不匹配。");
        }

        private static void VerifyExactJsonAndRoundTrip(double[] source, string expectedJson, Func<string, double[]> deserializer)
        {
            var json = SqliteFastJsonConvert.Serialize(source);
            FrameworkContractAssertions.EnsureEqual(expectedJson, json, "double 数组序列化 JSON 不匹配。");
            EnsureDoubleArrayEqual(source, deserializer(json), "double 数组反序列化结果不匹配。");
            EnsureDoubleArrayEqual(source, (double[])SqliteFastJsonConvert.DeserializeArray(typeof(double[]), json), "double 数组类型分发结果不匹配。");
        }

        private static void VerifyExactJsonAndRoundTrip(bool[] source, string expectedJson, Func<string, bool[]> deserializer)
        {
            var json = SqliteFastJsonConvert.Serialize(source);
            FrameworkContractAssertions.EnsureEqual(expectedJson, json, "bool 数组序列化 JSON 不匹配。");
            EnsureBoolArrayEqual(source, deserializer(json), "bool 数组反序列化结果不匹配。");
            EnsureBoolArrayEqual(source, (bool[])SqliteFastJsonConvert.DeserializeArray(typeof(bool[]), json), "bool 数组类型分发结果不匹配。");
        }

        private static void VerifyExactJsonAndRoundTrip(string[] source, string expectedJson, Func<string, string[]> deserializer)
        {
            var json = SqliteFastJsonConvert.Serialize(source);
            FrameworkContractAssertions.EnsureEqual(expectedJson, json, "string 数组序列化 JSON 不匹配。");
            EnsureStringArrayEqual(source, deserializer(json), "string 数组反序列化结果不匹配。");
            EnsureStringArrayEqual(source, (string[])SqliteFastJsonConvert.DeserializeArray(typeof(string[]), json), "string 数组类型分发结果不匹配。");
        }

        private static void EnsureIntArrayEqual(int[] expected, int[] actual, string message)
        {
            EnsureSameLength(expected.Length, actual.Length, message);
            for (var index = 0; index < expected.Length; index++)
            {
                FrameworkContractAssertions.EnsureEqual(expected[index], actual[index], $"{message} index={index}");
            }
        }

        private static void EnsureLongArrayEqual(long[] expected, long[] actual, string message)
        {
            EnsureSameLength(expected.Length, actual.Length, message);
            for (var index = 0; index < expected.Length; index++)
            {
                FrameworkContractAssertions.EnsureEqual(expected[index], actual[index], $"{message} index={index}");
            }
        }

        private static void EnsureFloatArrayEqual(float[] expected, float[] actual, string message)
        {
            EnsureSameLength(expected.Length, actual.Length, message);
            for (var index = 0; index < expected.Length; index++)
            {
                if (Math.Abs(expected[index] - actual[index]) > 0.0001f)
                {
                    throw new Exception($"{message} index={index} expected={expected[index]} actual={actual[index]}");
                }
            }
        }

        private static void EnsureDoubleArrayEqual(double[] expected, double[] actual, string message)
        {
            EnsureSameLength(expected.Length, actual.Length, message);
            for (var index = 0; index < expected.Length; index++)
            {
                if (Math.Abs(expected[index] - actual[index]) > 0.0001d)
                {
                    throw new Exception($"{message} index={index} expected={expected[index]} actual={actual[index]}");
                }
            }
        }

        private static void EnsureBoolArrayEqual(bool[] expected, bool[] actual, string message)
        {
            EnsureSameLength(expected.Length, actual.Length, message);
            for (var index = 0; index < expected.Length; index++)
            {
                FrameworkContractAssertions.EnsureEqual(expected[index], actual[index], $"{message} index={index}");
            }
        }

        private static void EnsureStringArrayEqual(string[] expected, string[] actual, string message)
        {
            EnsureSameLength(expected.Length, actual.Length, message);
            for (var index = 0; index < expected.Length; index++)
            {
                FrameworkContractAssertions.EnsureEqual(expected[index], actual[index], $"{message} index={index}");
            }
        }

        private static void EnsureSameLength(int expectedLength, int actualLength, string message)
        {
            FrameworkContractAssertions.EnsureEqual(expectedLength, actualLength, $"{message} length");
        }
    }
}