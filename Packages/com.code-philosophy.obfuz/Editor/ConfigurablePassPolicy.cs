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
using Obfuz.ObfusPasses;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Obfuz
{
    public class ConfigurablePassPolicy
    {
        class PassRule
        {
            public ObfuscationPassType? enablePasses;
            public ObfuscationPassType? disablePasses;
            public ObfuscationPassType? addPasses;
            public ObfuscationPassType? removePasses;
            public ObfuscationPassType finalPasses;

            public void InheritParent(PassRule parentRule, ObfuscationPassType globalEnabledPasses)
            {
                finalPasses = parentRule.finalPasses;
                if (enablePasses != null)
                {
                    finalPasses = enablePasses.Value;
                }
                if (disablePasses != null)
                {
                    finalPasses = ~disablePasses.Value;
                }
                if (addPasses != null)
                {
                    finalPasses |= addPasses.Value;
                }
                if (removePasses != null)
                {
                    finalPasses &= ~removePasses.Value;
                }
                finalPasses &= globalEnabledPasses;
            }
        }

        class SpecBase
        {
            public string name;
            public NameMatcher nameMatcher;
            public PassRule rule;
        }

        class MethodSpec : SpecBase
        {
        }

        class FieldSpec : SpecBase
        {
        }

        class PropertySpec : SpecBase
        {
        }

        class EventSpec : SpecBase
        {
        }

        class TypeSpec : SpecBase
        {
            public List<FieldSpec> fields = new List<FieldSpec>();
            public List<MethodSpec> methods = new List<MethodSpec>();
            public List<PropertySpec> properties = new List<PropertySpec>();
            public List<EventSpec> events = new List<EventSpec>();
        }

        class AssemblySpec
        {
            public string name;
            public NameMatcher nameMatcher;
            public PassRule rule;
            public List<TypeSpec> types = new List<TypeSpec>();
        }

        private readonly ObfuscationPassType _enabledPasses;
        private readonly HashSet<string> _toObfuscatedAssemblyNames;
        private readonly List<AssemblySpec> _assemblySpecs = new List<AssemblySpec>();
        private readonly PassRule _defaultPassRule;

        private string _curLoadingConfig;

        public ConfigurablePassPolicy(IEnumerable<string> toObfuscatedAssemblyNames, ObfuscationPassType enabledPasses, List<string> configFiles)
        {
            _toObfuscatedAssemblyNames = new HashSet<string>(toObfuscatedAssemblyNames);
            _enabledPasses = enabledPasses;
            _defaultPassRule = new PassRule { finalPasses = enabledPasses };
            LoadConfigs(configFiles);
            InheritParentRules(enabledPasses);
        }

        private void LoadConfigs(IEnumerable<string> configFiles)
        {
            foreach (var configFile in configFiles)
            {
                LoadConfig(configFile);
            }
        }

        private void InheritParentRules(ObfuscationPassType enablePasses)
        {
            var defaultRule = new PassRule
            {
                enablePasses = enablePasses,
                finalPasses = enablePasses,
            };
            foreach (AssemblySpec assSpec in _assemblySpecs)
            {
                assSpec.rule.InheritParent(defaultRule, enablePasses);
                foreach (TypeSpec typeSpec in assSpec.types)
                {
                    typeSpec.rule.InheritParent(assSpec.rule, enablePasses);
                    foreach (FieldSpec fieldSpec in typeSpec.fields)
                    {
                        fieldSpec.rule.InheritParent(typeSpec.rule, enablePasses);
                    }
                    foreach (MethodSpec methodSpec in typeSpec.methods)
                    {
                        methodSpec.rule.InheritParent(typeSpec.rule, enablePasses);
                    }
                    foreach (PropertySpec propertySpec in typeSpec.properties)
                    {
                        propertySpec.rule.InheritParent(typeSpec.rule, enablePasses);
                    }
                    foreach (EventSpec eventSpec in typeSpec.events)
                    {
                        eventSpec.rule.InheritParent(typeSpec.rule, enablePasses);
                    }
                }
            }
        }

        public void LoadConfig(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new Exception($"Invalid xml file {configFile}, file name is empty");
            }
            _curLoadingConfig = configFile;

            Debug.Log($"ConfigurablePassPolicy::LoadConfig {configFile}");
            var doc = new XmlDocument();
            doc.Load(configFile);
            var root = doc.DocumentElement;
            if (root.Name != "obfuz")
            {
                throw new Exception($"Invalid xml file {configFile}, root name should be 'obfuz'");
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "assembly":
                    {
                        AssemblySpec assSpec = ParseAssembly(ele);
                        _assemblySpecs.Add(assSpec);
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
                    }
                }
            }
        }

        (bool, ObfuscationPassType) ParseObfuscationType(string obfuscationPassTypesStr)
        {
            bool delta = false;
            if (obfuscationPassTypesStr[0] == '+' || obfuscationPassTypesStr[0] == '-')
            {
                delta = true;
                obfuscationPassTypesStr = obfuscationPassTypesStr.Substring(1);
            }
            ObfuscationPassType passType = ObfuscationPassType.None;
            foreach (var passName in obfuscationPassTypesStr.Split('|'))
            {
                if (Enum.TryParse<ObfuscationPassType>(passName, out var pass))
                {
                    passType |= pass;
                }
                else
                {
                    throw new Exception($"Invalid xml file {_curLoadingConfig}, unknown pass type {passName}");
                }
            }
            return (delta, passType);
        }

        private PassRule ParseRule(XmlElement ele)
        {
            var r = new PassRule();
            if (ele.HasAttribute("enable"))
            {
                string enablePassStr = ele.GetAttribute("enable");
                if (string.IsNullOrEmpty(enablePassStr))
                {
                    throw new Exception($"Invalid xml file {_curLoadingConfig}, enable attribute is empty");
                }
                var (delta, passType) = ParseObfuscationType(enablePassStr);
                if (delta)
                {
                    r.addPasses = passType;
                }
                else
                {
                    r.enablePasses = passType;
                }
            }
            if (ele.HasAttribute("disable"))
            {
                string disablePassStr = ele.GetAttribute("disable");
                if (string.IsNullOrEmpty(disablePassStr))
                {
                    throw new Exception($"Invalid xml file {_curLoadingConfig}, disable attribute is empty");
                }
                var (delta, passType) = ParseObfuscationType(disablePassStr);
                if (delta)
                {
                    r.removePasses = passType;
                }
                else
                {
                    r.disablePasses = passType;
                }
            }
            if (r.enablePasses != null && (r.disablePasses != null || r.addPasses != null || r.removePasses != null))
            {
                throw new Exception($"Invalid xml file {_curLoadingConfig}, enable and disable can't be used together");
            }
            if (r.disablePasses != null && (r.enablePasses != null || r.addPasses != null || r.removePasses != null))
            {
                throw new Exception($"Invalid xml file {_curLoadingConfig}, disable and enable can't be used together");
            }
            return r;
        }

        private AssemblySpec ParseAssembly(XmlElement ele)
        {
            var assemblySpec = new AssemblySpec();
            string name = ele.GetAttribute("name");
            if (!_toObfuscatedAssemblyNames.Contains(name))
            {
                throw new Exception($"Invalid xml file {_curLoadingConfig}, assembly name {name} isn't in toObfuscatedAssemblyNames");
            }
            assemblySpec.name = name;
            assemblySpec.nameMatcher = new NameMatcher(name);
            assemblySpec.rule = ParseRule(ele);


            var types = assemblySpec.types;
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement childEle))
                {
                    continue;
                }
                switch (childEle.Name)
                {
                    case "type":
                    {
                        types.Add(ParseType(childEle));
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                    }
                }
            }
            return assemblySpec;
        }

        private TypeSpec ParseType(XmlElement element)
        {
            var typeSpec = new TypeSpec();

            string name = element.GetAttribute("name");
            typeSpec.name = name;
            typeSpec.nameMatcher = new NameMatcher(name);
            typeSpec.rule = ParseRule(element);

            List<FieldSpec> fields = typeSpec.fields;
            List<MethodSpec> methods = typeSpec.methods;
            List<PropertySpec> properties = typeSpec.properties;
            List<EventSpec> events = typeSpec.events;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "field":
                    {
                        fields.Add(ParseField(ele));
                        break;
                    }
                    case "method":
                    {
                        methods.Add(ParseMethod(ele));
                        break;
                    }
                    case "property":
                    {
                        properties.Add(ParseProperty(ele));
                        break;
                    }
                    case "event":
                    {
                        events.Add(ParseEvent(ele));
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                    }
                }
            }
            return typeSpec;
        }

        private void ParseSpecObject(XmlElement element, SpecBase obj)
        {
            string name = element.GetAttribute("name");
            obj.name = name;
            obj.nameMatcher = new NameMatcher(name);
            obj.rule = ParseRule(element);
        }

        private FieldSpec ParseField(XmlElement element)
        {
            var fieldSpec = new FieldSpec();
            ParseSpecObject(element, fieldSpec);
            return fieldSpec;
        }

        private MethodSpec ParseMethod(XmlElement element)
        {
            var methodSpec = new MethodSpec();
            ParseSpecObject(element, methodSpec);
            return methodSpec;
        }

        private PropertySpec ParseProperty(XmlElement element)
        {
            var propertySpec = new PropertySpec();
            ParseSpecObject(element, propertySpec);
            return propertySpec;
        }

        private EventSpec ParseEvent(XmlElement element)
        {
            var eventSpec = new EventSpec();
            ParseSpecObject(element, eventSpec);
            return eventSpec;
        }

        private readonly Dictionary<ModuleDef, (AssemblySpec, PassRule)> _modulePassRuleCaches = new Dictionary<ModuleDef, (AssemblySpec, PassRule)>();
        private readonly Dictionary<TypeDef, (TypeSpec, PassRule)> _typePassRuleCaches = new Dictionary<TypeDef, (TypeSpec, PassRule)>();
        private readonly Dictionary<MethodDef, (MethodSpec, PassRule)> _methodPassRuleCaches = new Dictionary<MethodDef, (MethodSpec, PassRule)>();
        private readonly Dictionary<FieldDef, (FieldSpec, PassRule)> _fieldPassRuleCaches = new Dictionary<FieldDef, (FieldSpec, PassRule)>();
        private readonly Dictionary<PropertyDef, (PropertySpec, PassRule)> _propertyPassRuleCaches = new Dictionary<PropertyDef, (PropertySpec, PassRule)>();
        private readonly Dictionary<EventDef, (EventSpec, PassRule)> _eventPassRuleCaches = new Dictionary<EventDef, (EventSpec, PassRule)>();


        private (AssemblySpec, PassRule) GetAssemblySpec(ModuleDef module)
        {
            if (!_modulePassRuleCaches.TryGetValue(module, out var result))
            {
                result = (null, _defaultPassRule);
                string assName = module.Assembly.Name;
                foreach (var ass in _assemblySpecs)
                {
                    if (ass.nameMatcher.IsMatch(assName))
                    {
                        result = (ass, ass.rule);
                        break;
                    }
                }
                _modulePassRuleCaches.Add(module, result);
            }
            return result;
        }

        private (TypeSpec, PassRule) GetTypeSpec(TypeDef type)
        {
            if (!_typePassRuleCaches.TryGetValue(type, out var result))
            {
                var assResult = GetAssemblySpec(type.Module);
                result = (null, assResult.Item2);
                if (assResult.Item1 != null)
                {
                    string typeName = type.FullName;
                    foreach (var typeSpec in assResult.Item1.types)
                    {
                        if (typeSpec.nameMatcher.IsMatch(typeName))
                        {
                            result = (typeSpec, typeSpec.rule);
                            break;
                        }
                    }
                }
                _typePassRuleCaches.Add(type, result);
            }
            return result;
        }

        private (MethodSpec, PassRule) GetMethodSpec(MethodDef method)
        {
            if (!_methodPassRuleCaches.TryGetValue(method, out var result))
            {
                var typeResult = GetTypeSpec(method.DeclaringType);
                result = (null, typeResult.Item2);
                if (typeResult.Item1 != null)
                {
                    string methodName = method.Name;
                    foreach (var methodSpec in typeResult.Item1.methods)
                    {
                        if (methodSpec.nameMatcher.IsMatch(methodName))
                        {
                            result = (methodSpec, methodSpec.rule);
                            break;
                        }
                    }
                }
                _methodPassRuleCaches.Add(method, result);
            }
            return result;
        }

        private (FieldSpec, PassRule) GetFieldSpec(FieldDef field)
        {
            if (!_fieldPassRuleCaches.TryGetValue(field, out var result))
            {
                var typeResult = GetTypeSpec(field.DeclaringType);
                result = (null, typeResult.Item2);
                if (typeResult.Item1 != null)
                {
                    string fieldName = field.Name;
                    foreach (var fieldSpec in typeResult.Item1.fields)
                    {
                        if (fieldSpec.nameMatcher.IsMatch(fieldName))
                        {
                            result = (fieldSpec, fieldSpec.rule);
                            break;
                        }
                    }
                }
                _fieldPassRuleCaches.Add(field, result);
            }
            return result;
        }

        private (PropertySpec, PassRule) GetPropertySpec(PropertyDef property)
        {
            if (!_propertyPassRuleCaches.TryGetValue(property, out var result))
            {
                var typeResult = GetTypeSpec(property.DeclaringType);
                result = (null, typeResult.Item2);
                if (typeResult.Item1 != null)
                {
                    string propertyName = property.Name;
                    foreach (var propertySpec in typeResult.Item1.properties)
                    {
                        if (propertySpec.nameMatcher.IsMatch(propertyName))
                        {
                            result = (propertySpec, propertySpec.rule);
                            break;
                        }
                    }
                }
                _propertyPassRuleCaches.Add(property, result);
            }
            return result;
        }

        private (EventSpec, PassRule) GetEventSpec(EventDef eventDef)
        {
            if (!_eventPassRuleCaches.TryGetValue(eventDef, out var result))
            {
                var typeResult = GetTypeSpec(eventDef.DeclaringType);
                result = (null, typeResult.Item2);
                if (typeResult.Item1 != null)
                {
                    string eventName = eventDef.Name;
                    foreach (var eventSpec in typeResult.Item1.events)
                    {
                        if (eventSpec.nameMatcher.IsMatch(eventName))
                        {
                            result = (eventSpec, eventSpec.rule);
                            break;
                        }
                    }
                }
                _eventPassRuleCaches.Add(eventDef, result);
            }
            return result;
        }


        public ObfuscationPassType GetAssemblyObfuscationPasses(ModuleDef module)
        {
            return GetAssemblySpec(module).Item2.finalPasses;
        }

        public ObfuscationPassType GetTypeObfuscationPasses(TypeDef type)
        {
            return GetTypeSpec(type).Item2.finalPasses;
        }

        public ObfuscationPassType GetMethodObfuscationPasses(MethodDef method)
        {
            return GetMethodSpec(method).Item2.finalPasses;
        }

        public ObfuscationPassType GetFieldObfuscationPasses(FieldDef field)
        {
            return GetFieldSpec(field).Item2.finalPasses;
        }

        public ObfuscationPassType GetPropertyObfuscationPasses(PropertyDef property)
        {
            return GetPropertySpec(property).Item2.finalPasses;
        }

        public ObfuscationPassType GetEventObfuscationPasses(EventDef eventDef)
        {
            return GetEventSpec(eventDef).Item2.finalPasses;
        }
    }
}
