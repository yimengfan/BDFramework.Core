using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace System.IO
{
    static public class FileHelper
    {
        /// <summary>
        /// 检测路径是否存在
        /// </summary>
        /// <param name="path"></param>
        static  private void CheckDirectory(string path)
        { 
            var direct = Path.GetDirectoryName(path);
            if (!Directory.Exists(direct))
            {
                Directory.CreateDirectory(direct);
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
        
        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetHashFromFile(string fileName)
        {
            string hash = "null";
            if (File.Exists(fileName))
            {
                var bytes = File.ReadAllBytes(fileName);
                //这里为了防止碰撞 考虑Sha256 512 但是速度会更慢
                var    sha1   = SHA1.Create();
                byte[] retVal = sha1.ComputeHash(bytes.ToArray());
                //hash
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                hash = sb.ToString();
            }
            
            return hash;
        }
    }
}