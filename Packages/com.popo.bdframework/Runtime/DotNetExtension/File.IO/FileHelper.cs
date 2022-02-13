using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MurmurHash.Net;
using ServiceStack.Text;

namespace System.IO
{
    static public class FileHelper
    {
        /// <summary>
        /// 检测路径是否存在
        /// </summary>
        /// <param name="path"></param>
        static private void CheckDirectory(string path)
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
        static public void WriteAllBytes(string path, byte[] bytes)
        {
            CheckDirectory(path);
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// 写入所有字符串
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contents"></param>
        static public void WriteAllText(string path, string contents)
        {
            CheckDirectory(path);
            File.WriteAllText(path, contents);
        }

        /// <summary>
        /// 拷贝一个文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="targetPath"></param>
        /// <param name="overwrite"></param>
        static public void Copy(string path, string targetPath, bool overwrite)
        {
            CheckDirectory(targetPath);
            File.Copy(path, targetPath, overwrite);
        }

        /// <summary>
        /// 移动一个文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="targetPath"></param>
        /// <param name="overwrite"></param>
        static public void Move(string path, string targetPath)
        {
            CheckDirectory(targetPath);
            File.Move(path, targetPath);
        }

        /// <summary>
        /// 拷贝文件夹
        /// </summary>
        /// <param name="sourceDirt"></param>
        /// <param name="targetDirt"></param>
        static public void CopyAllFolderFiles(string sourceDirt, string targetDirt)
        {
            var sourceFilePaths = Directory.GetFiles(sourceDirt, "*", SearchOption.AllDirectories);
            var sourceDirts = Directory.GetDirectories(sourceDirt, "*", SearchOption.TopDirectoryOnly);
            //创建文件夹
            foreach (var dirt in sourceDirts)
            {
                var _targetDirt = dirt.Replace(sourceDirt, targetDirt);
                if (!Directory.Exists(_targetDirt))
                {
                    Directory.CreateDirectory(_targetDirt);
                }
            }

            //复制
            foreach (var sfp in sourceFilePaths)
            {
                var targetfilePath = sfp.Replace(sourceDirt, targetDirt);
                //复制
                File.Copy(sfp, targetfilePath);
            }
        }

        /// <summary>
        /// 写入所有行
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contents"></param>
        static public void WriteAllLines(string path, string[] contents)
        {
            CheckDirectory(path);
            File.WriteAllLines(path, contents);
        }


        #region 文件hash

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetMurmurHash3(string filePath)
        {
            string hash = "null";
            if (File.Exists(filePath))
            {
                var bytes = File.ReadAllBytes(filePath);
                var hash32 = MurmurHash3.Hash32(bytes);
                return hash32.ToString();
                // //这里为了防止碰撞 考虑Sha256 512 但是速度会更慢
                // var sha = SHA256.Create();
                // byte[] retVal = sha.ComputeHash(bytes.ToArray());
                // //hash
                // StringBuilder sb = new StringBuilder();
                // for (int i = 0; i < retVal.Length; i++)
                // {
                //     sb.Append(retVal[i].ToString("x2"));
                // }
                //
                // hash = sb.ToString();
            }

            return hash;
        }

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetMurmurHash3(byte[] bytes)
        {
            var hash32 = MurmurHash3.Hash32(bytes);
            return hash32.ToString();
        }

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetMurmurHash2(string filePath)
        {
            string hash = "null";
            if (File.Exists(filePath))
            {
                var bytes = File.ReadAllBytes(filePath);
                var hash32 = MurmurHash2.Hash(bytes, 12345678);
                return hash32.ToString();
                // //这里为了防止碰撞 考虑Sha256 512 但是速度会更慢
                // var sha = SHA256.Create();
                // byte[] retVal = sha.ComputeHash(bytes.ToArray());
                // //hash
                // StringBuilder sb = new StringBuilder();
                // for (int i = 0; i < retVal.Length; i++)
                // {
                //     sb.Append(retVal[i].ToString("x2"));
                // }
                //
                // hash = sb.ToString();
            }

            return hash;
        }

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetMurmurHash2(byte[] bytes)
        {
            var hash32 = MurmurHash2.Hash(bytes, 12345678);
            return hash32.ToString();
        }

        #endregion
    }
}
