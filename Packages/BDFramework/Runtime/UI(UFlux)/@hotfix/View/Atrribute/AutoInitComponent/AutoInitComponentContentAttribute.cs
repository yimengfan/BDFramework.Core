using System;
using System.Reflection;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 自动初始化Component属性街垒
    /// </summary>
    public class AutoInitComponentContentAttribute : Attribute
    {
        virtual public void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            
        }

        virtual public void AutoSetProperty(IComponent com, PropertyInfo propertyInfo)
        {
            
        }

        virtual public void AutoSetMethod(IComponent com, MethodInfo methodInfo)
        {
            
        }
    }
}