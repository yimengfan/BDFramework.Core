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
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{

    public class DispatchProxyObfuscator : ObfuscatorBase
    {
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public DispatchProxyObfuscator(GroupByModuleEntityManager moduleEntityManager)
        {
            _moduleEntityManager = moduleEntityManager;
        }

        public override void Done()
        {
            _moduleEntityManager.Done<ModuleDispatchProxyAllocator>();
        }

        public override bool Obfuscate(MethodDef callerMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            ModuleDispatchProxyAllocator proxyCallAllocator = _moduleEntityManager.GetEntity<ModuleDispatchProxyAllocator>(callerMethod.Module);
            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod, null));
            ProxyCallMethodData proxyCallMethodData = proxyCallAllocator.Allocate(calledMethod, callVir);
            DefaultMetadataImporter importer = proxyCallAllocator.GetDefaultModuleMetadataImporter();

            //if (needCacheCall)
            //{
            //    FieldDef cacheField = _constFieldAllocator.Allocate(callerMethod.Module, proxyCallMethodData.index);
            //    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            //}
            //else
            //{
            //    obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptedIndex));
            //    obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptOps));
            //    obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.salt));
            //    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            //}

            ConstFieldAllocator constFieldAllocator = proxyCallAllocator.GetEntity<ConstFieldAllocator>();
            FieldDef cacheField = constFieldAllocator.Allocate(proxyCallMethodData.index);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, proxyCallMethodData.proxyMethod));
            return true;
        }
    }
}
