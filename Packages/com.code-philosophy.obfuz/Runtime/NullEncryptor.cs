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
using System.Text;

namespace Obfuz
{
    public class NullEncryptor : EncryptorBase
    {
        private readonly byte[] _key;

        public override int OpCodeCount => 256;

        public NullEncryptor(byte[] key)
        {
            _key = key;
        }

        public override int Encrypt(int value, int opts, int salt)
        {
            return value;
        }

        public override int Decrypt(int value, int opts, int salt)
        {
            return value;
        }

        public override long Encrypt(long value, int opts, int salt)
        {
            return value;
        }

        public override long Decrypt(long value, int opts, int salt)
        {
            return value;
        }

        public override float Encrypt(float value, int opts, int salt)
        {
            return value;
        }

        public override float Decrypt(float value, int opts, int salt)
        {
            return value;
        }

        public override double Encrypt(double value, int opts, int salt)
        {
            return value;
        }

        public override double Decrypt(double value, int opts, int salt)
        {
            return value;
        }

        public override byte[] Encrypt(byte[] value, int offset, int length, int opts, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            var encryptedBytes = new byte[length];
            Buffer.BlockCopy(value, offset, encryptedBytes, 0, length);
            return encryptedBytes;
        }

        public override byte[] Decrypt(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            byte[] byteArr = new byte[length];
            Buffer.BlockCopy(value, 0, byteArr, 0, length);
            return byteArr;
        }

        public override byte[] Encrypt(string value, int ops, int salt)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public override string DecryptString(byte[] value, int offset, int length, int ops, int salt)
        {
            return Encoding.UTF8.GetString(value, offset, length);
        }
    }
}
