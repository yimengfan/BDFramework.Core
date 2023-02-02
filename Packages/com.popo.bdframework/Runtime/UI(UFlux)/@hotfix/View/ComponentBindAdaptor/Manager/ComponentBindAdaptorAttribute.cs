using System;
using BDFramework.Mgr;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Mono.Cecil;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 组件绑定的处理逻辑
    /// </summary>
    public class ComponentBindAdaptorAttribute : ManagerAttribute
    {
        /// <summary>
        /// 绑定组件的类型
        /// </summary>
        public Type BindType;

        public ComponentBindAdaptorAttribute(Type bindType) : base(bindType.GetHashCode())
        {
            //这里故意让破坏优化 ilrbug
            var ot = (object) bindType;
            if (ot is TypeReference)
            {
                var name = ((TypeReference) ot).FullName;
                if(!ILRuntimeHelper.UIComponentTypes.TryGetValue(name, out BindType))
                {
                    IType ilrtype = null;
                    if (ILRuntimeHelper.AppDomain.LoadedTypes.TryGetValue(name, out ilrtype))
                    {
                        this.BindType = ilrtype.ReflectionType;
                    }
                    else if(Application.isPlaying)
                    {
                        BDebug.LogError("【UFlux】不存在ComponentBindAdaptor:" + name);
                    }
                }
            }
            else
            {
                this.BindType = bindType;
            }
        }
    }
}