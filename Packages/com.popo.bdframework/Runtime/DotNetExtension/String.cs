using System;
using System.Security.Cryptography;
using System.Text;

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

        /// <summary>
        /// string转md5
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToMD5(this string source)
        {
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(source));
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                str.Append(data[i].ToString("x2")); //加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位
            }
            // 返回十六进制字符串  
            return str.ToString();
        }
}
}
