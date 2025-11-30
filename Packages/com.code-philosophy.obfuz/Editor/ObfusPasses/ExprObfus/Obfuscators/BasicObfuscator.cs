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
using Obfuz.Emit;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{

    class BasicObfuscator : ObfuscatorBase
    {
        private IMethod GetUnaryOpMethod(DefaultMetadataImporter importer, Code code, EvalDataType op1)
        {
            switch (code)
            {
                case Code.Neg:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.NegInt;
                        case EvalDataType.Int64: return importer.NegLong;
                        case EvalDataType.Float: return importer.NegFloat;
                        case EvalDataType.Double: return importer.NegDouble;
                        default: return null;
                    }
                }
                case Code.Not:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.NotInt;
                        case EvalDataType.Int64: return importer.NotLong;
                        default: return null;
                    }
                }
                default: return null;
            }
        }

        private IMethod GetBinaryOpMethod(DefaultMetadataImporter importer, Code code, EvalDataType op1, EvalDataType op2)
        {
            switch (code)
            {
                case Code.Add:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return op2 == op1 ? importer.AddInt : null;
                        case EvalDataType.Int64: return op2 == op1 ? importer.AddLong : null;
                        case EvalDataType.Float: return op2 == op1 ? importer.AddFloat : null;
                        case EvalDataType.Double: return op2 == op1 ? importer.AddDouble : null;
                        case EvalDataType.I:
                        {
                            switch (op2)
                            {
                                case EvalDataType.I: return importer.AddIntPtr;
                                case EvalDataType.Int32: return importer.AddIntPtrInt;
                                default: return null;
                            }
                        }
                        default: return null;
                    }
                }
                case Code.Sub:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return op2 == op1 ? importer.SubtractInt : null;
                        case EvalDataType.Int64: return op2 == op1 ? importer.SubtractLong : null;
                        case EvalDataType.Float: return op2 == op1 ? importer.SubtractFloat : null;
                        case EvalDataType.Double: return op2 == op1 ? importer.SubtractDouble : null;
                        case EvalDataType.I:
                        {
                            switch (op2)
                            {
                                case EvalDataType.I: return importer.SubtractIntPtr;
                                case EvalDataType.Int32: return importer.SubtractIntPtrInt;
                                default: return null;
                            }
                        }
                        default: return null;
                    }
                }
                case Code.Mul:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return op2 == op1 ? importer.MultiplyInt : null;
                        case EvalDataType.Int64: return op2 == op1 ? importer.MultiplyLong : null;
                        case EvalDataType.Float: return op2 == op1 ? importer.MultiplyFloat : null;
                        case EvalDataType.Double: return op2 == op1 ? importer.MultiplyDouble : null;
                        case EvalDataType.I:
                        {
                            switch (op2)
                            {
                                case EvalDataType.I: return importer.MultiplyIntPtr;
                                case EvalDataType.Int32: return importer.MultiplyIntPtrInt;
                                default: return null;
                            }
                        }
                        default: return null;
                    }
                }
                case Code.Div:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.DivideInt;
                        case EvalDataType.Int64: return importer.DivideLong;
                        case EvalDataType.Float: return importer.DivideFloat;
                        case EvalDataType.Double: return importer.DivideDouble;
                        default: return null;
                    }
                }
                case Code.Div_Un:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.DivideUnInt;
                        case EvalDataType.Int64: return importer.DivideUnLong;
                        default: return null;
                    }
                }
                case Code.Rem:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.RemInt;
                        case EvalDataType.Int64: return importer.RemLong;
                        case EvalDataType.Float: return importer.RemFloat;
                        case EvalDataType.Double: return importer.RemDouble;
                        default: return null;
                    }
                }
                case Code.Rem_Un:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.RemUnInt;
                        case EvalDataType.Int64: return importer.RemUnLong;
                        default: return null;
                    }
                }
                case Code.Neg:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.NegInt;
                        case EvalDataType.Int64: return importer.NegLong;
                        case EvalDataType.Float: return importer.NegFloat;
                        case EvalDataType.Double: return importer.NegDouble;
                        default: return null;
                    }
                }
                case Code.And:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.AndInt;
                        case EvalDataType.Int64: return importer.AndLong;
                        default: return null;
                    }
                }
                case Code.Or:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.OrInt;
                        case EvalDataType.Int64: return importer.OrLong;
                        default: return null;
                    }
                }
                case Code.Xor:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.XorInt;
                        case EvalDataType.Int64: return importer.XorLong;
                        default: return null;
                    }
                }
                case Code.Not:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.NotInt;
                        case EvalDataType.Int64: return importer.NotLong;
                        default: return null;
                    }
                }
                case Code.Shl:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.ShlInt;
                        case EvalDataType.Int64: return importer.ShlLong;
                        default: return null;
                    }
                }
                case Code.Shr:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.ShrInt;
                        case EvalDataType.Int64: return importer.ShrLong;
                        default: return null;
                    }
                }
                case Code.Shr_Un:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.ShrUnInt;
                        case EvalDataType.Int64: return importer.ShrUnLong;
                        default: return null;
                    }
                }
                default: return null;
            }
        }

        public override bool ObfuscateBasicUnaryOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IMethod opMethod = GetUnaryOpMethod(ctx.importer, inst.OpCode.Code, op);
            if (opMethod == null)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate unary operation {inst.OpCode.Code} with different operand types: op={op}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateBasicBinOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IMethod opMethod = GetBinaryOpMethod(ctx.importer, inst.OpCode.Code, op1, op2);
            if (opMethod == null)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with different operand types: op1={op1}, op2={op2}, ret={ret}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateUnaryBitwiseOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IMethod opMethod = GetUnaryOpMethod(ctx.importer, inst.OpCode.Code, op);
            if (opMethod == null)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate unary operation {inst.OpCode.Code} with different operand types: op={op}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateBinBitwiseOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IMethod opMethod = GetBinaryOpMethod(ctx.importer, inst.OpCode.Code, op1, op2);
            if (opMethod == null)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with different operand types: op1={op1}, op2={op2}, ret={ret}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateBitShiftOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            IMethod opMethod = GetBinaryOpMethod(ctx.importer, inst.OpCode.Code, op1, op2);
            if (opMethod == null)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with operand type {op2}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }
    }
}
