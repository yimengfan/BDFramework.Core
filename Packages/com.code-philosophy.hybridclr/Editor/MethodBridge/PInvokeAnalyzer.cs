using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.MethodBridge
{

    public class PInvokeAnalyzer
    {
        private readonly List<ModuleDefMD> _rootModules = new List<ModuleDefMD>();

        private readonly List<CallNativeMethodSignatureInfo> _pinvokeMethodSignatures = new List<CallNativeMethodSignatureInfo>();

        public List<CallNativeMethodSignatureInfo> PInvokeMethodSignatures => _pinvokeMethodSignatures;

        public PInvokeAnalyzer(AssemblyCache cache, List<string> assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                _rootModules.Add(cache.LoadModule(assemblyName));
            }
        }

        private CallingConvention GetCallingConvention(MethodDef method)
        {
            switch (method.ImplMap.CallConv)
            {
                case PInvokeAttributes.CallConvWinapi: return CallingConvention.Default;
                case PInvokeAttributes.CallConvCdecl: return CallingConvention.C;
                case PInvokeAttributes.CallConvStdCall: return CallingConvention.StdCall;
                case PInvokeAttributes.CallConvThiscall: return CallingConvention.ThisCall;
                case PInvokeAttributes.CallConvFastcall: return CallingConvention.FastCall;
                default: return CallingConvention.Default;
            }
        }

        public void Run()
        {
            foreach (var mod in _rootModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.IsPinvokeImpl)
                        {
                            if (!MetaUtil.IsSupportedPInvokeMethodSignature(method.MethodSig))
                            {
                                Debug.LogError($"PInvoke method {method.FullName} has unsupported parameter or return type. Please check the method signature.");
                            }
                            _pinvokeMethodSignatures.Add(new CallNativeMethodSignatureInfo
                            {
                                MethodSig = method.MethodSig,
                                Callvention = method.HasImplMap? GetCallingConvention(method) : (CallingConvention?)null,
                            });
                        }
                    }
                }
            }
        }
    }
}
