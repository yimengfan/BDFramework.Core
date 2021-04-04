using System;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Mono.Cecil;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 描述成员变量 是给哪个组件 哪个字段赋值
    /// </summary>
    public class ComponentValueBindAttribute : Attribute
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string FieldName { get;  private set; }
        /// <summary>
        /// 绑定UI类
        /// </summary>
        public Type UIType;
        /// <summary>
        /// 构造函数
        /// 热更Attr不支持基础类型以外
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="fieldName"></param>
        public ComponentValueBindAttribute(string typeName, string fieldName)
        {
            this.FieldName = fieldName;
            if (!ILRuntimeHelper.UIComponentTypes.TryGetValue(typeName, out UIType))
            {
                BDebug.LogError("【Uflux】type is null:" +typeName);
            }
            

        }
    }
}