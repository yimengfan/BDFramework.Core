using dnlib.DotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{
    public class MethodReferenceAnalyzer
    {
        private readonly Action<GenericMethod> _onNewMethod;

        private readonly ConcurrentDictionary<MethodDef, List<IMethod>> _methodEffectInsts = new ConcurrentDictionary<MethodDef, List<IMethod>>();

        public MethodReferenceAnalyzer(Action<GenericMethod> onNewMethod)
        {
            _onNewMethod = onNewMethod;
        }

        public void WalkMethod(MethodDef method, List<TypeSig> klassGenericInst, List<TypeSig> methodGenericInst)
        {
            if (klassGenericInst != null || methodGenericInst != null)
            {
                //var typeSig = klassGenericInst != null ? new GenericInstSig(method.DeclaringType.ToTypeSig().ToClassOrValueTypeSig(), klassGenericInst) : method.DeclaringType?.ToTypeSig();
                //Debug.Log($"== walk generic method {typeSig}::{method.Name} {method.MethodSig}");
            }
            else
            {
                //Debug.Log($"== walk not geneeric method:{method}");
            }
            var ctx = new GenericArgumentContext(klassGenericInst, methodGenericInst);

            if (_methodEffectInsts.TryGetValue(method, out var effectInsts))
            {
                foreach (var met in effectInsts)
                {
                    var resolveMet = GenericMethod.ResolveMethod(met, ctx)?.ToGenericShare();
                    _onNewMethod(resolveMet);
                }
                return;
            }

            var body = method.Body;
            if (body == null || !body.HasInstructions)
            {
                return;
            }

            effectInsts = new List<IMethod>();
            foreach (var inst in body.Instructions)
            {
                if (inst.Operand == null)
                {
                    continue;
                }
                switch (inst.Operand)
                {
                    case IMethod met:
                    {
                        if (!met.IsMethod)
                        {
                            continue;
                        }
                        var resolveMet = GenericMethod.ResolveMethod(met, ctx)?.ToGenericShare();
                        if (resolveMet == null)
                        {
                            continue;
                        }
                        effectInsts.Add(met);
                        _onNewMethod(resolveMet);
                        break;
                    }
                    case ITokenOperand token:
                    {
                        //GenericParamContext paramContext = method.HasGenericParameters || method.DeclaringType.HasGenericParameters ?
                        //            new GenericParamContext(method.DeclaringType, method) : default;
                        //method.Module.ResolveToken(token.MDToken, paramContext);
                        break;
                    }
                }
            }
            _methodEffectInsts.TryAdd(method, effectInsts);
        }
    }
}
