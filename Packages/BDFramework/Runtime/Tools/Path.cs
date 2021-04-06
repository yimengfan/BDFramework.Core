using System.IO;

namespace System.IO
{
   static public class IPath
    {
        //这里是修复Mamc下的 Path.Combine的Bug
        static public string Combine(params string[] strings)
        {
            string str = strings[0];
            for (int i = 1; i < strings.Length; i++)
            {
                
                str = str + "/" + strings[i];
            }
            return str;
        }
    }
}