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
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    class SpecialWhiteListMethodCalculator
    {
        private readonly RuntimeType _targetRuntime;
        private readonly bool _obfuscateCallToMethodInMscorlib;
        private readonly CachedDictionary<IMethod, bool> _specialWhiteListMethodCache;

        public SpecialWhiteListMethodCalculator(RuntimeType targetRuntime, bool obfuscateCallToMethodInMscorlib)
        {
            _targetRuntime = targetRuntime;
            _obfuscateCallToMethodInMscorlib = obfuscateCallToMethodInMscorlib;
            _specialWhiteListMethodCache = new CachedDictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes, this.ComputeIsInWhiteList);
        }

        public bool IsInWhiteList(IMethod calledMethod)
        {
            return _specialWhiteListMethodCache.GetValue(calledMethod);
        }

        private static readonly HashSet<string> _specialTypeFullNames = new HashSet<string>
        {
            "System.Enum",
            "System.Delegate",
            "System.MulticastDelegate",
            "Obfuz.EncryptionService`1",
        };

        private static readonly HashSet<string> _specialMethodNames = new HashSet<string>
        {
            "GetEnumerator", // List<T>.Enumerator.GetEnumerator()
            ".ctor", // constructor
        };

        private static readonly HashSet<string> _specialMethodFullNames = new HashSet<string>
        {
            "System.Reflection.MethodBase.GetCurrentMethod",
            "System.Reflection.Assembly.GetCallingAssembly",
            "System.Reflection.Assembly.GetExecutingAssembly",
            "System.Reflection.Assembly.GetEntryAssembly",
        };

        private bool ComputeIsInWhiteList(IMethod calledMethod)
        {
            MethodDef calledMethodDef = calledMethod.ResolveMethodDef();
            // mono has more strict access control, calls non-public method will raise exception.
            if (_targetRuntime == RuntimeType.Mono)
            {
                if (calledMethodDef != null && (!calledMethodDef.IsPublic || !IsTypeSelfAndParentPublic(calledMethodDef.DeclaringType)))
                {
                    return true;
                }
            }

            ITypeDefOrRef declaringType = calledMethod.DeclaringType;
            TypeSig declaringTypeSig = calledMethod.DeclaringType.ToTypeSig();
            declaringTypeSig = declaringTypeSig.RemovePinnedAndModifiers();
            switch (declaringTypeSig.ElementType)
            {
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    break;
                }
                case ElementType.GenericInst:
                {
                    if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
                    {
                        return true;
                    }
                    break;
                }
                default: return true;
            }

            TypeDef typeDef = declaringType.ResolveTypeDef();

            if (!_obfuscateCallToMethodInMscorlib && typeDef.Module.IsCoreLibraryModule == true)
            {
                return true;
            }

            if (typeDef.IsDelegate || typeDef.IsEnum)
                return true;

            string fullName = typeDef.FullName;
            if (_specialTypeFullNames.Contains(fullName))
            {
                return true;
            }
            //if (fullName.StartsWith("System.Runtime.CompilerServices."))
            //{
            //    return true;
            //}

            string methodName = calledMethod.Name;
            if (_specialMethodNames.Contains(methodName))
            {
                return true;
            }

            string methodFullName = $"{fullName}.{methodName}";
            if (_specialMethodFullNames.Contains(methodFullName))
            {
                return true;
            }
            return false;
        }

        private bool IsTypeSelfAndParentPublic(TypeDef type)
        {
            if (type.DeclaringType != null && !IsTypeSelfAndParentPublic(type.DeclaringType))
            {
                return false;
            }

            return type.IsPublic;
        }
    }
}
