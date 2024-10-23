using System;
using System.Diagnostics;
using AssetsManager.Sql;
using LitJson;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BDFramework.EditorTest.SQLite
{
    public class SqliteFastJsonConvertTest
    {
                // 测试方法
       // [MenuItem("BDFrameWork工具箱/TestPipeline/Sqlite/json序列化")]
       [Test, Order(1),Performance]
        public static void Benchmark_DeSerialize()
        {
            // 整型数组测试
            TestIntArraySerialization();
            Debug.Log("<color=red>-------------------------</color>");
            // 长整型数组测试
            TestLongArraySerialization();
            Debug.Log("<color=red>-------------------------</color>");
            // 浮点型数组测试
            TestFloatArraySerialization();
            Debug.Log("<color=red>-------------------------</color>");
            // 双精度浮点型数组测试
            TestDoubleArraySerialization();
            Debug.Log("<color=red>-------------------------</color>");
            // 布尔型数组测试
            TestBoolArraySerialization();
            Debug.Log("<color=red>-------------------------</color>");
            // 字符串数组测试
            TestStringArraySerialization();
            Debug.Log("<color=red>-------------------------</color>");
        }
        
        [Test, Order(1),Performance]
        public static void DeSerializeValueTest()
        {
            
        }

        static private void TestIntArraySerialization()
        {
            int[] ints = new int[5] {1, 2, 3, 4, 5};
            var jsonInts =SqliteFastJsonConvert.  Serialize(ints);
            var jsonInts2 = JsonMapper.ToJson(ints);
            Debug.Log("my ints json: " + jsonInts);
            Debug.Log("json ints: " + jsonInts2);
            if (!jsonInts.Equals(jsonInts2))
            {
                Assert.Fail("int 序列化失败!");
            }

            int[] intResult = SqliteFastJsonConvert. DeserializeArrayInt(jsonInts);
            int[] intResult2 = JsonMapper.ToObject<int[]>(jsonInts);
            for (int i = 0; i < intResult.Length; i++)
            {
                if (intResult[i] != intResult2[i])
                {
                    Assert.Fail("int 反序列化失败!");
                    break;
                }
            }

            TestCustomDeserialization(typeof(int[]), jsonInts, 100000);
            TestJsonMapperDeserialization(typeof(int[]), jsonInts, 100000);
        }

        static private void TestLongArraySerialization()
        {
            long[] longs = new long[5] {10000000000, 20000000000, 30000000000, 40000000000, 50000000000};
            var jsonLongs = SqliteFastJsonConvert. Serialize(longs);
            var jsonLongs2 = JsonMapper.ToJson(longs);
            Debug.Log("my longs json: " + jsonLongs);
            Debug.Log("json longs: " + jsonLongs2);
            if (!jsonLongs.Equals(jsonLongs2))
            {
                Assert.Fail("long 序列化失败!");
            }

            long[] longResult = SqliteFastJsonConvert. DeserializeArrayLong(jsonLongs);
            long[] longResult2 = JsonMapper.ToObject<long[]>(jsonLongs);
            for (int i = 0; i < longResult.Length; i++)
            {
                if (longResult[i] != longResult2[i])
                {
                    Assert.Fail("long 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(long[]), jsonLongs, 100000);
            TestJsonMapperDeserialization(typeof(long []), jsonLongs, 100000);
        }

        static private void TestFloatArraySerialization()
        {
            float[] floats = new float[5] {1.1f, 2.2f, 3.3f, 4.4f, 5.5f};
            var jsonFloats = SqliteFastJsonConvert. Serialize(floats);
            var jsonFloats2 = JsonMapper.ToJson(floats);
            Debug.Log("my floats json: " + jsonFloats);
            Debug.Log("json floats: " + jsonFloats2);
            if (!jsonFloats.Equals(jsonFloats2))
            {
                Assert.Fail("float 序列化失败!");
            }

            float[] floatResult = SqliteFastJsonConvert. DeserializeArrayFloat(jsonFloats);
            float[] floatResult2 = JsonMapper.ToObject<float[]>(jsonFloats);
            for (int i = 0; i < floatResult.Length; i++)
            {
                if (Math.Abs(floatResult[i] - floatResult2[i]) > 0.0001f) // 浮点数比较时要考虑精度
                {
                    Assert.Fail("float 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(float[]), jsonFloats, 100000);
            TestJsonMapperDeserialization(typeof(float []), jsonFloats, 100000);
        }

        static private void TestDoubleArraySerialization()
        {
            double[] doubles = new double[5] {1.11, 2.22, 3.33, 4.44, 5.55};
            var jsonDoubles = SqliteFastJsonConvert. Serialize(doubles);
            var jsonDoubles2 = JsonMapper.ToJson(doubles);
            Debug.Log("my doubles json: " + jsonDoubles);
            Debug.Log("json doubles: " + jsonDoubles2);
            if (!jsonDoubles.Equals(jsonDoubles2))
            {
                Assert.Fail("double 序列化失败!");
            }

            double[] doubleResult = SqliteFastJsonConvert. DeserializeArrayDouble(jsonDoubles);
            double[] doubleResult2 = JsonMapper.ToObject<double[]>(jsonDoubles);
            for (int i = 0; i < doubleResult.Length; i++)
            {
                if (Math.Abs(doubleResult[i] - doubleResult2[i]) > 0.0001) // 双精度浮点数比较时要考虑精度
                {
                    Assert.Fail("double 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(double[]), jsonDoubles, 100000);
            TestJsonMapperDeserialization(typeof(double []), jsonDoubles, 100000);
        }

        static private void TestBoolArraySerialization()
        {
            bool[] bools = new bool[3] {true, false, true};
            var jsonBools = SqliteFastJsonConvert. Serialize(bools);
            var jsonBools2 = JsonMapper.ToJson(bools);
            Debug.Log("my bools json: " + jsonBools);
            Debug.Log("json bools: " + jsonBools2);
            if (!jsonBools.Equals(jsonBools2))
            {
                Assert.Fail("bool 序列化失败!");
            }

            bool[] boolResult =SqliteFastJsonConvert. DeserializeArrayBool(jsonBools);
            bool[] boolResult2 = JsonMapper.ToObject<bool[]>(jsonBools2);
            for (int i = 0; i < boolResult.Length; i++)
            {
                if (boolResult[i] != boolResult2[i])
                {
                    Assert.Fail("bool 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(bool[]), jsonBools, 100000);
            TestJsonMapperDeserialization(typeof(bool []), jsonBools2, 100000);
        }

        static private void TestStringArraySerialization()
        {
            string[] strings = new string[3] {"Hello", "World", "!"};
            var jsonStrings = SqliteFastJsonConvert.Serialize(strings);
            var jsonStrings2 = JsonMapper.ToJson(strings);
            Debug.Log("my strings json: " + jsonStrings);
            Debug.Log("json strings: " + jsonStrings2);
            if (!jsonStrings.Equals(jsonStrings2))
            {
                Assert.Fail("string 序列化失败!");
            }

            string[] stringResult = SqliteFastJsonConvert.DeserializeArrayString(jsonStrings);
            string[] stringResult2 = JsonMapper.ToObject<string[]>(jsonStrings);
            for (int i = 0; i < stringResult.Length; i++)
            {
                if (stringResult[i] != stringResult2[i])
                {

                    Assert.Fail("string 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(string[]), jsonStrings, 100000);
            TestJsonMapperDeserialization(typeof(string []), jsonStrings, 100000);
        }


        static private void TestCustomDeserialization(Type type, string json, int count)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // 自定义反序列化
            for (int i = 0; i < count; i++)
            {
              SqliteFastJsonConvert. DeserializeArray(type, json);
            }

            stopwatch.Stop();
            Debug.Log($"自定义反序列化耗时:  <color=yellow>{stopwatch.ElapsedMilliseconds} ms</color>");
        }

        static private void TestJsonMapperDeserialization(Type type, string json, int count)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // JsonMapper 反序列化
            for (int i = 0; i < count; i++)
            {
                JsonMapper.ToObject(type, json);
            }

            stopwatch.Stop();
            Debug.Log($"JsonMapper 反序列化耗时: <color=yellow>{stopwatch.ElapsedMilliseconds} ms</color>");
        }
    }
}
