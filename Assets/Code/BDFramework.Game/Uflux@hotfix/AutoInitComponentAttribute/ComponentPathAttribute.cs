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
        public  string TansformPath { get; private set; }

        public ComponentPathAttribute(string path)
        {
            this.TansformPath = path;
        }

        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            var transform = com.Transform.Find(this.TansformPath);

            if (!transform)
            {
                throw new Exception($"不存在节点:{this.TansformPath}");
            }
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
                Type argType = fieldType.GenericTypeArguments[0];
                

                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] {childTrans});
                    instance.Init();
                    list.Add(instance);
                }
            }
        }

        public override void AutoSetProperty(IComponent com, PropertyInfo propertyInfo)
        {
            var transform = com.Transform.Find(this.TansformPath);

            if (!transform)
            {
                throw new Exception($"不存在节点:{this.TansformPath}");
            }
            var fieldType = propertyInfo.PropertyType;
            if (!fieldType.IsGenericType)
            {
                var instance = (IComponent) Activator.CreateInstance(fieldType, new object[] {transform});
                propertyInfo.SetValue(com, instance);
                instance.Init();
            }
            else
            {
                var list = (IList) ILRuntimeHelper.CreateInstance(fieldType);
                propertyInfo.SetValue(com, list);

                // 泛型T类型
                Type argType = fieldType.GenericTypeArguments[0];
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] {childTrans});
                    instance.Init();
                    list.Add(instance);
                }
            }
        }
    }
}
