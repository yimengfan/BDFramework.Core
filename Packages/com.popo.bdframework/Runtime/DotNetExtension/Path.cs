using System.IO;
using Cysharp.Text;

namespace System.IO
{
    static public class IPath
    {
        /// <summary>
        /// 路径合并
        /// 这里是修复Mac下的 Path.Combine的Bug
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public string Combine(string a, string b)
        {
            return ZString.Concat(a, "/", b);
        }

        /// <summary>
        /// 路径合并
        /// 这里是修复Mac下的 Path.Combine的Bug
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public string Combine(string a, string b, string c)
        {
            return ZString.Concat(a, "/", b, "/", c);
        }

        /// <summary>
        /// 路径合并
        /// 这里是修复Mac下的 Path.Combine的Bug
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public string Combine(string a, string b, string c, string d)
        {
            return ZString.Concat(a, "/", b, "/", c, "/", d);
        }
    }
}