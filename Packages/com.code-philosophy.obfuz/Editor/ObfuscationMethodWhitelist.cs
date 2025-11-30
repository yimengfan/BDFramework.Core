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
using Obfuz.Editor;
using Obfuz.Utils;
using System.Linq;
using UnityEngine;

namespace Obfuz
{
    public class ObfuscationMethodWhitelist
    {
        private readonly ObfuzIgnoreScopeComputeCache _obfuzComputeCache;
        private readonly BurstCompileComputeCache _burstCompileComputeCache;

        public ObfuscationMethodWhitelist(ObfuzIgnoreScopeComputeCache obfuzComputeCache, BurstCompileComputeCache burstCompileComputeCache)
        {
            _obfuzComputeCache = obfuzComputeCache;
            _burstCompileComputeCache = burstCompileComputeCache;
        }

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == ConstValues.ObfuzRuntimeAssemblyName)
            {
                return true;
            }
            //if (MetaUtil.HasObfuzIgnoreScope(module))
            //{
            //    return true;
            //}
            return false;
        }

        private bool DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(MethodDef method)
        {
            CustomAttribute ca = method.CustomAttributes.Find(ConstValues.RuntimeInitializedOnLoadMethodAttributeFullName);
            if (ca != null && ca.ConstructorArguments.Count > 0)
            {
                RuntimeInitializeLoadType loadType = (RuntimeInitializeLoadType)ca.ConstructorArguments[0].Value;
                if (loadType >= RuntimeInitializeLoadType.AfterAssembliesLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsInWhiteList(MethodDef method)
        {
            TypeDef typeDef = method.DeclaringType;
            //if (IsInWhiteList(typeDef))
            //{
            //    return true;
            //}
            if (method.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (_obfuzComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(method, typeDef, ObfuzScope.MethodBody))
            {
                return true;
            }
            CustomAttribute ca = method.CustomAttributes.Find(ConstValues.RuntimeInitializedOnLoadMethodAttributeFullName);
            if (DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(method))
            {
                return true;
            }
            if (MetaUtil.HasBurstCompileAttribute(method) || _burstCompileComputeCache.IsBurstCompileMethodOrReferencedByBurstCompileMethod(method))
            {
                return true;
            }

            // don't obfuscate cctor when it has RuntimeInitializeOnLoadMethodAttribute with load type AfterAssembliesLoaded
            if (method.IsStatic && method.Name == ".cctor" && typeDef.Methods.Any(m => DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(m)))
            {
                return true;
            }
            return false;
        }

        public bool IsInWhiteList(TypeDef type)
        {
            if (type.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (IsInWhiteList(type.Module))
            {
                return true;
            }
            if (MetaUtil.HasBurstCompileAttribute(type))
            {
                return true;
            }
            if (_obfuzComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(type, type.DeclaringType, ObfuzScope.MethodBody))
            {
                return true;
            }
            //if (type.DeclaringType != null && IsInWhiteList(type.DeclaringType))
            //{
            //    return true;
            //}
            if (type.FullName == ConstValues.GeneratedEncryptionVirtualMachineFullName)
            {
                return true;
            }
            return false;
        }
    }
}
