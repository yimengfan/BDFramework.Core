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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public enum ProxyMode
    {
        Dispatch,
        Delegate,
    }

    public class CallObfuscationSettingsFacade
    {
        public ProxyMode proxyMode;
        public int obfuscationLevel;
        public int maxProxyMethodCountPerDispatchMethod;
        public bool obfuscateCallToMethodInMscorlib;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class CallObfuscationSettings
    {
        public ProxyMode proxyMode = ProxyMode.Dispatch;

        [Tooltip("The obfuscation level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int obfuscationLevel = 1;

        [Tooltip("The maximum number of proxy methods that can be generated per dispatch method. This helps to limit the complexity of the generated code and improve performance.")]
        public int maxProxyMethodCountPerDispatchMethod = 100;

        [Tooltip("Whether to obfuscate calls to methods in mscorlib. Enable this option will impact performance.")]
        public bool obfuscateCallToMethodInMscorlib;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public CallObfuscationSettingsFacade ToFacade()
        {
            return new CallObfuscationSettingsFacade
            {
                proxyMode = proxyMode,
                obfuscationLevel = obfuscationLevel,
                maxProxyMethodCountPerDispatchMethod = maxProxyMethodCountPerDispatchMethod,
                obfuscateCallToMethodInMscorlib = obfuscateCallToMethodInMscorlib,
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
            };
        }
    }
}
