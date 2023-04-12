using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ABI
{
    public class TypeCreatorUniversal32 : TypeCreatorBase
    {
        public override bool IsArch32 => true;

        public override bool IsSupportHFA => false;

        protected override TypeInfo OptimizeSigType(TypeInfo type, bool returnType)
        {
            if (type.PorType > ParamOrReturnType.STRUCTURE_ALIGN1 && type.PorType <= ParamOrReturnType.STRUCTURE_ALIGN4)
            {
                return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, type.Size);
            }
            return type;
        }
    }
}
