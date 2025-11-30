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
using Obfuz.Emit;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz
{
    public delegate IRandom RandomCreator(int seed);

    public class EncryptionScopeInfo
    {
        public const int MaxEncryptionLevel = 4;

        public readonly IEncryptor encryptor;
        public readonly RandomCreator localRandomCreator;

        public EncryptionScopeInfo(IEncryptor encryptor, RandomCreator localRandomCreator)
        {
            this.encryptor = encryptor;
            this.localRandomCreator = localRandomCreator;
        }
    }

    public class EncryptionScopeProvider
    {
        private readonly EncryptionScopeInfo _defaultStaticScope;
        private readonly EncryptionScopeInfo _defaultDynamicScope;
        private readonly HashSet<string> _dynamicSecretAssemblyNames;

        public EncryptionScopeProvider(EncryptionScopeInfo defaultStaticScope, EncryptionScopeInfo defaultDynamicScope, HashSet<string> dynamicSecretAssemblyNames)
        {
            _defaultStaticScope = defaultStaticScope;
            _defaultDynamicScope = defaultDynamicScope;
            _dynamicSecretAssemblyNames = dynamicSecretAssemblyNames;
        }

        public EncryptionScopeInfo GetScope(ModuleDef module)
        {
            if (_dynamicSecretAssemblyNames.Contains(module.Assembly.Name))
            {
                return _defaultDynamicScope;
            }
            else
            {
                return _defaultStaticScope;
            }
        }

        public bool IsDynamicSecretAssembly(ModuleDef module)
        {
            return _dynamicSecretAssemblyNames.Contains(module.Assembly.Name);
        }
    }

    public class ObfuscationPassContext
    {
        public static ObfuscationPassContext Current { get; set; }

        public CoreSettingsFacade coreSettings;

        public GroupByModuleEntityManager moduleEntityManager;

        public AssemblyCache assemblyCache;
        public List<ModuleDef> modulesToObfuscate;
        public List<ModuleDef> allObfuscationRelativeModules;
        public ObfuzIgnoreScopeComputeCache obfuzIgnoreScopeComputeCache;
        public BurstCompileComputeCache burstCompileComputeCache;

        public ObfuscationMethodWhitelist whiteList;
        public ConfigurablePassPolicy passPolicy;
    }
}
