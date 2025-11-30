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
using Obfuz.Editor;
using System.Linq;

namespace Obfuz.Utils
{
    public class ObfuzIgnoreScopeComputeCache
    {
        private readonly CachedDictionary<IHasCustomAttribute, ObfuzScope?> _selfObfuzIgnoreScopeCache;
        private readonly CachedDictionary<TypeDef, ObfuzScope?> _enclosingObfuzIgnoreScopeCache;
        private readonly CachedDictionary<TypeDef, ObfuzScope?> _selfObfuzIgnoreApplyToChildTypesScopeCache;
        private readonly CachedDictionary<TypeDef, ObfuzScope?> _inheritedObfuzIgnoreScopeCache;

        public ObfuzIgnoreScopeComputeCache()
        {
            _selfObfuzIgnoreScopeCache = new CachedDictionary<IHasCustomAttribute, ObfuzScope?>(GetObfuzIgnoreScope);
            _enclosingObfuzIgnoreScopeCache = new CachedDictionary<TypeDef, ObfuzScope?>(GetEnclosingObfuzIgnoreScope);
            _selfObfuzIgnoreApplyToChildTypesScopeCache = new CachedDictionary<TypeDef, ObfuzScope?>(GetObfuzIgnoreScopeApplyToChildTypes);
            _inheritedObfuzIgnoreScopeCache = new CachedDictionary<TypeDef, ObfuzScope?>(GetInheritObfuzIgnoreScope);
        }

        private ObfuzScope? GetObfuzIgnoreScope(IHasCustomAttribute obj)
        {
            var ca = obj.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == ConstValues.ObfuzIgnoreAttributeFullName);
            if (ca == null)
            {
                return null;
            }
            var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
            return scope;
        }

        private ObfuzScope? GetEnclosingObfuzIgnoreScope(TypeDef typeDef)
        {
            TypeDef cur = typeDef.DeclaringType;
            while (cur != null)
            {
                var ca = cur.CustomAttributes?.FirstOrDefault(c => c.AttributeType.FullName == ConstValues.ObfuzIgnoreAttributeFullName);
                if (ca != null)
                {
                    var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
                    CANamedArgument inheritByNestedTypesArg = ca.GetNamedArgument("ApplyToNestedTypes", false);
                    bool inheritByNestedTypes = inheritByNestedTypesArg == null || (bool)inheritByNestedTypesArg.Value;
                    return inheritByNestedTypes ? (ObfuzScope?)scope : null;
                }
                cur = cur.DeclaringType;
            }
            return null;
        }

        private ObfuzScope? GetObfuzIgnoreScopeApplyToChildTypes(TypeDef cur)
        {
            if (cur.Module.IsCoreLibraryModule == true)
            {
                return null;
            }
            var ca = cur.CustomAttributes?.FirstOrDefault(c => c.AttributeType.FullName == ConstValues.ObfuzIgnoreAttributeFullName);
            if (ca != null)
            {
                var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
                CANamedArgument inheritByChildTypesArg = ca.GetNamedArgument("ApplyToChildTypes", false);
                bool inheritByChildTypes = inheritByChildTypesArg != null && (bool)inheritByChildTypesArg.Value;
                if (inheritByChildTypes)
                {
                    return scope;
                }
            }
            return null;
        }

        private ObfuzScope? GetInheritObfuzIgnoreScope(TypeDef typeDef)
        {
            TypeDef cur = typeDef;
            for (; cur != null; cur = MetaUtil.GetBaseTypeDef(cur))
            {
                ObfuzScope? scope = _selfObfuzIgnoreApplyToChildTypesScopeCache.GetValue(cur);
                if (scope != null)
                {
                    return scope;
                }
                foreach (var interfaceType in cur.Interfaces)
                {
                    TypeDef interfaceTypeDef = interfaceType.Interface.ResolveTypeDef();
                    if (interfaceTypeDef != null)
                    {
                        ObfuzScope? interfaceScope = _selfObfuzIgnoreApplyToChildTypesScopeCache.GetValue(interfaceTypeDef);
                        if (interfaceScope != null)
                        {
                            return interfaceScope;
                        }
                    }
                }
            }
            return null;
        }

        //private ObfuzScope? GetSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope((IHasCustomAttribute obj, TypeDef declaringType) objAndDeclaringType, ObfuzScope targetScope)
        //{
        //    ObfuzScope? scope = _selfObfuzIgnoreScopeCache.GetValue(objAndDeclaringType.obj);
        //    if (scope != null)
        //    {
        //        return scope;
        //    }
        //    if (objAndDeclaringType.declaringType == null)
        //    {
        //        return null;
        //    }
        //    ObfuzScope? declaringOrEnclosingScope = _selfObfuzIgnoreScopeCache.GetValue(declaringType) ?? _enclosingObfuzIgnoreScopeCache.GetValue(declaringType) ?? _inheritedObfuzIgnoreScopeCache.GetValue(declaringType);
        //    return declaringOrEnclosingScope != null && (declaringOrEnclosingScope & targetScope) != 0;
        //}

        //private bool HasObfuzIgnoreScope(IHasCustomAttribute obj, ObfuzScope targetScope)
        //{
        //    ObfuzScope? objScope = _selfObfuzIgnoreScopeCache.GetValue(obj);
        //    return objScope != null && (objScope & targetScope) != 0;
        //}

        //private bool HasDeclaringOrEnclosingOrInheritObfuzIgnoreScope(TypeDef typeDef, ObfuzScope targetScope)
        //{
        //    if (typeDef == null)
        //    {
        //        return false;
        //    }
        //    ObfuzScope? declaringOrEnclosingScope = _selfObfuzIgnoreScopeCache.GetValue(typeDef) ?? _enclosingObfuzIgnoreScopeCache.GetValue(typeDef) ?? _inheritedObfuzIgnoreScopeCache.GetValue(typeDef);
        //    return declaringOrEnclosingScope != null && (declaringOrEnclosingScope & targetScope) != 0;
        //}

        public ObfuzScope? GetSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(IHasCustomAttribute obj, TypeDef declaringType)
        {
            ObfuzScope? scope = _selfObfuzIgnoreScopeCache.GetValue(obj);
            if (scope != null)
            {
                return scope;
            }
            if (declaringType == null)
            {
                return null;
            }
            ObfuzScope? declaringOrEnclosingScope = _selfObfuzIgnoreScopeCache.GetValue(declaringType) ?? _enclosingObfuzIgnoreScopeCache.GetValue(declaringType) ?? _inheritedObfuzIgnoreScopeCache.GetValue(declaringType);
            return declaringOrEnclosingScope;
        }

        public bool HasSelfOrEnclosingOrInheritObfuzIgnoreScope(TypeDef typeDef, ObfuzScope targetScope)
        {
            ObfuzScope? scope = _selfObfuzIgnoreScopeCache.GetValue(typeDef) ?? _enclosingObfuzIgnoreScopeCache.GetValue(typeDef) ?? _inheritedObfuzIgnoreScopeCache.GetValue(typeDef);
            return scope != null && (scope & targetScope) != 0;
        }

        public bool HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(IHasCustomAttribute obj, TypeDef declaringType, ObfuzScope targetScope)
        {
            ObfuzScope? scope = _selfObfuzIgnoreScopeCache.GetValue(obj);
            if (scope != null)
            {
                return (scope & targetScope) != 0;
            }
            if (declaringType == null)
            {
                return false;
            }
            ObfuzScope? declaringOrEnclosingScope = _selfObfuzIgnoreScopeCache.GetValue(declaringType) ?? _enclosingObfuzIgnoreScopeCache.GetValue(declaringType) ?? _inheritedObfuzIgnoreScopeCache.GetValue(declaringType);
            return declaringOrEnclosingScope != null && (declaringOrEnclosingScope & targetScope) != 0;
        }

        public bool HasSelfOrInheritPropertyOrEventOrOrTypeDefObfuzIgnoreScope(MethodDef obj, ObfuzScope targetScope)
        {
            ObfuzScope? scope = _selfObfuzIgnoreScopeCache.GetValue(obj);
            if (scope != null && (scope & targetScope) != 0)
            {
                return true;
            }

            TypeDef declaringType = obj.DeclaringType;
            ObfuzScope? declaringOrEnclosingScope = _selfObfuzIgnoreScopeCache.GetValue(declaringType) ?? _enclosingObfuzIgnoreScopeCache.GetValue(declaringType) ?? _inheritedObfuzIgnoreScopeCache.GetValue(declaringType);

            foreach (var propertyDef in declaringType.Properties)
            {
                if (propertyDef.GetMethod == obj || propertyDef.SetMethod == obj)
                {
                    ObfuzScope? finalScope = _selfObfuzIgnoreScopeCache.GetValue(propertyDef);
                    if (finalScope != null && (finalScope & targetScope) != 0)
                    {
                        return true;
                    }
                    break;
                }
            }

            foreach (var eventDef in declaringType.Events)
            {
                if (eventDef.AddMethod == obj || eventDef.RemoveMethod == obj)
                {
                    ObfuzScope? finalScope = _selfObfuzIgnoreScopeCache.GetValue(eventDef);
                    if (finalScope != null && (finalScope & targetScope) != 0)
                    {
                        return true;
                    }
                    break;
                }
            }

            return declaringOrEnclosingScope != null && (declaringOrEnclosingScope & targetScope) != 0;
        }

        public bool HasSelfOrInheritPropertyOrEventOrOrTypeDefIgnoreMethodName(MethodDef obj)
        {
            ObfuzScope? scope = _selfObfuzIgnoreScopeCache.GetValue(obj);
            if (scope != null && (scope & ObfuzScope.MethodName) != 0)
            {
                return true;
            }

            TypeDef declaringType = obj.DeclaringType;

            foreach (var propertyDef in declaringType.Properties)
            {
                if (propertyDef.GetMethod == obj || propertyDef.SetMethod == obj)
                {
                    ObfuzScope? finalScope = GetObfuzIgnoreScope(propertyDef);
                    if (finalScope != null && (finalScope & ObfuzScope.PropertyGetterSetterName) != 0)
                    {
                        return true;
                    }
                    break;
                }
            }

            foreach (var eventDef in declaringType.Events)
            {
                if (eventDef.AddMethod == obj || eventDef.RemoveMethod == obj)
                {
                    ObfuzScope? finalScope = GetObfuzIgnoreScope(eventDef);
                    if (finalScope != null && (finalScope & ObfuzScope.EventAddRemoveFireName) != 0)
                    {
                        return true;
                    }
                    break;
                }
            }

            return HasSelfOrEnclosingOrInheritObfuzIgnoreScope(declaringType, ObfuzScope.MethodName | ObfuzScope.PropertyGetterSetterName | ObfuzScope.EventAddRemoveFireName);
        }
    }
}
