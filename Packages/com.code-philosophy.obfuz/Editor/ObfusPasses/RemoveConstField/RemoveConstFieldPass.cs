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

using dnlib.DotNet;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Linq;

namespace Obfuz.ObfusPasses.RemoveConstField
{

    public class RemoveConstFieldPass : ObfuscationPassBase
    {
        private RemoveConstFieldSettingsFacade _settings;
        private ObfuzIgnoreScopeComputeCache _obfuzIgnoreScopeComputeCache;
        private IRemoveConstFieldPolicy _removeConstFieldPolicy;

        public override ObfuscationPassType Type => ObfuscationPassType.RemoveConstField;

        public RemoveConstFieldPass(RemoveConstFieldSettingsFacade settings)
        {
            _settings = settings;
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _obfuzIgnoreScopeComputeCache = ctx.obfuzIgnoreScopeComputeCache;
            _removeConstFieldPolicy = new ConfigurableRemoveConstFieldPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        public override void Stop()
        {

        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            var modules = ctx.modulesToObfuscate;
            ConfigurablePassPolicy passPolicy = ctx.passPolicy;
            foreach (ModuleDef mod in modules)
            {
                // ToArray to avoid modify list exception
                foreach (TypeDef type in mod.GetTypes())
                {
                    if (type.IsEnum)
                    {
                        continue;
                    }
                    foreach (FieldDef field in type.Fields.ToArray())
                    {
                        if (!field.IsLiteral)
                        {
                            continue;
                        }
                        if (!Support(passPolicy.GetFieldObfuscationPasses(field)))
                        {
                            continue;
                        }
                        if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(field, field.DeclaringType, ObfuzScope.Field))
                        {
                            continue;
                        }
                        if (_removeConstFieldPolicy.NeedPreserved(field))
                        {
                            continue;
                        }
                        field.DeclaringType = null;
                        //Debug.Log($"Remove const field {field.FullName} in type {type.FullName} in module {mod.Name}");
                    }
                }
            }
        }
    }
}
