using System;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Mono.Cecil;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 描述成员变量 是给哪个组件 哪个字段赋值
    /// </summary>
    public class ComponentValueBind : Attribute
    {
        public Type   Type;
        public string FieldName;

        public ComponentValueBind(Type type, string field)
        {
            //这里故意让破坏优化 ilrbug
            var ot = (object) type;
            if (ot is TypeReference)
            {
                var name = ((TypeReference) ot).FullName;
                if (!ILRuntimeHelper.UIComponentTypes.TryGetValue(name, out Type))
                {
                    IType ilrtype = null;
                    if (ILRuntimeHelper.AppDomain.LoadedTypes.TryGetValue(name, out ilrtype))
                    {
                        this.Type =  ilrtype.ReflectionType;
                    }
                }
                
                if (Type == null)
                {
                    BDebug.LogError("isnull:" +name);
                }
 
            }
            else
            {
                this.Type = type;
            }

            this.FieldName = field;
        }
    }
}