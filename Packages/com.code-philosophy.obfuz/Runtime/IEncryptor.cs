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

﻿namespace Obfuz
{
    public interface IEncryptor
    {
        int OpCodeCount { get; }

        void EncryptBlock(byte[] data, int ops, int salt);
        void DecryptBlock(byte[] data, int ops, int salt);

        int Encrypt(int value, int opts, int salt);
        int Decrypt(int value, int opts, int salt);

        long Encrypt(long value, int opts, int salt);
        long Decrypt(long value, int opts, int salt);

        float Encrypt(float value, int opts, int salt);
        float Decrypt(float value, int opts, int salt);

        double Encrypt(double value, int opts, int salt);
        double Decrypt(double value, int opts, int salt);

        byte[] Encrypt(byte[] value, int offset, int length, int opts, int salt);
        byte[] Decrypt(byte[] value, int offset, int byteLength, int ops, int salt);

        byte[] Encrypt(string value, int ops, int salt);
        string DecryptString(byte[] value, int offset, int stringBytesLength, int ops, int salt);

        void DecryptInitializeArray(System.Array arr, System.RuntimeFieldHandle field, int length, int ops, int salt);
    }
}
