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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Assertions;

namespace Obfuz.Data
{
    public struct RvaData
    {
        public readonly FieldDef field;
        public readonly int offset;
        public readonly int size;

        public RvaData(FieldDef field, int offset, int size)
        {
            this.field = field;
            this.offset = offset;
            this.size = size;
        }
    }

    public class RvaDataAllocator : GroupByModuleEntityBase
    {
        const int maxRvaDataSize = 2 * 1024;

        // in HybridCLR version below 8.3.0, the max total static field size of a type is 16KB, so we limit the total size of RVA data to 16KB
        const int maxTotalRvaDataFieldSizeInHybridCLR = 16 * 1024;

        private IRandom _random;

        class RvaField
        {
            public FieldDef holderDataField;
            public FieldDef runtimeValueField;
            public int encryptionOps;
            public uint size;
            public List<byte> bytes;
            public int salt;

            public void FillPaddingToSize(int newSize)
            {
                for (int i = bytes.Count; i < newSize; i++)
                {
                    bytes.Add(0xAB);
                }
            }

            public void FillPaddingToEnd()
            {
                // fill with random value
                for (int i = bytes.Count; i < size; i++)
                {
                    bytes.Add(0xAB);
                }
            }
        }

        private class RvaTypeDefInfo
        {
            public readonly TypeDef typeDef;
            public readonly int index;
            public readonly List<RvaField> rvaFields = new List<RvaField>();

            public RvaTypeDefInfo(TypeDef typeDef, int index)
            {
                this.typeDef = typeDef;
                this.index = index;
            }
        }

        private RvaField _currentField;

        private RvaTypeDefInfo _currentRvaType;
        private readonly List<RvaTypeDefInfo> _rvaTypeDefs = new List<RvaTypeDefInfo>();

        private readonly Dictionary<int, TypeDef> _dataHolderTypeBySizes = new Dictionary<int, TypeDef>();
        private bool _done;

        public RvaDataAllocator()
        {
        }

        public override void Init()
        {
            _random = EncryptionScope.localRandomCreator(HashUtil.ComputeHash(Module.Name));
        }

        private (FieldDef, FieldDef) CreateDataHolderRvaField(TypeDef dataHolderType)
        {
            if (_currentRvaType == null || _currentRvaType.rvaFields.Count >= maxTotalRvaDataFieldSizeInHybridCLR / maxRvaDataSize - 1)
            {
                using (var scope = new DisableTypeDefFindCacheScope(Module))
                {
                    var rvaTypeDef = new TypeDefUser($"$Obfuz$RVA${_rvaTypeDefs.Count}", Module.CorLibTypes.Object.ToTypeDefOrRef());
                    Module.Types.Add(rvaTypeDef);
                    _currentRvaType = new RvaTypeDefInfo(rvaTypeDef, _rvaTypeDefs.Count);
                    _rvaTypeDefs.Add(_currentRvaType);
                }
            }

            var holderField = new FieldDefUser($"$RVA_Data{_currentRvaType.rvaFields.Count}", new FieldSig(dataHolderType.ToTypeSig()), FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
            holderField.DeclaringType = _currentRvaType.typeDef;

            var runtimeValueField = new FieldDefUser($"$RVA_Value{_currentRvaType.rvaFields.Count}", new FieldSig(new SZArraySig(Module.CorLibTypes.Byte)), FieldAttributes.Static | FieldAttributes.Public);
            runtimeValueField.DeclaringType = _currentRvaType.typeDef;
            return (holderField, runtimeValueField);
        }

        private TypeDef GetDataHolderType(int size)
        {
            size = (size + 15) & ~15; // align to 16 bytes
            if (_dataHolderTypeBySizes.TryGetValue(size, out var type))
                return type;

            using (var scope = new DisableTypeDefFindCacheScope(Module))
            {
                var dataHolderType = new TypeDefUser($"$ObfuzRVA$DataHolder{size}", Module.Import(typeof(ValueType)));
                dataHolderType.Attributes = TypeAttributes.Public | TypeAttributes.Sealed;
                dataHolderType.Layout = TypeAttributes.ExplicitLayout;
                dataHolderType.PackingSize = 1;
                dataHolderType.ClassSize = (uint)size;
                _dataHolderTypeBySizes.Add(size, dataHolderType);
                Module.Types.Add(dataHolderType);
                return dataHolderType;
            }
        }

        private static int AlignTo(int size, int alignment)
        {
            return (size + alignment - 1) & ~(alignment - 1);
        }

        private RvaField CreateRvaField(int size)
        {
            TypeDef dataHolderType = GetDataHolderType(size);
            var (holderDataField, runtimeValueField) = CreateDataHolderRvaField(dataHolderType);
            var newRvaField = new RvaField
            {
                holderDataField = holderDataField,
                runtimeValueField = runtimeValueField,
                size = dataHolderType.ClassSize,
                bytes = new List<byte>((int)dataHolderType.ClassSize),
                encryptionOps = _random.NextInt(),
                salt = _random.NextInt(),
            };
            _currentRvaType.rvaFields.Add(newRvaField);
            return newRvaField;
        }

        private RvaField GetRvaField(int preservedSize, int alignment)
        {
            if (_done)
            {
                throw new Exception("can't GetRvaField after done");
            }
            Assert.IsTrue(preservedSize % alignment == 0);
            // for big size, create a new field
            if (preservedSize >= maxRvaDataSize)
            {
                return CreateRvaField(preservedSize);
            }

            if (_currentField != null)
            {
                int offset = AlignTo(_currentField.bytes.Count, alignment);

                int expectedSize = offset + preservedSize;
                if (expectedSize <= _currentField.size)
                {
                    _currentField.FillPaddingToSize(offset);
                    return _currentField;
                }

                _currentField.FillPaddingToEnd();
            }
            _currentField = CreateRvaField(maxRvaDataSize);
            return _currentField;
        }

        public RvaData Allocate(int value)
        {
            RvaField field = GetRvaField(4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(long value)
        {
            RvaField field = GetRvaField(8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(float value)
        {
            RvaField field = GetRvaField(4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(double value)
        {
            RvaField field = GetRvaField(8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Allocate(bytes);
        }

        public RvaData Allocate(byte[] value)
        {
            RvaField field = GetRvaField(value.Length, 1);
            int offset = field.bytes.Count;
            field.bytes.AddRange(value);
            return new RvaData(field.runtimeValueField, offset, value.Length);
        }


        private void AddVerifyCodes(IList<Instruction> insts, DefaultMetadataImporter importer)
        {
            int verifyIntValue = 0x12345678;
            EncryptionScopeInfo encryptionScope = this.EncryptionScope;
            IRandom verifyRandom = encryptionScope.localRandomCreator(verifyIntValue);
            int verifyOps = EncryptionUtil.GenerateEncryptionOpCodes(verifyRandom, encryptionScope.encryptor, EncryptionScopeInfo.MaxEncryptionLevel, false);
            int verifySalt = verifyRandom.NextInt();
            int encryptedVerifyIntValue = encryptionScope.encryptor.Encrypt(verifyIntValue, verifyOps, verifySalt);

            insts.Add(Instruction.Create(OpCodes.Ldc_I4, verifyIntValue));
            insts.Add(Instruction.CreateLdcI4(encryptedVerifyIntValue));
            insts.Add(Instruction.CreateLdcI4(verifyOps));
            insts.Add(Instruction.CreateLdcI4(verifySalt));
            insts.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            insts.Add(Instruction.Create(OpCodes.Call, importer.VerifySecretKey));

        }

        private void CreateCCtorOfRvaTypeDef()
        {
            foreach (RvaTypeDefInfo rvaTypeDef in _rvaTypeDefs)
            {
                ModuleDef mod = rvaTypeDef.typeDef.Module;
                var cctorMethod = new MethodDefUser(".cctor",
                    MethodSig.CreateStatic(Module.CorLibTypes.Void),
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
                cctorMethod.DeclaringType = rvaTypeDef.typeDef;
                //_rvaTypeDef.Methods.Add(cctor);
                var body = new CilBody();
                cctorMethod.Body = body;
                var ins = body.Instructions;

                DefaultMetadataImporter importer = this.GetDefaultModuleMetadataImporter();
                AddVerifyCodes(ins, importer);
                foreach (var field in rvaTypeDef.rvaFields)
                {
                    // ldc
                    // newarr
                    // dup
                    // stsfld
                    // ldtoken
                    // RuntimeHelpers.InitializeArray(array, fieldHandle);
                    ins.Add(Instruction.Create(OpCodes.Ldc_I4, (int)field.size));
                    ins.Add(Instruction.Create(OpCodes.Newarr, field.runtimeValueField.FieldType.Next.ToTypeDefOrRef()));
                    ins.Add(Instruction.Create(OpCodes.Dup));
                    ins.Add(Instruction.Create(OpCodes.Dup));
                    ins.Add(Instruction.Create(OpCodes.Stsfld, field.runtimeValueField));
                    ins.Add(Instruction.Create(OpCodes.Ldtoken, field.holderDataField));
                    ins.Add(Instruction.Create(OpCodes.Call, importer.InitializedArray));

                    // EncryptionService.DecryptBlock(array, field.encryptionOps, field.salt);
                    ins.Add(Instruction.CreateLdcI4(field.encryptionOps));
                    ins.Add(Instruction.Create(OpCodes.Ldc_I4, field.salt));
                    ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptBlock));

                }
                ins.Add(Instruction.Create(OpCodes.Ret));
            }
        }

        private void SetFieldsRVA()
        {
            foreach (var field in _rvaTypeDefs.SelectMany(t => t.rvaFields))
            {
                Assert.IsTrue(field.bytes.Count <= field.size);
                if (field.bytes.Count < field.size)
                {
                    field.FillPaddingToEnd();
                }
                byte[] data = field.bytes.ToArray();
                EncryptionScope.encryptor.EncryptBlock(data, field.encryptionOps, field.salt);
                field.holderDataField.InitialValue = data;
            }
        }

        public override void Done()
        {
            if (_done)
            {
                throw new Exception("can't call Done twice");
            }
            _done = true;
            SetFieldsRVA();
            CreateCCtorOfRvaTypeDef();
        }
    }
}
