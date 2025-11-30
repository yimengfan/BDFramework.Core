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
using Obfuz.Emit;
using Obfuz.ObfusPasses.ExprObfus.Obfuscators;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ExprObfus
{
    class ObfusMethodContext
    {
        public MethodDef method;
        public EvalStackCalculator evalStackCalculator;
        public LocalVariableAllocator localVariableAllocator;
        public IRandom localRandom;
        public EncryptionScopeInfo encryptionScope;
        public DefaultMetadataImporter importer;
        public ConstFieldAllocator constFieldAllocator;
        public float obfuscationPercentage;
    }

    class ExprObfusPass : ObfuscationMethodPassBase
    {
        private readonly ExprObfuscationSettingsFacade _settings;
        private readonly IObfuscator _basicObfuscator;
        private readonly IObfuscator _advancedObfuscator;
        private readonly IObfuscator _mostAdvancedObfuscator;

        private IObfuscationPolicy _obfuscationPolicy;

        public ExprObfusPass(ExprObfuscationSettingsFacade settings)
        {
            _settings = settings;
            _basicObfuscator = new BasicObfuscator();
            _advancedObfuscator = new AdvancedObfuscator();
            _mostAdvancedObfuscator = new MostAdvancedObfuscator();
        }

        public override ObfuscationPassType Type => ObfuscationPassType.ExprObfus;

        public override void Start()
        {
            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            _obfuscationPolicy = new ConfigurableObfuscationPolicy(
                ctx.coreSettings.assembliesToObfuscate,
                _settings.ruleFiles);
        }

        private IObfuscator GetObfuscator(ObfuscationLevel level)
        {
            switch (level)
            {
                case ObfuscationLevel.None: return null;
                case ObfuscationLevel.Basic: return _basicObfuscator;
                case ObfuscationLevel.Advanced: return _advancedObfuscator;
                case ObfuscationLevel.MostAdvanced: return _mostAdvancedObfuscator;
                default: throw new System.ArgumentOutOfRangeException(nameof(level), level, "Unknown obfuscation level");
            }
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _obfuscationPolicy.NeedObfuscate(method);
        }

        protected bool TryObfuscateInstruction(IObfuscator obfuscator, InstructionParameterInfo pi, Instruction inst, List<Instruction> outputInstructions, ObfusMethodContext ctx)
        {
            //Debug.Log($"Obfuscating instruction: {inst} in method: {ctx.method.FullName}");
            IRandom localRandom = ctx.localRandom;
            float obfuscationPercentage = ctx.obfuscationPercentage;
            switch (inst.OpCode.Code)
            {
                case Code.Neg:
                {
                    return localRandom.NextInPercentage(obfuscationPercentage) && obfuscator.ObfuscateBasicUnaryOp(inst, pi.op1, pi.retType, outputInstructions, ctx);
                }
                case Code.Add:
                case Code.Sub:
                case Code.Mul:
                case Code.Div:
                case Code.Div_Un:
                case Code.Rem:
                case Code.Rem_Un:
                {
                    return localRandom.NextInPercentage(obfuscationPercentage) && obfuscator.ObfuscateBasicBinOp(inst, pi.op1, pi.op2, pi.retType, outputInstructions, ctx);
                }
                case Code.And:
                case Code.Or:
                case Code.Xor:
                {
                    return localRandom.NextInPercentage(obfuscationPercentage) && obfuscator.ObfuscateBinBitwiseOp(inst, pi.op1, pi.op2, pi.retType, outputInstructions, ctx);
                }
                case Code.Not:
                {
                    return localRandom.NextInPercentage(obfuscationPercentage) && obfuscator.ObfuscateUnaryBitwiseOp(inst, pi.op1, pi.retType, outputInstructions, ctx);
                }
                case Code.Shl:
                case Code.Shr:
                case Code.Shr_Un:
                {
                    return localRandom.NextInPercentage(obfuscationPercentage) && obfuscator.ObfuscateBitShiftOp(inst, pi.op1, pi.op2, pi.retType, outputInstructions, ctx);
                }
            }
            return false;
        }

        protected override void ObfuscateData(MethodDef method)
        {
            //Debug.Log($"Obfuscating method: {method.FullName} with ExprObfusPass");
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();

            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            var calc = new EvalStackCalculator(method);

            GroupByModuleEntityManager moduleEntityManager = ctx.moduleEntityManager;
            var encryptionScope = moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            var ruleData = _obfuscationPolicy.GetObfuscationRuleData(method);
            var obfuscator = GetObfuscator(ruleData.obfuscationLevel);
            var obfusMethodCtx = new ObfusMethodContext
            {
                method = method,
                evalStackCalculator = calc,
                localVariableAllocator = new LocalVariableAllocator(method),
                encryptionScope = encryptionScope,
                constFieldAllocator = moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module),
                localRandom = encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method)),
                importer = moduleEntityManager.GetEntity<DefaultMetadataImporter>(method.Module),
                obfuscationPercentage = ruleData.obfuscationPercentage,
            };
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                bool add = false;
                if (calc.TryGetParameterInfo(inst, out InstructionParameterInfo pi))
                {
                    outputInstructions.Clear();
                    if (TryObfuscateInstruction(obfuscator, pi, inst, outputInstructions, obfusMethodCtx))
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
                        add = true;
                    }
                }
                if (!add)
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
