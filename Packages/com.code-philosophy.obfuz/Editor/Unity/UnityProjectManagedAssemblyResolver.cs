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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Obfuz.Unity
{
    public class UnityProjectManagedAssemblyResolver : AssemblyResolverBase
    {
        private readonly Dictionary<string, string> _managedAssemblyNameToPaths = new Dictionary<string, string>();

        public UnityProjectManagedAssemblyResolver(BuildTarget target)
        {
            string[] dllGuids = AssetDatabase.FindAssets("t:DefaultAsset");
            var dllPaths = dllGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(f => f.EndsWith(".dll"))
                .Where(dllPath =>
                {
                    PluginImporter importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;
                    if (importer == null || importer.isNativePlugin)
                    {
                        return false;
                    }
                    if (!importer.GetCompatibleWithAnyPlatform() && !importer.GetCompatibleWithPlatform(target))
                    {
                        return false;
                    }
                    return true;
                }).ToArray();

            foreach (string dllPath in dllPaths)
            {
                Debug.Log($"UnityProjectManagedAssemblyResolver find managed dll:{dllPath}");
                string assName = Path.GetFileNameWithoutExtension(dllPath);
                if (_managedAssemblyNameToPaths.TryGetValue(assName, out var existAssPath))
                {
                    Debug.LogWarning($"UnityProjectManagedAssemblyResolver find duplicate assembly1:{existAssPath} assembly2:{dllPath}");
                }
                else
                {
                    _managedAssemblyNameToPaths.Add(Path.GetFileNameWithoutExtension(dllPath), dllPath);
                }
            }
        }

        public override string ResolveAssembly(string assemblyName)
        {
            if (_managedAssemblyNameToPaths.TryGetValue(assemblyName, out string assemblyPath))
            {
                return assemblyPath;
            }
            return null;
        }
    }
}
