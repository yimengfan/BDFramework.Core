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
using Obfuz.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.CallObfus
{

    public class DelegateProxyObfuscator : ObfuscatorBase
    {
        private readonly GroupByModuleEntityManager _entityManager;

        public DelegateProxyObfuscator(GroupByModuleEntityManager moduleEntityManager)
        {
            _entityManager = moduleEntityManager;
        }

        public override void Done()
        {
            _entityManager.Done<DelegateProxyAllocator>();
        }

        private MethodSig CreateProxyMethodSig(ModuleDef module, IMethod method)
        {
            MethodSig methodSig = MetaUtil.ToSharedMethodSig(module.CorLibTypes, MetaUtil.GetInflatedMethodSig(method, null));
            //MethodSig methodSig = MetaUtil.GetInflatedMethodSig(method).Clone();
            //methodSig.Params
            switch (MetaUtil.GetThisArgType(method))
            {
                case ThisArgType.Class:
                {
                    methodSig.Params.Insert(0, module.CorLibTypes.Object);
                    break;
                }
                case ThisArgType.ValueType:
                {
                    methodSig.Params.Insert(0, module.CorLibTypes.IntPtr);
                    break;
                }
            }
            return MethodSig.CreateStatic(methodSig.RetType, methodSig.Params.ToArray());
        }

        public override bool Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            DelegateProxyAllocator allocator = _entityManager.GetEntity<DelegateProxyAllocator>(callingMethod.Module);
            LocalVariableAllocator localVarAllocator = new LocalVariableAllocator(callingMethod);
            MethodSig methodSig = CreateProxyMethodSig(callingMethod.Module, calledMethod);
            DelegateProxyMethodData proxyData = allocator.Allocate(calledMethod, callVir, methodSig);
            bool isVoidReturn = MetaUtil.IsVoidType(methodSig.RetType);

            using (var varScope = localVarAllocator.CreateScope())
            {
                List<Local> localVars = new List<Local>();
                if (!isVoidReturn)
                {
                    varScope.AllocateLocal(methodSig.RetType);
                }
                foreach (var p in methodSig.Params)
                {
                    localVars.Add(varScope.AllocateLocal(p));
                }
                // save args
                for (int i = localVars.Count - 1; i >= 0; i--)
                {
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Stloc, localVars[i]));
                }
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, proxyData.delegateInstanceField));
                foreach (var local in localVars)
                {
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                }
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Callvirt, proxyData.delegateInvokeMethod));
            }

            return true;
        }
    }
}
