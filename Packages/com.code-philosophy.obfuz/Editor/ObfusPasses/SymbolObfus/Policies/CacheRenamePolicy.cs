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
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class CacheRenamePolicy : ObfuscationPolicyBase
    {
        private readonly IObfuscationPolicy _underlyingPolicy;

        private readonly Dictionary<object, bool> _computeCache = new Dictionary<object, bool>();

        public CacheRenamePolicy(IObfuscationPolicy underlyingPolicy)
        {
            _underlyingPolicy = underlyingPolicy;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            if (!_computeCache.TryGetValue(typeDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(typeDef);
                _computeCache[typeDef] = value;
            }
            return value;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (!_computeCache.TryGetValue(methodDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(methodDef);
                _computeCache[methodDef] = value;
            }
            return value;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (!_computeCache.TryGetValue(fieldDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(fieldDef);
                _computeCache[fieldDef] = value;
            }
            return value;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            if (!_computeCache.TryGetValue(propertyDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(propertyDef);
                _computeCache[propertyDef] = value;
            }
            return value;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (!_computeCache.TryGetValue(eventDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(eventDef);
                _computeCache[eventDef] = value;
            }
            return value;
        }
    }
}
