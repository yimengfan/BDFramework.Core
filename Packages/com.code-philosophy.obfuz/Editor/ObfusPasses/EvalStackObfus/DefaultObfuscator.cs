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

﻿using dnlib.DotNet.Emit;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.EvalStackObfus
{
    class DefaultObfuscator : ObfuscatorBase
    {
        public override bool ObfuscateInt(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IRandom random = ctx.localRandom;
            switch (random.NextInt(4))
            {
                case 0:
                {
                    // x = x + a
                    int a = 0;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstInt(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                case 1:
                {
                    // x = x * a * ra
                    int a = random.NextInt() | 0x1; // Ensure a is not zero
                    int ra = MathUtil.ModInverse32(a);
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstInt(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstInt(ra, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    return true;
                }
                case 2:
                {
                    // x = (x * a + b) * ra - (b * ra)
                    int a = random.NextInt() | 0x1; // Ensure a is not zero
                    int ra = MathUtil.ModInverse32(a);
                    int b = random.NextInt();
                    int b_ra = -b * ra;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstInt(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstInt(b, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstInt(ra, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstInt(b_ra, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                case 3:
                {
                    // x = ((x + a) * b + c) * rb - (a*b + c) * rb
                    int a = random.NextInt();
                    int b = random.NextInt() | 0x1; // Ensure b is not zero
                    int rb = MathUtil.ModInverse32(b);
                    int c = random.NextInt();
                    int r = -(a * b + c) * rb;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstInt(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstInt(b, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstInt(c, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstInt(rb, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstInt(r, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                default: return false;
            }
        }

        public override bool ObfuscateLong(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IRandom random = ctx.localRandom;
            switch (random.NextInt(4))
            {
                case 0:
                {
                    // x = x + a
                    long a = 0;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstLong(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                case 1:
                {
                    // x = x * a * ra
                    long a = random.NextLong() | 0x1L; // Ensure a is not zero
                    long ra = MathUtil.ModInverse64(a);
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstLong(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstLong(ra, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    return true;
                }
                case 2:
                {
                    // x = (x * a + b) * ra - (b * ra)
                    long a = random.NextLong() | 0x1L; // Ensure a is not zero
                    long ra = MathUtil.ModInverse64(a);
                    long b = random.NextLong();
                    long b_ra = -b * ra;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstLong(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstLong(b, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstLong(ra, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstLong(b_ra, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                case 3:
                {
                    // x = ((x + a) * b + c) * rb - (a*b + c) * rb
                    long a = random.NextLong();
                    long b = random.NextLong() | 0x1L; // Ensure b is not zero
                    long rb = MathUtil.ModInverse64(b);
                    long c = random.NextLong();
                    long r = -(a * b + c) * rb;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstLong(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstLong(b, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstLong(c, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstLong(rb, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    ConstObfusUtil.LoadConstLong(r, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                default: return false;
            }
        }

        public override bool ObfuscateFloat(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IRandom random = ctx.localRandom;
            switch (random.NextInt(3))
            {
                case 0:
                {
                    // x = x + 0f
                    float a = 0.0f;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstFloat(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                case 1:
                {
                    // x = x * 1f;
                    float a = 1.0f;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstFloat(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    return true;
                }
                case 2:
                {
                    // x = (x + a) * b; a = 0.0f, b = 1.0f
                    float a = 0.0f;
                    float b = 1.0f;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstFloat(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstFloat(b, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    return true;
                }
                default: return false;
            }
        }

        public override bool ObfuscateDouble(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IRandom random = ctx.localRandom;
            switch (random.NextInt(3))
            {
                case 0:
                {
                    // x = x + 0.0
                    double a = 0.0;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstDouble(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    return true;
                }
                case 1:
                {
                    // x = x * 1.0;
                    double a = 1.0;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstDouble(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    return true;
                }
                case 2:
                {
                    // x = (x + a) * b; a = 0.0, b = 1.0
                    double a = 0.0;
                    double b = 1.0;
                    float constProbability = 0f;
                    ConstObfusUtil.LoadConstDouble(a, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Add));
                    ConstObfusUtil.LoadConstDouble(b, random, constProbability, ctx.constFieldAllocator, outputInsts);
                    outputInsts.Add(Instruction.Create(OpCodes.Mul));
                    return true;
                }
                default: return false;
            }
        }
    }
}
