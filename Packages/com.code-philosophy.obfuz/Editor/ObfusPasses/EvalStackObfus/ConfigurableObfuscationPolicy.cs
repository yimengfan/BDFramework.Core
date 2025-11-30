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
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Obfuz.ObfusPasses.EvalStackObfus
{
    struct ObfuscationRuleData
    {
        public readonly ObfuscationLevel obfuscationLevel;
        public readonly float obfuscationPercentage;
        public ObfuscationRuleData(ObfuscationLevel level, float percentage)
        {
            obfuscationLevel = level;
            obfuscationPercentage = percentage;
        }
    }

    interface IObfuscationPolicy
    {
        bool NeedObfuscate(MethodDef method);

        ObfuscationRuleData GetObfuscationRuleData(MethodDef method);
    }

    abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscate(MethodDef method);

        public abstract ObfuscationRuleData GetObfuscationRuleData(MethodDef method);
    }

    class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public ObfuscationLevel? obfuscationLevel;
            public float? obfuscationPercentage;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (obfuscationLevel == null)
                    obfuscationLevel = parentRule.obfuscationLevel;
                if (obfuscationPercentage == null)
                    obfuscationPercentage = parentRule.obfuscationPercentage;
            }
        }

        class MethodSpec : MethodRuleBase<ObfuscationRule>
        {
        }

        class TypeSpec : TypeRuleBase<MethodSpec, ObfuscationRule>
        {
        }

        class AssemblySpec : AssemblyRuleBase<TypeSpec, MethodSpec, ObfuscationRule>
        {
        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            obfuscationLevel = ObfuscationLevel.Basic,
            obfuscationPercentage = 0.05f,
        };

        private ObfuscationRule _global;

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _xmlParser;

        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _xmlParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(
                toObfuscatedAssemblyNames, ParseObfuscationRule, ParseGlobal);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _xmlParser.LoadConfigs(configFiles);

            if (_global == null)
            {
                _global = s_default;
            }
            else
            {
                _global.InheritParent(s_default);
            }
            if (_global.obfuscationPercentage.Value > 0.1f)
            {
                UnityEngine.Debug.LogWarning($"EvalStackObfus significantly increases the size of the obfuscated hot-update DLL. It is recommended to keep the obfuscationPercentage ≤ 0.1 (currently set to {_global.obfuscationPercentage.Value}).");
            }
            _xmlParser.InheritParentRules(_global);
        }

        private void ParseGlobal(string configFile, XmlElement ele)
        {
            switch (ele.Name)
            {
                case "global": _global = ParseObfuscationRule(configFile, ele); break;
                default: throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
            }
        }

        private ObfuscationRule ParseObfuscationRule(string configFile, XmlElement ele)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("obfuscationLevel"))
            {
                rule.obfuscationLevel = ConfigUtil.ParseObfuscationLevel(ele.GetAttribute("obfuscationLevel"));
            }
            if (ele.HasAttribute("obfuscationPercentage"))
            {
                rule.obfuscationPercentage = float.Parse(ele.GetAttribute("obfuscationPercentage"));
            }
            return rule;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = _xmlParser.GetMethodRule(method, _global);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedObfuscate(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.obfuscationLevel.Value > ObfuscationLevel.None;
        }

        public override ObfuscationRuleData GetObfuscationRuleData(MethodDef method)
        {
            var rule = GetMethodObfuscationRule(method);
            return new ObfuscationRuleData(rule.obfuscationLevel.Value, rule.obfuscationPercentage.Value);
        }
    }
}
