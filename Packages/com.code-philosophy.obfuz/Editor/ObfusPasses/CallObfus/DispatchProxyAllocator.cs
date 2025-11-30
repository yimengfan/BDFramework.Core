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
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace Obfuz.ObfusPasses.CallObfus
{

    public struct ProxyCallMethodData
    {
        public readonly MethodDef proxyMethod;
        public readonly int encryptOps;
        public readonly int salt;
        public readonly int encryptedIndex;
        public readonly int index;

        public ProxyCallMethodData(MethodDef proxyMethod, int encryptOps, int salt, int encryptedIndex, int index)
        {
            this.proxyMethod = proxyMethod;
            this.encryptOps = encryptOps;
            this.salt = salt;
            this.encryptedIndex = encryptedIndex;
            this.index = index;
        }
    }

    class ModuleDispatchProxyAllocator : GroupByModuleEntityBase
    {
        private bool _done;
        private CallObfuscationSettingsFacade _settings;


        class MethodProxyInfo
        {
            public MethodDef proxyMethod;

            public int index;
            public int encryptedOps;
            public int salt;
            public int encryptedIndex;
        }

        private readonly Dictionary<MethodKey, MethodProxyInfo> _methodProxys = new Dictionary<MethodKey, MethodProxyInfo>();

        class CallInfo
        {
            public string id;
            public IMethod method;
            public bool callVir;
        }

        class DispatchMethodInfo
        {
            public MethodDef methodDef;
            public List<CallInfo> methods = new List<CallInfo>();
        }

        private readonly Dictionary<MethodSig, List<DispatchMethodInfo>> _dispatchMethods = new Dictionary<MethodSig, List<DispatchMethodInfo>>(SignatureEqualityComparer.Instance);


        private TypeDef _proxyTypeDef;

        public ModuleDispatchProxyAllocator()
        {
        }

        public override void Init()
        {
            _settings = CallObfusPass.CurrentSettings;
        }

        private TypeDef CreateProxyTypeDef()
        {
            ModuleDef mod = Module;
            using (var scope = new DisableTypeDefFindCacheScope(mod))
            {
                var typeDef = new TypeDefUser($"{ConstValues.ObfuzInternalSymbolNamePrefix}ProxyCall", mod.CorLibTypes.Object.ToTypeDefOrRef());
                typeDef.Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed;
                mod.Types.Add(typeDef);
                return typeDef;
            }
        }

        private readonly HashSet<string> _uniqueMethodNames = new HashSet<string>();


        private string ToUniqueMethodName(string originalName)
        {
            if (_uniqueMethodNames.Add(originalName))
            {
                return originalName;
            }
            for (int index = 1; ; index++)
            {
                string uniqueName = $"{originalName}${index}";
                if (_uniqueMethodNames.Add(uniqueName))
                {
                    return uniqueName;
                }
            }
        }

        private string CreateDispatchMethodName(MethodSig methodSig, int index)
        {
            // use a stable name for the dispatch method, so that we can reuse it across different modules
            // this is important for cross-module calls
            return ToUniqueMethodName($"{ConstValues.ObfuzInternalSymbolNamePrefix}Dispatch_{HashUtil.ComputeHash(methodSig.Params) & 0xFFFF}_{HashUtil.ComputeHash(methodSig.RetType) & 0xFFFFFF}");
        }

        private MethodDef CreateDispatchMethodInfo(MethodSig methodSig, int index)
        {
            if (_proxyTypeDef == null)
            {
                _proxyTypeDef = CreateProxyTypeDef();
            }
            MethodDef methodDef = new MethodDefUser(CreateDispatchMethodName(methodSig, index), methodSig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.Public);
            methodDef.DeclaringType = _proxyTypeDef;
            return methodDef;
        }

        private MethodSig CreateDispatchMethodSig(IMethod method)
        {
            ModuleDef mod = Module;
            MethodSig methodSig = MetaUtil.ToSharedMethodSig(mod.CorLibTypes, MetaUtil.GetInflatedMethodSig(method, null));
            //MethodSig methodSig = MetaUtil.GetInflatedMethodSig(method).Clone();
            //methodSig.Params
            switch (MetaUtil.GetThisArgType(method))
            {
                case ThisArgType.Class:
                {
                    methodSig.Params.Insert(0, mod.CorLibTypes.Object);
                    break;
                }
                case ThisArgType.ValueType:
                {
                    methodSig.Params.Insert(0, mod.CorLibTypes.IntPtr);
                    break;
                }
            }
            // extra param for index
            methodSig.Params.Add(mod.CorLibTypes.Int32);
            return MethodSig.CreateStatic(methodSig.RetType, methodSig.Params.ToArray());
        }

        private int GenerateSalt(IRandom random)
        {
            return random.NextInt();
        }

        private int GenerateEncryptOps(IRandom random)
        {
            return EncryptionUtil.GenerateEncryptionOpCodes(random, EncryptionScope.encryptor, _settings.obfuscationLevel);
        }

        private DispatchMethodInfo GetDispatchMethod(IMethod method)
        {
            MethodSig methodSig = CreateDispatchMethodSig(method);
            if (!_dispatchMethods.TryGetValue(methodSig, out var dispatchMethods))
            {
                dispatchMethods = new List<DispatchMethodInfo>();
                _dispatchMethods.Add(methodSig, dispatchMethods);
            }
            if (dispatchMethods.Count == 0 || dispatchMethods.Last().methods.Count >= _settings.maxProxyMethodCountPerDispatchMethod)
            {
                var newDispatchMethodInfo = new DispatchMethodInfo
                {
                    methodDef = CreateDispatchMethodInfo(methodSig, dispatchMethods.Count),
                };
                dispatchMethods.Add(newDispatchMethodInfo);
            }
            return dispatchMethods.Last();
        }

        private IRandom CreateRandomForMethod(IMethod method, bool callVir)
        {
            int seed = MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method);
            return EncryptionScope.localRandomCreator(seed);
        }

        public ProxyCallMethodData Allocate(IMethod method, bool callVir)
        {
            if (_done)
            {
                throw new Exception("can't Allocate after done");
            }
            var key = new MethodKey(method, callVir);
            if (!_methodProxys.TryGetValue(key, out var proxyInfo))
            {
                var methodDispatcher = GetDispatchMethod(method);

                int index = methodDispatcher.methods.Count;
                IRandom localRandom = CreateRandomForMethod(method, callVir);
                int encryptOps = GenerateEncryptOps(localRandom);
                int salt = GenerateSalt(localRandom);
                int encryptedIndex = EncryptionScope.encryptor.Encrypt(index, encryptOps, salt);
                proxyInfo = new MethodProxyInfo()
                {
                    proxyMethod = methodDispatcher.methodDef,
                    index = index,
                    encryptedOps = encryptOps,
                    salt = salt,
                    encryptedIndex = encryptedIndex,
                };
                methodDispatcher.methods.Add(new CallInfo { id = $"{method}{(callVir ? "" : "v")}", method = method, callVir = callVir });
                _methodProxys.Add(key, proxyInfo);
            }
            return new ProxyCallMethodData(proxyInfo.proxyMethod, proxyInfo.encryptedOps, proxyInfo.salt, proxyInfo.encryptedIndex, proxyInfo.index);
        }

        public override void Done()
        {
            if (_done)
            {
                throw new Exception("Already done");
            }
            _done = true;
            if (_proxyTypeDef == null)
            {
                return;
            }

            // for stable order, we sort methods by name
            var methodWithNamePairList = _proxyTypeDef.Methods.Select(m => (m, m.ToString())).ToList();
            methodWithNamePairList.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            _proxyTypeDef.Methods.Clear();
            foreach (var methodPair in methodWithNamePairList)
            {
                methodPair.Item1.DeclaringType = _proxyTypeDef;
            }

            foreach (DispatchMethodInfo dispatchMethod in _dispatchMethods.Values.SelectMany(ms => ms))
            {
                var methodDef = dispatchMethod.methodDef;
                var methodSig = methodDef.MethodSig;


                var body = new CilBody();
                methodDef.Body = body;
                var ins = body.Instructions;

                foreach (Parameter param in methodDef.Parameters)
                {
                    ins.Add(Instruction.Create(OpCodes.Ldarg, param));
                }

                var switchCases = new List<Instruction>();
                var switchInst = Instruction.Create(OpCodes.Switch, switchCases);
                ins.Add(switchInst);
                var ret = Instruction.Create(OpCodes.Ret);

                // sort methods by signature to ensure stable order
                //dispatchMethod.methods.Sort((a, b) => a.id.CompareTo(b.id));
                foreach (CallInfo ci in dispatchMethod.methods)
                {
                    var callTargetMethod = Instruction.Create(ci.callVir ? OpCodes.Callvirt : OpCodes.Call, ci.method);
                    switchCases.Add(callTargetMethod);
                    ins.Add(callTargetMethod);
                    ins.Add(Instruction.Create(OpCodes.Br, ret));
                }
                ins.Add(ret);
            }
        }
    }
}
