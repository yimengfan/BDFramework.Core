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
    public class BitRotateInstruction : EncryptionInstructionBase
    {
        private readonly int _rotateBitNum;
        private readonly int _opKeyIndex;

        public BitRotateInstruction(int rotateBitNum, int opKeyIndex)
        {
            _rotateBitNum = rotateBitNum;
            _opKeyIndex = opKeyIndex;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            uint part1 = (uint)value << _rotateBitNum;
            uint part2 = (uint)value >> (32 - _rotateBitNum);
            return ((int)(part1 | part2) ^ secretKey[_opKeyIndex]) + salt;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            uint value2 = (uint)((value - salt) ^ secretKey[_opKeyIndex]);
            uint part1 = value2 >> _rotateBitNum;
            uint part2 = value2 << (32 - _rotateBitNum);
            return (int)(part1 | part2);
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"uint part1 = (uint)value << {_rotateBitNum};");
            lines.Add(indent + $"uint part2 = (uint)value >> (32 - {_rotateBitNum});");
            lines.Add(indent + $"value = ((int)(part1 | part2) ^ _secretKey[{_opKeyIndex}]) + salt;");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"uint value2 = (uint)((value - salt) ^ _secretKey[{_opKeyIndex}]);");
            lines.Add(indent + $"uint part1 = value2 >> {_rotateBitNum};");
            lines.Add(indent + $"uint part2 = value2 << (32 - {_rotateBitNum});");
            lines.Add(indent + $"value = (int)(part1 | part2);");
        }
    }
}
