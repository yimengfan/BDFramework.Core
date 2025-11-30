// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace Obfuz.Utils
{
    public static class KeyGenerator
    {
        public static byte[] GenerateKey(string initialString, int keyLength)
        {
            byte[] initialBytes = Encoding.UTF8.GetBytes(initialString);
            using (var sha512 = SHA512.Create())
            {
                byte[] hash = sha512.ComputeHash(initialBytes);
                byte[] key = new byte[keyLength];
                int bytesCopied = 0;
                while (bytesCopied < key.Length)
                {
                    if (bytesCopied > 0)
                    {
                        // 再次哈希之前的哈希值以生成更多数据
                        hash = sha512.ComputeHash(hash);
                    }
                    int bytesToCopy = Math.Min(hash.Length, key.Length - bytesCopied);
                    Buffer.BlockCopy(hash, 0, key, bytesCopied, bytesToCopy);
                    bytesCopied += bytesToCopy;
                }
                return key;
            }
        }

        public static int[] ConvertToIntKey(byte[] key)
        {
            return EncryptorBase.ConvertToIntKey(key);
        }
    }
}
