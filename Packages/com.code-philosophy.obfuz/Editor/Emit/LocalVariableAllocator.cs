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
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    class ScopeLocalVariables : IDisposable
    {
        private readonly LocalVariableAllocator _localVariableAllocator;

        private readonly List<Local> _allocatedVars = new List<Local>();

        public IReadOnlyList<Local> AllocatedLocals => _allocatedVars;


        public ScopeLocalVariables(LocalVariableAllocator localVariableAllocator)
        {
            _localVariableAllocator = localVariableAllocator;
        }

        public Local AllocateLocal(TypeSig type)
        {
            var local = _localVariableAllocator.AllocateLocal(type);
            _allocatedVars.Add(local);
            return local;
        }

        public void Dispose()
        {
            foreach (var local in _allocatedVars)
            {
                _localVariableAllocator.ReturnLocal(local);
            }
        }
    }

    class LocalVariableAllocator
    {
        private readonly MethodDef _method;
        private readonly List<Local> _freeLocals = new List<Local>();

        public LocalVariableAllocator(MethodDef method)
        {
            _method = method;
        }

        public Local AllocateLocal(TypeSig type)
        {
            foreach (var local in _freeLocals)
            {
                if (TypeEqualityComparer.Instance.Equals(local.Type, type))
                {
                    _freeLocals.Remove(local);
                    return local;
                }
            }
            var newLocal = new Local(type);
            // _freeLocals.Add(newLocal);
            _method.Body.Variables.Add(newLocal);
            return newLocal;
        }

        public void ReturnLocal(Local local)
        {
            _freeLocals.Add(local);
        }

        public ScopeLocalVariables CreateScope()
        {
            return new ScopeLocalVariables(this);
        }
    }
}
