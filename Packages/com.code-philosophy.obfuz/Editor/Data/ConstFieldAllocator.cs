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
using Obfuz.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Obfuz.Data
{
    public class ConstFieldAllocator : GroupByModuleEntityBase
    {
        private RandomCreator _randomCreator;
        private IEncryptor _encryptor;

        private TypeDef _holderTypeDef;

        class ConstFieldInfo
        {
            public FieldDef field;
            public object value;
        }

        class AnyComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                if (x is byte[] xBytes && y is byte[] yBytes)
                {
                    return StructuralComparisons.StructuralEqualityComparer.Equals(xBytes, yBytes);
                }
                return x.Equals(y);
            }

            public static int ComputeHashCode(object obj)
            {
                return HashUtil.ComputePrimitiveOrStringOrBytesHashCode(obj);
            }

            public int GetHashCode(object obj)
            {
                return ComputeHashCode(obj);
            }
        }

        private readonly Dictionary<object, ConstFieldInfo> _allocatedFields = new Dictionary<object, ConstFieldInfo>(new AnyComparer());
        private readonly Dictionary<FieldDef, ConstFieldInfo> _field2Fields = new Dictionary<FieldDef, ConstFieldInfo>();

        private readonly List<TypeDef> _holderTypeDefs = new List<TypeDef>();
        private bool _done;


        public ConstFieldAllocator()
        {
        }

        public override void Init()
        {
            _randomCreator = EncryptionScope.localRandomCreator;
            _encryptor = EncryptionScope.encryptor;
        }

        const int maxFieldCount = 1000;


        private TypeSig GetTypeSigOfValue(object value)
        {
            ModuleDef mod = Module;
            if (value is int)
                return mod.CorLibTypes.Int32;
            if (value is long)
                return mod.CorLibTypes.Int64;
            if (value is float)
                return mod.CorLibTypes.Single;
            if (value is double)
                return mod.CorLibTypes.Double;
            if (value is string)
                return mod.CorLibTypes.String;
            if (value is byte[])
                return new SZArraySig(mod.CorLibTypes.Byte);
            throw new NotSupportedException($"Unsupported type: {value.GetType()}");
        }

        private ConstFieldInfo CreateConstFieldInfo(object value)
        {
            ModuleDef mod = Module;
            if (_holderTypeDef == null || _holderTypeDef.Fields.Count >= maxFieldCount)
            {
                using (var scope = new DisableTypeDefFindCacheScope(mod))
                {
                    ITypeDefOrRef objectTypeRef = mod.Import(typeof(object));
                    _holderTypeDef = new TypeDefUser($"{ConstValues.ObfuzInternalSymbolNamePrefix}ConstFieldHolder${_holderTypeDefs.Count}", objectTypeRef);
                    mod.Types.Add(_holderTypeDef);
                    _holderTypeDefs.Add(_holderTypeDef);
                }
            }

            var field = new FieldDefUser($"{ConstValues.ObfuzInternalSymbolNamePrefix}RVA_Value{_holderTypeDef.Fields.Count}", new FieldSig(GetTypeSigOfValue(value)), FieldAttributes.Static | FieldAttributes.Public);
            field.DeclaringType = _holderTypeDef;
            return new ConstFieldInfo
            {
                field = field,
                value = value,
            };
        }

        private FieldDef AllocateAny(object value)
        {
            if (_done)
            {
                throw new Exception("can't Allocate after done");
            }
            if (!_allocatedFields.TryGetValue(value, out var field))
            {
                field = CreateConstFieldInfo(value);
                _allocatedFields.Add(value, field);
                _field2Fields.Add(field.field, field);
            }
            return field.field;
        }

        public FieldDef Allocate(int value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(long value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(float value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(double value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(string value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(byte[] value)
        {
            return AllocateAny(value);
        }


        private void CreateCCtorOfRvaTypeDef(TypeDef type)
        {
            ModuleDef mod = Module;
            var cctor = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(mod.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctor.DeclaringType = type;
            var body = new CilBody();
            cctor.Body = body;
            var ins = body.Instructions;


            DefaultMetadataImporter importer = this.GetDefaultModuleMetadataImporter();
            RvaDataAllocator rvaDataAllocator = GetEntity<RvaDataAllocator>();
            // TODO. obfuscate init codes
            foreach (var field in type.Fields)
            {
                ConstFieldInfo constInfo = _field2Fields[field];
                IRandom localRandom = _randomCreator(HashUtil.ComputePrimitiveOrStringOrBytesHashCode(constInfo.value));
                int ops = EncryptionUtil.GenerateEncryptionOpCodes(localRandom, _encryptor, EncryptionScopeInfo.MaxEncryptionLevel, false);
                int salt = localRandom.NextInt();
                switch (constInfo.value)
                {
                    case int i:
                    {
                        int encryptedValue = _encryptor.Encrypt(i, ops, salt);
                        RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
                        break;
                    }
                    case long l:
                    {
                        long encryptedValue = _encryptor.Encrypt(l, ops, salt);
                        RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaLong));
                        break;
                    }
                    case float f:
                    {
                        float encryptedValue = _encryptor.Encrypt(f, ops, salt);
                        RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaFloat));
                        break;
                    }
                    case double d:
                    {
                        double encryptedValue = _encryptor.Encrypt(d, ops, salt);
                        RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaDouble));
                        break;
                    }
                    case string s:
                    {
                        byte[] encryptedValue = _encryptor.Encrypt(s, ops, salt);
                        RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        Assert.AreEqual(encryptedValue.Length, rvaData.size);
                        ins.Add(Instruction.CreateLdcI4(encryptedValue.Length));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaString));
                        break;
                    }
                    case byte[] bs:
                    {
                        byte[] encryptedValue = _encryptor.Encrypt(bs, 0, bs.Length, ops, salt);
                        Assert.AreEqual(encryptedValue.Length, bs.Length);
                        RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(bs.Length));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaBytes));
                        break;
                    }
                    default: throw new NotSupportedException($"Unsupported type: {constInfo.value.GetType()}");
                }
                ins.Add(Instruction.Create(OpCodes.Stsfld, field));
            }
            ins.Add(Instruction.Create(OpCodes.Ret));
        }

        public override void Done()
        {
            if (_done)
            {
                throw new Exception("Already done");
            }
            _done = true;
            foreach (var typeDef in _holderTypeDefs)
            {
                CreateCCtorOfRvaTypeDef(typeDef);
            }
        }
    }
}
