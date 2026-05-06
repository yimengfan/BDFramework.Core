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
                sb.Append("\"").Append(array[i]?.Replace("\"", "\"\"") ?? "").Append("\"");
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


        #region 反序列化 — 零GC Span版本

        /// <summary>
        /// 去除 Span 首尾的方括号。
        /// .NET Standard 2.0 / Unity 2021 不支持 ReadOnlySpan&lt;char&gt;.Trim(char, char)，
        /// 因此手动跳过前导 '[' 和尾部 ']'。
        /// Trim leading '[' and trailing ']' from Span.
        /// .NET Standard 2.0 / Unity 2021 does not support ReadOnlySpan&lt;char&gt;.Trim(char, char),
        /// so we manually skip leading '[' and trailing ']'.
        /// </summary>
        private static ReadOnlySpan<char> TrimBrackets(ReadOnlySpan<char> span)
        {
            int start = 0, end = span.Length;
            while (start < end && span[start] == '[') start++;
            while (end > start && span[end - 1] == ']') end--;
            return span.Slice(start, end - start);
        }

        /// <summary>
        /// 解析方括号内的元素数量（预分配数组用）。
        /// Parse the number of elements inside brackets for pre-allocation.
        /// </summary>
        private static int CountElements(ReadOnlySpan<char> content)
        {
            if (content.IsEmpty || content.IsWhiteSpace()) return 0;
            int count = 1;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == ',') count++;
            }
            return count;
        }

        /// <summary>
        /// 跳过字符串字面量（引号内的内容，含转义）。
        /// Skip a string literal inside quotes, handling escape sequences.
        /// </summary>
        private static void SkipStringLiteral(ReadOnlySpan<char> span, ref int i)
        {
            i++; // skip opening quote
            while (i < span.Length)
            {
                if (span[i] == '"' && (i + 1 >= span.Length || span[i + 1] != '"'))
                {
                    i++; // skip closing quote
                    return;
                }
                if (span[i] == '"') i++; // escaped quote ""
                i++;
            }
        }

        /// <summary>
        /// 从 [a,b,c] 格式的 JSON 字符串中提取 string[] 元素。
        /// 使用 Span 逐字符扫描，避免 string.Split 产生的 string[] GC 分配。
        /// Extract string[] elements from [a,b,c] JSON format.
        /// Uses Span character-by-character scanning to avoid string.Split GC allocation.
        /// </summary>
        private static string[] ParseStringElements(ReadOnlySpan<char> content)
        {
            if (content.IsEmpty) return Array.Empty<string>();
            int estimated = CountElements(content);
            var results = new string[estimated];
            int index = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '"')
                {
                    if (!inString)
                    {
                        start = i + 1;
                        inString = true;
                        // 跳过字符串内容
                        SkipStringLiteral(content, ref i);
                        // SkipStringLiteral 跳过整个字符串(含闭合引号), i 现在指向闭合引号之后
                        // 闭合引号在 i-1 位置, 元素内容在 [start, i-1-start)
                        var element = content.Slice(start, i - 1 - start).ToString();
                        if (index < results.Length)
                        {
                            results[index++] = element.Replace("\"\"", "\"");
                        }
                        else
                        {
                            Array.Resize(ref results, results.Length * 2);
                            results[index++] = element.Replace("\"\"", "\"");
                        }
                        inString = false;
                        // i 已经在闭合引号之后, 回退以让循环的 i++ 正确推进
                        i--;
                    }
                    else
                    {
                        // 此分支在修复后不再需要到达
                        // 但保留以兼容边界情况
                        var element = content.Slice(start, i - start).ToString();
                        if (index < results.Length)
                        {
                            results[index++] = element.Replace("\"\"", "\"");
                        }
                        else
                        {
                            Array.Resize(ref results, results.Length * 2);
                            results[index++] = element.Replace("\"\"", "\"");
                        }
                        inString = false;
                    }
                }
                else if (!inString && content[i] == ',')
                {
                    // 非字符串元素之间的逗号 — 忽略
                }
            }

            if (index == 0) return Array.Empty<string>();
            if (index != results.Length)
            {
                Array.Resize(ref results, index);
            }
            return results;
        }

        /// <summary>
        /// 通用值类型数组解析。逐段扫描逗号分隔的值，避免 Split 分配。
        /// Generic value-type array parser. Scans comma-separated segments without Split allocation.
        /// </summary>
        private static T[] ParseValueElements<T>(ReadOnlySpan<char> content, Func<string, T> parser)
        {
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<T>();
            int estimated = CountElements(content);
            var results = new T[estimated];
            int index = 0;
            int segStart = 0;

            for (int i = 0; i <= content.Length; i++)
            {
                bool isEnd = (i == content.Length);
                bool isComma = !isEnd && content[i] == ',';
                // 逗号在引号内不作为分隔符
                if (isComma || isEnd)
                {
                    var segment = content.Slice(segStart, i - segStart).Trim();
                    if (!segment.IsEmpty)
                    {
                        T val = parser(segment.ToString());
                        if (index < results.Length)
                        {
                            results[index++] = val;
                        }
                        else
                        {
                            Array.Resize(ref results, results.Length * 2);
                            results[index++] = val;
                        }
                    }
                    segStart = i + 1;
                }
            }

            if (index == 0) return Array.Empty<T>();
            if (index != results.Length)
            {
                Array.Resize(ref results, index);
            }
            return results;
        }

        public static int[] DeserializeArrayInt(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<int>();
            var content = TrimBrackets(json.AsSpan());
            return ParseValueElements(content, s => int.Parse(s));
        }

        public static long[] DeserializeArrayLong(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<long>();
            var content = TrimBrackets(json.AsSpan());
            return ParseValueElements(content, s => long.Parse(s));
        }

        public static float[] DeserializeArrayFloat(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<float>();
            var content = TrimBrackets(json.AsSpan());
            return ParseValueElements(content, s => float.Parse(s));
        }

        public static double[] DeserializeArrayDouble(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<double>();
            var content = TrimBrackets(json.AsSpan());
            return ParseValueElements(content, s => double.Parse(s));
        }

        public static bool[] DeserializeArrayBool(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<bool>();
            var content = TrimBrackets(json.AsSpan());
            return ParseValueElements(content, s => bool.Parse(s));
        }

        public static string[] DeserializeArrayString(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
            var content = TrimBrackets(json.AsSpan());
            return ParseStringElements(content);
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
