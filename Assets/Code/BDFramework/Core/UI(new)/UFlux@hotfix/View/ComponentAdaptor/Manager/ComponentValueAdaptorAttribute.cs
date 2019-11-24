using System;
using BDFramework.Mgr;
using Game.ILRuntime;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Mono.Cecil;
using UnityEngine;

namespace BDFramework.UFlux
{
    public class ComponentAdaptorProcessAttribute : ManagerAtrribute
    {
        public Type Type;

        public ComponentAdaptorProcessAttribute(Type type) : base(type.GetHashCode())
        {
            var ot = (object) type;
            if (ot is TypeReference)
            {
                var name = ((TypeReference) ot).FullName;
                if(!ILTypeHelper.UIComponentTypes.TryGetValue(name, out Type))
                {
                    IType ilrtype = null;
                    if (ILRuntimeHelper.AppDomain.LoadedTypes.TryGetValue(name, out ilrtype))
                    {
                        this.Type =  ilrtype.ReflectionType;
                    }
                }
            }
            else
            {
                this.Type = type;
            }
            
        }
    }
}