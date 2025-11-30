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
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Obfuz.Conf
{
    public interface IRule<T>
    {
        void InheritParent(T parentRule);
    }


    public interface IMethodRule<R> where R : IRule<R>
    {
        string Name { get; set; }
        NameMatcher NameMatcher { get; set; }

        R Rule { get; set; }
    }

    public abstract class MethodRuleBase<R> : IMethodRule<R> where R : IRule<R>
    {
        public string Name { get; set; }
        public NameMatcher NameMatcher { get; set; }

        public R Rule { get; set; }
    }

    public interface ITypeRule<T, R> where T : IMethodRule<R> where R : IRule<R>
    {
        string Name { get; set; }

        NameMatcher NameMatcher { get; set; }

        R Rule { get; set; }

        List<T> Methods { get; set; }
    }

    public abstract class TypeRuleBase<T, R> : ITypeRule<T, R> where T : IMethodRule<R> where R : IRule<R>
    {
        public string Name { get; set; }

        public NameMatcher NameMatcher { get; set; }

        public R Rule { get; set; }

        public List<T> Methods { get; set; }
    }

    public interface IAssemblyRule<TType, TMethod, TRule> where TType : ITypeRule<TMethod, TRule> where TMethod : IMethodRule<TRule> where TRule : IRule<TRule>
    {
        string Name { get; set; }

        TRule Rule { get; set; }

        List<TType> Types { get; set; }
    }
    public abstract class AssemblyRuleBase<TType, TMethod, TRule> : IAssemblyRule<TType, TMethod, TRule> where TType : ITypeRule<TMethod, TRule> where TMethod : IMethodRule<TRule> where TRule : IRule<TRule>
    {
        public string Name { get; set; }

        public TRule Rule { get; set; }

        public List<TType> Types { get; set; }
    }

    public class XmlAssemblyTypeMethodRuleParser<TAssembly, TType, TMethod, TRule>
        where TMethod : IMethodRule<TRule>, new()
        where TType : ITypeRule<TMethod, TRule>, new()
        where TAssembly : IAssemblyRule<TType, TMethod, TRule>, new()
         where TRule : IRule<TRule>, new()
    {
        private readonly HashSet<string> _toObfuscatedAssemblyNames;
        private readonly Func<string, XmlElement, TRule> _ruleParser;
        private readonly Action<string, XmlElement> _unknownNodeTypeHandler;
        private readonly Dictionary<string, TAssembly> _assemblySpecs = new Dictionary<string, TAssembly>();

        public XmlAssemblyTypeMethodRuleParser(IEnumerable<string> toObfuscatedAssemblyNames, Func<string, XmlElement, TRule> ruleParser, Action<string, XmlElement> unknownNodeTypeHandler)
        {
            _toObfuscatedAssemblyNames = new HashSet<string>(toObfuscatedAssemblyNames);
            _ruleParser = ruleParser;
            _unknownNodeTypeHandler = unknownNodeTypeHandler;
        }

        public Dictionary<string, TAssembly> AssemblySpecs => _assemblySpecs;

        public void LoadConfigs(IEnumerable<string> configFiles)
        {
            foreach (var configFile in configFiles)
            {
                LoadConfig(configFile);
            }
        }

        public void LoadConfig(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new Exception($"Invalid xml file {configFile}, file name is empty");
            }
            Debug.Log($"ConfigurableObfuscationPolicy::LoadConfig {configFile}");
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
                        TAssembly assSpec = ParseAssembly(configFile, ele);
                        _assemblySpecs.Add(assSpec.Name, assSpec);
                        break;
                    }
                    default:
                    {
                        if (_unknownNodeTypeHandler == null)
                        {
                            throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
                        }
                        _unknownNodeTypeHandler(configFile, ele);
                        break;
                    }
                }
            }
        }

        private TAssembly ParseAssembly(string configFile, XmlElement ele)
        {
            var assemblySpec = new TAssembly();
            string name = ele.GetAttribute("name");
            if (!_toObfuscatedAssemblyNames.Contains(name))
            {
                throw new Exception($"Invalid xml file {configFile}, assembly name {name} isn't in toObfuscatedAssemblyNames");
            }
            if (_assemblySpecs.ContainsKey(name))
            {
                throw new Exception($"Invalid xml file {configFile}, assembly name {name} is duplicated");
            }
            assemblySpec.Name = name;
            assemblySpec.Rule = _ruleParser(configFile, ele);

            var types = new List<TType>();
            assemblySpec.Types = types;
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
                        types.Add(ParseType(configFile, childEle));
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

        private TType ParseType(string configFile, XmlElement element)
        {
            var typeSpec = new TType();

            string name = element.GetAttribute("name");
            typeSpec.Name = name;
            typeSpec.NameMatcher = new NameMatcher(name);
            typeSpec.Rule = _ruleParser(configFile, element);

            var methods = new List<TMethod>();
            typeSpec.Methods = methods;
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
                        methods.Add(ParseMethod(configFile, ele));
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

        private TMethod ParseMethod(string configFile, XmlElement element)
        {
            var methodSpec = new TMethod();
            string name = element.GetAttribute("name");
            methodSpec.Name = name;
            methodSpec.NameMatcher = new NameMatcher(name);
            methodSpec.Rule = _ruleParser(configFile, element);
            return methodSpec;
        }

        public TRule GetMethodRule(MethodDef method, TRule defaultRule)
        {
            var assemblyName = method.DeclaringType.Module.Assembly.Name;
            if (!_assemblySpecs.TryGetValue(assemblyName, out var assSpec))
            {
                return defaultRule;
            }
            string declaringTypeName = method.DeclaringType.FullName;
            foreach (var typeSpec in assSpec.Types)
            {
                if (typeSpec.NameMatcher.IsMatch(declaringTypeName))
                {
                    foreach (var methodSpec in typeSpec.Methods)
                    {
                        if (methodSpec.NameMatcher.IsMatch(method.Name))
                        {
                            return methodSpec.Rule;
                        }
                    }
                    return typeSpec.Rule;
                }
            }
            return assSpec.Rule;
        }

        public void InheritParentRules(TRule defaultRule)
        {
            foreach (TAssembly assSpec in _assemblySpecs.Values)
            {
                assSpec.Rule.InheritParent(defaultRule);
                foreach (TType typeSpec in assSpec.Types)
                {
                    typeSpec.Rule.InheritParent(assSpec.Rule);
                    foreach (TMethod methodSpec in typeSpec.Methods)
                    {
                        methodSpec.Rule.InheritParent(typeSpec.Rule);
                    }
                }
            }
        }
    }
}
