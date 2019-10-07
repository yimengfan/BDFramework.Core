using System;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 描述成员变量 是给哪个组件 哪个字段赋值
    /// </summary>
    public class ComponentValueBind :Attribute
    {
        public Type Type;
        public string FieldName;

        public ComponentValueBind(Type t, string f)
        {
            this.Type = t;
            this.FieldName = f;
        }

    }
}