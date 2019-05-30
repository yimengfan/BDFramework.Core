using System.IO;

namespace System.IO
{
   static public class IPath
    {
        //这里是修复Mamc下的 Path.Combine的Bug
        static public string Combine(string a, string b)
        {
            return a + "/" + b;
        }
    }
}