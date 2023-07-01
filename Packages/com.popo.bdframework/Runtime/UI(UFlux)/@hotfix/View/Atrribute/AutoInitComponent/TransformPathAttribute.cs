using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 自动初始化ui组件（只要是UI组件都能被初始化）
    /// </summary>
    public class TransformPathAttribute : AutoInitComponentAttribute
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
                throw new Exception($"赋值目标不是UI组件,fieldName:{fieldInfo.Name} => {fieldInfo.FieldType.Name} - {this.Path}");
                return;
            }

            if (!com.Transform)
            {
               throw new Exception($"transformRoot为空:{this.Path} type:{fieldInfo.FieldType.Name}");
                return;
            }
            Type uiType = fieldInfo.FieldType;
            var node = com.Transform.Find(this.Path);
            if (!node)
            {
                BDebug.LogError($"窗口:{com} 不存在节点:{ this.Path}");
                return;
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
                    BDebug.LogError($"窗口:{com} 节点:{ this.Path} 不存在:{uiType.FullName}");
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
            if (!com.Transform)
            {
                throw new Exception($"transformRoot为空:{this.Path} type:{propertyInfo.PropertyType.Name}");
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
                    BDebug.LogError("窗口:" + com + "组件不存在:" + uiType.FullName + " - " + this.Path);
                }
            }
        }
    }
}