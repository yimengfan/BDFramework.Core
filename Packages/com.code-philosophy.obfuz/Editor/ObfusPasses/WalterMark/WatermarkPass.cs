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
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using HashUtil = Obfuz.Utils.HashUtil;
using IRandom = Obfuz.Utils.IRandom;
using KeyGenerator = Obfuz.Utils.KeyGenerator;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace Obfuz.ObfusPasses.Watermark
{
    public class WatermarkPass : ObfuscationPassBase
    {
        private readonly WatermarkSettingsFacade _watermarkSettings;

        public WatermarkPass(WatermarkSettingsFacade watermarkSettingsFacade)
        {
            this._watermarkSettings = watermarkSettingsFacade;
        }

        public override ObfuscationPassType Type => ObfuscationPassType.WaterMark;

        public override void Start()
        {
        }

        public override void Stop()
        {

        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            foreach (ModuleDef mod in ctx.modulesToObfuscate)
            {
                AddWaterMarkToAssembly(mod, _watermarkSettings.text);
            }
        }

        private TypeDef GetDataHolderType(ModuleDef module, TypeDef declaringType, int size)
        {

            using (var scope = new DisableTypeDefFindCacheScope(module))
            {
                var dataHolderType = new TypeDefUser($"$Obfuz$WatermarkDataHolderSize{size}_{declaringType.NestedTypes.Count}", module.Import(typeof(System.ValueType)));
                dataHolderType.Attributes = TypeAttributes.NestedPrivate | TypeAttributes.Sealed;
                dataHolderType.Layout = TypeAttributes.ExplicitLayout;
                dataHolderType.PackingSize = 1;
                dataHolderType.ClassSize = (uint)size;
                dataHolderType.DeclaringType = declaringType;
                return dataHolderType;
            }
        }

        class WatermarkInfo
        {
            public string text;
            public byte[] signature;
            public readonly List<FieldDef> signatureHoldFields = new List<FieldDef>();
        }

        private WatermarkInfo CreateWatermarkInfo(ModuleDef module, EncryptionScopeInfo encryptionScope, string waterMarkText)
        {
            string finalWatermarkText = $"{waterMarkText} [{module.Name}]";
            byte[] watermarkBytes = KeyGenerator.GenerateKey(finalWatermarkText, _watermarkSettings.signatureLength);

            var watermarkInfo = new WatermarkInfo()
            {
                text = finalWatermarkText,
                signature = watermarkBytes,
            };

            TypeDef moduleType = module.FindNormal("<PrivateImplementationDetails>");
            if (moduleType == null)
            {
                //throw new Exception($"Module '{module.Name}' does not contain a '<PrivateImplementationDetails>' type.");
                moduleType = new TypeDefUser("<PrivateImplementationDetails>", module.Import(typeof(object)));
                moduleType.Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed;
                moduleType.CustomAttributes.Add(new CustomAttribute(module.Import(module.Import(typeof(CompilerGeneratedAttribute)).ResolveTypeDefThrow().FindDefaultConstructor())));
                module.Types.Add(moduleType);
            }
            var random = encryptionScope.localRandomCreator(0);
            for (int subIndex = 0; subIndex < watermarkBytes.Length;)
            {
                int subSegmentLength = Math.Min(random.NextInt(16, 32) & ~3, watermarkBytes.Length - subIndex);
                int paddingLength = random.NextInt(8, 32) & ~3;
                int totalLength = subSegmentLength + paddingLength;
                TypeDef dataHolderType = GetDataHolderType(module, moduleType, totalLength);

                byte[] subSegment = new byte[totalLength];
                Buffer.BlockCopy(watermarkBytes, subIndex, subSegment, 0, subSegmentLength);

                for (int i = subSegmentLength; i < totalLength; i++)
                {
                    subSegment[i] = (byte)random.NextInt(0, 256);
                }

                subIndex += subSegmentLength;
                var field = new FieldDefUser($"$Obfuz$WatermarkDataHolderField{moduleType.Fields.Count}",
                    new FieldSig(dataHolderType.ToTypeSig()),
                    FieldAttributes.Assembly | FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
                field.DeclaringType = moduleType;
                field.InitialValue = subSegment;
                watermarkInfo.signatureHoldFields.Add(field);
            }

            var moduleTypeFields = moduleType.Fields.ToList();
            RandomUtil.ShuffleList(moduleTypeFields, random);
            moduleType.Fields.Clear();
            foreach (var field in moduleTypeFields)
            {
                moduleType.Fields.Add(field);
            }
            return watermarkInfo;
        }

        private int GetRandomInsertPosition(List<Instruction> instructions, IRandom random)
        {
            var insertPositions = instructions
                .Select((inst, index) => new { Instruction = inst, Index = index })
                .Where(x => x.Instruction.OpCode.FlowControl == FlowControl.Next)
                .Select(x => x.Index)
                .ToList();
            if (insertPositions.Count == 0)
            {
                return 0; // No valid position to insert
            }
            return insertPositions[random.NextInt(insertPositions.Count)] + 1;
        }

        private void AddFieldAccessToSignatureHolder(ModuleDef module, EncryptionScopeInfo encryptionScope, WatermarkInfo watermarkInfo)
        {
            BurstCompileComputeCache burstCompileComputeCache = ObfuscationPassContext.Current.burstCompileComputeCache;
            var insertTargetMethods = module.Types
                .Where(t => !MetaUtil.HasBurstCompileAttribute(t))
                .SelectMany(t => t.Methods)
                .Where(m => m.HasBody && m.Body.Instructions.Count > 10 && !MetaUtil.HasBurstCompileAttribute(m)
                && !burstCompileComputeCache.IsBurstCompileMethodOrReferencedByBurstCompileMethod(m))
                .ToList();

            if (insertTargetMethods.Count == 0)
            {
                Debug.LogWarning($"No suitable methods found in module '{module.Name}' to insert access to watermark signature.");
                return;
            }

            var random = encryptionScope.localRandomCreator(HashUtil.ComputeHash($"AddFieldAccessToSignatureHolder:{module.Name}"));
            DefaultMetadataImporter importer = ObfuscationPassContext.Current.moduleEntityManager.GetEntity<DefaultMetadataImporter>(module);
            foreach (var fieldDef in watermarkInfo.signatureHoldFields)
            {
                // Randomly select a method to insert the access
                var targetMethod = insertTargetMethods[random.NextInt(insertTargetMethods.Count)];
                var insts = (List<Instruction>)targetMethod.Body.Instructions;
                int insertIndex = GetRandomInsertPosition(insts, random);
                Instruction nop = Instruction.Create(OpCodes.Nop);
                insts.InsertRange(insertIndex, new[]
                {
	                Instruction.CreateLdcI4(random.NextInt(1, 10000000)),
	                Instruction.Create(OpCodes.Brtrue, nop),
	                Instruction.CreateLdcI4(random.NextInt(fieldDef.InitialValue.Length)),
	                Instruction.Create(OpCodes.Newarr, module.CorLibTypes.Byte),
	                Instruction.Create(OpCodes.Ldtoken, fieldDef),
	                Instruction.Create(OpCodes.Call, importer.InitializedArray),
	                nop,
                });
                //Debug.Log($"Inserted watermark access for field '{fieldDef.Name}' in method '{targetMethod.FullName}' at index {insertIndex}.");
            }
        }

        private readonly OpCode[] binOpCodes = new[]
        {
                OpCodes.Add, OpCodes.Sub, OpCodes.Mul, OpCodes.Div, OpCodes.Rem,
                OpCodes.And, OpCodes.Or, OpCodes.Xor
            };

        private OpCode GetRandomBinOpCode(IRandom random)
        {
            return binOpCodes[random.NextInt(binOpCodes.Length)];
        }

        private void AddWaterMarkILSequences(ModuleDef module, EncryptionScopeInfo encryptionScope, WatermarkInfo watermarkInfo)
        {
            var insertTargetMethods = module.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.HasBody && m.Body.Instructions.Count > 10)
                .ToList();

            if (insertTargetMethods.Count == 0)
            {
                Debug.LogWarning($"No suitable methods found in module '{module.Name}' to insert watermark IL sequences.");
                return;
            }
            var random = encryptionScope.localRandomCreator(HashUtil.ComputeHash($"AddWaterMarkILSequences:{module.Name}"));
            int[] signature = KeyGenerator.ConvertToIntKey(watermarkInfo.signature);
            for (int intIndex = 0; intIndex < signature.Length;)
            {
                int ldcCount = Math.Min(random.NextInt(2, 4), signature.Length - intIndex);
                // Randomly select a method to insert the IL sequence
                var targetMethod = insertTargetMethods[random.NextInt(insertTargetMethods.Count)];
                var insts = (List<Instruction>)targetMethod.Body.Instructions;
                int insertIndex = GetRandomInsertPosition(insts, random);
                var insertInstructions = new List<Instruction>()
                {
                    Instruction.CreateLdcI4(random.NextInt(1, 10000000)),
                    Instruction.Create(OpCodes.Brtrue, insts[insertIndex]),
                };
                for (int i = 0; i < ldcCount; i++)
                {
                    insertInstructions.Add(Instruction.CreateLdcI4(signature[intIndex + i]));
                    if (i > 0)
                    {
                        insertInstructions.Add(Instruction.Create(GetRandomBinOpCode(random)));
                    }
                }
                insertInstructions.Add(Instruction.Create(OpCodes.Pop));

                insts.InsertRange(insertIndex, insertInstructions);
                intIndex += ldcCount;
                //Debug.Log($"Inserted watermark IL sequence for in method '{targetMethod.FullName}' at index {insertIndex}.");
            }
        }

        private void AddWaterMarkToAssembly(ModuleDef module, string waterMarkText)
        {
            var ctx = ObfuscationPassContext.Current;
            EncryptionScopeInfo encryptionScope = ctx.moduleEntityManager.EncryptionScopeProvider.GetScope(module);
            WatermarkInfo watermarkInfo = CreateWatermarkInfo(module, encryptionScope, waterMarkText);
            AddFieldAccessToSignatureHolder(module, encryptionScope, watermarkInfo);
            AddWaterMarkILSequences(module, encryptionScope, watermarkInfo);
        }


    }
}
