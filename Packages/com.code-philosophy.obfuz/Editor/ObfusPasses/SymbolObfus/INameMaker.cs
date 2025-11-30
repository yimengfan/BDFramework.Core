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

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public interface INameMaker
    {
        void AddPreservedName(TypeDef typeDef, string name);

        void AddPreservedNamespace(TypeDef typeDef, string name);

        void AddPreservedName(MethodDef methodDef, string name);

        void AddPreservedName(FieldDef fieldDef, string name);

        void AddPreservedName(PropertyDef propertyDef, string name);

        void AddPreservedName(EventDef eventDef, string name);

        bool IsNamePreserved(VirtualMethodGroup virtualMethodGroup, string name);

        string GetNewName(TypeDef typeDef, string originalName);

        string GetNewNamespace(TypeDef typeDef, string originalNamespace, bool reuse);

        string GetNewName(MethodDef methodDef, string originalName);

        string GetNewName(VirtualMethodGroup virtualMethodGroup, string originalName);

        string GetNewName(ParamDef param, string originalName);

        string GetNewName(FieldDef fieldDef, string originalName);

        string GetNewName(PropertyDef propertyDef, string originalName);

        string GetNewName(EventDef eventDef, string originalName);
    }
}
