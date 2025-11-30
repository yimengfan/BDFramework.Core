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

﻿using dnlib.DotNet;
using System.Collections;
using System.Collections.Generic;

namespace Obfuz.Utils
{
    public static class HashUtil
    {
        public static int CombineHash(int hash1, int hash2)
        {
            return hash1 * 1566083941 + hash2;
        }

        public static int ComputeHash(TypeSig sig)
        {
            return TypeEqualityComparer.Instance.GetHashCode(sig);
        }

        public static int ComputeHash(IList<TypeSig> sigs)
        {
            int hash = 135781321;
            TypeEqualityComparer tc = TypeEqualityComparer.Instance;
            foreach (var sig in sigs)
            {
                hash = hash * 1566083941 + tc.GetHashCode(sig);
            }
            return hash;
        }

        public static unsafe int ComputeHash(string s)
        {
            fixed (char* ptr = s)
            {
                int num = 352654597;
                int num2 = num;
                int* ptr2 = (int*)ptr;
                int num3;
                for (num3 = s.Length; num3 > 2; num3 -= 4)
                {
                    num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
                    num2 = ((num2 << 5) + num2 + (num2 >> 27)) ^ ptr2[1];
                    ptr2 += 2;
                }

                if (num3 > 0)
                {
                    num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
                }

                return num + num2 * 1566083941;
            }
        }

        public static int ComputePrimitiveOrStringOrBytesHashCode(object obj)
        {
            if (obj is byte[] bytes)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(bytes);
            }
            if (obj is string s)
            {
                return HashUtil.ComputeHash(s);
            }
            return obj.GetHashCode();
        }
    }
}
