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
        public string FieldName { get; private set; }

        /// <summary>
        /// 绑定UI类
        /// </summary>
        public Type UIComponentType;

        /// <summary>
        /// 构造函数
        /// 热更Attr不支持基础类型以外
        /// </summary>
        /// <param name="uiType"></param>
        /// <param name="fieldName"></param>
        public ComponentValueBindAttribute(Type uiType, string fieldName)
        {
            this.FieldName = fieldName;
            //这里故意让破坏优化 ilrbug
            var ot = (object) uiType;
            string typeFullname = "";
            if (ot is TypeReference)
            {
                typeFullname = ((TypeReference) ot).FullName;
            }
            else
            {
                typeFullname = uiType.FullName;
            }
            

            var type = ComponentBindAdaptorManager.Inst.GetBindComponentType(typeFullname);
            this.UIComponentType = type;

            if (type == null)
            {
                BDebug.LogError("ComponentBindAdaptor不存在:" +  typeFullname);
            }
        }
    }
}