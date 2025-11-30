using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{

    public class GenericArgumentContext
    {
        private readonly List<TypeSig> typeArgsStack;
        private readonly List<TypeSig> methodArgsStack;

        public GenericArgumentContext(List<TypeSig> typeArgsStack, List<TypeSig> methodArgsStack)
        {
            this.typeArgsStack = typeArgsStack;
            this.methodArgsStack = methodArgsStack;
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
            return args[(int)number];
        }
    }

}
