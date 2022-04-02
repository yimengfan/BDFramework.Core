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
            if (a.EndsWith("/"))
            {
                return ZString.Concat(a, b);
            }
            else
            {
                return ZString.Concat(a, "/", b);
            }
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

        /// <summary>
        /// 路径合并
        /// 这里是修复Mac下的 Path.Combine的Bug
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        // static public string Combine(params string[] paths)
        // {
        //     var ret = "";
        //
        //     for (int i = 0; i < paths.Length; i++)
        //     {
        //         var str = paths[i];
        //         if (str.EndsWith("/"))
        //         {
        //             return ZString.Concat(ret, str);
        //         }
        //         else
        //         {
        //             return ZString.Concat(ret, "/", str);
        //         }
        //     }
        //     return ret;
        // }
    }
}
