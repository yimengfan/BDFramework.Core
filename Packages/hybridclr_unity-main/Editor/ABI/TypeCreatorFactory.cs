using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ABI
{
    public static class TypeCreatorFactory
    {
        public static TypeCreatorBase CreateTypeCreator(PlatformABI abi)
        {
            switch(abi)
            {
                case PlatformABI.Arm64: return new TypeCreatorArm64();
                case PlatformABI.Universal32: return new TypeCreatorUniversal32();
                case PlatformABI.Universal64: return new TypeCreatorUniversal64();
                case PlatformABI.WebGL32: return new TypeCreatorWebGL32();
                default: throw new NotSupportedException(abi.ToString());
            }
        }
    }
}
