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

﻿using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public class SymbolObfuscationSettingsFacade
    {
        public bool debug;
        public string obfuscatedNamePrefix;
        public bool useConsistentNamespaceObfuscation;
        public bool detectReflectionCompatibility;
        public bool keepUnknownSymbolInSymbolMappingFile;
        public string symbolMappingFile;
        public List<string> ruleFiles;
        public List<Type> customRenamePolicyTypes;
    }

    [Serializable]
    public class SymbolObfuscationSettings
    {
        public bool debug;

        [Tooltip("prefix for obfuscated name to avoid name confliction with original name")]
        public string obfuscatedNamePrefix = "$";

        [Tooltip("obfuscate same namespace to one name")]
        public bool useConsistentNamespaceObfuscation = true;

        [Tooltip("detect reflection compatibility, if true, will detect if the obfuscated name is compatibility with reflection, such as Type.GetType(), Enum.Parse(), etc.")]
        public bool detectReflectionCompatibility = true;

        [Tooltip("keep unknown symbol in symbol mapping file, if false, unknown symbol will be removed from mapping file")]
        public bool keepUnknownSymbolInSymbolMappingFile = true;

        [Tooltip("symbol mapping file path")]
        public string symbolMappingFile = "Assets/Obfuz/SymbolObfus/symbol-mapping.xml";

        [Tooltip("debug symbol mapping file path, used for debugging purposes")]
        public string debugSymbolMappingFile = "Assets/Obfuz/SymbolObfus/symbol-mapping-debug.xml";

        [Tooltip("rule files")]
        public string[] ruleFiles;

        [Tooltip("custom rename policy types")]
        public string[] customRenamePolicyTypes;

        public string GetSymbolMappingFile()
        {
            return debug ? debugSymbolMappingFile : symbolMappingFile;
        }

        public SymbolObfuscationSettingsFacade ToFacade()
        {
            return new SymbolObfuscationSettingsFacade
            {
                debug = debug,
                obfuscatedNamePrefix = obfuscatedNamePrefix,
                useConsistentNamespaceObfuscation = useConsistentNamespaceObfuscation,
                detectReflectionCompatibility = detectReflectionCompatibility,
                keepUnknownSymbolInSymbolMappingFile = keepUnknownSymbolInSymbolMappingFile,
                symbolMappingFile = GetSymbolMappingFile(),
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
                customRenamePolicyTypes = customRenamePolicyTypes?.Select(typeName => ReflectionUtil.FindUniqueTypeInCurrentAppDomain(typeName)).ToList() ?? new List<Type>(),
            };
        }
    }
}
