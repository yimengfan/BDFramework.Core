using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
public  class HashHelper
{
    public static string CreateMD5ByString(string input)
    {
        // Use input string to calculate MD5 hash
        MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        // Convert the byte array to hexadecimal string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2")); 
        }
        return sb.ToString();
    }

    /// <summary>
    ///  计算指定文件的MD5值
    /// </summary>
    /// <param name="fileName">指定文件的完全限定名称</param>
    /// <returns>返回值的字符串形式</returns>
    public static string CreateMD5ByFile(String fileName)
    {
        string hashMD5 = String.Empty;
        //检查文件是否存在，如果文件存在则进行计算，否则返回空值
        if (System.IO.File.Exists(fileName))
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                //计算文件的MD5值
                System.Security.Cryptography.MD5 calculator = System.Security.Cryptography.MD5.Create();
                Byte[] buffer = calculator.ComputeHash(fs);
                calculator.Clear();
                //将字节数组转换成十六进制的字符串形式
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    stringBuilder.Append(buffer[i].ToString("x2"));
                }
                hashMD5 = stringBuilder.ToString();
            }//关闭文件流
        }//结束计算
        return hashMD5;
    }
}

