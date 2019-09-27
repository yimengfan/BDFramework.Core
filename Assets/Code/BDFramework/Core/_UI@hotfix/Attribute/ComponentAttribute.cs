using System;
using UnityEngine;

namespace BDFramework.UI
{
    public enum ComponentType :int
    {
        Image=0,
        Text,
        Toggle,
        Slider,
        ScrollBar,
    }
    public enum  CustomField:int
    {
         Null =0,
        /// <summary>
        /// 资源加载路径
        /// </summary>
         ResourcePath,
        /// <summary>
        /// 设置GameObject状态
        /// </summary>
         GameObjectActive,
        /// <summary>
        /// 设置component的状态
        /// </summary>
         ComponentEnable,
        
    }
    public class ComponentAttribute: Attribute
    {
        /// <summary>
        /// UITools_Attribute中定义的字段
        /// </summary>
        /// <returns></returns>
        public string ToolTag_FieldName { get; private set; }
        /// <summary>
        /// 组件的类型
        /// </summary>
        public ComponentType ComponentType { get; private set; }
        /// <summary>
        /// 需要修改 组件的字段
        /// </summary>
        public string ComponentField { get; private set; }
        /// <summary>
        /// 自定义的字段,一些特殊功能
        /// </summary>
        public CustomField CustomField { get; private set; }
        public ComponentAttribute(string toolTag_fieldname, ComponentType componentEnum ,string componentField)
        {
            this.ToolTag_FieldName = toolTag_fieldname;
            this.ComponentType = componentEnum;
            this.ComponentField = componentField;
        }
        
        
        public ComponentAttribute(string toolTag_fieldname,ComponentType componentEnum , CustomField customField)
        {
            this.ToolTag_FieldName = toolTag_fieldname;
            this.ComponentType = componentEnum;
            this.CustomField = customField;
        }
        
        
    }
}