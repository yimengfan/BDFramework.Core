using System;
using System.Collections;
using System.Reflection;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 继承于AComponent的自动注册
    /// </summary>
    public class UfluxComponentPathAttribute : AutoAssignAttribute
    {
        private readonly string path;

        public UfluxComponentPathAttribute(string path)
        {
            this.path = path;
        }

        public override void AutoSetField(IComponent winComponent, FieldInfo fieldInfo)
        {
            if (!winComponent.Transform)
            {
                BDebug.Log($"component的Transform为null:{winComponent.GetType().FullName}");
            }

            var transform = winComponent.Transform.Find(this.path);

            if (!transform)
            {
                throw new Exception($"ComponentPath-> 传入Transform 不存在:{this.path}");
                return;
            }

            var fieldType = fieldInfo.FieldType;
            if (!fieldType.IsGenericType)
            {
                var instance = (IComponent) Activator.CreateInstance(fieldType, new object[] {transform});
                fieldInfo.SetValue(winComponent, instance);
                instance.Init();

                if (winComponent is IWindow window)
                {
                    window.AddComponent(instance);
                }
            }
            else
            {
                var list = (IList) HotfixAssembliesHelper.CreateHotfixInstance(fieldType);
                fieldInfo.SetValue(winComponent, list);

                // 泛型T类型
                Type argType = null;
                // if (fieldInfo is ILRuntimeFieldInfo runtimeFieldInfo)
                // {
                //     var value = runtimeFieldInfo.ILFieldType.GenericArguments[0].Value;
                //     argType = value.ReflectionType;
                // }
                // else
                {
                    argType = fieldType.GenericTypeArguments[0];
                }

                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] {childTrans});
                    instance.Init();
                    list.Add(instance);

                    if (winComponent is IWindow window)
                    {
                        window.AddComponent(instance);
                    }
                }
            }
        }

        /// <summary>
        /// 自动设置属性
        /// </summary>
        /// <param name="winComponent"></param>
        /// <param name="propertyInfo"></param>
        /// <exception cref="Exception"></exception>
        public override void AutoSetProperty(IComponent winComponent, PropertyInfo propertyInfo)
        {
            if (!winComponent.Transform)
            {
                BDebug.Log($"component的Transform为null:{winComponent.GetType().FullName}");
            }

            var transform = winComponent.Transform.Find(this.path);

            if (!transform)
            {
                throw new Exception($"ComponentPath-> 传入Transform 不存在:{this.path}");
                return;
            }

            var fieldType = propertyInfo.PropertyType;
            if (!fieldType.IsGenericType)
            {
                var instance = (IComponent) Activator.CreateInstance(fieldType, new object[] {transform});
                propertyInfo.SetValue(winComponent, instance);
                instance.Init();

                if (winComponent is IWindow window)
                {
                    window.AddComponent(instance);
                }
            }
            else
            {
                var list = (IList) HotfixAssembliesHelper.CreateHotfixInstance(fieldType);
                propertyInfo.SetValue(winComponent, list);

                // 泛型T类型
                Type argType = null;
                argType = fieldType.GenericTypeArguments[0];
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    var instance = (IComponent) Activator.CreateInstance(argType, new object[] {childTrans});
                    instance.Init();
                    list.Add(instance);

                    if (winComponent is IWindow window)
                    {
                        window.AddComponent(instance);
                    }
                }
            }
        }
    }
}
