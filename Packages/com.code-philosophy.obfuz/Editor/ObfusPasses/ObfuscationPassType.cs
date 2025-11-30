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

namespace Obfuz.ObfusPasses
{
    [Flags]
    public enum ObfuscationPassType
    {
        None = 0,

        ConstEncrypt = 0x1,
        FieldEncrypt = 0x2,

        SymbolObfus = 0x100,
        CallObfus = 0x200,
        ExprObfus = 0x400,
        ControlFlowObfus = 0x800,
        EvalStackObfus = 0x1000,

        RemoveConstField = 0x100000,
        WaterMark = 0x200000,

        AllObfus = SymbolObfus | CallObfus | ExprObfus | ControlFlowObfus | EvalStackObfus,
        AllEncrypt = ConstEncrypt | FieldEncrypt,

        MethodBodyObfusOrEncrypt = ConstEncrypt | CallObfus | ExprObfus | ControlFlowObfus | EvalStackObfus,

        All = ~0,
    }
}
