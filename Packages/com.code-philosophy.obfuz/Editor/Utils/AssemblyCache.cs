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
using System.Collections.Generic;
using System.IO;

namespace Obfuz.Utils
{

    public class AssemblyCache
    {
        private readonly IAssemblyResolver _assemblyPathResolver;
        private readonly ModuleContext _modCtx;
        private readonly AssemblyResolver _asmResolver;
        private bool _enableTypeDefCache;


        public ModuleContext ModCtx => _modCtx;

        public Dictionary<string, ModuleDefMD> LoadedModules { get; } = new Dictionary<string, ModuleDefMD>();

        public AssemblyCache(IAssemblyResolver assemblyResolver)
        {
            _enableTypeDefCache = true;
            _assemblyPathResolver = assemblyResolver;
            _modCtx = ModuleDef.CreateModuleContext();
            _asmResolver = (AssemblyResolver)_modCtx.AssemblyResolver;
            _asmResolver.EnableTypeDefCache = _enableTypeDefCache;
            _asmResolver.UseGAC = false;
        }

        public bool EnableTypeDefCache
        {
            get => _enableTypeDefCache;
            set
            {
                _enableTypeDefCache = value;
                _asmResolver.EnableTypeDefCache = value;
                foreach (var mod in LoadedModules.Values)
                {
                    mod.EnableTypeDefFindCache = value;
                }
            }
        }


        public ModuleDefMD TryLoadModule(string moduleName)
        {
            string dllPath = _assemblyPathResolver.ResolveAssembly(moduleName);
            if (string.IsNullOrEmpty(dllPath))
            {
                return null;
            }
            return LoadModule(moduleName);
        }

        public ModuleDefMD LoadModule(string moduleName)
        {
            // Debug.Log($"load module:{moduleName}");
            if (LoadedModules.TryGetValue(moduleName, out var mod))
            {
                return mod;
            }
            string assemblyPath = _assemblyPathResolver.ResolveAssembly(moduleName);
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new FileNotFoundException($"Assembly {moduleName} not found");
            }
            mod = DoLoadModule(assemblyPath);
            LoadedModules.Add(moduleName, mod);


            foreach (var refAsm in mod.GetAssemblyRefs())
            {
                LoadModule(refAsm.Name);
            }

            return mod;
        }

        private ModuleDefMD DoLoadModule(string dllPath)
        {
            //Debug.Log($"do load module:{dllPath}");
            ModuleDefMD mod = ModuleDefMD.Load(File.ReadAllBytes(dllPath), _modCtx);
            mod.EnableTypeDefFindCache = _enableTypeDefCache;
            _asmResolver.AddToCache(mod);
            return mod;
        }
    }
}
