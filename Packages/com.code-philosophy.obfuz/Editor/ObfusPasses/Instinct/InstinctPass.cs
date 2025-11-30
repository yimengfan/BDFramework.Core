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
using Obfuz.Editor;
using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.Instinct
{

    public class InstinctPass : InstructionObfuscationPassBase
    {
        public override ObfuscationPassType Type => ObfuscationPassType.None;

        protected override bool ForceProcessAllAssembliesAndIgnoreAllPolicy => true;

        public InstinctPass()
        {
        }

        public override void Start()
        {
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        private string GetTypeName(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.Class:
                case ElementType.ValueType:
                {
                    return type.ReflectionName;
                }
                case ElementType.GenericInst:
                {
                    type = ((GenericInstSig)type).GenericType;
                    return type.ReflectionName;
                }
                default: return type.ReflectionName;
            }
        }

        private string GetTypeFullName(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();

            switch (type.ElementType)
            {
                case ElementType.Class:
                case ElementType.ValueType:
                {
                    return type.ReflectionFullName;
                }
                case ElementType.GenericInst:
                {
                    GenericInstSig genericInstSig = (GenericInstSig)type;
                    var typeName = new StringBuilder(genericInstSig.GenericType.ReflectionFullName);
                    typeName.Append("<").Append(string.Join(",", genericInstSig.GenericArguments.Select(GetTypeFullName))).Append(">");
                    return typeName.ToString();
                }
                default: return type.ReflectionFullName;
            }
        }

        protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            Code code = inst.OpCode.Code;
            if (!(inst.Operand is IMethod method) || !method.IsMethod)
            {
                return false;
            }
            MethodDef methodDef = method.ResolveMethodDef();
            if (methodDef == null || methodDef.DeclaringType.Name != "ObfuscationInstincts" || methodDef.DeclaringType.DefinitionAssembly.Name != ConstValues.ObfuzRuntimeAssemblyName)
            {
                return false;
            }

            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            var importer = ctx.moduleEntityManager.GetEntity<DefaultMetadataImporter>(callingMethod.Module);

            string methodName = methodDef.Name;
            switch (methodName)
            {
                case "FullNameOf":
                case "NameOf":
                case "RegisterReflectionType":
                {
                    MethodSpec methodSpec = (MethodSpec)method;
                    GenericInstMethodSig gims = methodSpec.GenericInstMethodSig;
                    Assert.AreEqual(1, gims.GenericArguments.Count, "FullNameOf should have exactly one generic argument");
                    TypeSig type = gims.GenericArguments[0];
                    switch (methodName)
                    {
                        case "FullNameOf":
                        {
                            string typeFullName = GetTypeFullName(type);
                            outputInstructions.Add(Instruction.Create(OpCodes.Ldstr, typeFullName));
                            break;
                        }
                        case "NameOf":
                        {
                            string typeName = GetTypeName(type);
                            outputInstructions.Add(Instruction.Create(OpCodes.Ldstr, typeName));
                            break;
                        }
                        case "RegisterReflectionType":
                        {
                            string typeFullName = GetTypeFullName(type);
                            outputInstructions.Add(Instruction.Create(OpCodes.Ldstr, typeFullName));
                            var finalMethod = new MethodSpecUser((IMethodDefOrRef)importer.ObfuscationTypeMapperRegisterType, gims);
                            outputInstructions.Add(Instruction.Create(OpCodes.Call, finalMethod));
                            break;
                        }
                        default: throw new NotSupportedException($"Unsupported instinct method: {methodDef.FullName}");
                    }
                    break;
                }
                default: throw new NotSupportedException($"Unsupported instinct method: {methodDef.FullName}");
            }
            //Debug.Log($"memory encrypt field: {field}");
            return true;
        }
    }
}
