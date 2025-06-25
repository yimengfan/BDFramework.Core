using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{
    /// <summary>
    /// Replaces generic type/method var with its generic argument
    /// </summary>
    public sealed class GenericArgumentContext
    {
        List<TypeSig> typeArgsStack = new List<TypeSig>();
        List<TypeSig> methodArgsStack = new List<TypeSig>();

        public GenericArgumentContext(List<TypeSig> typeArgsStack, List<TypeSig> methodArgsStack)
        {
            this.typeArgsStack = typeArgsStack;
            this.methodArgsStack = methodArgsStack;
        }



        /// <summary>
        /// Replaces a generic type/method var with its generic argument (if any). If
        /// <paramref name="typeSig"/> isn't a generic type/method var or if it can't
        /// be resolved, it itself is returned. Else the resolved type is returned.
        /// </summary>
        /// <param name="typeSig">Type signature</param>
        /// <returns>New <see cref="TypeSig"/> which is never <c>null</c> unless
        /// <paramref name="typeSig"/> is <c>null</c></returns>
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
				case ElementType.ByRef: return new PtrSig(Resolve(typeSig.Next));

                case ElementType.SZArray: return new PtrSig(Resolve(typeSig.Next));
				case ElementType.Array:
                {
                    var ara = (ArraySig)typeSig;
                    return new ArraySig(Resolve(typeSig.Next), ara.Rank, ara.Sizes, ara.LowerBounds);
                }

				case ElementType.Var:
                {
                    GenericVar genericVar = (GenericVar)typeSig;
                    var newSig = Resolve(typeArgsStack, genericVar.Number, true);
                    if (newSig == null)
                    {
                        throw new Exception();
                    }
                    return newSig;
                }

				case ElementType.MVar:
                {
                    GenericMVar genericVar = (GenericMVar)typeSig;
                    var newSig = Resolve(methodArgsStack, genericVar.Number, true);
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
                    throw new NotSupportedException(typeSig.ToString());
                }

				case ElementType.ValueArray:
                {
                    var vas = (ValueArraySig)typeSig;
                    return new ValueArraySig(Resolve(vas.Next), vas.Size);
                }
                default: return typeSig;
			}
        }

        private TypeSig Resolve(List<TypeSig> args, uint number, bool isTypeVar)
        {
            var typeSig = args[(int)number];
            var gvar = typeSig as GenericSig;
            if (gvar is null || gvar.IsTypeVar != isTypeVar)
                return typeSig;
            return gvar;
        }
    }

}
