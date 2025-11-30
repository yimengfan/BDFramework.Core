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

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    internal class SupportPassPolicy : ObfuscationPolicyBase
    {
        private readonly ConfigurablePassPolicy _policy;


        private bool Support(ObfuscationPassType passType)
        {
            return passType.HasFlag(ObfuscationPassType.SymbolObfus);
        }

        public SupportPassPolicy(ConfigurablePassPolicy policy)
        {
            _policy = policy;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            return Support(_policy.GetTypeObfuscationPasses(typeDef));
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            return Support(_policy.GetMethodObfuscationPasses(methodDef));
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            return Support(_policy.GetFieldObfuscationPasses(fieldDef));
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            return Support(_policy.GetPropertyObfuscationPasses(propertyDef));
        }

        public override bool NeedRename(EventDef eventDef)
        {
            return Support(_policy.GetEventObfuscationPasses(eventDef));
        }
    }
}
