using System;

namespace BDFramework.StringEx
{
    /// <summary>
    /// string方法的扩展
    /// </summary>
    static public class String
    {
        /// <summary>
        /// 判断string中是否存在某字符串，带stringCompare 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return (source.IndexOf(value, comparisonType) >= 0);
        }
    }
}
