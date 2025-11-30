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
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.EvalStackObfus
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

    internal class EvalStackObfusPass : ObfuscationMethodPassBase
    {
        private readonly EvalStackObfuscationSettingsFacade _settings;

        private IObfuscationPolicy _obfuscationPolicy;
        private IObfuscator _obfuscator;

        public EvalStackObfusPass(EvalStackObfuscationSettingsFacade settings)
        {
            _settings = settings;
            _obfuscator = new DefaultObfuscator();
        }

        public override ObfuscationPassType Type => ObfuscationPassType.EvalStackObfus;

        public override void Start()
        {
            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            _obfuscationPolicy = new ConfigurableObfuscationPolicy(
                ctx.coreSettings.assembliesToObfuscate,
                _settings.ruleFiles);
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _obfuscationPolicy.NeedObfuscate(method);
        }

        protected bool TryObfuscateInstruction(Instruction inst, EvalDataType dataType, List<Instruction> outputInstructions, ObfusMethodContext ctx)
        {
            switch (dataType)
            {
                case EvalDataType.Int32: return _obfuscator.ObfuscateInt(inst, outputInstructions, ctx);
                case EvalDataType.Int64: return _obfuscator.ObfuscateLong(inst, outputInstructions, ctx);
                case EvalDataType.Float: return _obfuscator.ObfuscateFloat(inst, outputInstructions, ctx);
                case EvalDataType.Double: return _obfuscator.ObfuscateDouble(inst, outputInstructions, ctx);
                default: return false;
            }
        }

        protected override void ObfuscateData(MethodDef method)
        {
            //Debug.Log($"Obfuscating method: {method.FullName} with EvalStackObfusPass");
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();

            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            var calc = new EvalStackCalculator(method);

            GroupByModuleEntityManager moduleEntityManager = ctx.moduleEntityManager;
            var encryptionScope = moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            var ruleData = _obfuscationPolicy.GetObfuscationRuleData(method);
            var localRandom = encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
            var obfusMethodCtx = new ObfusMethodContext
            {
                method = method,
                evalStackCalculator = calc,
                localVariableAllocator = new LocalVariableAllocator(method),
                encryptionScope = encryptionScope,
                constFieldAllocator = moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module),
                localRandom = localRandom,
                importer = moduleEntityManager.GetEntity<DefaultMetadataImporter>(method.Module),
                obfuscationPercentage = ruleData.obfuscationPercentage,
            };
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                totalFinalInstructions.Add(inst);
                if (calc.TryGetPushResult(inst, out EvalDataType dataType) && localRandom.NextInPercentage(ruleData.obfuscationPercentage))
                {
                    outputInstructions.Clear();
                    if (TryObfuscateInstruction(inst, dataType, outputInstructions, obfusMethodCtx))
                    {
                        totalFinalInstructions.AddRange(outputInstructions);
                    }
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
