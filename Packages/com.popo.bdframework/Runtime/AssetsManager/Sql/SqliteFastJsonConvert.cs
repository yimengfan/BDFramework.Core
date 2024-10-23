using System;
using System.Diagnostics;
using System.Text;

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
                sb.Append(array[i].ToString().ToLower());
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

        public static int[] DeserializeArrayInt(string json)
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

        public static long[] DeserializeArrayLong(string json)
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

        public static float[] DeserializeArrayFloat(string json)
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

        public static double[] DeserializeArrayDouble(string json)
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

        public static bool[] DeserializeArrayBool(string json)
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

        public static string[] DeserializeArrayString(string json)
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

    }
}
