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
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        private readonly ObfuzIgnoreScopeComputeCache _obfuzIgnoreScopeComputeCache;

        public SystemRenamePolicy(ObfuzIgnoreScopeComputeCache obfuzIgnoreScopeComputeCache)
        {
            _obfuzIgnoreScopeComputeCache = obfuzIgnoreScopeComputeCache;
        }

        private readonly HashSet<string> _fullIgnoreTypeFullNames = new HashSet<string>
        {
            ConstValues.ObfuzIgnoreAttributeFullName,
            ConstValues.ObfuzScopeFullName,
            ConstValues.EncryptFieldAttributeFullName,
            ConstValues.EmbeddedAttributeFullName,
            ConstValues.ZluaLuaInvokeAttributeFullName,
            ConstValues.ZluaLuaCallbackAttributeFullName,
            ConstValues.ZluaLuaMarshalAsAttributeFullName,
            ConstValues.BurstCompileFullName,
        };


        private readonly HashSet<string> _fullIgnoreTypeNames = new HashSet<string>
        {
            ConstValues.MonoPInvokeCallbackAttributeName,
        };

        private bool IsFullIgnoreObfuscatedType(TypeDef typeDef)
        {
            return _fullIgnoreTypeFullNames.Contains(typeDef.FullName) || _fullIgnoreTypeNames.Contains(typeDef.Name) || MetaUtil.HasMicrosoftCodeAnalysisEmbeddedAttribute(typeDef);
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            string name = typeDef.Name;
            if (name == "<Module>")
            {
                return false;
            }
            if (IsFullIgnoreObfuscatedType(typeDef))
            {
                return false;
            }

            if (_obfuzIgnoreScopeComputeCache.HasSelfOrEnclosingOrInheritObfuzIgnoreScope(typeDef, ObfuzScope.TypeName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (methodDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(methodDef.DeclaringType))
            {
                return false;
            }
            if (methodDef.Name == ".ctor" || methodDef.Name == ".cctor")
            {
                return false;
            }

            if (_obfuzIgnoreScopeComputeCache.HasSelfOrInheritPropertyOrEventOrOrTypeDefIgnoreMethodName(methodDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (fieldDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(fieldDef.DeclaringType))
            {
                return false;
            }
            if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(fieldDef, fieldDef.DeclaringType, ObfuzScope.Field))
            {
                return false;
            }
            if (fieldDef.DeclaringType.IsEnum && !fieldDef.IsStatic)
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            if (propertyDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(propertyDef.DeclaringType))
            {
                return false;
            }
            if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(propertyDef, propertyDef.DeclaringType, ObfuzScope.PropertyName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (eventDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(eventDef.DeclaringType))
            {
                return false;
            }
            if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(eventDef, eventDef.DeclaringType, ObfuzScope.EventName))
            {
                return false;
            }
            return true;
        }
    }
}
