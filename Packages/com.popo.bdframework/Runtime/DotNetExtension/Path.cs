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
            if (string.IsNullOrEmpty(a))
            {
                return b;
            }
            else if (string.IsNullOrEmpty(b))
            {
                return a;
            }
            else if (a.EndsWith("/") || b.StartsWith("/")) //目录格式
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
        /// 添加/
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string AddEndSymbol(string path)
        {
            if (!path.EndsWith("/"))
            {
                path = ZString.Concat(path, "/");
            }

            return path;
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
