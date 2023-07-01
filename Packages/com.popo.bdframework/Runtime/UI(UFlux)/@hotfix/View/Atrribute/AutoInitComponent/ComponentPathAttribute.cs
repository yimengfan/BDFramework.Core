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
    /// <summary>
    /// 继承于Acomponent的自动注册
    /// </summary>
    public class ComponentPathAttribute : AutoInitComponentAttribute
    {
        private readonly string path;

        public ComponentPathAttribute(string path)
        {
            this.path = path;
        }

        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            if (!com.Transform)
            {
                BDebug.Log($"component的Transform为null:{com.GetType().FullName}");
            }

            var transform = com.Transform.Find(this.path);

            if (!transform)
            {
                throw new Exception($"ComponentPath-> 传入Transform 不存在:{this.path}");
                return;
            }

            var fieldType = fieldInfo.FieldType;
            if (!fieldType.IsGenericType)
            {
                var instance = (IComponent) Activator.CreateInstance(fieldType, new object[] {transform});
                fieldInfo.SetValue(com, instance);
                instance.Init();

                if (com is IWindow window)
                {
                    window.AddComponent(instance);
                }
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
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] {childTrans});
                    instance.Init();
                    list.Add(instance);

                    if (com is IWindow window)
                    {
                        window.AddComponent(instance);
                    }
                }
            }
        }

        /// <summary>
        /// 自动设置属性
        /// </summary>
        /// <param name="com"></param>
        /// <param name="propertyInfo"></param>
        /// <exception cref="Exception"></exception>
        public override void AutoSetProperty(IComponent com, PropertyInfo propertyInfo)
        {
            if (!com.Transform)
            {
                BDebug.Log($"component的Transform为null:{com.GetType().FullName}");
            }

            var transform = com.Transform.Find(this.path);

            if (!transform)
            {
                throw new Exception($"ComponentPath-> 传入Transform 不存在:{this.path}");
                return;
            }

            var fieldType = propertyInfo.PropertyType;
            if (!fieldType.IsGenericType)
            {
                var instance = (IComponent) Activator.CreateInstance(fieldType, new object[] {transform});
                propertyInfo.SetValue(com, instance);
                instance.Init();

                if (com is IWindow window)
                {
                    window.AddComponent(instance);
                }
            }
            else
            {
                var list = (IList) ILRuntimeHelper.CreateInstance(fieldType);
                propertyInfo.SetValue(com, list);

                // 泛型T类型
                Type argType = null;
                argType = fieldType.GenericTypeArguments[0];
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] {childTrans});
                    instance.Init();
                    list.Add(instance);

                    if (com is IWindow window)
                    {
                        window.AddComponent(instance);
                    }
                }
            }
        }
    }
}
