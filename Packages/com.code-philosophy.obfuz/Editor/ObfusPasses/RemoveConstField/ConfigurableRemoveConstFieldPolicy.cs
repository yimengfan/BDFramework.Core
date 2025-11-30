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
using Obfuz.Conf;
using System.Collections.Generic;
using System.Xml;

namespace Obfuz.ObfusPasses.RemoveConstField
{
    public class ConfigurableRemoveConstFieldPolicy : RemoveConstFieldBase
    {
        class ObfuscationRule
        {

        }

        private readonly XmlFieldRuleParser<ObfuscationRule> _configParser;

        public ConfigurableRemoveConstFieldPolicy(List<string> toObfuscatedAssemblyNames, List<string> configFiles)
        {
            _configParser = new XmlFieldRuleParser<ObfuscationRule>(toObfuscatedAssemblyNames, ParseRule, null);
            _configParser.LoadConfigs(configFiles);
        }

        private ObfuscationRule ParseRule(string configFile, XmlElement ele)
        {
            return new ObfuscationRule();
        }

        public override bool NeedPreserved(FieldDef field)
        {
            var rule = _configParser.GetFieldRule(field);
            return rule != null;
        }
    }
}
