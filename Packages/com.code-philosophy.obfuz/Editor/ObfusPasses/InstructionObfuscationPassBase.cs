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

namespace Obfuz.ObfusPasses
{
    public abstract class InstructionObfuscationPassBase : ObfuscationMethodPassBase
    {
        protected abstract bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex,
            List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions);

        protected override void ObfuscateData(MethodDef method)
        {
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                outputInstructions.Clear();
                if (TryObfuscateInstruction(method, inst, instructions, i, outputInstructions, totalFinalInstructions))
                {
                    // current instruction may be the target of control flow instruction, so we can't remove it directly.
                    // we replace it with nop now, then remove it in CleanUpInstructionPass
                    inst.OpCode = outputInstructions[0].OpCode;
                    inst.Operand = outputInstructions[0].Operand;
                    totalFinalInstructions.Add(inst);
                    for (int k = 1; k < outputInstructions.Count; k++)
                    {
                        totalFinalInstructions.Add(outputInstructions[k]);
                    }
                }
                else
                {
                    totalFinalInstructions.Add(inst);
                }
            }

            instructions.Clear();
            foreach (var obInst in totalFinalInstructions)
            {
                instructions.Add(obInst);
            }
        }
    }
}
