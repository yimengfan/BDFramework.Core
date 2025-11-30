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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.CallObfus
{

    struct DelegateProxyMethodData
    {
        public readonly FieldDef delegateInstanceField;
        public readonly MethodDef delegateInvokeMethod;

        public DelegateProxyMethodData(FieldDef delegateInstanceField, MethodDef delegateInvokeMethod)
        {
            this.delegateInstanceField = delegateInstanceField;
            this.delegateInvokeMethod = delegateInvokeMethod;
        }
    }

    class DelegateProxyAllocator : GroupByModuleEntityBase
    {
        private readonly CachedDictionary<MethodSig, TypeDef> _delegateTypes;
        private readonly HashSet<string> _allocatedDelegateNames = new HashSet<string>();

        private TypeDef _delegateInstanceHolderType;
        private bool _done;

        class CallInfo
        {
            public string key1;
            public int key2;
            public IMethod method;
            public bool callVir;

            public int index;
            public TypeDef delegateType;
            public FieldDef delegateInstanceField;
            public MethodDef delegateInvokeMethod;
            public MethodDef proxyMethod;
        }
        private readonly Dictionary<MethodKey, CallInfo> _callMethods = new Dictionary<MethodKey, CallInfo>();
        private CallObfuscationSettingsFacade _settings;

        public DelegateProxyAllocator()
        {
            _delegateTypes = new CachedDictionary<MethodSig, TypeDef>(SignatureEqualityComparer.Instance, CreateDelegateForSignature);
        }

        public override void Init()
        {
            _delegateInstanceHolderType = CreateDelegateInstanceHolderTypeDef();
            _settings = CallObfusPass.CurrentSettings;
        }

        private string AllocateDelegateTypeName(MethodSig delegateInvokeSig)
        {
            uint hashCode = (uint)SignatureEqualityComparer.Instance.GetHashCode(delegateInvokeSig);
            string typeName = $"$Obfuz$Delegate_{hashCode}";
            if (_allocatedDelegateNames.Add(typeName))
            {
                return typeName;
            }
            for (int i = 0; ; i++)
            {
                typeName = $"$Obfuz$Delegate_{hashCode}_{i}";
                if (_allocatedDelegateNames.Add(typeName))
                {
                    return typeName;
                }
            }
        }

        private TypeDef CreateDelegateForSignature(MethodSig delegateInvokeSig)
        {
            ModuleDef mod = Module;
            using (var scope = new DisableTypeDefFindCacheScope(mod))
            {

                string typeName = AllocateDelegateTypeName(delegateInvokeSig);
                mod.Import(typeof(MulticastDelegate));

                TypeDef delegateType = new TypeDefUser("", typeName, mod.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
                delegateType.Attributes = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public;
                mod.Types.Add(delegateType);

                MethodDef ctor = new MethodDefUser(
                    ".ctor",
                    MethodSig.CreateInstance(mod.CorLibTypes.Void, mod.CorLibTypes.Object, mod.CorLibTypes.IntPtr),
                    MethodImplAttributes.Runtime,
                    MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public
                );
                ctor.DeclaringType = delegateType;


                MethodDef invokeMethod = new MethodDefUser(
                    "Invoke",
                    MethodSig.CreateInstance(delegateInvokeSig.RetType, delegateInvokeSig.Params.ToArray()),
                    MethodImplAttributes.Runtime,
                    MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual
                );
                invokeMethod.DeclaringType = delegateType;
                return delegateType;
            }
        }

        private TypeDef CreateDelegateInstanceHolderTypeDef()
        {
            ModuleDef mod = Module;
            using (var scope = new DisableTypeDefFindCacheScope(mod))
            {
                string typeName = "$Obfuz$DelegateInstanceHolder";
                TypeDef holderType = new TypeDefUser("", typeName, mod.CorLibTypes.Object.ToTypeDefOrRef());
                holderType.Attributes = TypeAttributes.Class | TypeAttributes.Public;
                mod.Types.Add(holderType);
                return holderType;
            }
        }

        private string AllocateFieldName(IMethod method, bool callVir)
        {
            uint hashCode = (uint)MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method);
            string typeName = $"$Obfuz$Delegate$Field_{hashCode}_{callVir}";
            if (_allocatedDelegateNames.Add(typeName))
            {
                return typeName;
            }
            for (int i = 0; ; i++)
            {
                typeName = $"$Obfuz$Delegate$Field_{hashCode}_{callVir}_{i}";
                if (_allocatedDelegateNames.Add(typeName))
                {
                    return typeName;
                }
            }
        }

        private MethodDef CreateProxyMethod(string name, IMethod calledMethod, bool callVir, MethodSig delegateInvokeSig)
        {
            var proxyMethod = new MethodDefUser(name, delegateInvokeSig, MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static);
            var body = new CilBody();
            proxyMethod.Body = body;
            var ins = body.Instructions;

            foreach (Parameter param in proxyMethod.Parameters)
            {
                ins.Add(Instruction.Create(OpCodes.Ldarg, param));
            }

            ins.Add(Instruction.Create(callVir ? OpCodes.Callvirt : OpCodes.Call, calledMethod));
            ins.Add(Instruction.Create(OpCodes.Ret));
            return proxyMethod;
        }

        public DelegateProxyMethodData Allocate(IMethod method, bool callVir, MethodSig delegateInvokeSig)
        {
            var key = new MethodKey(method, callVir);
            if (!_callMethods.TryGetValue(key, out var callInfo))
            {
                TypeDef delegateType = _delegateTypes.GetValue(delegateInvokeSig);
                MethodDef delegateInvokeMethod = delegateType.FindMethod("Invoke");
                string fieldName = AllocateFieldName(method, callVir);
                FieldDef delegateInstanceField = new FieldDefUser(fieldName, new FieldSig(delegateType.ToTypeSig()), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
                string key1 = $"{method.FullName}_{callVir}";
                callInfo = new CallInfo
                {
                    key1 = key1,
                    key2 = HashUtil.ComputePrimitiveOrStringOrBytesHashCode(key1) * 33445566,
                    method = method,
                    callVir = callVir,
                    delegateType = delegateType,
                    delegateInstanceField = delegateInstanceField,
                    delegateInvokeMethod = delegateInvokeMethod,
                    proxyMethod = CreateProxyMethod($"{fieldName}$Proxy", method, callVir, delegateInvokeSig),
                };
                _callMethods.Add(key, callInfo);
            }
            return new DelegateProxyMethodData(callInfo.delegateInstanceField, callInfo.delegateInvokeMethod);
        }

        public override void Done()
        {
            if (_done)
            {
                throw new Exception("Already done");
            }
            _done = true;

            ModuleDef mod = Module;

            // for stable order, we sort methods by name
            List<CallInfo> callMethodList = _callMethods.Values.ToList();
            callMethodList.Sort((a, b) => a.key1.CompareTo(b.key1));

            var cctor = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(mod.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctor.DeclaringType = _delegateInstanceHolderType;
            //_rvaTypeDef.Methods.Add(cctor);
            var body = new CilBody();
            cctor.Body = body;
            var ins = body.Instructions;

            // var arr = new array[];
            // var d = new delegate;
            // arr[index] = d;
            int index = 0;
            ins.Add(Instruction.CreateLdcI4(callMethodList.Count));
            ins.Add(Instruction.Create(OpCodes.Newarr, mod.CorLibTypes.Object));
            foreach (CallInfo ci in callMethodList)
            {
                ci.index = index;
                _delegateInstanceHolderType.Methods.Add(ci.proxyMethod);
                ins.Add(Instruction.Create(OpCodes.Dup));
                ins.Add(Instruction.CreateLdcI4(index));
                ins.Add(Instruction.Create(OpCodes.Ldnull));
                ins.Add(Instruction.Create(OpCodes.Ldftn, ci.proxyMethod));
                MethodDef ctor = ci.delegateType.FindMethod(".ctor");
                UnityEngine.Assertions.Assert.IsNotNull(ctor, $"Delegate type {ci.delegateType.FullName} does not have a constructor.");
                ins.Add(Instruction.Create(OpCodes.Newobj, ctor));
                ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
                ++index;
            }



            List<CallInfo> callMethodList2 = callMethodList.ToList();
            callMethodList2.Sort((a, b) => a.key2.CompareTo(b.key2));

            EncryptionScopeInfo encryptionScope = EncryptionScope;
            DefaultMetadataImporter importer = this.GetDefaultModuleMetadataImporter();
            RvaDataAllocator rvaDataAllocator = this.GetEntity<RvaDataAllocator>();
            foreach (CallInfo ci in callMethodList2)
            {
                _delegateInstanceHolderType.Fields.Add(ci.delegateInstanceField);


                ins.Add(Instruction.Create(OpCodes.Dup));

                IRandom localRandom = encryptionScope.localRandomCreator(HashUtil.ComputePrimitiveOrStringOrBytesHashCode(ci.key1));
                int ops = EncryptionUtil.GenerateEncryptionOpCodes(localRandom, encryptionScope.encryptor, _settings.obfuscationLevel);
                int salt = localRandom.NextInt();

                int encryptedValue = encryptionScope.encryptor.Encrypt(ci.index, ops, salt);
                RvaData rvaData = rvaDataAllocator.Allocate(encryptedValue);
                ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                ins.Add(Instruction.CreateLdcI4(ops));
                ins.Add(Instruction.CreateLdcI4(salt));
                ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
                ins.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                ins.Add(Instruction.Create(OpCodes.Stsfld, ci.delegateInstanceField));
            }

            ins.Add(Instruction.Create(OpCodes.Pop));
            ins.Add(Instruction.Create(OpCodes.Ret));
        }
    }
}
