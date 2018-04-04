using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace BDFramework
{
   static public class FileHelper
    {
        /// <summary>
        /// 检测路径是否存在
        /// </summary>
        /// <param name="path"></param>
        static  private void CheckDirectory(string path)
        { 
            var _directoryName = Path.GetDirectoryName(path);
            if (Directory.Exists(_directoryName) == false)
            {
                Directory.CreateDirectory(path);
            }
            
        }
        /// <summary>
        ///  写入所有字节码
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
       static public void WriteAllBytes(string path,byte[] bytes)
        {
            CheckDirectory(path);
           File.WriteAllBytes(path,bytes);
        }

        /// <summary>
        /// 写入所有字符串
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contents"></param>
        static public void WriteAllText(string path, string contents)
        {
            CheckDirectory(path);
            File.WriteAllText(path,contents);
        }

        /// <summary>
        /// 写入所有行
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contents"></param>
        static public void WriteAllLines(string path, string[] contents)
        {
            CheckDirectory(path);
            File.WriteAllLines(path,contents);
        }
    }
}