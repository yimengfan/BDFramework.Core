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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class ReflectionCompatibilityDetector
    {
        private readonly HashSet<ModuleDef> _assembliesToObfuscate;
        private readonly List<ModuleDef> _obfuscatedAndNotObfuscatedModules;
        private readonly IObfuscationPolicy _renamePolicy;

        public ReflectionCompatibilityDetector(List<ModuleDef> assembliesToObfuscate, List<ModuleDef> obfuscatedAndNotObfuscatedModules, IObfuscationPolicy renamePolicy)
        {
            _assembliesToObfuscate = new HashSet<ModuleDef>(assembliesToObfuscate);
            _obfuscatedAndNotObfuscatedModules = obfuscatedAndNotObfuscatedModules;
            _renamePolicy = renamePolicy;
        }

        public void Analyze()
        {
            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        AnalyzeMethod(method);
                    }
                }
            }
        }

        private MethodDef _curCallingMethod;
        private IList<Instruction> _curInstructions;
        private int _curInstIndex;

        private void AnalyzeMethod(MethodDef method)
        {
            if (!method.HasBody)
            {
                return;
            }
            _curCallingMethod = method;
            _curInstructions = method.Body.Instructions;
            _curInstIndex = 0;
            for (int n = _curInstructions.Count; _curInstIndex < n; _curInstIndex++)
            {
                var inst = _curInstructions[_curInstIndex];
                switch (inst.OpCode.Code)
                {
                    case Code.Call:
                    {
                        AnalyzeCall(inst.Operand as IMethod);
                        break;
                    }
                    case Code.Callvirt:
                    {
                        ITypeDefOrRef constrainedType = null;
                        if (_curInstIndex > 0)
                        {
                            var prevInst = _curInstructions[_curInstIndex - 1];
                            if (prevInst.OpCode.Code == Code.Constrained)
                            {
                                constrainedType = prevInst.Operand as ITypeDefOrRef;
                            }
                        }
                        AnalyzeCallvir(inst.Operand as IMethod, constrainedType);
                        break;
                    }
                }
            }
        }

        private ITypeDefOrRef FindLatestTypeOf(int backwardFindInstructionCount)
        {
            // find sequence ldtoken <type>; 
            for (int i = 2; i <= backwardFindInstructionCount; i++)
            {
                int index = _curInstIndex - i;
                if (index < 0)
                {
                    return null;
                }
                Instruction inst1 = _curInstructions[index];
                Instruction inst2 = _curInstructions[index + 1];
                if (inst1.OpCode.Code == Code.Ldtoken && inst2.OpCode.Code == Code.Call)
                {
                    if (!(inst1.Operand is ITypeDefOrRef typeDefOrRef))
                    {
                        continue;
                    }
                    IMethod method = inst2.Operand as IMethod;
                    if (method.Name == "GetTypeFromHandle" && method.DeclaringType.FullName == "System.Type")
                    {
                        // Ldtoken <type>; Call System.Type.GetTypeFromHandle(System.RuntimeTypeHandle handle)
                        return typeDefOrRef;
                    }
                }
            }
            return null;
        }

        private void AnalyzeCall(IMethod calledMethod)
        {
            TypeDef callType = calledMethod.DeclaringType.ResolveTypeDef();
            if (callType == null)
            {
                return;
            }
            switch (callType.FullName)
            {
                case "System.Enum":
                {
                    AnalyzeEnum(calledMethod, callType);
                    break;
                }
                case "System.Type":
                {
                    AnalyzeGetType(calledMethod, callType);
                    break;
                }
                case "System.Reflection.Assembly":
                {
                    if (calledMethod.Name == "GetType")
                    {
                        AnalyzeGetType(calledMethod, callType);
                    }
                    break;
                }
            }
        }


        private bool IsAnyEnumItemRenamed(TypeDef typeDef)
        {
            return _assembliesToObfuscate.Contains(typeDef.Module) && typeDef.Fields.Any(f => _renamePolicy.NeedRename(f));
        }

        private void AnalyzeCallvir(IMethod calledMethod, ITypeDefOrRef constrainedType)
        {
            TypeDef callType = calledMethod.DeclaringType.ResolveTypeDef();
            if (callType == null)
            {
                return;
            }
            string calledMethodName = calledMethod.Name;
            switch (callType.FullName)
            {
                case "System.Object":
                {
                    if (calledMethodName == "ToString")
                    {
                        if (constrainedType != null)
                        {
                            TypeDef enumTypeDef = constrainedType.ResolveTypeDef();
                            if (enumTypeDef != null && enumTypeDef.IsEnum && IsAnyEnumItemRenamed(enumTypeDef))
                            {
                                Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: {enumTypeDef.FullName}.ToString() the enum members are renamed.");
                            }
                        }
                    }
                    break;
                }
                case "System.Type":
                {
                    AnalyzeGetType(calledMethod, callType);
                    break;
                }
            }
        }

        private TypeSig GetMethodGenericParameter(IMethod method)
        {
            if (method is MethodSpec ms)
            {
                return ms.GenericInstMethodSig.GenericArguments.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        private void AnalyzeEnum(IMethod method, TypeDef typeDef)
        {
            const int extraSearchInstructionCount = 3;
            TypeSig parseTypeSig = GetMethodGenericParameter(method);
            TypeDef parseType = parseTypeSig?.ToTypeDefOrRef().ResolveTypeDef();
            switch (method.Name)
            {
                case "Parse":
                {
                    if (parseTypeSig != null)
                    {
                        // Enum.Parse<T>(string name) or Enum.Parse<T>(string name, bool caseInsensitive)
                        if (parseType != null)
                        {
                            if (IsAnyEnumItemRenamed(parseType))
                            {
                                Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.Parse<T> field of T:{parseType.FullName} is renamed.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.Parse<T> field of T should not be renamed.");
                        }
                    }
                    else
                    {
                        // Enum.Parse(Type type, string name) or Enum.Parse(Type type, string name, bool ignoreCase)
                        TypeDef enumType = FindLatestTypeOf(method.GetParamCount() + extraSearchInstructionCount)?.ResolveTypeDef();
                        if (enumType != null && enumType.IsEnum && IsAnyEnumItemRenamed(enumType))
                        {
                            Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.Parse field of argument type:{enumType.FullName} is renamed.");
                        }
                        else
                        {
                            Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.Parse field of argument `type` should not be renamed.");
                        }
                    }
                    break;
                }
                case "TryParse":
                {
                    if (parseTypeSig != null)
                    {
                        // Enum.TryParse<T>(string name, out T result) or Enum.TryParse<T>(string name, bool ignoreCase, out T result)
                        if (parseType != null)
                        {
                            if (IsAnyEnumItemRenamed(parseType))
                            {
                                Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.TryParse<T> field of T:{parseType.FullName} is renamed.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.TryParse<T> field of T should not be renamed.");
                        }
                    }
                    else
                    {
                        TypeDef enumType = FindLatestTypeOf(method.GetParamCount() + extraSearchInstructionCount)?.ResolveTypeDef();
                        if (enumType != null && enumType.IsEnum && IsAnyEnumItemRenamed(enumType))
                        {
                            Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.TryParse field of argument type:{enumType.FullName} is renamed.");
                        }
                        else
                        {
                            Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.TryParse field of argument `type` should not be renamed.");
                        }
                    }
                    break;
                }
                case "GetName":
                {
                    // Enum.GetName(Type type, object value)
                    TypeDef enumType = FindLatestTypeOf(method.GetParamCount() + extraSearchInstructionCount)?.ResolveTypeDef();
                    if (enumType != null && enumType.IsEnum && IsAnyEnumItemRenamed(enumType))
                    {
                        Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.GetName field of type:{enumType.FullName} is renamed.");
                    }
                    else
                    {
                        Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.GetName field of argument `type` should not be renamed.");
                    }
                    break;
                }
                case "GetNames":
                {
                    // Enum.GetNames(Type type)
                    TypeDef enumType = FindLatestTypeOf(method.GetParamCount() + extraSearchInstructionCount)?.ResolveTypeDef();
                    if (enumType != null && enumType.IsEnum && IsAnyEnumItemRenamed(enumType))
                    {
                        Debug.LogError($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.GetNames field of type:{enumType.FullName} is renamed.");
                    }
                    else
                    {
                        Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Enum.GetNames field of argument `type` should not be renamed.");
                    }
                    break;
                }
            }
        }

        private void AnalyzeGetType(IMethod method, TypeDef declaringType)
        {
            switch (method.Name)
            {
                case "GetType":
                {
                    Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: Type.GetType argument `typeName` should not be renamed.");
                    break;
                }
                case "GetField":
                case "GetFields":
                case "GetMethod":
                case "GetMethods":
                case "GetProperty":
                case "GetProperties":
                case "GetEvent":
                case "GetEvents":
                case "GetMembers":
                {
                    Debug.LogWarning($"[ReflectionCompatibilityDetector] Reflection compatibility issue in {_curCallingMethod}: called method:{method} the members of type should not be renamed.");
                    break;
                }
            }
        }
    }
}
