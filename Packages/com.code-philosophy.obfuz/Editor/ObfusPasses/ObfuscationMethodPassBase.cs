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
using System.Linq;

namespace Obfuz.ObfusPasses
{
    public abstract class ObfuscationMethodPassBase : ObfuscationPassBase
    {
        protected virtual bool ForceProcessAllAssembliesAndIgnoreAllPolicy => false;

        protected abstract bool NeedObfuscateMethod(MethodDef method);

        protected abstract void ObfuscateData(MethodDef method);

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            var modules = ForceProcessAllAssembliesAndIgnoreAllPolicy ? ctx.allObfuscationRelativeModules : ctx.modulesToObfuscate;
            ObfuscationMethodWhitelist whiteList = ctx.whiteList;
            ConfigurablePassPolicy passPolicy = ctx.passPolicy;
            foreach (ModuleDef mod in modules)
            {
                if (!ForceProcessAllAssembliesAndIgnoreAllPolicy && whiteList.IsInWhiteList(mod))
                {
                    continue;
                }
                // ToArray to avoid modify list exception
                foreach (TypeDef type in mod.GetTypes().ToArray())
                {
                    if (!ForceProcessAllAssembliesAndIgnoreAllPolicy && whiteList.IsInWhiteList(type))
                    {
                        continue;
                    }
                    // ToArray to avoid modify list exception
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!method.HasBody || (!ForceProcessAllAssembliesAndIgnoreAllPolicy && (ctx.whiteList.IsInWhiteList(method) || !Support(passPolicy.GetMethodObfuscationPasses(method)) || !NeedObfuscateMethod(method))))
                        {
                            continue;
                        }
                        // TODO if isGeneratedBy Obfuscator, continue
                        ObfuscateData(method);
                    }
                }
            }
        }
    }
}
