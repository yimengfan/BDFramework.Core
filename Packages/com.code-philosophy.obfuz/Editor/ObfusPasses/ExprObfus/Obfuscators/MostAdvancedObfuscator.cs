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
using Obfuz.Emit;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{
    class MostAdvancedObfuscator : AdvancedObfuscator
    {
        private readonly BasicObfuscator _basicObfuscator = new BasicObfuscator();

        public override bool ObfuscateBasicUnaryOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBasicUnaryOp(inst, op, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBasicUnaryOp(inst, op, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBasicBinOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBasicBinOp(inst, op1, op2, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBasicBinOp(inst, op1, op2, ret, outputInsts, ctx);
        }

        public override bool ObfuscateUnaryBitwiseOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateUnaryBitwiseOp(inst, op, ret, outputInsts, ctx))
            {
                return false;
            }

            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateUnaryBitwiseOp(inst, op, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBinBitwiseOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBinBitwiseOp(inst, op1, op2, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBinBitwiseOp(inst, op1, op2, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBitShiftOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBitShiftOp(inst, op1, op2, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBitShiftOp(inst, op1, op2, ret, outputInsts, ctx);
        }
    }
}
