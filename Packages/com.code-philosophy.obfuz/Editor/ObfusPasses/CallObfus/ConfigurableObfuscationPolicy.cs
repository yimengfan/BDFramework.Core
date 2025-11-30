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

namespace Obfuz.ObfusPasses.CallObfus
{
    public class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        class WhiteListAssembly
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscate;
            public List<WhiteListType> types = new List<WhiteListType>();
        }

        class WhiteListType
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscate;
            public List<WhiteListMethod> methods = new List<WhiteListMethod>();
        }

        class WhiteListMethod
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscate;
        }

        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public ObfuscationLevel? obfuscationLevel;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (obfuscationLevel == null)
                    obfuscationLevel = parentRule.obfuscationLevel;
            }
        }

        class AssemblySpec : AssemblyRuleBase<TypeSpec, MethodSpec, ObfuscationRule>
        {
        }

        class TypeSpec : TypeRuleBase<MethodSpec, ObfuscationRule>
        {
        }

        class MethodSpec : MethodRuleBase<ObfuscationRule>
        {

        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            obfuscationLevel = ObfuscationLevel.Basic,
        };

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _configParser;

        private ObfuscationRule _global;
        private readonly List<WhiteListAssembly> _whiteListAssemblies = new List<WhiteListAssembly>();

        private readonly CachedDictionary<IMethod, bool> _whiteListMethodCache;
        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _whiteListMethodCache = new CachedDictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes, this.ComputeIsInWhiteList);
            _configParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(toObfuscatedAssemblyNames,
                ParseObfuscationRule, ParseGlobalElement);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _configParser.LoadConfigs(configFiles);

            if (_global == null)
            {
                _global = s_default;
            }
            else
            {
                _global.InheritParent(s_default);
            }
            _configParser.InheritParentRules(_global);
            InheritWhitelistRules();
        }

        private void InheritWhitelistRules()
        {
            foreach (var ass in _whiteListAssemblies)
            {
                foreach (var type in ass.types)
                {
                    if (type.obfuscate == null)
                    {
                        type.obfuscate = ass.obfuscate;
                    }
                    foreach (var method in type.methods)
                    {
                        if (method.obfuscate == null)
                        {
                            method.obfuscate = type.obfuscate;
                        }
                    }
                }
            }
        }

        private void ParseGlobalElement(string configFile, XmlElement ele)
        {
            switch (ele.Name)
            {
                case "global": _global = ParseObfuscationRule(configFile, ele); break;
                case "whitelist": ParseWhitelist(ele); break;
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
            return rule;
        }

        private void ParseWhitelist(XmlElement ruleEle)
        {
            foreach (XmlNode xmlNode in ruleEle.ChildNodes)
            {
                if (!(xmlNode is XmlElement childEle))
                {
                    continue;
                }
                switch (childEle.Name)
                {
                    case "assembly":
                    {
                        var ass = ParseWhiteListAssembly(childEle);
                        _whiteListAssemblies.Add(ass);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                }
            }
        }

        private WhiteListAssembly ParseWhiteListAssembly(XmlElement element)
        {
            var ass = new WhiteListAssembly();
            ass.name = element.GetAttribute("name");
            ass.nameMatcher = new NameMatcher(ass.name);

            ass.obfuscate = ConfigUtil.ParseNullableBool(element.GetAttribute("obfuscate")) ?? false;

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "type":
                    ass.types.Add(ParseWhiteListType(ele));
                    break;
                    default:
                    throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }
            return ass;
        }

        private WhiteListType ParseWhiteListType(XmlElement element)
        {
            var type = new WhiteListType();
            type.name = element.GetAttribute("name");
            type.nameMatcher = new NameMatcher(type.name);
            type.obfuscate = ConfigUtil.ParseNullableBool(element.GetAttribute("obfuscate"));

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "method":
                    {
                        type.methods.Add(ParseWhiteListMethod(ele));
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }

            return type;
        }

        private WhiteListMethod ParseWhiteListMethod(XmlElement element)
        {
            var method = new WhiteListMethod();
            method.name = element.GetAttribute("name");
            method.nameMatcher = new NameMatcher(method.name);
            method.obfuscate = ConfigUtil.ParseNullableBool(element.GetAttribute("obfuscate"));
            return method;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = _configParser.GetMethodRule(method, _global);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedObfuscateCallInMethod(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.obfuscationLevel != null && rule.obfuscationLevel.Value >= ObfuscationLevel.Basic;
        }

        private bool ComputeIsInWhiteList(IMethod calledMethod)
        {
            ITypeDefOrRef declaringType = calledMethod.DeclaringType;
            TypeSig declaringTypeSig = calledMethod.DeclaringType.ToTypeSig();
            declaringTypeSig = declaringTypeSig.RemovePinnedAndModifiers();
            switch (declaringTypeSig.ElementType)
            {
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    break;
                }
                case ElementType.GenericInst:
                {
                    if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
                    {
                        return true;
                    }
                    break;
                }
                default: return true;
            }

            TypeDef typeDef = declaringType.ResolveTypeDef();

            string assName = typeDef.Module.Assembly.Name;
            string typeFullName = typeDef.FullName;
            string methodName = calledMethod.Name;
            foreach (var ass in _whiteListAssemblies)
            {
                if (!ass.nameMatcher.IsMatch(assName))
                {
                    continue;
                }
                foreach (var type in ass.types)
                {
                    if (!type.nameMatcher.IsMatch(typeFullName))
                    {
                        continue;
                    }
                    foreach (var method in type.methods)
                    {
                        if (method.nameMatcher.IsMatch(methodName))
                        {
                            return !method.obfuscate.Value;
                        }
                    }
                    return !type.obfuscate.Value;
                }
                return !ass.obfuscate.Value;
            }
            return false;
        }

        public override bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir)
        {
            if (_whiteListMethodCache.GetValue(calledMethod))
            {
                return false;
            }
            return true;
        }
    }
}
