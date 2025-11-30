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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.Utils
{
    public sealed class GenericArgumentContext
    {
        public readonly List<TypeSig> typeArgsStack;
        public readonly List<TypeSig> methodArgsStack;

        public GenericArgumentContext(IList<TypeSig> typeArgsStack, IList<TypeSig> methodArgsStack)
        {
            this.typeArgsStack = typeArgsStack?.ToList();
            this.methodArgsStack = methodArgsStack?.ToList();
        }

        public TypeSig Resolve(TypeSig typeSig)
        {
            if (!typeSig.ContainsGenericParameter)
            {
                return typeSig;
            }
            typeSig = typeSig.RemovePinnedAndModifiers();
            switch (typeSig.ElementType)
            {
                case ElementType.Ptr: return new PtrSig(Resolve(typeSig.Next));
                case ElementType.ByRef: return new ByRefSig(Resolve(typeSig.Next));

                case ElementType.SZArray: return new SZArraySig(Resolve(typeSig.Next));
                case ElementType.Array:
                {
                    var ara = (ArraySig)typeSig;
                    return new ArraySig(Resolve(typeSig.Next), ara.Rank, ara.Sizes, ara.LowerBounds);
                }

                case ElementType.Var:
                {
                    GenericVar genericVar = (GenericVar)typeSig;
                    var newSig = Resolve(typeArgsStack, genericVar.Number);
                    if (newSig == null)
                    {
                        throw new Exception();
                    }
                    return newSig;
                }

                case ElementType.MVar:
                {
                    GenericMVar genericVar = (GenericMVar)typeSig;
                    var newSig = Resolve(methodArgsStack, genericVar.Number);
                    if (newSig == null)
                    {
                        throw new Exception();
                    }
                    return newSig;
                }
                case ElementType.GenericInst:
                {
                    var gia = (GenericInstSig)typeSig;
                    return new GenericInstSig(gia.GenericType, gia.GenericArguments.Select(ga => Resolve(ga)).ToList());
                }

                case ElementType.FnPtr:
                {
                    var fptr = (FnPtrSig)typeSig;
                    var cs = fptr.Signature;
                    CallingConventionSig ccs;
                    switch (cs)
                    {
                        case MethodSig ms:
                        {
                            ccs = new MethodSig(ms.GetCallingConvention(), ms.GenParamCount, Resolve(ms.RetType), ms.Params.Select(p => Resolve(p)).ToList());
                            break;
                        }
                        case PropertySig ps:
                        {
                            ccs = new PropertySig(ps.HasThis, Resolve(ps.RetType));
                            break;
                        }
                        case GenericInstMethodSig gims:
                        {
                            ccs = new GenericInstMethodSig(gims.GenericArguments.Select(ga => Resolve(ga)).ToArray());
                            break;
                        }
                        default: throw new NotSupportedException(cs.ToString());
                    }
                    return new FnPtrSig(ccs);
                }

                case ElementType.ValueArray:
                {
                    var vas = (ValueArraySig)typeSig;
                    return new ValueArraySig(Resolve(vas.Next), vas.Size);
                }
                default: return typeSig;
            }
        }

        private TypeSig Resolve(List<TypeSig> args, uint number)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }
            return args[(int)number];
        }
    }

}
