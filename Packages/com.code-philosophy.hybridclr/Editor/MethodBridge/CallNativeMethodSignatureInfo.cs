using dnlib.DotNet;

namespace HybridCLR.Editor.MethodBridge
{
    public class CallNativeMethodSignatureInfo
    {
        public MethodSig MethodSig { get; set; }

        public CallingConvention? Callvention { get; set; }
    }
}
