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

﻿using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class XorInstruction : EncryptionInstructionBase
    {
        private readonly int _xorValue;
        private readonly int _opKeyIndex;

        public XorInstruction(int xorValue, int opKeyIndex)
        {
            _xorValue = xorValue;
            _opKeyIndex = opKeyIndex;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return ((value ^ secretKey[_opKeyIndex]) + salt) ^ _xorValue;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return ((value ^ _xorValue) - salt) ^ secretKey[_opKeyIndex];
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = ((value ^ _secretKey[{_opKeyIndex}]) + salt) ^ {_xorValue};");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = ((value ^ {_xorValue}) - salt) ^ _secretKey[{_opKeyIndex}];");
        }
    }
}
