using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ABI
{
    public class HFATypeInfo
    {
        public TypeSig Type { get; set; }

        public int Count { get; set; }
    }

    public class TypeCreatorArm64 : TypeCreatorBase
    {
        public override bool IsArch32 => false;

        public override bool IsSupportHFA => true;

        protected override TypeInfo OptimizeSigType(TypeInfo type, bool returnType)
        {
            if (!type.IsGeneralValueType)
            {
                return type;
            }
            int typeSize = type.Size;
            if (typeSize <= 8)
            {
                return TypeInfo.s_i8;
            }
            if (typeSize <= 16)
            {
                return TypeInfo.s_i16;
            }
            if (returnType)
            {
                return type.PorType != ParamOrReturnType.STRUCTURE_ALIGN1 ? new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, typeSize) : type;
            }
            return TypeInfo.s_ref;
        }
    }
}
