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

namespace Obfuz.Utils
{

    internal static class MathUtil
    {
        //public static int ModInverseOdd32(int sa)
        //{
        //    uint a = (uint)sa;
        //    if (a % 2 == 0)
        //        throw new ArgumentException("Input must be an odd number.", nameof(a));

        //    uint x = 1; // 初始解：x₀ = 1 (mod 2)
        //    for (int i = 0; i < 5; i++) // 迭代5次（2^1 → 2^32）
        //    {
        //        int shift = 2 << i;        // 当前模数为 2^(2^(i+1))
        //        ulong mod = 1UL << shift; // 使用 ulong 避免溢出
        //        ulong ax = (ulong)a * x;  // 计算 a*x（64位避免截断）
        //        ulong term = (2 - ax) % mod;
        //        x = (uint)((x * term) % mod); // 更新 x，结果截断为 uint
        //    }
        //    return (int)x; // 最终解为 x₅ mod 2^32
        //}

        public static int ModInverse32(int sa)
        {
            uint x = (uint)sa;
            if ((x & 1) == 0)
                throw new ArgumentException("x must be odd (coprime with 2^32)");

            uint inv = x;
            inv = inv * (2 - x * inv); // 1
            inv = inv * (2 - x * inv); // 2
            inv = inv * (2 - x * inv); // 3
            inv = inv * (2 - x * inv); // 4
            inv = inv * (2 - x * inv); // 5
            return (int)inv;
        }

        public static long ModInverse64(long sx)
        {
            ulong x = (ulong)sx;
            if ((x & 1) == 0)
                throw new ArgumentException("x must be odd (coprime with 2^64)");

            ulong inv = x;
            inv *= 2 - x * inv; // 1
            inv *= 2 - x * inv; // 2
            inv *= 2 - x * inv; // 3
            inv *= 2 - x * inv; // 4
            inv *= 2 - x * inv; // 5
            inv *= 2 - x * inv; // 6

            return (long)inv;
        }
    }
}
