using System;
using System.IO;
using AssetsManager.Sql;
using NUnit.Framework;
using SQLite4Unity3d;
using Debug = UnityEngine.Debug;

namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// SqliteFastJsonConvert 优化项反序列化单元测试。
    /// 覆盖 O6（两遍扫描替代 List&lt;T&gt;.ToArray()）、O7（Span float/double 解析）
    /// 和 O10（UnescapeString 内联转义引号）的正确性。
    /// 测试目的=验证各类型数组的反序列化结果与输入一致，包括边界值和特殊格式。
    /// 实现手段=直接调用 SqliteFastJsonConvert.DeserializeArray* 方法并断言结果。
    /// Unit tests for SqliteFastJsonConvert optimization deserialization.
    /// Covers correctness of O6 (two-pass scan replacing List&lt;T&gt;.ToArray()),
    /// O7 (Span-based float/double parsing), and O10 (UnescapeString inline escape).
    /// Test purpose=verify deserialized arrays match input for all types, including boundary and special formats.
    /// Method=directly call SqliteFastJsonConvert.DeserializeArray* methods and assert results.
    /// </summary>
    [TestFixture]
    public class SqliteFastJsonConvertOptimizationTest
    {
        [Table("SqliteFastJsonHotIntArrayRoundtrip")]
        private class HotIntArrayRoundtripPoco
        {
            [PrimaryKey]
            public int Id { get; set; }

            public int[] Values { get; set; }
        }

        [SetUp]
        public void LogCurrentTestPurpose()
        {
            Debug.Log("[测试开始] 测试目的=验证 SqliteFastJsonConvert 的热点 int[] 直解路径及回退路径结果正确。 实现手段=直接调用 DeserializeArray* 或通过 SQLiteConnection 执行真实查询 roundtrip。");
        }

        // ═══════════════════════════════════════════
        // int[] 测试 / int[] tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayInt_Basic()
        {
            var json = "[1,2,3,100,-5,0,999999]";
            var result = SqliteFastJsonConvert.DeserializeArrayInt(json);
            Assert.AreEqual(new int[] { 1, 2, 3, 100, -5, 0, 999999 }, result);
        }

        [Test]
        public void DeserializeArrayInt_EmptyArray()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[]");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayInt_NullInput()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt(null);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayInt_EmptyString()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayInt_SingleElement()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[42]");
            Assert.AreEqual(new int[] { 42 }, result);
        }

        [Test]
        public void DeserializeArrayInt_NegativeNumbers()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[-1,-100,-999]");
            Assert.AreEqual(new int[] { -1, -100, -999 }, result);
        }

        [Test]
        public void DeserializeArrayInt_WhitespaceBetweenElements()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[1 , 2 , 3]");
            Assert.AreEqual(new int[] { 1, 2, 3 }, result);
        }

        [Test]
        public void DeserializeArrayInt_LargeArray()
        {
            var elements = new int[200];
            var parts = new string[200];
            for (int i = 0; i < 200; i++)
            {
                elements[i] = i * 7;
                parts[i] = (i * 7).ToString();
            }
            var json = "[" + string.Join(",", parts) + "]";
            var result = SqliteFastJsonConvert.DeserializeArrayInt(json);
            Assert.AreEqual(elements, result);
        }

        [Test]
        public void DeserializeArrayInt_CompactEightElements()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[1,20,300,4000,50000,6,7,8]");
            Assert.AreEqual(new int[] { 1, 20, 300, 4000, 50000, 6, 7, 8 }, result);
        }

        [Test]
        public void DeserializeArrayInt_CompactPositiveFiveElements()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[12,345,6,7890,42]");
            Assert.AreEqual(new int[] { 12, 345, 6, 7890, 42 }, result);
        }

        [Test]
        public void DeserializeArrayInt_PositiveSignFallsBackToGeneralParser()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayInt("[+1,+20,+300]");
            Assert.AreEqual(new int[] { 1, 20, 300 }, result);
        }

        [Test]
        public void QueryIntArray_CompactPositiveValues_RoundTripsThroughUtf8HotPath()
        {
            var result = RoundTripIntArrayThroughQuery(new[] { 12, 345, 6, 7890, 42 });
            CollectionAssert.AreEqual(new[] { 12, 345, 6, 7890, 42 }, result);
        }

        [Test]
        public void QueryIntArray_NegativeValues_FallsBackAndStillRoundTrips()
        {
            var result = RoundTripIntArrayThroughQuery(new[] { -1, 20, -300, 0 });
            CollectionAssert.AreEqual(new[] { -1, 20, -300, 0 }, result);
        }

        // ═══════════════════════════════════════════
        // long[] 测试 / long[] tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayLong_Basic()
        {
            var json = "[1,9999999999,-123456789,0]";
            var result = SqliteFastJsonConvert.DeserializeArrayLong(json);
            Assert.AreEqual(new long[] { 1L, 9999999999L, -123456789L, 0L }, result);
        }

        [Test]
        public void DeserializeArrayLong_EmptyArray()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayLong("[]");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayLong_SingleElement()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayLong("[1234567890123]");
            Assert.AreEqual(new long[] { 1234567890123L }, result);
        }

        // ═══════════════════════════════════════════
        // float[] 测试 / float[] tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayFloat_Basic()
        {
            var json = "[1.1,2.5,-3.14,0,999.999]";
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(1.1f, result[0], 0.001f);
            Assert.AreEqual(2.5f, result[1], 0.001f);
            Assert.AreEqual(-3.14f, result[2], 0.01f);
            Assert.AreEqual(0f, result[3], 0.001f);
            Assert.AreEqual(999.999f, result[4], 0.01f);
        }

        [Test]
        public void DeserializeArrayFloat_EmptyArray()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayFloat("[]");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayFloat_SingleElement()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayFloat("[3.14]");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(3.14f, result[0], 0.001f);
        }

        /// <summary>
        /// 通过真实 SQLite 查询链路验证 int[] 会经过 FastSetter 与列读取热路径。
        /// Verifies int[] roundtrips through the real SQLite query path so FastSetter and column hot paths are exercised.
        /// </summary>
        private static int[] RoundTripIntArrayThroughQuery(int[] values)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"sqlite-fastjson-hotpath-{Guid.NewGuid():N}.db");

            try
            {
                using (var writeConnection = new SQLiteConnection(dbPath))
                {
                    writeConnection.CreateTable<HotIntArrayRoundtripPoco>();
                    writeConnection.Insert(new HotIntArrayRoundtripPoco
                    {
                        Id = 1,
                        Values = values
                    });
                }

                using (var readConnection = new SQLiteConnection(dbPath))
                {
                    var rows = readConnection.Query<HotIntArrayRoundtripPoco>("SELECT * FROM SqliteFastJsonHotIntArrayRoundtrip LIMIT 1");
                    Assert.AreEqual(1, rows.Count, "应当只查询到一条 roundtrip 测试数据。/ The roundtrip query should return exactly one row.");
                    return rows[0].Values;
                }
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
        }

        [Test]
        public void DeserializeArrayFloat_NegativeNumbers()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayFloat("[-1.5,-0.001,-99.99]");
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(-1.5f, result[0], 0.001f);
            Assert.AreEqual(-0.001f, result[1], 0.0001f);
            Assert.AreEqual(-99.99f, result[2], 0.01f);
        }

        [Test]
        public void DeserializeArrayFloat_ZeroValues()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayFloat("[0,0.0,-0]");
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(0f, result[0], 0.001f);
            Assert.AreEqual(0f, result[1], 0.001f);
            Assert.AreEqual(0f, result[2], 0.001f);
        }

        [Test]
        public void DeserializeArrayFloat_WhitespaceBetweenElements()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayFloat("[1.1 , 2.2 , 3.3]");
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1.1f, result[0], 0.001f);
            Assert.AreEqual(2.2f, result[1], 0.001f);
            Assert.AreEqual(3.3f, result[2], 0.001f);
        }

        [Test]
        public void DeserializeArrayFloat_IntegerValues()
        {
            // 整数格式的 float（如 "1" 而非 "1.0"）
            // Integer-format floats (e.g. "1" instead of "1.0")
            var result = SqliteFastJsonConvert.DeserializeArrayFloat("[1,2,3]");
            Assert.AreEqual(new float[] { 1f, 2f, 3f }, result);
        }

        [Test]
        public void DeserializeArrayFloat_LargeArray()
        {
            var elements = new float[100];
            var parts = new string[100];
            var rng = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                elements[i] = (float)Math.Round(rng.NextDouble() * 1000, 2);
                parts[i] = elements[i].ToString("G");
            }
            var json = "[" + string.Join(",", parts) + "]";
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(100, result.Length);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(elements[i], result[i], 0.02f, $"Mismatch at index {i}");
            }
        }

        // ═══════════════════════════════════════════
        // double[] 测试 / double[] tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayDouble_Basic()
        {
            var json = "[1.12345678,2,-3.14,0]";
            var result = SqliteFastJsonConvert.DeserializeArrayDouble(json);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(1.12345678d, result[0], 0.00000001d);
            Assert.AreEqual(2.0d, result[1], 0.001d);
            Assert.AreEqual(-3.14d, result[2], 0.01d);
            Assert.AreEqual(0d, result[3], 0.001d);
        }

        [Test]
        public void DeserializeArrayDouble_EmptyArray()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayDouble("[]");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayDouble_SingleElement()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayDouble("[3.14159265358979]");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(3.14159265358979d, result[0], 0.0000000001d);
        }

        [Test]
        public void DeserializeArrayDouble_NegativeNumbers()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayDouble("[-1.5,-0.001,-99.99]");
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(-1.5d, result[0], 0.001d);
            Assert.AreEqual(-0.001d, result[1], 0.0001d);
            Assert.AreEqual(-99.99d, result[2], 0.01d);
        }

        // ═══════════════════════════════════════════
        // bool[] 测试 / bool[] tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayBool_Basic()
        {
            var json = "[true,false,true]";
            var result = SqliteFastJsonConvert.DeserializeArrayBool(json);
            Assert.AreEqual(new bool[] { true, false, true }, result);
        }

        [Test]
        public void DeserializeArrayBool_EmptyArray()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayBool("[]");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayBool_AllTrue()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayBool("[true,true,true]");
            Assert.AreEqual(new bool[] { true, true, true }, result);
        }

        [Test]
        public void DeserializeArrayBool_AllFalse()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayBool("[false,false]");
            Assert.AreEqual(new bool[] { false, false }, result);
        }

        // ═══════════════════════════════════════════
        // string[] 测试 / string[] tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayString_Basic()
        {
            var json = "[\"hello\",\"world\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(new string[] { "hello", "world" }, result);
        }

        [Test]
        public void DeserializeArrayString_EmptyArray()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayString("[]");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void DeserializeArrayString_SingleElement()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayString("[\"test\"]");
            Assert.AreEqual(new string[] { "test" }, result);
        }

        [Test]
        public void DeserializeArrayString_EmptyStringElement()
        {
            var result = SqliteFastJsonConvert.DeserializeArrayString("[\"\"]");
            Assert.AreEqual(new string[] { "" }, result);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote()
        {
            // 包含转义引号的字符串 "" → "
            // String with escaped quotes "" → "
            var json = "[\"say \\\"hello\\\"\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("say \"hello\"", result[0]);
        }

        // ═══════════════════════════════════════════
        // O10 专项: UnescapeString 内联转义测试
        // O10 specific: UnescapeString inline escape tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_MultipleEscapes()
        {
            // 多个转义引号 / Multiple escaped quotes
            var json = "[\"a\\\"b\\\"c\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("a\"b\"c", result[0]);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_AdjacentEscapes()
        {
            // 相邻转义引号 """" → "" / Adjacent escaped quotes
            var json = "[\"\\\"\\\"\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("\"\"", result[0]);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_NoEscape()
        {
            // 无转义引号的快速路径 / Fast path with no escaped quotes
            var json = "[\"hello\",\"world\",\"test\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(new string[] { "hello", "world", "test" }, result);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_MixedElements()
        {
            // 混合有转义和无转义的元素 / Mix of escaped and non-escaped elements
            var json = "[\"normal\",\"with\\\"quote\",\"another\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("normal", result[0]);
            Assert.AreEqual("with\"quote", result[1]);
            Assert.AreEqual("another", result[2]);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_QuoteAtStart()
        {
            // 转义引号在开头 / Escaped quote at start
            var json = "[\"\\\"hello\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("\"hello", result[0]);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_QuoteAtEnd()
        {
            // 转义引号在末尾 / Escaped quote at end
            var json = "[\"hello\\\"\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("hello\"", result[0]);
        }

        [Test]
        public void DeserializeArrayString_EscapedQuote_O10_RoundTrip()
        {
            // 序列化+反序列化往返测试（含引号）/ Round-trip with quotes
            var original = new string[] { "say \"hello\"", "a\"b\"c", "\"quoted\"" };
            var json = SqliteFastJsonConvert.Serialize(original);
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void DeserializeArrayString_CommaInElement()
        {
            var json = "[\"a,b\",\"c\"]";
            var result = SqliteFastJsonConvert.DeserializeArrayString(json);
            Assert.AreEqual(new string[] { "a,b", "c" }, result);
        }

        // ═══════════════════════════════════════════
        // 序列化+反序列化 往返测试 / Round-trip tests
        // ═══════════════════════════════════════════

        [Test]
        public void RoundTrip_IntArray()
        {
            var original = new int[] { 1, 2, 3, 100, -5, 0, 999999 };
            var json = SqliteFastJsonConvert.Serialize(original);
            var result = SqliteFastJsonConvert.DeserializeArrayInt(json);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void RoundTrip_FloatArray()
        {
            var original = new float[] { 1.1f, 2.5f, -3.14f, 0f, 999.999f };
            var json = SqliteFastJsonConvert.Serialize(original);
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(original.Length, result.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.AreEqual(original[i], result[i], 0.01f, $"Mismatch at index {i}");
            }
        }

        [Test]
        public void RoundTrip_DoubleArray()
        {
            var original = new double[] { 1.12345678d, 2.0d, -3.14d, 0d };
            var json = SqliteFastJsonConvert.Serialize(original);
            var result = SqliteFastJsonConvert.DeserializeArrayDouble(json);
            Assert.AreEqual(original.Length, result.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.AreEqual(original[i], result[i], 0.0001d, $"Mismatch at index {i}");
            }
        }

        [Test]
        public void RoundTrip_BoolArray()
        {
            var original = new bool[] { true, false, true, false, true };
            var json = SqliteFastJsonConvert.Serialize(original);
            var result = SqliteFastJsonConvert.DeserializeArrayBool(json);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void RoundTrip_LongArray()
        {
            var original = new long[] { 1L, 9999999999L, -123456789L, 0L };
            var json = SqliteFastJsonConvert.Serialize(original);
            var result = SqliteFastJsonConvert.DeserializeArrayLong(json);
            Assert.AreEqual(original, result);
        }

        // ═══════════════════════════════════════════
        // O7 专项: ParseFloatFromSpan 边界值测试
        // O7 specific: ParseFloatFromSpan boundary value tests
        // ═══════════════════════════════════════════

        [Test]
        public void DeserializeArrayFloat_Precision_6DecimalDigits()
        {
            // 6 位小数精度测试 — 游戏配置常见精度
            // 6-digit decimal precision test — common in game configs
            var json = "[0.123456,1.654321]";
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(0.123456f, result[0], 0.000001f);
            Assert.AreEqual(1.654321f, result[1], 0.000001f);
        }

        [Test]
        public void DeserializeArrayFloat_LeadingZero()
        {
            var json = "[0.5,0.001,0.999]";
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(0.5f, result[0], 0.001f);
            Assert.AreEqual(0.001f, result[1], 0.0001f);
            Assert.AreEqual(0.999f, result[2], 0.001f);
        }

        [Test]
        public void DeserializeArrayDouble_Precision_10DecimalDigits()
        {
            var json = "[1.1234567890]";
            var result = SqliteFastJsonConvert.DeserializeArrayDouble(json);
            Assert.AreEqual(1.123456789d, result[0], 0.000000001d);
        }

        [Test]
        public void DeserializeArrayFloat_PositiveSign()
        {
            // 带正号的 float / float with explicit positive sign
            var json = "[+1.5,+3.14]";
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(1.5f, result[0], 0.001f);
            Assert.AreEqual(3.14f, result[1], 0.01f);
        }

        [Test]
        public void DeserializeArrayDouble_PositiveSign()
        {
            var json = "[+2.5,+99.99]";
            var result = SqliteFastJsonConvert.DeserializeArrayDouble(json);
            Assert.AreEqual(2.5d, result[0], 0.001d);
            Assert.AreEqual(99.99d, result[1], 0.01d);
        }

        [Test]
        public void DeserializeArrayFloat_MixedIntegerAndDecimal()
        {
            // 混合整数和小数格式 / Mixed integer and decimal formats
            var json = "[1,2.5,3,4.75,5]";
            var result = SqliteFastJsonConvert.DeserializeArrayFloat(json);
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(1f, result[0], 0.001f);
            Assert.AreEqual(2.5f, result[1], 0.001f);
            Assert.AreEqual(3f, result[2], 0.001f);
            Assert.AreEqual(4.75f, result[3], 0.01f);
            Assert.AreEqual(5f, result[4], 0.001f);
        }

        // ═══════════════════════════════════════════
        // 序列化测试 / Serialization tests
        // ═══════════════════════════════════════════

        [Test]
        public void Serialize_IntArray()
        {
            var original = new int[] { 1, 2, 3 };
            var json = SqliteFastJsonConvert.Serialize(original);
            Assert.AreEqual("[1,2,3]", json);
        }

        [Test]
        public void Serialize_FloatArray()
        {
            var original = new float[] { 1.1f, 2.5f };
            var json = SqliteFastJsonConvert.Serialize(original);
            Assert.IsTrue(json.StartsWith("["));
            Assert.IsTrue(json.EndsWith("]"));
        }

        [Test]
        public void Serialize_StringArray()
        {
            var original = new string[] { "hello", "world" };
            var json = SqliteFastJsonConvert.Serialize(original);
            Assert.AreEqual("[\"hello\",\"world\"]", json);
        }
    }
}
