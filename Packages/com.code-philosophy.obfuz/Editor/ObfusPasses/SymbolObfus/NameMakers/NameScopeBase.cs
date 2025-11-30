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

﻿using System.Collections.Generic;
using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public abstract class NameScopeBase : INameScope
    {

        private readonly Dictionary<string, string> _nameMap = new Dictionary<string, string>();

        private readonly HashSet<string> _preservedNames = new HashSet<string>();


        public bool AddPreservedName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return _preservedNames.Add(name);
            }
            return false;
        }

        public bool IsNamePreserved(string name)
        {
            return _preservedNames.Contains(name);
        }


        protected abstract void BuildNewName(StringBuilder nameBuilder, string originalName, string lastName);

        private string CreateNewName(string originalName)
        {
            var nameBuilder = new StringBuilder();
            string lastName = null;
            while (true)
            {
                nameBuilder.Clear();
                BuildNewName(nameBuilder, originalName, lastName);
                string newName = nameBuilder.ToString();
                lastName = newName;
                if (_preservedNames.Add(newName))
                {
                    return newName;
                }
            }
        }

        public string GetNewName(string originalName, bool reuse)
        {
            if (!reuse)
            {
                return CreateNewName(originalName);
            }
            if (_nameMap.TryGetValue(originalName, out var newName))
            {
                return newName;
            }
            newName = CreateNewName(originalName);
            _nameMap[originalName] = newName;
            return newName;
        }
    }
}
