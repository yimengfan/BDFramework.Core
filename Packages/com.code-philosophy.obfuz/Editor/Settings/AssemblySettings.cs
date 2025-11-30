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

using Obfuz.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class AssemblySettings
    {

        [Tooltip("name of assemblies to obfuscate, please don't add 'Obfuz.Runtime'")]
        public string[] assembliesToObfuscate;

        [Tooltip("name of assemblies not obfuscated but reference assemblies to obfuscated ")]
        public string[] nonObfuscatedButReferencingObfuscatedAssemblies;

        [Tooltip("additional assembly search paths")]
        public string[] additionalAssemblySearchPaths;

        [Tooltip("obfuscate Obfuz.Runtime")]
        public bool obfuscateObfuzRuntime = true;

        public List<string> GetAssembliesToObfuscate()
        {
            var asses = new List<string>(assembliesToObfuscate ?? Array.Empty<string>());
            if (obfuscateObfuzRuntime && !asses.Contains(ConstValues.ObfuzRuntimeAssemblyName))
            {
                asses.Add(ConstValues.ObfuzRuntimeAssemblyName);
            }
            return asses;
        }

        public List<string> GetObfuscationRelativeAssemblyNames()
        {
            var asses = GetAssembliesToObfuscate();
            asses.AddRange(nonObfuscatedButReferencingObfuscatedAssemblies ?? Array.Empty<string>());
            return asses;
        }
    }
}
