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
using UnityEngine;

namespace Obfuz.Utils
{
    public static class EncryptionUtil
    {
        public static int GetBitCount(int value)
        {
            int count = 0;
            while (value > 0)
            {
                count++;
                value >>= 1;
            }
            return count;
        }

        public static int GenerateEncryptionOpCodes(IRandom random, IEncryptor encryptor, int encryptionLevel, bool logWarningWhenExceedMaxOps = true)
        {
            if (encryptionLevel <= 0 || encryptionLevel > 4)
            {
                throw new ArgumentException($"Invalid encryption level: {encryptionLevel}, should be in range [1,4]");
            }
            int vmOpCodeCount = encryptor.OpCodeCount;
            long ops = 0;
            for (int i = 0; i < encryptionLevel; i++)
            {
                long newOps = ops * vmOpCodeCount;
                // don't use 0
                int op = random.NextInt(1, vmOpCodeCount);
                newOps |= (uint)op;
                if (newOps > uint.MaxValue)
                {
                    if (logWarningWhenExceedMaxOps)
                    {
                        Debug.LogWarning($"OpCode overflow. encryptionLevel:{encryptionLevel}, vmOpCodeCount:{vmOpCodeCount}");
                    }
                    break;
                }
                else
                {
                    ops = newOps;
                }
            }
            return (int)ops;
        }
    }
}
