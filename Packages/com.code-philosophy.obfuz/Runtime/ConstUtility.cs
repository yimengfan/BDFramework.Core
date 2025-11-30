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

using System;
using System.Text;
using UnityEngine.Assertions;

namespace Obfuz
{
    public static class ConstUtility
    {
        public static int GetInt(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static long GetLong(byte[] data, int offset)
        {
            return BitConverter.ToInt64(data, offset);
        }

        public static float GetFloat(byte[] data, int offset)
        {
            return BitConverter.ToSingle(data, offset);
        }

        public static double GetDouble(byte[] data, int offset)
        {
            return BitConverter.ToDouble(data, offset);
        }

        public static string GetString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        public static byte[] GetBytes(byte[] data, int offset, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, offset, result, 0, length);
            return result;
        }

        public static int[] GetInts(byte[] data, int offset, int byteLength)
        {
            Assert.IsTrue(byteLength % 4 == 0);
            int[] result = new int[byteLength >> 2];
            Buffer.BlockCopy(data, offset, result, 0, byteLength);
            return result;
        }

        public static void InitializeArray(Array array, byte[] data, int offset, int length)
        {
            Buffer.BlockCopy(data, offset, array, 0, length);
        }

        public static unsafe int CastFloatAsInt(float value)
        {
            int* intValue = (int*)&value;
            return *intValue;
        }

        public static unsafe float CastIntAsFloat(int value)
        {
            float* floatValue = (float*)&value;
            return *floatValue;
        }

        public static unsafe long CastDoubleAsLong(double value)
        {
            long* longValue = (long*)&value;
            return *longValue;
        }

        public static unsafe double CastLongAsDouble(long value)
        {
            double* doubleValue = (double*)&value;
            return *doubleValue;
        }
    }
}
