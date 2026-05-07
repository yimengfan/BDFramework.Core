using System;
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
        /// 将 Span 中的 JSON 转义引号 "" 还原为单引号 "，直接在 char[] 中完成，只分配一次 string。
        /// 若无转义引号（常见路径），直接 ToString() 零额外分配。
        /// O10 优化：替代 element.Replace("\"\"", "\"") 的两次 string 分配
        /// （一次 ToString + 一次 Replace 返回新 string）。
        /// Unescape JSON escaped quotes "" → " directly in char[], allocating only one string.
        /// If no escaped quotes (common path), use ToString() with zero extra allocation.
        /// O10 optimization: replaces element.Replace("\"\"", "\"") which allocates two strings
        /// (one ToString + one Replace returning a new string).
        /// </summary>
        private static string UnescapeString(ReadOnlySpan<char> span)
        {
            // 快速路径：无转义引号时直接 ToString()，省去 char[] 分配
            // Fast path: no escaped quotes → ToString() directly, avoiding char[] allocation
            bool hasEscape = false;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '"' && i + 1 < span.Length && span[i + 1] == '"')
                {
                    hasEscape = true;
                    break;
                }
            }

            if (!hasEscape)
            {
                return span.ToString();
            }

            // 慢速路径：有转义引号，逐字符复制到 char[]，将 "" 折叠为 "
            // Slow path: has escaped quotes, copy char-by-char into char[], collapsing "" → "
            var buffer = new char[span.Length];
            int writeIndex = 0;
            int readIndex = 0;
            while (readIndex < span.Length)
            {
                if (span[readIndex] == '"' && readIndex + 1 < span.Length && span[readIndex + 1] == '"')
                {
                    buffer[writeIndex++] = '"';
                    readIndex += 2; // skip both quotes
                }
                else
                {
                    buffer[writeIndex++] = span[readIndex++];
                }
            }

            return new string(buffer, 0, writeIndex);
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
        /// 从 ReadOnlySpan&lt;char&gt; 直接解析 float，不创建中间 string。
        /// 手动解析：符号 + 整数部分 + 小数部分，支持负号和前导空格。
        /// 对特殊值（NaN, Infinity, 科学计数法）回退到 float.Parse(string)。
        /// Parse float directly from ReadOnlySpan&lt;char&gt; without creating intermediate string.
        /// Manual parsing: sign + integer part + decimal part, supports negative and leading whitespace.
        /// Falls back to float.Parse(string) for special values (NaN, Infinity, scientific notation).
        /// </summary>
        private static float ParseFloatFromSpan(ReadOnlySpan<char> span)
        {
            int i = 0;
            // 跳过前导空格 / Skip leading whitespace
            while (i < span.Length && span[i] == ' ') i++;
            if (i >= span.Length) return 0f;

            // 检测特殊值 → 回退到 float.Parse / Detect special values → fallback to float.Parse
            char first = span[i];
            if (first == 'N' || first == 'n' || first == 'I' || first == 'i' ||
                (first == '-' && i + 1 < span.Length &&
                 (span[i + 1] == 'N' || span[i + 1] == 'n' || span[i + 1] == 'I' || span[i + 1] == 'i')))
            {
                return float.Parse(span.ToString());
            }

            bool negative = false;
            if (first == '-') { negative = true; i++; }
            else if (first == '+') { i++; }

            // 整数部分 / Integer part
            long intValue = 0;
            bool hasDigits = false;
            while (i < span.Length)
            {
                int ch = span[i] - '0';
                if (ch < 0 || ch > 9) break;
                intValue = intValue * 10 + ch;
                hasDigits = true;
                i++;
            }

            // 小数部分 / Decimal part
            float decimalValue = 0f;
            if (i < span.Length && span[i] == '.')
            {
                i++;
                float divisor = 10f;
                while (i < span.Length)
                {
                    int ch = span[i] - '0';
                    if (ch < 0 || ch > 9) break;
                    decimalValue += ch / divisor;
                    divisor *= 10f;
                    i++;
                }
            }

            // 科学计数法 → 回退 / Scientific notation → fallback
            if (i < span.Length && (span[i] == 'e' || span[i] == 'E'))
            {
                return float.Parse(span.ToString());
            }

            if (!hasDigits) return 0f;

            float result = (float)intValue + decimalValue;
            return negative ? -result : result;
        }

        /// <summary>
        /// 从 ReadOnlySpan&lt;char&gt; 直接解析 double，不创建中间 string。
        /// 手动解析：符号 + 整数部分 + 小数部分，支持负号和前导空格。
        /// 对特殊值（NaN, Infinity, 科学计数法）回退到 double.Parse(string)。
        /// Parse double directly from ReadOnlySpan&lt;char&gt; without creating intermediate string.
        /// Manual parsing: sign + integer part + decimal part, supports negative and leading whitespace.
        /// Falls back to double.Parse(string) for special values (NaN, Infinity, scientific notation).
        /// </summary>
        private static double ParseDoubleFromSpan(ReadOnlySpan<char> span)
        {
            int i = 0;
            // 跳过前导空格 / Skip leading whitespace
            while (i < span.Length && span[i] == ' ') i++;
            if (i >= span.Length) return 0d;

            // 检测特殊值 → 回退到 double.Parse / Detect special values → fallback to double.Parse
            char first = span[i];
            if (first == 'N' || first == 'n' || first == 'I' || first == 'i' ||
                (first == '-' && i + 1 < span.Length &&
                 (span[i + 1] == 'N' || span[i + 1] == 'n' || span[i + 1] == 'I' || span[i + 1] == 'i')))
            {
                return double.Parse(span.ToString());
            }

            bool negative = false;
            if (first == '-') { negative = true; i++; }
            else if (first == '+') { i++; }

            // 整数部分 / Integer part
            long intValue = 0;
            bool hasDigits = false;
            while (i < span.Length)
            {
                int ch = span[i] - '0';
                if (ch < 0 || ch > 9) break;
                intValue = intValue * 10 + ch;
                hasDigits = true;
                i++;
            }

            // 小数部分 / Decimal part
            double decimalValue = 0d;
            if (i < span.Length && span[i] == '.')
            {
                i++;
                double divisor = 10d;
                while (i < span.Length)
                {
                    int ch = span[i] - '0';
                    if (ch < 0 || ch > 9) break;
                    decimalValue += ch / divisor;
                    divisor *= 10d;
                    i++;
                }
            }

            // 科学计数法 → 回退 / Scientific notation → fallback
            if (i < span.Length && (span[i] == 'e' || span[i] == 'E'))
            {
                return double.Parse(span.ToString());
            }

            if (!hasDigits) return 0d;

            double result = (double)intValue + decimalValue;
            return negative ? -result : result;
        }

        /// <summary>
        /// 计算逗号分隔的元素个数（逗号数+1），用于预分配数组大小。
        /// 两遍扫描策略：第一遍计数，第二遍填充预分配数组，避免 List&lt;T&gt; + ToArray() 的双重分配。
        /// Count comma-separated elements (commas+1) for pre-allocating array size.
        /// Two-pass strategy: first pass counts, second pass fills pre-sized array,
        /// avoiding the double allocation of List&lt;T&gt; + ToArray().
        /// </summary>
        private static int CountElements(ReadOnlySpan<char> content)
        {
            if (content.IsEmpty) return 0;
            int count = 1; // 至少1个元素 / at least 1 element
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == ',') count++;
            }
            return count;
        }

        /// <summary>
        /// 两遍扫描解析 int[]。
        /// 第一遍：计数元素个数（逗号数+1），预分配数组，避免 List&lt;int&gt; + ToArray() 的双重分配。
        /// 第二遍：解析元素值，直接填入预分配数组。
        /// 热路径优化：ParseIntFromSpan 不创建临时 string，直接从 Span 读取数字。
        /// Two-pass int[] parser. First pass: count elements (commas+1), pre-allocate array,
        /// avoiding double allocation of List&lt;int&gt; + ToArray().
        /// Second pass: parse element values directly into pre-allocated array.
        /// Hot-path optimization: ParseIntFromSpan does not create temporary strings.
        /// </summary>
        public static int[] DeserializeArrayInt(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<int>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<int>();

            // 第一遍：计数 / First pass: count
            int capacity = CountElements(content);
            var array = new int[capacity];
            int index = 0;
            int segStart = 0;

            // 第二遍：解析并填充 / Second pass: parse and fill
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
                            array[index++] = ParseIntFromSpan(segment);
                        }
                    }
                    segStart = i + 1;
                }
            }

            if (index == 0) return Array.Empty<int>();
            if (index == capacity) return array;
            // 计数偏大时（如空元素），截断到实际长度 / Truncate if count was overestimated (e.g. empty elements)
            var result = new int[index];
            Array.Copy(array, result, index);
            return result;
        }

        /// <summary>
        /// 两遍扫描解析 long[]。
        /// 第一遍：计数元素个数，预分配数组，避免 List&lt;long&gt; + ToArray() 的双重分配。
        /// 第二遍：解析元素值，直接填入预分配数组。
        /// 热路径优化：ParseLongFromSpan 不创建临时 string，直接从 Span 读取数字。
        /// Two-pass long[] parser. First pass: count elements, pre-allocate array,
        /// avoiding double allocation of List&lt;long&gt; + ToArray().
        /// Second pass: parse element values directly into pre-allocated array.
        /// Hot-path optimization: ParseLongFromSpan does not create temporary strings.
        /// </summary>
        public static long[] DeserializeArrayLong(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<long>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<long>();

            // 第一遍：计数 / First pass: count
            int capacity = CountElements(content);
            var array = new long[capacity];
            int index = 0;
            int segStart = 0;

            // 第二遍：解析并填充 / Second pass: parse and fill
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
                            array[index++] = ParseLongFromSpan(segment);
                        }
                    }
                    segStart = i + 1;
                }
            }

            if (index == 0) return Array.Empty<long>();
            if (index == capacity) return array;
            var result = new long[index];
            Array.Copy(array, result, index);
            return result;
        }

        /// <summary>
        /// 两遍扫描解析 float[]。
        /// 第一遍：计数元素个数，预分配数组，避免 List&lt;float&gt; + ToArray() 的双重分配。
        /// 第二遍：解析元素值，直接填入预分配数组。使用 ParseFloatFromSpan 零分配解析，
        /// 仅对 NaN/Infinity/科学计数法回退到 float.Parse(string)。
        /// Two-pass float[] parser. First pass: count elements, pre-allocate array,
        /// avoiding double allocation of List&lt;float&gt; + ToArray().
        /// Second pass: parse element values directly into pre-allocated array.
        /// Uses ParseFloatFromSpan for zero-allocation parsing,
        /// falls back to float.Parse(string) only for NaN/Infinity/scientific notation.
        /// </summary>
        public static float[] DeserializeArrayFloat(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<float>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<float>();

            // 第一遍：计数 / First pass: count
            int capacity = CountElements(content);
            var array = new float[capacity];
            int index = 0;
            int segStart = 0;

            // 第二遍：解析并填充 / Second pass: parse and fill
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
                            array[index++] = ParseFloatFromSpan(segment);
                        }
                    }
                    segStart = i + 1;
                }
            }

            if (index == 0) return Array.Empty<float>();
            if (index == capacity) return array;
            var result = new float[index];
            Array.Copy(array, result, index);
            return result;
        }

        /// <summary>
        /// 两遍扫描解析 double[]。同 float[] 的优化策略。
        /// 第一遍：计数元素个数，预分配数组，避免 List&lt;double&gt; + ToArray() 的双重分配。
        /// 第二遍：解析元素值，直接填入预分配数组。使用 ParseDoubleFromSpan 零分配解析，
        /// 仅对 NaN/Infinity/科学计数法回退到 double.Parse(string)。
        /// Two-pass double[] parser. Same optimization strategy as float[].
        /// First pass: count elements, pre-allocate array, avoiding double allocation.
        /// Second pass: parse element values directly into pre-allocated array.
        /// Uses ParseDoubleFromSpan for zero-allocation parsing,
        /// falls back to double.Parse(string) only for NaN/Infinity/scientific notation.
        /// </summary>
        public static double[] DeserializeArrayDouble(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<double>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<double>();

            // 第一遍：计数 / First pass: count
            int capacity = CountElements(content);
            var array = new double[capacity];
            int index = 0;
            int segStart = 0;

            // 第二遍：解析并填充 / Second pass: parse and fill
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
                            array[index++] = ParseDoubleFromSpan(segment);
                        }
                    }
                    segStart = i + 1;
                }
            }

            if (index == 0) return Array.Empty<double>();
            if (index == capacity) return array;
            var result = new double[index];
            Array.Copy(array, result, index);
            return result;
        }

        /// <summary>
        /// 两遍扫描解析 bool[]。
        /// 第一遍：计数元素个数，预分配数组，避免 List&lt;bool&gt; + ToArray() 的双重分配。
        /// 第二遍：解析元素值，直接填入预分配数组。bool 只有 true/false，直接比较首字符。
        /// Two-pass bool[] parser. First pass: count elements, pre-allocate array,
        /// avoiding double allocation of List&lt;bool&gt; + ToArray().
        /// Second pass: parse element values directly into pre-allocated array.
        /// bool only has true/false, directly compare first char.
        /// </summary>
        public static bool[] DeserializeArrayBool(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<bool>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty || content.IsWhiteSpace()) return Array.Empty<bool>();

            // 第一遍：计数 / First pass: count
            int capacity = CountElements(content);
            var array = new bool[capacity];
            int index = 0;
            int segStart = 0;

            // 第二遍：解析并填充 / Second pass: parse and fill
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
                            array[index++] = segment[0] == 't' || segment[0] == 'T';
                        }
                    }
                    segStart = i + 1;
                }
            }

            if (index == 0) return Array.Empty<bool>();
            if (index == capacity) return array;
            var result = new bool[index];
            Array.Copy(array, result, index);
            return result;
        }

        /// <summary>
        /// 两遍扫描解析 string[]。
        /// 第一遍：计数元素个数，预分配数组，避免 List&lt;string&gt; + ToArray() 的双重分配。
        /// 第二遍：解析元素值，直接填入预分配数组。
        /// 使用 Span 逐字符扫描，避免 string.Split 产生的 string[] GC 分配。
        /// Two-pass string[] parser. First pass: count elements, pre-allocate array,
        /// avoiding double allocation of List&lt;string&gt; + ToArray().
        /// Second pass: parse element values directly into pre-allocated array.
        /// Uses Span character-by-character scanning to avoid string.Split GC allocation.
        /// </summary>
        public static string[] DeserializeArrayString(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
            var content = TrimBrackets(json.AsSpan());
            if (content.IsEmpty) return Array.Empty<string>();

            // 第一遍：计数引号对数（每个字符串元素由一对引号包裹）
            // First pass: count quote pairs (each string element is wrapped in a pair of quotes)
            int capacity = 0;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '"')
                {
                    capacity++;
                    i++; // skip opening quote
                    while (i < content.Length)
                    {
                        if (content[i] == '"' && (i + 1 >= content.Length || content[i + 1] != '"'))
                        {
                            break; // closing quote
                        }
                        if (content[i] == '"') i++; // escaped quote ""
                        i++;
                    }
                }
            }
            // capacity = 引号对数 = 字符串元素数
            // capacity = number of quote pairs = number of string elements

            if (capacity == 0) return Array.Empty<string>();

            var array = new string[capacity];
            int index = 0;
            int start = 0;
            bool inString = false;

            // 第二遍：解析并填充 / Second pass: parse and fill
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
                        // O10: UnescapeString 内联处理 ""→" 转义，避免 .ToString() + .Replace() 的双重 string 分配
                        // O10: UnescapeString handles ""→" inline, avoiding double string allocation of .ToString() + .Replace()
                        array[index++] = UnescapeString(content.Slice(start, i - 1 - start));
                        inString = false;
                        // i 已经在闭合引号之后, 回退以让循环的 i++ 正确推进
                        // i is already after closing quote, step back so loop's i++ advances correctly
                        i--;
                    }
                    else
                    {
                        // 此分支在修复后不再需要到达，但保留以兼容边界情况
                        // This branch should no longer be reached after the fix, but kept for edge case compatibility
                        // O10: UnescapeString 内联处理 ""→" 转义
                        // O10: UnescapeString handles ""→" inline
                        array[index++] = UnescapeString(content.Slice(start, i - start));
                        inString = false;
                    }
                }
                else if (!inString && content[i] == ',')
                {
                    // 非字符串元素之间的逗号 — 忽略
                    // Comma between non-string elements — ignore
                }
            }

            if (index == 0) return Array.Empty<string>();
            if (index == capacity) return array;
            var result = new string[index];
            Array.Copy(array, result, index);
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
