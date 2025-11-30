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
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public class DefaultConstEncryptor : IConstEncryptor
    {
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private readonly ConstEncryptionSettingsFacade _settings;

        public DefaultConstEncryptor(GroupByModuleEntityManager moduleEntityManager, ConstEncryptionSettingsFacade settings)
        {
            _moduleEntityManager = moduleEntityManager;
            _settings = settings;
        }

        private IRandom CreateRandomForValue(EncryptionScopeInfo encryptionScope, int value)
        {
            return encryptionScope.localRandomCreator(value);
        }

        private int GenerateEncryptionOperations(EncryptionScopeInfo encryptionScope, IRandom random)
        {
            return EncryptionUtil.GenerateEncryptionOpCodes(random, encryptionScope.encryptor, _settings.encryptionLevel);
        }

        public int GenerateSalt(IRandom random)
        {
            return random.NextInt();
        }

        private DefaultMetadataImporter GetModuleMetadataImporter(MethodDef method)
        {
            return _moduleEntityManager.GetEntity<DefaultMetadataImporter>(method.Module);
        }

        public void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions)
        {
            EncryptionScopeInfo encryptionScope = _moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            ConstFieldAllocator constFieldAllocator = _moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module);
            RvaDataAllocator rvaDataAllocator = _moduleEntityManager.GetEntity<RvaDataAllocator>(method.Module);
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);

            switch (random.NextInt(5))
            {
                case 0:
                {
                    // = c = encrypted static field
                    FieldDef cacheField = constFieldAllocator.Allocate(value);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                    break;
                }
                case 1:
                {
                    // c = a + b
                    int a = random.NextInt();
                    int b = value - a;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstTwoInt(a, b, random, constProbability, constFieldAllocator, obfuscatedInstructions);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Add));
                    break;
                }
                case 2:
                {
                    // c = a * b
                    int a = random.NextInt() | 0x1;
                    int ra = MathUtil.ModInverse32(a);
                    int b = ra * value;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstTwoInt(a, b, random, constProbability, constFieldAllocator, obfuscatedInstructions);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Mul));
                    break;
                }
                case 3:
                {
                    // c = a ^ b
                    int a = random.NextInt();
                    int b = a ^ value;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstTwoInt(a, b, random, constProbability, constFieldAllocator, obfuscatedInstructions);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Xor));
                    break;
                }
                default:
                {
                    if (needCacheValue)
                    {
                        FieldDef cacheField = constFieldAllocator.Allocate(value);
                        obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                        return;
                    }
                    int ops = GenerateEncryptionOperations(encryptionScope, random);
                    int salt = GenerateSalt(random);
                    int encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
                    RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                    obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
                    obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
                    obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
                    break;
                }
            }


        }

        public void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions)
        {
            EncryptionScopeInfo encryptionScope = _moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            ConstFieldAllocator constFieldAllocator = _moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module);
            RvaDataAllocator rvaDataAllocator = _moduleEntityManager.GetEntity<RvaDataAllocator>(method.Module);
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);

            switch (random.NextInt(5))
            {
                case 0:
                {
                    // c = encrypted static field
                    FieldDef cacheField = constFieldAllocator.Allocate(value);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                    break;
                }
                case 1:
                {
                    // c = a + b
                    long a = random.NextLong();
                    long b = value - a;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstTwoLong(a, b, random, constProbability, constFieldAllocator, obfuscatedInstructions);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Add));
                    break;
                }
                case 2:
                {
                    // c = a * b
                    long a = random.NextLong() | 0x1;
                    long ra = MathUtil.ModInverse64(a);
                    long b = ra * value;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstTwoLong(a, b, random, constProbability, constFieldAllocator, obfuscatedInstructions);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Mul));
                    break;
                }
                case 3:
                {
                    // c = a ^ b
                    long a = random.NextLong();
                    long b = a ^ value;
                    float constProbability = 0.5f;
                    ConstObfusUtil.LoadConstTwoLong(a, b, random, constProbability, constFieldAllocator, obfuscatedInstructions);
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Xor));
                    break;
                }
                default:
                {
                    if (needCacheValue)
                    {
                        FieldDef cacheField = constFieldAllocator.Allocate(value);
                        obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                        return;
                    }

                    int ops = GenerateEncryptionOperations(encryptionScope, random);
                    int salt = GenerateSalt(random);
                    long encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
                    RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);

                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                    obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
                    obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
                    obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaLong));
                    break;
                }
            }


        }

        public void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions)
        {
            EncryptionScopeInfo encryptionScope = _moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            ConstFieldAllocator constFieldAllocator = _moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module);
            RvaDataAllocator rvaDataAllocator = _moduleEntityManager.GetEntity<RvaDataAllocator>(method.Module);
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);

            if (needCacheValue)
            {
                FieldDef cacheField = constFieldAllocator.Allocate(value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }


            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            float encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);

            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaFloat));
        }

        public void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions)
        {
            EncryptionScopeInfo encryptionScope = _moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            ConstFieldAllocator constFieldAllocator = _moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module);
            RvaDataAllocator rvaDataAllocator = _moduleEntityManager.GetEntity<RvaDataAllocator>(method.Module);
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);

            if (needCacheValue)
            {
                FieldDef cacheField = constFieldAllocator.Allocate(value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }


            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            double encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);

            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaDouble));
        }


        class EncryptedRvaDataInfo
        {
            public readonly FieldDef fieldDef;
            public readonly byte[] originalBytes;
            public readonly byte[] encryptedBytes;
            public readonly int opts;
            public readonly int salt;

            public EncryptedRvaDataInfo(FieldDef fieldDef, byte[] originalBytes, byte[] encryptedBytes, int opts, int salt)
            {
                this.fieldDef = fieldDef;
                this.originalBytes = originalBytes;
                this.encryptedBytes = encryptedBytes;
                this.opts = opts;
                this.salt = salt;
            }
        }

        private readonly Dictionary<FieldDef, EncryptedRvaDataInfo> _encryptedRvaFields = new Dictionary<FieldDef, EncryptedRvaDataInfo>();

        private EncryptedRvaDataInfo GetEncryptedRvaData(FieldDef fieldDef)
        {
            if (!_encryptedRvaFields.TryGetValue(fieldDef, out var encryptedRvaData))
            {
                EncryptionScopeInfo encryptionScope = _moduleEntityManager.EncryptionScopeProvider.GetScope(fieldDef.Module);
                IRandom random = CreateRandomForValue(encryptionScope, FieldEqualityComparer.CompareDeclaringTypes.GetHashCode(fieldDef));
                int ops = GenerateEncryptionOperations(encryptionScope, random);
                int salt = GenerateSalt(random);
                byte[] originalBytes = fieldDef.InitialValue;
                byte[] encryptedBytes = (byte[])originalBytes.Clone();
                encryptionScope.encryptor.EncryptBlock(encryptedBytes, ops, salt);
                Assert.AreNotEqual(originalBytes, encryptedBytes, "Original bytes should not be the same as encrypted bytes.");
                encryptedRvaData = new EncryptedRvaDataInfo(fieldDef, originalBytes, encryptedBytes, ops, salt);
                _encryptedRvaFields.Add(fieldDef, encryptedRvaData);
                fieldDef.InitialValue = encryptedBytes;
                byte[] decryptedBytes = (byte[])encryptedBytes.Clone();
                encryptionScope.encryptor.DecryptBlock(decryptedBytes, ops, salt);
                AssertUtil.AreArrayEqual(originalBytes, decryptedBytes, "Decrypted bytes should match the original bytes after encryption and decryption.");
            }
            return encryptedRvaData;
        }


        public void ObfuscateBytes(MethodDef method, bool needCacheValue, FieldDef field, byte[] value, List<Instruction> obfuscatedInstructions)
        {
            EncryptedRvaDataInfo encryptedData = GetEncryptedRvaData(field);
            Assert.AreEqual(value.Length, encryptedData.encryptedBytes.Length);

            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(encryptedData.encryptedBytes.Length));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(encryptedData.opts));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(encryptedData.salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInitializeArray));
        }

        public void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions)
        {
            EncryptionScopeInfo encryptionScope = _moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            ConstFieldAllocator constFieldAllocator = _moduleEntityManager.GetEntity<ConstFieldAllocator>(method.Module);
            RvaDataAllocator rvaDataAllocator = _moduleEntityManager.GetEntity<RvaDataAllocator>(method.Module);
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);

            if (needCacheValue)
            {
                FieldDef cacheField = constFieldAllocator.Allocate(value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            int stringByteLength = Encoding.UTF8.GetByteCount(value);
            byte[] encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            Assert.AreEqual(stringByteLength, encryptedValue.Length);
            RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);

            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            // should use stringByteLength, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(stringByteLength));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaString));
        }

        public void Done()
        {
        }
    }
}
