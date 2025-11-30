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
using System.Collections.Generic;

namespace Obfuz.Utils
{
    public class BurstCompileComputeCache
    {
        private readonly List<ModuleDef> _modulesToObfuscate;
        private readonly List<ModuleDef> _allObfuscationRelativeModules;

        private readonly HashSet<MethodDef> _burstCompileMethods = new HashSet<MethodDef>();
        private readonly HashSet<MethodDef> _burstCompileRelativeMethods = new HashSet<MethodDef>();
        public BurstCompileComputeCache(List<ModuleDef> modulesToObfuscate, List<ModuleDef> allObfuscationRelativeModules)
        {
            _modulesToObfuscate = modulesToObfuscate;
            _allObfuscationRelativeModules = allObfuscationRelativeModules;
            Build();
        }


        private void BuildBurstCompileMethods()
        {
            foreach (var module in _allObfuscationRelativeModules)
            {
                foreach (var type in module.GetTypes())
                {
                    bool hasBurstCompileAttribute = MetaUtil.HasBurstCompileAttribute(type);
                    foreach (var method in type.Methods)
                    {
                        if (hasBurstCompileAttribute || MetaUtil.HasBurstCompileAttribute(method))
                        {
                            _burstCompileMethods.Add(method);
                        }
                    }
                }
            }
        }

        private void CollectBurstCompileReferencedMethods()
        {
            var modulesToObfuscateSet = new HashSet<ModuleDef>(_modulesToObfuscate);
            var allObfuscationRelativeModulesSet = new HashSet<ModuleDef>(_allObfuscationRelativeModules);

            var pendingWalking = new Queue<MethodDef>(_burstCompileMethods);
            var visitedMethods = new HashSet<MethodDef>();
            while (pendingWalking.Count > 0)
            {
                var method = pendingWalking.Dequeue();

                if (!visitedMethods.Add(method))
                {
                    continue; // Skip already visited methods
                }
                if (modulesToObfuscateSet.Contains(method.Module))
                {
                    _burstCompileRelativeMethods.Add(method);
                }
                if (!method.HasBody)
                {
                    continue;
                }
                // Check for calls to other methods
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.Code == dnlib.DotNet.Emit.Code.Call ||
                        instruction.OpCode.Code == dnlib.DotNet.Emit.Code.Callvirt)
                    {
                        MethodDef calledMethod = ((IMethod)instruction.Operand).ResolveMethodDef();
                        if (calledMethod == null || !allObfuscationRelativeModulesSet.Contains(calledMethod.Module) || visitedMethods.Contains(calledMethod))
                        {
                            continue; // Skip if the method could not be resolved
                        }
                        pendingWalking.Enqueue(calledMethod);
                    }
                }
            }
        }

        private void Build()
        {
            BuildBurstCompileMethods();
            CollectBurstCompileReferencedMethods();
        }

        public bool IsBurstCompileMethodOrReferencedByBurstCompileMethod(MethodDef method)
        {
            return _burstCompileRelativeMethods.Contains(method);
        }
    }
}
