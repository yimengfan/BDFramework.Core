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
using System.Reflection;

namespace Obfuz
{
    public static class ObfuscationTypeMapper
    {
        private static readonly Dictionary<Type, string> _type2OriginalFullName = new Dictionary<Type, string>();
        private static readonly Dictionary<Assembly, Dictionary<string, Type>> _originalFullName2Types = new Dictionary<Assembly, Dictionary<string, Type>>();

        public static void RegisterType<T>(string originalFullName)
        {
            RegisterType(typeof(T), originalFullName);
        }

        public static void RegisterType(Type type, string originalFullName)
        {
            if (_type2OriginalFullName.ContainsKey(type))
            {
                throw new ArgumentException($"Type '{type.FullName}' is already registered with original name '{_type2OriginalFullName[type]}'.");
            }
            _type2OriginalFullName.Add(type, originalFullName);
            Assembly assembly = type.Assembly;
            if (!_originalFullName2Types.TryGetValue(assembly, out var originalFullName2Types))
            {
                originalFullName2Types = new Dictionary<string, Type>();
                _originalFullName2Types[assembly] = originalFullName2Types;
            }
            if (originalFullName2Types.ContainsKey(originalFullName))
            {
                throw new ArgumentException($"Original full name '{originalFullName}' is already registered with type '{originalFullName2Types[originalFullName].FullName}'.");
            }
            originalFullName2Types.Add(originalFullName, type);
        }

        public static string GetOriginalTypeFullName(Type type)
        {
            return _type2OriginalFullName.TryGetValue(type, out string originalFullName)
                ? originalFullName
                : throw new KeyNotFoundException($"Type '{type.FullName}' not found in the obfuscation mapping.");
        }

        public static string GetOriginalTypeFullNameOrCurrent(Type type)
        {
            if (_type2OriginalFullName.TryGetValue(type, out string originalFullName))
            {
                return originalFullName;
            }
            return type.FullName;
        }

        public static Type GetTypeByOriginalFullName(Assembly assembly, string originalFullName)
        {
            if (_originalFullName2Types.TryGetValue(assembly, out var n2t))
            {
                if (n2t.TryGetValue(originalFullName, out Type type))
                {
                    return type;
                }
            }
            return null;
        }

        public static void Clear()
        {
            _type2OriginalFullName.Clear();
            _originalFullName2Types.Clear();
        }
    }

}
