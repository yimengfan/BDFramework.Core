using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 自动初始化，节点自动赋值
    /// </summary>
    public class TransformPathAttribute : AutoInitComponentContentAttribute
    {
        public string Path;

        public TransformPathAttribute(string path)
        {
            this.Path = path;
        }
        /// <summary>
        /// 设置字段
        /// </summary>
        /// <param name="com"></param>
        /// <param name="fieldInfo"></param>
        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            if (!fieldInfo.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return;
            }
            
            Type uiType = fieldInfo.FieldType;
            var node = com.Transform.Find(this.Path);

            if (!node)
            {
                BDebug.LogError("节点存在:" + this.Path);
            }

            if (uiType == typeof(Transform))
            {
                fieldInfo.SetValue(com, node);
            }
            else
            {
                var ui = node.GetComponent(uiType);
                if (ui)
                {
                    fieldInfo.SetValue(com,ui);
                }
                else
                {
                    BDebug.LogError("组件不存在:" + uiType.FullName + " - " + this.Path);
                }
            }
        }


        /// <summary>
        /// 设置property
        /// </summary>
        /// <param name="com"></param>
        /// <param name="propertyInfo"></param>
        public override void AutoSetProperty(IComponent com, PropertyInfo propertyInfo)
        {
            if (!propertyInfo.PropertyType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return;
            }
            
            Type uiType = propertyInfo.PropertyType;
            var node = com.Transform.Find(this.Path);
            if (!node)
            {
                BDebug.LogError("节点存在:" + this.Path);
            }

            if (uiType == typeof(Transform))
            {
                propertyInfo.SetValue(com, node);
            }
            else
            {
                var ui = node.GetComponent(uiType);
                if (ui)
                {
                    propertyInfo.SetValue(com,ui);
                }
                else
                {
                    BDebug.LogError("组件不存在:" + uiType.FullName + " - " + this.Path);
                }
            }
        }
    }
}