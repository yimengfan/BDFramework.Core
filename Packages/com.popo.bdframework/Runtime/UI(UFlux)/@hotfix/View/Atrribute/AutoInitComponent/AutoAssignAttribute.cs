using System;
using System.Reflection;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 自动初始化Component属性基类
    /// </summary>
   abstract public class AutoAssignAttribute : Attribute
    {
        virtual public void AutoSetField(IComponent winComponent, FieldInfo fieldInfo)
        {
            
        }

        virtual public void AutoSetProperty(IComponent winComponent, PropertyInfo propertyInfo)
        {
            
        }

        virtual public void AutoSetMethod(IComponent com, MethodInfo methodInfo)
        {
            
        }
    }
}