using System;
using System.Collections.Generic;
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
        /// 从 ReadOnlySpan&lt;char&gt; 直接解析 int，不创建中间 string。
        /// 手动遍历字符计算数值，支持负号和前导空格。
        /// Parse int directly from ReadOnlySpan&lt;char&gt; without creating intermediate string.
        /// Manually iterates characters to compute value, supports negative sign and leading whitespace.
        /// </summary>
        private static int ParseIntFromSpan(ReadOnlySpan<char> span)
        {
            int i = 0;
            // 跳过前导空格 / Skip leading whitespace
            while (i < span.Length && span[i] == ' ') i++;

            bool negative = false;
            if (i < span.Length && span[i] == '-')
            {
                negative = true;
                i++;
            }
            else if (i < span.Length && span[i] == '+')
            {
                i++;
            }

            int result = 0;
            while (i < span.Length)
            {
                int ch = span[i] - '0';
                if (ch < 0 || ch > 9) break;
                result = result * 10 + ch;
                i++;
            }

            return negative ? -result : result;
        }

        /// <summary>
        /// 从 ReadOnlySpan&lt;char&gt; 直接解析 long，不创建中间 string。
        /// 手动遍历字符计算数值，支持负号和前导空格。
        /// Parse long directly from ReadOnlySpan&lt;char&gt; without creating intermediate string.
        /// Manually iterates characters to compute value, supports negative sign and leading whitespace.
        /// </summary>
        private static long ParseLongFromSpan(ReadOnlySpan<char> span)
        {
            int i = 0;
            // 跳过前导空格 / Skip leading whitespace
            while (i < span.Length && span[i] == ' ') i++;

            bool negative = false;
            if (i < span.Length && span[i] == '-')
            {
                negative = true;
                i++;
            }
            else if (i < span.Length && span[i] == '+')
            {
                i++;
            }

            long result = 0;
            while (i < span.Length)
            {
                int ch = span[i] - '0';
                if (ch < 0 || ch > 9) break;
                result = result * 10 + ch;
                i++;
            }

            return negative ? -result : result;
        }

        /// <summary>
        /// 跳过 Span 中的空白字符，返回去除前后空白的子切片。
        /// 不使用 .Trim() 以避免 .NET Standard 2.0 下的潜在分配。
        /// Skip whitespace in Span, return sub-slice with leading/trailing whitespace removed.
        /// Avoids .Trim() to prevent potential allocation under .NET Standard 2.0.
        /// </summary>
        private static ReadOnlySpan<char> TrimSpan(ReadOnlySpan<char> span)
        {
            int start = 0;
            while (start < span.Length && (span[start] == ' ' || span[start] == '\t' || span[start] == '\r' || span[start] == '\n'))
                start++;
            int end = span.Length;
            while (end > start && (span[end - 1] == ' ' || span[end - 1] == '\t' || span[end - 1] == '\r' || span[end - 1] == '\n'))
                end--;
            return span.Slice(start, end - start);
        }

        /// <summary>
        /// 单次遍历解析 int[]。合并计数+解析为一次遍历，使用 List&lt;int&gt; 避免二次扫描。
        /// 热路径优化：ParseIntFromSpan 不创建临时 string，直接从 Span 读取数字。
        /// Single-pass int[] parser. Merges counting + parsing into one traversal, uses List&lt;int&gt; to avoid double scan.
        /// Hot-path optimization: ParseIntFromSpan does not create temporary strings, reads digits directly from Span.
        /// </summary>
        public static int[] DeserializeArrayInt(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<int>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<int>();

            var list = new List<int>(16);
            int segStart = 0;

            for (int i = 0; i <= content.Length; i++)
            {
                bool isEnd = (i == content.Length);
                bool isComma = !isEnd && content[i] == ',';

                if (isComma || isEnd)
                {
                    if (i > segStart)
                    {
                        var segment = TrimSpan(content.Slice(segStart, i - segStart));
                        if (!segment.IsEmpty)
                        {
                            list.Add(ParseIntFromSpan(segment));
                        }
                    }
                    segStart = i + 1;
                }
            }

            return list.Count == 0 ? Array.Empty<int>() : list.ToArray();
        }

        /// <summary>
        /// 单次遍历解析 long[]。合并计数+解析为一次遍历，使用 List&lt;long&gt; 避免二次扫描。
        /// 热路径优化：ParseLongFromSpan 不创建临时 string，直接从 Span 读取数字。
        /// Single-pass long[] parser. Merges counting + parsing into one traversal, uses List&lt;long&gt; to avoid double scan.
        /// Hot-path optimization: ParseLongFromSpan does not create temporary strings, reads digits directly from Span.
        /// </summary>
        public static long[] DeserializeArrayLong(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<long>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<long>();

            var list = new List<long>(16);
            int segStart = 0;

            for (int i = 0; i <= content.Length; i++)
            {
                bool isEnd = (i == content.Length);
                bool isComma = !isEnd && content[i] == ',';

                if (isComma || isEnd)
                {
                    if (i > segStart)
                    {
                        var segment = TrimSpan(content.Slice(segStart, i - segStart));
                        if (!segment.IsEmpty)
                        {
                            list.Add(ParseLongFromSpan(segment));
                        }
                    }
                    segStart = i + 1;
                }
            }

            return list.Count == 0 ? Array.Empty<long>() : list.ToArray();
        }

        /// <summary>
        /// 单次遍历解析 float[]。float/double 无法从 Span 手动解析（IEEE 754），
        /// 但使用 TrimSpan 避免了 segment.ToString().Trim() 的双重分配，
        /// 且只在有非空内容时才调用 segment.ToString()。
        /// Single-pass float[] parser. float/double cannot be manually parsed from Span (IEEE 754),
        /// but TrimSpan avoids the double allocation of segment.ToString().Trim(),
        /// and segment.ToString() is only called when there is non-empty content.
        /// </summary>
        public static float[] DeserializeArrayFloat(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<float>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<float>();

            var list = new List<float>(16);
            int segStart = 0;

            for (int i = 0; i <= content.Length; i++)
            {
                bool isEnd = (i == content.Length);
                bool isComma = !isEnd && content[i] == ',';

                if (isComma || isEnd)
                {
                    if (i > segStart)
                    {
                        var segment = TrimSpan(content.Slice(segStart, i - segStart));
                        if (!segment.IsEmpty)
                        {
                            list.Add(float.Parse(segment.ToString()));
                        }
                    }
                    segStart = i + 1;
                }
            }

            return list.Count == 0 ? Array.Empty<float>() : list.ToArray();
        }

        /// <summary>
        /// 单次遍历解析 double[]。同 float[] 的优化策略。
        /// Single-pass double[] parser. Same optimization strategy as float[].
        /// </summary>
        public static double[] DeserializeArrayDouble(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<double>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<double>();

            var list = new List<double>(16);
            int segStart = 0;

            for (int i = 0; i <= content.Length; i++)
            {
                bool isEnd = (i == content.Length);
                bool isComma = !isEnd && content[i] == ',';

                if (isComma || isEnd)
                {
                    if (i > segStart)
                    {
                        var segment = TrimSpan(content.Slice(segStart, i - segStart));
                        if (!segment.IsEmpty)
                        {
                            list.Add(double.Parse(segment.ToString()));
                        }
                    }
                    segStart = i + 1;
                }
            }

            return list.Count == 0 ? Array.Empty<double>() : list.ToArray();
        }

        /// <summary>
        /// 单次遍历解析 bool[]。bool 只有 true/false 两个值，直接比较 Span 首字符。
        /// Single-pass bool[] parser. bool only has true/false values, directly compare first character of Span.
        /// </summary>
        public static bool[] DeserializeArrayBool(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<bool>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<bool>();

            var list = new List<bool>(16);
            int segStart = 0;

            for (int i = 0; i <= content.Length; i++)
            {
                bool isEnd = (i == content.Length);
                bool isComma = !isEnd && content[i] == ',';

                if (isComma || isEnd)
                {
                    if (i > segStart)
                    {
                        var segment = TrimSpan(content.Slice(segStart, i - segStart));
                        if (!segment.IsEmpty)
                        {
                            // bool 值只有 true/false，直接比较首字符避免 bool.Parse 的字符串分配
                            // Bool values are only true/false, compare first char to avoid bool.Parse string allocation
                            list.Add(segment[0] == 't' || segment[0] == 'T');
                        }
                    }
                    segStart = i + 1;
                }
            }

            return list.Count == 0 ? Array.Empty<bool>() : list.ToArray();
        }

        /// <summary>
        /// 从 [a,b,c] 格式的 JSON 字符串中提取 string[] 元素。
        /// 使用 Span 逐字符扫描，避免 string.Split 产生的 string[] GC 分配。
        /// 单次遍历，使用 List&lt;string&gt; 避免二次扫描。
        /// Extract string[] elements from [a,b,c] JSON format.
        /// Uses Span character-by-character scanning to avoid string.Split GC allocation.
        /// Single-pass, uses List&lt;string&gt; to avoid double scan.
        /// </summary>
        public static string[] DeserializeArrayString(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty) return Array.Empty<string>();

            var list = new List<string>(16);
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
                        // 跳过字符串内容 / Skip string content
                        SkipStringLiteral(content, ref i);
                        // SkipStringLiteral 跳过整个字符串(含闭合引号), i 现在指向闭合引号之后
                        // 闭合引号在 i-1 位置, 元素内容在 [start, i-1-start)
                        // SkipStringLiteral skips entire string (including closing quote), i now points after closing quote
                        // Closing quote at i-1, element content in [start, i-1-start)
                        var element = content.Slice(start, i - 1 - start).ToString();
                        list.Add(element.Replace("\"\"", "\""));
                        inString = false;
                        // i 已经在闭合引号之后, 回退以让循环的 i++ 正确推进
                        // i is already after closing quote, step back so loop's i++ advances correctly
                        i--;
                    }
                    else
                    {
                        // 此分支在修复后不再需要到达，但保留以兼容边界情况
                        // This branch should no longer be reached after the fix, but kept for edge case compatibility
                        var element = content.Slice(start, i - start).ToString();
                        list.Add(element.Replace("\"\"", "\""));
                        inString = false;
                    }
                }
                else if (!inString && content[i] == ',')
                {
                    // 非字符串元素之间的逗号 — 忽略
                    // Comma between non-string elements — ignore
                }
            }

            return list.Count == 0 ? Array.Empty<string>() : list.ToArray();
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
