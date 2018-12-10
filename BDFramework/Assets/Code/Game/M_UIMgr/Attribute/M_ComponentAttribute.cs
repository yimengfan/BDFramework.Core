using System;
using UnityEngine;

namespace Game.UI
{
    public enum M_ComponentType :int
    {
        Image=0,
        Text,
        Toggle,
        Slider,
        ScrollBar,
    }
    public enum  M_CustomField:int
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
    public class M_ComponentAttribute: Attribute
    {
        /// <summary>
        /// UITools_Attribute中定义的字段
        /// </summary>
        /// <returns></returns>
        public string ToolTag_FieldName { get; private set; }
        /// <summary>
        /// 组件的类型
        /// </summary>
        public M_ComponentType MComponentType { get; private set; }
        /// <summary>
        /// 需要修改 组件的字段
        /// </summary>
        public string ComponentField { get; private set; }
        /// <summary>
        /// 自定义的字段,一些特殊功能
        /// </summary>
        public M_CustomField MCustomField { get; private set; }
        public M_ComponentAttribute(string toolTag_fieldname, M_ComponentType mComponentEnum ,string componentField)
        {
            this.ToolTag_FieldName = toolTag_fieldname;
            this.MComponentType = mComponentEnum;
            this.ComponentField = componentField;
        }
        
        
        public M_ComponentAttribute(string toolTag_fieldname,M_ComponentType mComponentEnum , M_CustomField mCustomField)
        {
            this.ToolTag_FieldName = toolTag_fieldname;
            this.MComponentType = mComponentEnum;
            this.MCustomField = mCustomField;
        }
        
        
    }
}