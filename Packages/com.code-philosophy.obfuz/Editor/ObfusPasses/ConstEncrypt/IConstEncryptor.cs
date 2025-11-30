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
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public interface IConstEncryptor
    {
        void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions);

        void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions);

        void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions);

        void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions);

        void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions);

        void ObfuscateBytes(MethodDef method, bool needCacheValue, FieldDef field, byte[] value, List<Instruction> obfuscatedInstructions);
    }

    public abstract class ConstEncryptorBase : IConstEncryptor
    {
        public abstract void ObfuscateBytes(MethodDef method, bool needCacheValue, byte[] value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateBytes(MethodDef method, bool needCacheValue, FieldDef field, byte[] value, List<Instruction> obfuscatedInstructions);
    }
}
