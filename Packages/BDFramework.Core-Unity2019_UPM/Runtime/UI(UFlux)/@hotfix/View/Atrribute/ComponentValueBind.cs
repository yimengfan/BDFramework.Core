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
        public string FieldNameName { get;  private set; }
        public Type Type;
        /// <summary>
        /// 构造函数
        /// 热更Attr不支持基础类型以外
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="fieldName"></param>
        public ComponentValueBind(string typeName, string fieldName)
        {
            this.FieldNameName = fieldName;
            if (!ILRuntimeHelper.UIComponentTypes.TryGetValue(typeName, out Type))
            {
                BDebug.LogError("【Uflux】type is null:" +typeName);
            }
            

        }
    }
}