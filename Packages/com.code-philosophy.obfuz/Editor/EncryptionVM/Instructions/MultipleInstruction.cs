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

﻿using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class MultipleInstruction : EncryptionInstructionBase
    {
        private readonly int _multiValue;
        private readonly int _revertMultiValue;
        private readonly int _opKeyIndex;

        public MultipleInstruction(int addValue, int opKeyIndex)
        {
            _multiValue = addValue;
            _opKeyIndex = opKeyIndex;
            _revertMultiValue = MathUtil.ModInverse32(addValue);
            Verify();
        }

        private void Verify()
        {
            int a = 1122334;
            UnityEngine.Assertions.Assert.AreEqual(a, a * _multiValue * _revertMultiValue);
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return value * _multiValue + secretKey[_opKeyIndex] + salt;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return (value - secretKey[_opKeyIndex] - salt) * _revertMultiValue;
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = value * {_multiValue} + _secretKey[{_opKeyIndex}] + salt;");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = (value - _secretKey[{_opKeyIndex}] - salt) * {_revertMultiValue};");
        }
    }
}
