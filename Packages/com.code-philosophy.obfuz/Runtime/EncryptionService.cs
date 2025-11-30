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

namespace Obfuz
{

    public static class EncryptionService<T> where T : IEncryptionScope
    {
        // for compatibility with Mono because Mono will raise FieldAccessException when try access private field
        public static IEncryptor _encryptor;

        public static IEncryptor Encryptor
        {
            get => _encryptor;
            set { _encryptor = value; }
        }

        public static void EncryptBlock(byte[] data, int ops, int salt)
        {
            _encryptor.EncryptBlock(data, ops, salt);
        }

        public static void DecryptBlock(byte[] data, int ops, int salt)
        {
            _encryptor.DecryptBlock(data, ops, salt);
        }

        public static int Encrypt(int value, int opts, int salt)
        {
            return _encryptor.Encrypt(value, opts, salt);
        }

        public static int Decrypt(int value, int opts, int salt)
        {
            return _encryptor.Decrypt(value, opts, salt);
        }

        public static long Encrypt(long value, int opts, int salt)
        {
            return _encryptor.Encrypt(value, opts, salt);
        }

        public static long Decrypt(long value, int opts, int salt)
        {
            return _encryptor.Decrypt(value, opts, salt);
        }

        public static float Encrypt(float value, int opts, int salt)
        {
            return _encryptor.Encrypt(value, opts, salt);
        }

        public static float Decrypt(float value, int opts, int salt)
        {
            return _encryptor.Decrypt(value, opts, salt);
        }

        public static double Encrypt(double value, int opts, int salt)
        {
            return _encryptor.Encrypt(value, opts, salt);
        }

        public static double Decrypt(double value, int opts, int salt)
        {
            return _encryptor.Decrypt(value, opts, salt);
        }

        public static byte[] Encrypt(byte[] value, int offset, int length, int opts, int salt)
        {
            return _encryptor.Encrypt(value, offset, length, opts, salt);
        }

        public static byte[] Decrypt(byte[] value, int offset, int byteLength, int ops, int salt)
        {
            return _encryptor.Decrypt(value, offset, byteLength, ops, salt);
        }

        public static byte[] Encrypt(string value, int ops, int salt)
        {
            return _encryptor.Encrypt(value, ops, salt);
        }

        public static string DecryptString(byte[] value, int offset, int stringBytesLength, int ops, int salt)
        {
            return _encryptor.DecryptString(value, offset, stringBytesLength, ops, salt);
        }


        public static int DecryptFromRvaInt(byte[] data, int offset, int ops, int salt)
        {
            int encryptedValue = BitConverter.ToInt32(data, offset);
            return Decrypt(encryptedValue, ops, salt);
        }

        public static long DecryptFromRvaLong(byte[] data, int offset, int ops, int salt)
        {
            long encryptedValue = BitConverter.ToInt64(data, offset);
            return Decrypt(encryptedValue, ops, salt);
        }

        public static float DecryptFromRvaFloat(byte[] data, int offset, int ops, int salt)
        {
            float encryptedValue = BitConverter.ToSingle(data, offset);
            return Decrypt(encryptedValue, ops, salt);
        }

        public static double DecryptFromRvaDouble(byte[] data, int offset, int ops, int salt)
        {
            double encryptedValue = BitConverter.ToDouble(data, offset);
            return Decrypt(encryptedValue, ops, salt);
        }

        public static string DecryptFromRvaString(byte[] data, int offset, int length, int ops, int salt)
        {
            return DecryptString(data, offset, length, ops, salt);
        }

        public static byte[] DecryptFromRvaBytes(byte[] data, int offset, int bytesLength, int ops, int salt)
        {
            return Decrypt(data, offset, bytesLength, ops, salt);
        }

        public static void DecryptInitializeArray(System.Array arr, System.RuntimeFieldHandle field, int length, int ops, int salt)
        {
            _encryptor.DecryptInitializeArray(arr, field, length, ops, salt);
        }
    }
}
