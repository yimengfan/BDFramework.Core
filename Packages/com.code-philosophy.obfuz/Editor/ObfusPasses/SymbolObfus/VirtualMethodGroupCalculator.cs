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
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.SymbolObfus
{

    public class VirtualMethodGroup
    {
        public List<MethodDef> methods;

        private HashSet<TypeDef> _nameScopes;

        private HashSet<TypeDef> _rootNameScope;

        public HashSet<TypeDef> GetNameConflictTypeScopes()
        {
            if (_nameScopes != null)
            {
                return _nameScopes;
            }

            _nameScopes = new HashSet<TypeDef>();
            foreach (var method in methods)
            {
                TypeDef cur = method.DeclaringType;
                while (cur != null)
                {
                    _nameScopes.Add(cur);
                    cur = MetaUtil.GetBaseTypeDef(cur);
                }
            }
            return _nameScopes;
        }

        public HashSet<TypeDef> GetRootBeInheritedTypes()
        {
            if (_rootNameScope != null)
            {
                return _rootNameScope;
            }
            _rootNameScope = new HashSet<TypeDef>();
            var nameScopes = GetNameConflictTypeScopes();
            foreach (var type in nameScopes)
            {
                TypeDef parentType = MetaUtil.GetBaseTypeDef(type);
                if (parentType == null || !nameScopes.Contains(parentType))
                {
                    _rootNameScope.Add(type);
                }
            }
            return _rootNameScope;
        }

        public IEnumerable<TypeDef> GetNameDeclaringTypeScopes()
        {
            foreach (var method in methods)
            {
                yield return method.DeclaringType;
            }
        }
    }

    public class VirtualMethodGroupCalculator
    {

        private class TypeFlatMethods
        {
            public HashSet<MethodDef> flatMethods = new HashSet<MethodDef>();


            private bool IsFinalTypeSig(TypeSig type)
            {
                switch (type.ElementType)
                {
                    case ElementType.Void:
                    case ElementType.Boolean:
                    case ElementType.Char:
                    case ElementType.I1:
                    case ElementType.I2:
                    case ElementType.I4:
                    case ElementType.I8:
                    case ElementType.U1:
                    case ElementType.U2:
                    case ElementType.U4:
                    case ElementType.U8:
                    case ElementType.R4:
                    case ElementType.R8:
                    case ElementType.String:
                    case ElementType.Object:
                    case ElementType.Class:
                    case ElementType.ValueType:
                    return true;
                    default: return false;
                }
            }

            private bool IsVarType(TypeSig t)
            {
                return t.ElementType == ElementType.MVar || t.ElementType == ElementType.Var;
            }

            private bool IsClassOrValueType(TypeSig t)
            {
                return t.ElementType == ElementType.Class || t.ElementType == ElementType.ValueType;
            }

            private bool IsLooseTypeSigMatch(TypeSig t1, TypeSig t2)
            {
                t1 = t1.RemovePinnedAndModifiers();
                t2 = t2.RemovePinnedAndModifiers();

                if (t1.ElementType != t2.ElementType)
                {
                    return IsVarType(t1) || IsVarType(t2);
                }

                switch (t1.ElementType)
                {
                    case ElementType.Void:
                    case ElementType.Boolean:
                    case ElementType.Char:
                    case ElementType.I1:
                    case ElementType.I2:
                    case ElementType.I4:
                    case ElementType.I8:
                    case ElementType.U1:
                    case ElementType.U2:
                    case ElementType.U4:
                    case ElementType.U8:
                    case ElementType.R4:
                    case ElementType.R8:
                    case ElementType.I:
                    case ElementType.U:
                    case ElementType.R:
                    case ElementType.String:
                    case ElementType.Object:
                    case ElementType.TypedByRef:
                    return true;
                    case ElementType.Class:
                    case ElementType.ValueType:
                    {
                        return t1.AssemblyQualifiedName == t2.AssemblyQualifiedName;
                    }
                    case ElementType.Ptr:
                    case ElementType.ByRef:
                    case ElementType.SZArray:
                    {
                        break;
                    }
                    case ElementType.Array:
                    {
                        var a1 = (ArraySig)t1;
                        var a2 = (ArraySig)t2;
                        if (a1.Rank != a2.Rank)
                        {
                            return false;
                        }
                        break;
                    }
                    case ElementType.Var:
                    case ElementType.MVar:
                    {
                        //var v1 = (GenericSig)t1;
                        //var v2 = (GenericSig)t2;
                        //return v1.Number == v2.Number;
                        return true;
                    }
                    default: return true;
                }
                if (t1.Next != null && t2.Next != null)
                {
                    return IsLooseTypeSigMatch(t1.Next, t2.Next);
                }
                return true;
            }

            private bool IsLooseMatch(MethodDef method1, MethodDef method2)
            {
                if (method1.Name != method2.Name)
                {
                    return false;
                }
                if (method1.GetParamCount() != method2.GetParamCount())
                {
                    return false;
                }
                if (!IsLooseTypeSigMatch(method1.ReturnType, method2.ReturnType))
                {
                    return false;
                }
                for (int i = 0, n = method1.GetParamCount(); i < n; i++)
                {
                    if (!IsLooseTypeSigMatch(method1.GetParam(i), method2.GetParam(i)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool TryFindMatchVirtualMethod(MethodDef method, out MethodDef matchMethodDef)
            {
                foreach (var parentOrInterfaceMethod in flatMethods)
                {
                    if (IsLooseMatch(method, parentOrInterfaceMethod))
                    {
                        matchMethodDef = parentOrInterfaceMethod;
                        return true;
                    }
                }
                matchMethodDef = null;
                return false;
            }
        }


        private readonly Dictionary<MethodDef, VirtualMethodGroup> _methodGroups = new Dictionary<MethodDef, VirtualMethodGroup>();
        private readonly Dictionary<TypeDef, TypeFlatMethods> _visitedTypes = new Dictionary<TypeDef, TypeFlatMethods>();



        public VirtualMethodGroup GetMethodGroup(MethodDef methodDef)
        {
            if (_methodGroups.TryGetValue(methodDef, out var group))
            {
                return group;
            }
            return null;
        }

        public void CalculateType(TypeDef typeDef)
        {
            if (_visitedTypes.ContainsKey(typeDef))
            {
                return;
            }

            var typeMethods = new TypeFlatMethods();

            var interfaceMethods = new List<MethodDef>();
            if (typeDef.BaseType != null)
            {
                TypeDef baseTypeDef = MetaUtil.GetTypeDefOrGenericTypeBaseThrowException(typeDef.BaseType);
                CalculateType(baseTypeDef);
                typeMethods.flatMethods.AddRange(_visitedTypes[baseTypeDef].flatMethods);
                foreach (var intfType in typeDef.Interfaces)
                {
                    TypeDef intfTypeDef = MetaUtil.GetTypeDefOrGenericTypeBaseThrowException(intfType.Interface);
                    CalculateType(intfTypeDef);
                    //typeMethods.flatMethods.AddRange(_visitedTypes[intfTypeDef].flatMethods);
                    interfaceMethods.AddRange(_visitedTypes[intfTypeDef].flatMethods);
                }
            }
            foreach (MethodDef method in interfaceMethods)
            {
                if (typeMethods.TryFindMatchVirtualMethod(method, out var matchMethodDef))
                {
                    // merge group
                    var group = _methodGroups[matchMethodDef];
                    var matchGroup = _methodGroups[method];
                    if (group != matchGroup)
                    {
                        foreach (var m in matchGroup.methods)
                        {
                            group.methods.Add(m);
                            _methodGroups[m] = group;
                        }
                    }
                }
                typeMethods.flatMethods.Add(method);
            }

            foreach (MethodDef method in typeDef.Methods)
            {
                if (!method.IsVirtual)
                {
                    continue;
                }
                if (typeMethods.TryFindMatchVirtualMethod(method, out var matchMethodDef))
                {
                    var group = _methodGroups[matchMethodDef];
                    group.methods.Add(method);
                    _methodGroups.Add(method, group);
                }
                else
                {
                    _methodGroups.Add(method, new VirtualMethodGroup() { methods = new List<MethodDef> { method } });
                }
                if (method.IsNewSlot)
                {
                    typeMethods.flatMethods.Add(method);
                }
            }
            _visitedTypes.Add(typeDef, typeMethods);
        }
    }
}
