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

﻿using System.Collections.Generic;
using System.Linq;

namespace Obfuz.Utils
{
    public class CombinedAssemblyResolver : AssemblyResolverBase
    {
        private readonly List<IAssemblyResolver> _resolvers;

        public CombinedAssemblyResolver(params IAssemblyResolver[] resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        public override string ResolveAssembly(string assemblyName)
        {
            foreach (var resolver in _resolvers)
            {
                var assemblyPath = resolver.ResolveAssembly(assemblyName);
                if (assemblyPath != null)
                {
                    return assemblyPath;
                }
            }
            return null;
        }

        public void InsertFirst(IAssemblyResolver resolver)
        {
            _resolvers.Insert(0, resolver);
        }
    }
}
