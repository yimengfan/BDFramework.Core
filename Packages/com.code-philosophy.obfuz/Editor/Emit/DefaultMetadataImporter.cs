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
using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public class EncryptionServiceMetadataImporter
    {
        private readonly ModuleDef _module;
        private readonly Type _encryptionServiceType;

        private IMethod _encryptBlock;
        private IMethod _decryptBlock;
        private IMethod _encryptInt;
        private IMethod _decryptInt;
        private IMethod _encryptLong;
        private IMethod _decryptLong;
        private IMethod _encryptFloat;
        private IMethod _decryptFloat;
        private IMethod _encryptDouble;
        private IMethod _decryptDouble;
        private IMethod _encryptString;
        private IMethod _decryptString;
        private IMethod _encryptBytes;
        private IMethod _decryptBytes;

        private IMethod _decryptFromRvaInt;
        private IMethod _decryptFromRvaLong;
        private IMethod _decryptFromRvaFloat;
        private IMethod _decryptFromRvaDouble;
        private IMethod _decryptFromRvaString;
        private IMethod _decryptFromRvaBytes;

        private IMethod _decryptInitializeArray;

        public IMethod EncryptBlock => _encryptBlock;
        public IMethod DecryptBlock => _decryptBlock;

        public IMethod EncryptInt => _encryptInt;
        public IMethod DecryptInt => _decryptInt;
        public IMethod EncryptLong => _encryptLong;
        public IMethod DecryptLong => _decryptLong;
        public IMethod EncryptFloat => _encryptFloat;
        public IMethod DecryptFloat => _decryptFloat;
        public IMethod EncryptDouble => _encryptDouble;
        public IMethod DecryptDouble => _decryptDouble;
        public IMethod EncryptString => _encryptString;
        public IMethod DecryptString => _decryptString;
        public IMethod EncryptBytes => _encryptBytes;
        public IMethod DecryptBytes => _decryptBytes;

        public IMethod DecryptFromRvaInt => _decryptFromRvaInt;
        public IMethod DecryptFromRvaLong => _decryptFromRvaLong;
        public IMethod DecryptFromRvaFloat => _decryptFromRvaFloat;
        public IMethod DecryptFromRvaDouble => _decryptFromRvaDouble;
        public IMethod DecryptFromRvaBytes => _decryptFromRvaBytes;
        public IMethod DecryptFromRvaString => _decryptFromRvaString;

        public IMethod DecryptInitializeArray => _decryptInitializeArray;

        public EncryptionServiceMetadataImporter(ModuleDef mod, Type encryptionServiceType)
        {
            _module = mod;
            _encryptionServiceType = encryptionServiceType;
            _encryptBlock = mod.Import(encryptionServiceType.GetMethod("EncryptBlock", new[] { typeof(byte[]), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptBlock);
            _decryptBlock = mod.Import(encryptionServiceType.GetMethod("DecryptBlock", new[] { typeof(byte[]), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptBlock);
            _encryptInt = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptInt);
            _decryptInt = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptInt);
            _encryptLong = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(long), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptLong);
            _decryptLong = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(long), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptLong);
            _encryptFloat = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(float), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptFloat);
            _decryptFloat = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(float), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFloat);
            _encryptDouble = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(double), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptDouble);
            _decryptDouble = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(double), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptDouble);
            _encryptString = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(string), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptString);
            _decryptString = mod.Import(encryptionServiceType.GetMethod("DecryptString", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptString);
            _encryptBytes = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptBytes);
            _decryptBytes = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptBytes);

            _decryptFromRvaInt = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaInt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaInt);
            _decryptFromRvaLong = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaLong", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaLong);
            _decryptFromRvaFloat = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaFloat", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaFloat);
            _decryptFromRvaDouble = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaDouble", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaDouble);
            _decryptFromRvaBytes = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaBytes", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaBytes);
            _decryptFromRvaString = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaString", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaString);
            _decryptInitializeArray = mod.Import(encryptionServiceType.GetMethod("DecryptInitializeArray", new[] { typeof(System.Array), typeof(System.RuntimeFieldHandle), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptInitializeArray);
        }
    }

    public class DefaultMetadataImporter : GroupByModuleEntityBase
    {
        private EncryptionServiceMetadataImporter _defaultEncryptionServiceMetadataImporter;


        private EncryptionServiceMetadataImporter _staticDefaultEncryptionServiceMetadataImporter;
        private EncryptionServiceMetadataImporter _dynamicDefaultEncryptionServiceMetadataImporter;

        public DefaultMetadataImporter()
        {
        }

        public override void Init()
        {
            ModuleDef mod = Module;

            var constUtilityType = typeof(ConstUtility);

            _castIntAsFloat = mod.Import(constUtilityType.GetMethod("CastIntAsFloat"));
            Assert.IsNotNull(_castIntAsFloat, "CastIntAsFloat not found");
            _castLongAsDouble = mod.Import(constUtilityType.GetMethod("CastLongAsDouble"));
            Assert.IsNotNull(_castLongAsDouble, "CastLongAsDouble not found");
            _castFloatAsInt = mod.Import(constUtilityType.GetMethod("CastFloatAsInt"));
            Assert.IsNotNull(_castFloatAsInt, "CastFloatAsInt not found");
            _castDoubleAsLong = mod.Import(constUtilityType.GetMethod("CastDoubleAsLong"));
            Assert.IsNotNull(_castDoubleAsLong, "CastDoubleAsLong not found");

            _initializeArray = mod.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) }));
            Assert.IsNotNull(_initializeArray);
            _verifySecretKey = mod.Import(typeof(AssertUtility).GetMethod("VerifySecretKey", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_verifySecretKey, "VerifySecretKey not found");

            _obfuscationTypeMapperRegisterType = mod.Import(typeof(ObfuscationTypeMapper).GetMethod("RegisterType", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null));
            Assert.IsNotNull(_obfuscationTypeMapperRegisterType, "ObfuscationTypeMapper.RegisterType not found");

            var exprUtilityType = typeof(ExprUtility);
            _addInt = mod.Import(exprUtilityType.GetMethod("Add", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_addInt, "ExprUtility.Add(int, int) not found");
            _addLong = mod.Import(exprUtilityType.GetMethod("Add", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_addLong, "ExprUtility.Add(long, long) not found");
            _addFloat = mod.Import(exprUtilityType.GetMethod("Add", new[] { typeof(float), typeof(float) }));
            Assert.IsNotNull(_addFloat, "ExprUtility.Add(float, float) not found");
            _addDouble = mod.Import(exprUtilityType.GetMethod("Add", new[] { typeof(double), typeof(double) }));
            Assert.IsNotNull(_addDouble, "ExprUtility.Add(double, double) not found");
            _addIntPtr = mod.Import(exprUtilityType.GetMethod("Add", new[] { typeof(IntPtr), typeof(IntPtr) }));
            Assert.IsNotNull(_addIntPtr, "ExprUtility.Add(IntPtr, IntPtr) not found");
            _addIntPtrInt = mod.Import(exprUtilityType.GetMethod("Add", new[] { typeof(IntPtr), typeof(int) }));
            Assert.IsNotNull(_addIntPtrInt, "ExprUtility.Add(IntPtr, int) not found");

            _subtractInt = mod.Import(exprUtilityType.GetMethod("Subtract", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_subtractInt, "ExprUtility.Subtract(int, int) not found");
            _subtractLong = mod.Import(exprUtilityType.GetMethod("Subtract", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_subtractLong, "ExprUtility.Subtract(long, long) not found");
            _subtractFloat = mod.Import(exprUtilityType.GetMethod("Subtract", new[] { typeof(float), typeof(float) }));
            Assert.IsNotNull(_subtractFloat, "ExprUtility.Subtract(float, float) not found");
            _subtractDouble = mod.Import(exprUtilityType.GetMethod("Subtract", new[] { typeof(double), typeof(double) }));
            Assert.IsNotNull(_subtractDouble, "ExprUtility.Subtract(double, double) not found");
            _subtractIntPtr = mod.Import(exprUtilityType.GetMethod("Subtract", new[] { typeof(IntPtr), typeof(IntPtr) }));
            Assert.IsNotNull(_subtractIntPtr, "ExprUtility.Subtract(IntPtr, IntPtr) not found");
            _subtractIntPtrInt = mod.Import(exprUtilityType.GetMethod("Subtract", new[] { typeof(IntPtr), typeof(int) }));
            Assert.IsNotNull(_subtractIntPtrInt, "ExprUtility.Subtract(IntPtr, int) not found");

            _multiplyInt = mod.Import(exprUtilityType.GetMethod("Multiply", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_multiplyInt, "ExprUtility.Multiply(int, int) not found");
            _multiplyLong = mod.Import(exprUtilityType.GetMethod("Multiply", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_multiplyLong, "ExprUtility.Multiply(long, long) not found");
            _multiplyFloat = mod.Import(exprUtilityType.GetMethod("Multiply", new[] { typeof(float), typeof(float) }));
            Assert.IsNotNull(_multiplyFloat, "ExprUtility.Multiply(float, float) not found");
            _multiplyDouble = mod.Import(exprUtilityType.GetMethod("Multiply", new[] { typeof(double), typeof(double) }));
            Assert.IsNotNull(_multiplyDouble, "ExprUtility.Multiply(double, double) not found");
            _multiplyIntPtr = mod.Import(exprUtilityType.GetMethod("Multiply", new[] { typeof(IntPtr), typeof(IntPtr) }));
            Assert.IsNotNull(_multiplyIntPtr, "ExprUtility.Multiply(IntPtr, IntPtr) not found");
            _multiplyIntPtrInt = mod.Import(exprUtilityType.GetMethod("Multiply", new[] { typeof(IntPtr), typeof(int) }));
            Assert.IsNotNull(_multiplyIntPtrInt, "ExprUtility.Multiply(IntPtr, int) not found");

            _divideInt = mod.Import(exprUtilityType.GetMethod("Divide", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_divideInt, "ExprUtility.Divide(int, int) not found");
            _divideLong = mod.Import(exprUtilityType.GetMethod("Divide", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_divideLong);
            _divideFloat = mod.Import(exprUtilityType.GetMethod("Divide", new[] { typeof(float), typeof(float) }));
            Assert.IsNotNull(_divideFloat, "ExprUtility.Divide(float, float) not found");
            _divideDouble = mod.Import(exprUtilityType.GetMethod("Divide", new[] { typeof(double), typeof(double) }));
            Assert.IsNotNull(_divideDouble, "ExprUtility.Divide(double, double) not found");
            _divideUnInt = mod.Import(exprUtilityType.GetMethod("DivideUn", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_divideUnInt, "ExprUtility.DivideUn(int, int) not found");
            _divideUnLong = mod.Import(exprUtilityType.GetMethod("DivideUn", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_divideUnLong, "ExprUtility.DivideUn(long, long) not found");
            _remInt = mod.Import(exprUtilityType.GetMethod("Rem", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_remInt, "ExprUtility.Rem(int, int) not found");
            _remLong = mod.Import(exprUtilityType.GetMethod("Rem", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_remLong, "ExprUtility.Rem(long, long) not found");
            _remFloat = mod.Import(exprUtilityType.GetMethod("Rem", new[] { typeof(float), typeof(float) }));
            Assert.IsNotNull(_remFloat, "ExprUtility.Rem(float, float) not found");
            _remDouble = mod.Import(exprUtilityType.GetMethod("Rem", new[] { typeof(double), typeof(double) }));
            Assert.IsNotNull(_remDouble, "ExprUtility.Rem(double, double) not found");
            _remUnInt = mod.Import(exprUtilityType.GetMethod("RemUn", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_remUnInt, "ExprUtility.RemUn(int, int) not found");
            _remUnLong = mod.Import(exprUtilityType.GetMethod("RemUn", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_remUnLong, "ExprUtility.RemUn(long, long) not found");
            _negInt = mod.Import(exprUtilityType.GetMethod("Negate", new[] { typeof(int) }));
            Assert.IsNotNull(_negInt, "ExprUtility.Negate(int) not found");
            _negLong = mod.Import(exprUtilityType.GetMethod("Negate", new[] { typeof(long) }));
            Assert.IsNotNull(_negLong, "ExprUtility.Negate(long) not found");
            _negFloat = mod.Import(exprUtilityType.GetMethod("Negate", new[] { typeof(float) }));
            Assert.IsNotNull(_negFloat, "ExprUtility.Negate(float) not found");
            _negDouble = mod.Import(exprUtilityType.GetMethod("Negate", new[] { typeof(double) }));
            Assert.IsNotNull(_negDouble, "ExprUtility.Negate(double) not found");

            _andInt = mod.Import(exprUtilityType.GetMethod("And", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_andInt, "ExprUtility.And(int, int) not found");
            _andLong = mod.Import(exprUtilityType.GetMethod("And", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_andLong, "ExprUtility.And(long, long) not found");
            _orInt = mod.Import(exprUtilityType.GetMethod("Or", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_orInt, "ExprUtility.Or(int, int) not found");
            _orLong = mod.Import(exprUtilityType.GetMethod("Or", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_orLong, "ExprUtility.Or(long, long) not found");
            _xorInt = mod.Import(exprUtilityType.GetMethod("Xor", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_xorInt, "ExprUtility.Xor(int, int) not found");
            _xorLong = mod.Import(exprUtilityType.GetMethod("Xor", new[] { typeof(long), typeof(long) }));
            Assert.IsNotNull(_xorLong, "ExprUtility.Xor(long, long) not found");
            _notInt = mod.Import(exprUtilityType.GetMethod("Not", new[] { typeof(int) }));
            Assert.IsNotNull(_notInt, "ExprUtility.Not(int) not found");
            _notLong = mod.Import(exprUtilityType.GetMethod("Not", new[] { typeof(long) }));
            Assert.IsNotNull(_notLong, "ExprUtility.Not(long) not found");

            _shlInt = mod.Import(exprUtilityType.GetMethod("ShiftLeft", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_shlInt, "ExprUtility.ShiftLeft(int, int) not found");
            _shlLong = mod.Import(exprUtilityType.GetMethod("ShiftLeft", new[] { typeof(long), typeof(int) }));
            Assert.IsNotNull(_shlLong, "ExprUtility.ShiftLeft(long, int) not found");
            _shrInt = mod.Import(exprUtilityType.GetMethod("ShiftRight", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_shrInt, "ExprUtility.ShiftRight(int, int) not found");
            _shrLong = mod.Import(exprUtilityType.GetMethod("ShiftRight", new[] { typeof(long), typeof(int) }));
            Assert.IsNotNull(_shrLong, "ExprUtility.ShiftRight(long, int) not found");
            _shrUnInt = mod.Import(exprUtilityType.GetMethod("ShiftRightUn", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_shrUnInt, "ExprUtility.ShiftRightUn(int, int) not found");
            _shrUnLong = mod.Import(exprUtilityType.GetMethod("ShiftRightUn", new[] { typeof(long), typeof(int) }));
            Assert.IsNotNull(_shrUnLong, "ExprUtility.ShiftRightUn(long, int) not found");


            _staticDefaultEncryptionServiceMetadataImporter = new EncryptionServiceMetadataImporter(mod, typeof(EncryptionService<DefaultStaticEncryptionScope>));
            _dynamicDefaultEncryptionServiceMetadataImporter = new EncryptionServiceMetadataImporter(mod, typeof(EncryptionService<DefaultDynamicEncryptionScope>));
            if (EncryptionScopeProvider.IsDynamicSecretAssembly(mod))
            {
                _defaultEncryptionServiceMetadataImporter = _dynamicDefaultEncryptionServiceMetadataImporter;
            }
            else
            {
                _defaultEncryptionServiceMetadataImporter = _staticDefaultEncryptionServiceMetadataImporter;
            }
        }

        public override void Done()
        {

        }

        public EncryptionServiceMetadataImporter GetEncryptionServiceMetadataImporterOfModule(ModuleDef mod)
        {
            return EncryptionScopeProvider.IsDynamicSecretAssembly(mod) ? _dynamicDefaultEncryptionServiceMetadataImporter : _staticDefaultEncryptionServiceMetadataImporter;
        }

        private ModuleDef _module;
        private IMethod _castIntAsFloat;
        private IMethod _castLongAsDouble;
        private IMethod _castFloatAsInt;
        private IMethod _castDoubleAsLong;
        private IMethod _initializeArray;
        private IMethod _verifySecretKey;

        private IMethod _obfuscationTypeMapperRegisterType;

        private IMethod _addInt;
        private IMethod _addLong;
        private IMethod _addFloat;
        private IMethod _addDouble;
        private IMethod _addIntPtr;
        private IMethod _addIntPtrInt;
        private IMethod _subtractInt;
        private IMethod _subtractLong;
        private IMethod _subtractFloat;
        private IMethod _subtractDouble;
        private IMethod _subtractIntPtr;
        private IMethod _subtractIntPtrInt;
        private IMethod _multiplyInt;
        private IMethod _multiplyLong;
        private IMethod _multiplyFloat;
        private IMethod _multiplyDouble;
        private IMethod _multiplyIntPtr;
        private IMethod _multiplyIntPtrInt;
        private IMethod _divideInt;
        private IMethod _divideLong;
        private IMethod _divideFloat;
        private IMethod _divideDouble;
        private IMethod _divideUnInt;
        private IMethod _divideUnLong;
        private IMethod _remInt;
        private IMethod _remLong;
        private IMethod _remFloat;
        private IMethod _remDouble;
        private IMethod _remUnInt;
        private IMethod _remUnLong;
        private IMethod _negInt;
        private IMethod _negLong;
        private IMethod _negFloat;
        private IMethod _negDouble;

        private IMethod _andInt;
        private IMethod _andLong;
        private IMethod _orInt;
        private IMethod _orLong;
        private IMethod _xorInt;
        private IMethod _xorLong;
        private IMethod _notInt;
        private IMethod _notLong;

        private IMethod _shlInt;
        private IMethod _shlLong;
        private IMethod _shrInt;
        private IMethod _shrLong;
        private IMethod _shrUnInt;
        private IMethod _shrUnLong;

        public IMethod CastIntAsFloat => _castIntAsFloat;
        public IMethod CastLongAsDouble => _castLongAsDouble;
        public IMethod CastFloatAsInt => _castFloatAsInt;
        public IMethod CastDoubleAsLong => _castDoubleAsLong;

        public IMethod InitializedArray => _initializeArray;

        public IMethod VerifySecretKey => _verifySecretKey;

        public IMethod ObfuscationTypeMapperRegisterType => _obfuscationTypeMapperRegisterType;

        public IMethod EncryptBlock => _defaultEncryptionServiceMetadataImporter.EncryptBlock;
        public IMethod DecryptBlock => _defaultEncryptionServiceMetadataImporter.DecryptBlock;

        public IMethod EncryptInt => _defaultEncryptionServiceMetadataImporter.EncryptInt;
        public IMethod DecryptInt => _defaultEncryptionServiceMetadataImporter.DecryptInt;
        public IMethod EncryptLong => _defaultEncryptionServiceMetadataImporter.EncryptLong;
        public IMethod DecryptLong => _defaultEncryptionServiceMetadataImporter.DecryptLong;
        public IMethod EncryptFloat => _defaultEncryptionServiceMetadataImporter.EncryptFloat;
        public IMethod DecryptFloat => _defaultEncryptionServiceMetadataImporter.DecryptFloat;
        public IMethod EncryptDouble => _defaultEncryptionServiceMetadataImporter.EncryptDouble;
        public IMethod DecryptDouble => _defaultEncryptionServiceMetadataImporter.DecryptDouble;
        public IMethod EncryptString => _defaultEncryptionServiceMetadataImporter.EncryptString;
        public IMethod DecryptString => _defaultEncryptionServiceMetadataImporter.DecryptString;
        public IMethod EncryptBytes => _defaultEncryptionServiceMetadataImporter.EncryptBytes;
        public IMethod DecryptBytes => _defaultEncryptionServiceMetadataImporter.DecryptBytes;

        public IMethod DecryptFromRvaInt => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaInt;
        public IMethod DecryptFromRvaLong => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaLong;
        public IMethod DecryptFromRvaFloat => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaFloat;
        public IMethod DecryptFromRvaDouble => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaDouble;
        public IMethod DecryptFromRvaBytes => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaBytes;
        public IMethod DecryptFromRvaString => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaString;

        public IMethod DecryptInitializeArray => _defaultEncryptionServiceMetadataImporter.DecryptInitializeArray;

        public IMethod AddInt => _addInt;
        public IMethod AddLong => _addLong;
        public IMethod AddFloat => _addFloat;
        public IMethod AddDouble => _addDouble;
        public IMethod AddIntPtr => _addIntPtr;
        public IMethod AddIntPtrInt => _addIntPtrInt;
        public IMethod SubtractInt => _subtractInt;
        public IMethod SubtractLong => _subtractLong;
        public IMethod SubtractFloat => _subtractFloat;
        public IMethod SubtractDouble => _subtractDouble;
        public IMethod SubtractIntPtr => _subtractIntPtr;
        public IMethod SubtractIntPtrInt => _subtractIntPtrInt;

        public IMethod MultiplyInt => _multiplyInt;
        public IMethod MultiplyLong => _multiplyLong;
        public IMethod MultiplyFloat => _multiplyFloat;
        public IMethod MultiplyDouble => _multiplyDouble;
        public IMethod MultiplyIntPtr => _multiplyIntPtr;
        public IMethod MultiplyIntPtrInt => _multiplyIntPtrInt;

        public IMethod DivideInt => _divideInt;
        public IMethod DivideLong => _divideLong;
        public IMethod DivideFloat => _divideFloat;
        public IMethod DivideDouble => _divideDouble;
        public IMethod DivideUnInt => _divideUnInt;
        public IMethod DivideUnLong => _divideUnLong;
        public IMethod RemInt => _remInt;
        public IMethod RemLong => _remLong;
        public IMethod RemFloat => _remFloat;
        public IMethod RemDouble => _remDouble;
        public IMethod RemUnInt => _remUnInt;
        public IMethod RemUnLong => _remUnLong;
        public IMethod NegInt => _negInt;
        public IMethod NegLong => _negLong;
        public IMethod NegFloat => _negFloat;
        public IMethod NegDouble => _negDouble;
        public IMethod AndInt => _andInt;
        public IMethod AndLong => _andLong;
        public IMethod OrInt => _orInt;
        public IMethod OrLong => _orLong;
        public IMethod XorInt => _xorInt;
        public IMethod XorLong => _xorLong;
        public IMethod NotInt => _notInt;
        public IMethod NotLong => _notLong;
        public IMethod ShlInt => _shlInt;
        public IMethod ShlLong => _shlLong;
        public IMethod ShrInt => _shrInt;
        public IMethod ShrLong => _shrLong;
        public IMethod ShrUnInt => _shrUnInt;
        public IMethod ShrUnLong => _shrUnLong;


    }
}
