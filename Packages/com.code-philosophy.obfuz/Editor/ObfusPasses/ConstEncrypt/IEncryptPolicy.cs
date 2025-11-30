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

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public struct ConstCachePolicy
    {
        public bool cacheConstInLoop;
        public bool cacheConstNotInLoop;
        public bool cacheStringInLoop;
        public bool cacheStringNotInLoop;
    }

    public interface IEncryptPolicy
    {
        bool NeedObfuscateMethod(MethodDef method);

        ConstCachePolicy GetMethodConstCachePolicy(MethodDef method);

        bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value);

        bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value);

        bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value);

        bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value);

        bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value);

        bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array);
    }

    public abstract class EncryptPolicyBase : IEncryptPolicy
    {
        public abstract bool NeedObfuscateMethod(MethodDef method);
        public abstract ConstCachePolicy GetMethodConstCachePolicy(MethodDef method);
        public abstract bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value);
        public abstract bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value);
        public abstract bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value);
        public abstract bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value);
        public abstract bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value);
        public abstract bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array);
    }
}
