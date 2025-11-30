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
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Obfuz.ObfusPasses.ConstEncrypt
{

    public class ConfigurableEncryptPolicy : EncryptPolicyBase
    {
        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public bool? disableEncrypt;
            public bool? encryptInt;
            public bool? encryptLong;
            public bool? encryptFloat;
            public bool? encryptDouble;
            public bool? encryptArray;
            public bool? encryptString;

            public bool? encryptConstInLoop;
            public bool? encryptStringInLoop;

            public bool? cacheConstInLoop;
            public bool? cacheConstNotInLoop;
            public bool? cacheStringInLoop;
            public bool? cacheStringNotInLoop;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (disableEncrypt == null)
                    disableEncrypt = parentRule.disableEncrypt;
                if (encryptInt == null)
                    encryptInt = parentRule.encryptInt;
                if (encryptLong == null)
                    encryptLong = parentRule.encryptLong;
                if (encryptFloat == null)
                    encryptFloat = parentRule.encryptFloat;
                if (encryptDouble == null)
                    encryptDouble = parentRule.encryptDouble;
                if (encryptArray == null)
                    encryptArray = parentRule.encryptArray;
                if (encryptString == null)
                    encryptString = parentRule.encryptString;

                if (encryptConstInLoop == null)
                    encryptConstInLoop = parentRule.encryptConstInLoop;
                if (encryptStringInLoop == null)
                    encryptStringInLoop = parentRule.encryptStringInLoop;

                if (cacheConstInLoop == null)
                    cacheConstInLoop = parentRule.cacheConstInLoop;
                if (cacheConstNotInLoop == null)
                    cacheConstNotInLoop = parentRule.cacheConstNotInLoop;
                if (cacheStringInLoop == null)
                    cacheStringInLoop = parentRule.cacheStringInLoop;
                if (cacheStringNotInLoop == null)
                    cacheStringNotInLoop = parentRule.cacheStringNotInLoop;
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
            disableEncrypt = false,
            encryptInt = true,
            encryptLong = true,
            encryptFloat = true,
            encryptDouble = true,
            encryptArray = true,
            encryptString = true,
            encryptConstInLoop = true,
            encryptStringInLoop = true,
            cacheConstInLoop = true,
            cacheConstNotInLoop = false,
            cacheStringInLoop = true,
            cacheStringNotInLoop = true,
        };

        private ObfuscationRule _global;

        public HashSet<int> notEncryptInts = new HashSet<int>();
        public HashSet<long> notEncryptLongs = new HashSet<long>();
        public HashSet<string> notEncryptStrings = new HashSet<string>();
        public List<NumberRange<int>> notEncryptIntRanges = new List<NumberRange<int>>();
        public List<NumberRange<long>> notEncryptLongRanges = new List<NumberRange<long>>();
        public List<NumberRange<float>> notEncryptFloatRanges = new List<NumberRange<float>>();
        public List<NumberRange<double>> notEncryptDoubleRanges = new List<NumberRange<double>>();
        public List<NumberRange<int>> notEncryptArrayLengthRanges = new List<NumberRange<int>>();
        public List<NumberRange<int>> notEncryptStringLengthRanges = new List<NumberRange<int>>();

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _xmlParser;

        private readonly Dictionary<string, AssemblySpec> _assemblySpecs = new Dictionary<string, AssemblySpec>();
        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableEncryptPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _xmlParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(
                toObfuscatedAssemblyNames, ParseObfuscationRule, ParseGlobalElement);
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
            _xmlParser.InheritParentRules(_global);
        }

        private void ParseGlobalElement(string configFile, XmlElement ele)
        {
            switch (ele.Name)
            {
                case "global": _global = ParseObfuscationRule(configFile, ele); break;
                case "whitelist": ParseWhitelist(configFile, ele); break;
                default: throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
            }
        }

        private ObfuscationRule ParseObfuscationRule(string configFile, XmlElement ele)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("disableEncrypt"))
            {
                rule.disableEncrypt = ConfigUtil.ParseBool(ele.GetAttribute("disableEncrypt"));
            }
            if (ele.HasAttribute("encryptInt"))
            {
                rule.encryptInt = ConfigUtil.ParseBool(ele.GetAttribute("encryptInt"));
            }
            if (ele.HasAttribute("encryptLong"))
            {
                rule.encryptLong = ConfigUtil.ParseBool(ele.GetAttribute("encryptLong"));
            }
            if (ele.HasAttribute("encryptFloat"))
            {
                rule.encryptFloat = ConfigUtil.ParseBool(ele.GetAttribute("encryptFloat"));
            }
            if (ele.HasAttribute("encryptDouble"))
            {
                rule.encryptDouble = ConfigUtil.ParseBool(ele.GetAttribute("encryptDouble"));
            }
            if (ele.HasAttribute("encryptBytes"))
            {
                rule.encryptArray = ConfigUtil.ParseBool(ele.GetAttribute("encryptArray"));
            }
            if (ele.HasAttribute("encryptString"))
            {
                rule.encryptString = ConfigUtil.ParseBool(ele.GetAttribute("encryptString"));
            }

            if (ele.HasAttribute("encryptConstInLoop"))
            {
                rule.encryptConstInLoop = ConfigUtil.ParseBool(ele.GetAttribute("encryptConstInLoop"));
            }
            if (ele.HasAttribute("encryptStringInLoop"))
            {
                rule.encryptStringInLoop = ConfigUtil.ParseBool(ele.GetAttribute("encryptStringInLoop"));
            }
            if (ele.HasAttribute("cacheConstInLoop"))
            {
                rule.cacheConstInLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheConstInLoop"));
            }
            if (ele.HasAttribute("cacheConstNotInLoop"))
            {
                rule.cacheConstNotInLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheConstNotInLoop"));
            }
            if (ele.HasAttribute("cacheStringInLoop"))
            {
                rule.cacheStringInLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheStringInLoop"));
            }
            if (ele.HasAttribute("cacheStringNotInLoop"))
            {
                rule.cacheStringNotInLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheStringNotInLoop"));
            }
            return rule;
        }

        private void ParseWhitelist(string configFile, XmlElement childEle)
        {
            string type = childEle.GetAttribute("type");
            if (string.IsNullOrEmpty(type))
            {
                throw new Exception($"Invalid xml file, whitelist type is empty");
            }
            string value = childEle.InnerText;
            switch (type)
            {
                case "int":
                {
                    notEncryptInts.AddRange(value.Split(',').Select(s => int.Parse(s.Trim())));
                    break;
                }
                case "long":
                {
                    notEncryptLongs.AddRange(value.Split(',').Select(s => long.Parse(s.Trim())));
                    break;
                }
                case "string":
                {
                    notEncryptStrings.AddRange(value.Split(',').Select(s => s.Trim()));
                    break;
                }
                case "int-range":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Invalid xml file, int-range {value} is invalid");
                    }
                    notEncryptIntRanges.Add(new NumberRange<int>(ConfigUtil.ParseNullableInt(parts[0]), ConfigUtil.ParseNullableInt(parts[1])));
                    break;
                }
                case "long-range":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Invalid xml file, long-range {value} is invalid");
                    }
                    notEncryptLongRanges.Add(new NumberRange<long>(ConfigUtil.ParseNullableLong(parts[0]), ConfigUtil.ParseNullableLong(parts[1])));
                    break;
                }
                case "float-range":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Invalid xml file, float-range {value} is invalid");
                    }
                    notEncryptFloatRanges.Add(new NumberRange<float>(ConfigUtil.ParseNullableFloat(parts[0]), ConfigUtil.ParseNullableFloat(parts[1])));
                    break;
                }
                case "double-range":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Invalid xml file, double-range {value} is invalid");
                    }
                    notEncryptDoubleRanges.Add(new NumberRange<double>(ConfigUtil.ParseNullableDouble(parts[0]), ConfigUtil.ParseNullableDouble(parts[1])));
                    break;
                }
                case "string-length-range":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Invalid xml file, string-length-range {value} is invalid");
                    }
                    notEncryptStringLengthRanges.Add(new NumberRange<int>(ConfigUtil.ParseNullableInt(parts[0]), ConfigUtil.ParseNullableInt(parts[1])));
                    break;
                }
                case "array-length-range":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Invalid xml file, array-length-range {value} is invalid");
                    }
                    notEncryptArrayLengthRanges.Add(new NumberRange<int>(ConfigUtil.ParseNullableInt(parts[0]), ConfigUtil.ParseNullableInt(parts[1])));
                    break;
                }
                default: throw new Exception($"Invalid xml file, unknown whitelist type {type} in {childEle.Name} node");
            }
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

        public override bool NeedObfuscateMethod(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.disableEncrypt != true;
        }

        public override ConstCachePolicy GetMethodConstCachePolicy(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return new ConstCachePolicy
            {
                cacheConstInLoop = rule.cacheConstInLoop.Value,
                cacheConstNotInLoop = rule.cacheConstNotInLoop.Value,
                cacheStringInLoop = rule.cacheStringInLoop.Value,
                cacheStringNotInLoop = rule.cacheStringNotInLoop.Value,
            };
        }

        public override bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptInt == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            if (notEncryptInts.Contains(value))
            {
                return false;
            }
            foreach (var range in notEncryptIntRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptLong == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            if (notEncryptLongs.Contains(value))
            {
                return false;
            }
            foreach (var range in notEncryptLongRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptFloat == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            foreach (var range in notEncryptFloatRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptDouble == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            foreach (var range in notEncryptDoubleRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptString == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            if (notEncryptStrings.Contains(value))
            {
                return false;
            }
            foreach (var range in notEncryptStringLengthRanges)
            {
                if (range.min != null && value.Length < range.min)
                {
                    continue;
                }
                if (range.max != null && value.Length > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptArray == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            foreach (var range in notEncryptArrayLengthRanges)
            {
                if (range.min != null && array.Length < range.min)
                {
                    continue;
                }
                if (range.max != null && array.Length > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }
    }
}
