using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BDFramework.Logs
{
    public static class LogCrypto
    {
        public const string DEFAULT_PASSWORD = "368219a2c858d5e97946b5ff764f1b48f7a1a59af54cb4169f88b041050748c3";
        private const int AES_BLOCK_SIZE = 16;

        public static byte[] DeriveKey(string password)
        {
            var raw = string.IsNullOrEmpty(password) ? DEFAULT_PASSWORD : password;
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
            }
        }

        public static byte[] Encrypt(byte[] data, int length, byte[] key)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key == null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, true))
                    {
                        cryptoStream.Write(data, 0, length);
                        cryptoStream.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] encryptedData, string password = null)
        {
            return Decrypt(encryptedData, DeriveKey(password));
        }

        public static byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            if (encryptedData.Length <= AES_BLOCK_SIZE)
            {
                throw new InvalidDataException("加密日志数据长度非法。");
            }

            if (key == null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var iv = new byte[AES_BLOCK_SIZE];
                    Buffer.BlockCopy(encryptedData, 0, iv, 0, AES_BLOCK_SIZE);
                    aes.IV = iv;

                    using (var input = new MemoryStream(encryptedData, AES_BLOCK_SIZE, encryptedData.Length - AES_BLOCK_SIZE, false))
                    using (var cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var output = new MemoryStream())
                    {
                        cryptoStream.CopyTo(output);
                        return output.ToArray();
                    }
                }
            }
            catch (CryptographicException e)
            {
                throw new InvalidDataException("BDebug playerlog 解密失败，请确认密码是否正确。", e);
            }
        }
    }
}


