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
using Obfuz.Data;
using System.Collections.Generic;

namespace Obfuz.Utils
{
    internal static class ConstObfusUtil
    {
        public static void LoadConstInt(int a, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_I4, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public static void LoadConstLong(long a, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_I8, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public static void LoadConstFloat(float a, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_R4, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public static void LoadConstDouble(double a, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_R8, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }


        public static void LoadConstTwoInt(int a, int b, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_I4, a));

                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));

                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_I4, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }

        public static void LoadConstTwoLong(long a, long b, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_I8, a));
                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_I8, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }

        public static void LoadConstTwoFloat(float a, float b, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_R4, a));
                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_R4, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }

        public static void LoadConstTwoDouble(double a, double b, IRandom random, float constProbability, ConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_R8, a));
                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_R8, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }
    }
}
