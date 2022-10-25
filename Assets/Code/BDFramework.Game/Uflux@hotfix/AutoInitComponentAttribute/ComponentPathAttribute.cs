using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Reflection;
using ILRuntime.Runtime.Intepreter;
using UnityEngine;

namespace BDFramework.UFlux
{
    public class ComponentPathAttribute : AutoInitComponentAttribute
    {
        private readonly string path;

        public ComponentPathAttribute(string path)
        {
            this.path = path;
        }

        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            var transform = com.Transform.Find(this.path);
            
            var fieldType = fieldInfo.FieldType;
            if (!fieldType.IsGenericType)
            {
                var instance = (IComponent) Activator.CreateInstance(fieldType, new object[] {transform});
                fieldInfo.SetValue(com, instance);
                instance.Init();
            }
            else
            {
                var list = (IList) ILRuntimeHelper.CreateInstance(fieldType);
                fieldInfo.SetValue(com, list);

                // 泛型T类型
                Type argType = null;
                if (fieldInfo is ILRuntimeFieldInfo runtimeFieldInfo)
                {
                    var value = runtimeFieldInfo.ILFieldType.GenericArguments[0].Value;
                    argType = value.ReflectionType;
                }
                else
                {
                    argType = fieldType.GenericTypeArguments[0];
                }

                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] { childTrans });
                    instance.Init();
                    list.Add(instance);
                }
            }
        }
    }
}