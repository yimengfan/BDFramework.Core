using System;
using System.Diagnostics;
using System.Text;
using LitJson;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AssetsManager.Sql
{
    static public class SqliteFastJsonConvert
    {
        #region 序列化

        /// <summary>
        /// 将 string[] 转换为 JSON 数组格式字符串
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static string SerializeString(string[] array)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append("\"").Append(array[i]).Append("\"");
                if (i < array.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }


        /// <summary>
        /// 泛型方法以支持数组的序列化
        /// </summary>
        /// <param name="array"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static string SerializeAny<T>(T[] array)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                if (i < array.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        #endregion


        #region 反序列化

        private static int[] DeserializeArrayInt(string json)
        {
            json = json.Trim('[', ']');
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<int>();
            }

            var items = json.Split(',');
            int[] result = new int[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string trimmedItem = items[i];
                if (int.TryParse(trimmedItem, out int intValue))
                {
                    result[index++] = intValue;
                }
            }


            return result;
        }

        private static long[] DeserializeArrayLong(string json)
        {
            json = json.Trim('[', ']');
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<long>();
            }

            var items = json.Split(',');
            long[] result = new long[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string trimmedItem = items[i];
                if (long.TryParse(trimmedItem, out long longValue))
                {
                    result[index++] = longValue;
                }
            }


            return result;
        }

        private static float[] DeserializeArrayFloat(string json)
        {
            json = json.Trim('[', ']');
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<float>();
            }

            var items = json.Split(',');
            float[] result = new float[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string trimmedItem = items[i];
                if (float.TryParse(trimmedItem, out float floatValue))
                {
                    result[index++] = floatValue;
                }
            }


            return result;
        }

        private static double[] DeserializeArrayDouble(string json)
        {
            json = json.Trim('[', ']');
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<double>();
            }

            var items = json.Split(',');
            double[] result = new double[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string trimmedItem = items[i];
                if (double.TryParse(trimmedItem, out double doubleValue))
                {
                    result[index++] = doubleValue;
                }
            }


            return result;
        }

        private static bool[] DeserializeArrayBool(string json)
        {
            json = json.Trim('[', ']');
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<bool>();
            }

            var items = json.Split(',');
            bool[] result = new bool[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string trimmedItem = items[i];
                if (bool.TryParse(trimmedItem, out bool boolValue))
                {
                    result[index++] = boolValue;
                }
            }


            return result;
        }

        private static string[] DeserializeArrayString(string json)
        {
            json = json.Trim('[', ']');
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<string>();
            }

            var items = json.Split(',');
            string[] result = new string[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string trimmedItem = items[i];
                result[index++] = trimmedItem.Trim('"'); // 去掉引号
            }


            return result;
        }


        #endregion

        /// <summary>
        /// 泛型方法以支持数组的序列化
        /// </summary>
        /// <param name="array"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string Serialize(object array)
        {
            var arrayType = array.GetType();
            if (arrayType == typeof(string[]))
            {
                return SerializeString((string[]) array);
            }
            else if (arrayType == typeof(int[]))
            {
                return SerializeAny((int[]) array);
            }
            else if (arrayType == typeof(float[]))
            {
                return SerializeAny((float[]) array);
            }
            else if (arrayType == typeof(double[]))
            {
                return SerializeAny((double[]) array);
            }
            else if (arrayType == typeof(long[]))
            {
                return SerializeAny((long[]) array);
            }
            else if (arrayType == typeof(bool[]))
            {
                return SerializeAny((bool[]) array);
            }
            else
            {
                throw new Exception("不支持类型:" + array.GetType().FullName);
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static object DeserializeArray(Type arrayType, string json)
        {
            // 去掉开始和结束的方括号
            if (arrayType == typeof(int[]))
            {
                var array = DeserializeArrayInt(json);
                return array;
            }
            else if (arrayType == typeof(string[]))
            {
                var array = DeserializeArrayString(json);
                return array;
            }
            else if (arrayType == typeof(float[]))
            {
                var array = DeserializeArrayFloat(json);
                return array;
            }
            else if (arrayType == typeof(double[]))
            {
                var array = DeserializeArrayDouble(json);
                return array;
            }
            else if (arrayType == typeof(long[]))
            {
                var array = DeserializeArrayLong(json);
                return array;
            }
            else if (arrayType == typeof(bool[]))
            {
                var array = DeserializeArrayBool(json);
                return array;
            }
            else
            {
                throw new Exception("不支持类型:" + arrayType.FullName);
            }

            return null;
        }
#if UNITY_EDITOR
        // 测试方法
        [MenuItem("BDFrameWork工具箱/TestPipeline/Sqlite/json序列化")]
        public static void Main()
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

        static private void TestIntArraySerialization()
        {
            int[] ints = new int[5] {1, 2, 3, 4, 5};
            var jsonInts = Serialize(ints);
            var jsonInts2 = JsonMapper.ToJson(ints);
            Debug.Log("my ints json: " + jsonInts);
            Debug.Log("json ints: " + jsonInts2);
            if (!jsonInts.Equals(jsonInts2))
            {
                Debug.LogError("int 序列化失败!");
            }

            int[] intResult = DeserializeArrayInt(jsonInts);
            int[] intResult2 = JsonMapper.ToObject<int[]>(jsonInts);
            for (int i = 0; i < intResult.Length; i++)
            {
                if (intResult[i] != intResult2[i])
                {
                    Debug.LogError("int 反序列化失败!");
                    break;
                }
            }

            TestCustomDeserialization(typeof(int[]), jsonInts, 100000);
            TestJsonMapperDeserialization(typeof(int[]), jsonInts, 100000);
        }

        static private void TestLongArraySerialization()
        {
            long[] longs = new long[5] {10000000000, 20000000000, 30000000000, 40000000000, 50000000000};
            var jsonLongs = Serialize(longs);
            var jsonLongs2 = JsonMapper.ToJson(longs);
            Debug.Log("my longs json: " + jsonLongs);
            Debug.Log("json longs: " + jsonLongs2);
            if (!jsonLongs.Equals(jsonLongs2))
            {
                Debug.LogError("long 序列化失败!");
            }

            long[] longResult = DeserializeArrayLong(jsonLongs);
            long[] longResult2 = JsonMapper.ToObject<long[]>(jsonLongs);
            for (int i = 0; i < longResult.Length; i++)
            {
                if (longResult[i] != longResult2[i])
                {
                    Debug.LogError("long 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(long[]), jsonLongs, 100000);
            TestJsonMapperDeserialization(typeof(long []), jsonLongs, 100000);
        }

        static private void TestFloatArraySerialization()
        {
            float[] floats = new float[5] {1.1f, 2.2f, 3.3f, 4.4f, 5.5f};
            var jsonFloats = Serialize(floats);
            var jsonFloats2 = JsonMapper.ToJson(floats);
            Debug.Log("my floats json: " + jsonFloats);
            Debug.Log("json floats: " + jsonFloats2);
            if (!jsonFloats.Equals(jsonFloats2))
            {
                Debug.LogError("float 序列化失败!");
            }

            float[] floatResult = DeserializeArrayFloat(jsonFloats);
            float[] floatResult2 = JsonMapper.ToObject<float[]>(jsonFloats);
            for (int i = 0; i < floatResult.Length; i++)
            {
                if (Math.Abs(floatResult[i] - floatResult2[i]) > 0.0001f) // 浮点数比较时要考虑精度
                {
                    Debug.LogError("float 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(float[]), jsonFloats, 100000);
            TestJsonMapperDeserialization(typeof(float []), jsonFloats, 100000);
        }

        static private void TestDoubleArraySerialization()
        {
            double[] doubles = new double[5] {1.11, 2.22, 3.33, 4.44, 5.55};
            var jsonDoubles = Serialize(doubles);
            var jsonDoubles2 = JsonMapper.ToJson(doubles);
            Debug.Log("my doubles json: " + jsonDoubles);
            Debug.Log("json doubles: " + jsonDoubles2);
            if (!jsonDoubles.Equals(jsonDoubles2))
            {
                Debug.LogError("double 序列化失败!");
            }

            double[] doubleResult = DeserializeArrayDouble(jsonDoubles);
            double[] doubleResult2 = JsonMapper.ToObject<double[]>(jsonDoubles);
            for (int i = 0; i < doubleResult.Length; i++)
            {
                if (Math.Abs(doubleResult[i] - doubleResult2[i]) > 0.0001) // 双精度浮点数比较时要考虑精度
                {
                    Debug.LogError("double 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(double[]), jsonDoubles, 100000);
            TestJsonMapperDeserialization(typeof(double []), jsonDoubles, 100000);
        }

        static private void TestBoolArraySerialization()
        {
            bool[] bools = new bool[3] {true, false, true};
            var jsonBools = Serialize(bools);
            var jsonBools2 = JsonMapper.ToJson(bools);
            Debug.Log("my bools json: " + jsonBools);
            Debug.Log("json bools: " + jsonBools2);
            if (!jsonBools.Equals(jsonBools2))
            {
                Debug.LogError("bool 序列化失败!");
            }

            bool[] boolResult = DeserializeArrayBool(jsonBools);
            bool[] boolResult2 = JsonMapper.ToObject<bool[]>(jsonBools2);
            for (int i = 0; i < boolResult.Length; i++)
            {
                if (boolResult[i] != boolResult2[i])
                {
                    Debug.LogError("bool 反序列化失败!");
                    break;
                }
            }
            
            TestCustomDeserialization(typeof(bool[]), jsonBools, 100000);
            TestJsonMapperDeserialization(typeof(bool []), jsonBools2, 100000);
        }

        static private void TestStringArraySerialization()
        {
            string[] strings = new string[3] {"Hello", "World", "!"};
            var jsonStrings = Serialize(strings);
            var jsonStrings2 = JsonMapper.ToJson(strings);
            Debug.Log("my strings json: " + jsonStrings);
            Debug.Log("json strings: " + jsonStrings2);
            if (!jsonStrings.Equals(jsonStrings2))
            {
                Debug.LogError("string 序列化失败!");
            }

            string[] stringResult = DeserializeArrayString(jsonStrings);
            string[] stringResult2 = JsonMapper.ToObject<string[]>(jsonStrings);
            for (int i = 0; i < stringResult.Length; i++)
            {
                if (stringResult[i] != stringResult2[i])
                {
                    Debug.LogError("string 反序列化失败!");
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
                DeserializeArray(type, json);
            }

            stopwatch.Stop();
            Debug.Log($"自定义反序列化耗时: {stopwatch.ElapsedMilliseconds} ms");
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
            Debug.Log($"JsonMapper 反序列化耗时: {stopwatch.ElapsedMilliseconds} ms");
        }
#endif
    }
}
