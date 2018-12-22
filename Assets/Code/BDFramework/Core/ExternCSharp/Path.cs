using System.IO;

namespace System.IO
{
   static public class IPath
    {
        //解决Mac下Path接口失效问题
        static public string Combine(string a, string b)
        {
            return a + "/" + b;
        }
    }
}