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
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    class ObfusMethodContext
    {
        public MethodDef method;
        public LocalVariableAllocator localVariableAllocator;
        public IRandom localRandom;
        public EncryptionScopeInfo encryptionScope;
        public DefaultMetadataImporter importer;
        public ConstFieldAllocator constFieldAllocator;
        public int minInstructionCountOfBasicBlockToObfuscate;

        public IRandom CreateRandom()
        {
            return encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
        }
    }

    internal class ControlFlowObfusPass : ObfuscationMethodPassBase
    {
        private readonly ControlFlowObfuscationSettingsFacade _settings;

        private IObfuscationPolicy _obfuscationPolicy;
        private IObfuscator _obfuscator;

        public ControlFlowObfusPass(ControlFlowObfuscationSettingsFacade settings)
        {
            _settings = settings;
            _obfuscator = new DefaultObfuscator();
        }

        public override ObfuscationPassType Type => ObfuscationPassType.ControlFlowObfus;

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

        protected override void ObfuscateData(MethodDef method)
        {
            //Debug.Log($"Obfuscating method: {method.FullName} with EvalStackObfusPass");

            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            GroupByModuleEntityManager moduleEntityManager = ctx.moduleEntityManager;
            var encryptionScope = moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            var ruleData = _obfuscationPolicy.GetObfuscationRuleData(method);
            var localRandom = encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
            var obfusMethodCtx = new ObfusMethodContext
            {
                method = method,
                localVariableAllocator = new LocalVariableAllocator(method),
                encryptionScope = encryptionScope,
                constFieldAllocator = moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module),
                localRandom = localRandom,
                importer = moduleEntityManager.GetEntity<DefaultMetadataImporter>(method.Module),
                minInstructionCountOfBasicBlockToObfuscate = _settings.minInstructionCountOfBasicBlockToObfuscate,
            };
            _obfuscator.Obfuscate(method, obfusMethodCtx);
        }
    }
}
