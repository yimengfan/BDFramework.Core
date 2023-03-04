using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BDFramework.UFlux
{
    static public partial class UFlux
    {
        #region 自动设置值

        private static Type checkType = typeof(Object);
        /// <summary>
        /// 绑定Windows的值
        /// </summary>
        /// <param name="o"></param>
        static public void SetTransformPath(IComponent component)
        {
            var vt = component.GetType();
            var fields = vt.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            // 排除基类中的字段，上一步的GetFields将获取基类中的所有字段包括隐藏的字段，导致使用基类中的TransformPath绑定找不到路径的问题
            fields = RemoveBaseFieldInfos(fields.ToList()).ToArray();

            var vTransform = component.Transform;
            foreach (var f in fields)
            {
                if (f.FieldType.IsSubclassOf(checkType) == false)
                {
                    continue;
                }

                //1.自动获取节点
                //TODO 热更层必须这样获取属性
                var _attrs = f.GetCustomAttributes(typeof(TransformPath), false); //as Attribute[];
                if (_attrs != null && _attrs.Length > 0)
                {
                    var attr = _attrs.ToList().Find((a) => a is TransformPath) as TransformPath;
                    if (attr == null) continue;
                    //获取节点,并且获取组件
                    var trans = vTransform.Find(attr.Path);
                    if (trans == null)
                    {
                        BDebug.LogError(string.Format("自动设置节点失败：{0} - {1}", vt.FullName, attr.Path));
                        continue;
                    }
                    var com = trans.GetComponent(f.FieldType);
                    if (com == null)
                    {
                        BDebug.LogError(string.Format("节点没有对应组件：type【{0}】 - {1}", f.FieldType, attr.Path));
                    }

                    //设置属性
                    f.SetValue(component, com);
                }
            }

            #endregion
        }

        /// <summary>
        /// 排除父类中定义的字段
        /// </summary>
        /// <param name="fieldInfos"></param>
        /// <returns></returns>
        static List<FieldInfo> RemoveBaseFieldInfos(List<FieldInfo> fieldInfos)
        {
            if (fieldInfos == null) return fieldInfos;
            var targetFieldInfos = new List<FieldInfo>();// 最终处理完成的字段名
            var sortedFieldName = new List<string>(); // 已处理过的字段名
            foreach (var fielfInfo in fieldInfos)
            {
                if (!sortedFieldName.Contains(fielfInfo.Name))
                {
                    // 获取所有同名字段
                    var fis = fieldInfos.FindAll(f => f.Name.Equals(fielfInfo.Name));
                    FieldInfo fi = null;
                    if (fis.Count == 1)
                    {
                        fi = fis[0];
                    }
                    else
                    {
                        // 获取派生类字段
                        fi = GetFieldInDerivedClasses(fis);
                    }
                    targetFieldInfos.Add(fi);
                    sortedFieldName.Add(fi.Name);
                }
            }
            return targetFieldInfos;
        }

        /// <summary>
        /// 获取最后一级派生类中的字段
        /// </summary>
        /// <param name="fieldInfos"></param>
        static FieldInfo GetFieldInDerivedClasses(List<FieldInfo> fieldInfos)
        {
            if (fieldInfos.Count == 1) return fieldInfos[0];
            // 获取所有的基类和派生类
            FieldInfo targetFieldInfo = null;
            var targetDefTypeFullName = string.Empty;
            foreach (var fieldInfo in fieldInfos)
            {
                var dFullName = string.Empty;
                var definitionField = fieldInfo.GetType().GetField("definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                // 直接执行
                if (definitionField == null)
                {
                    dFullName = fieldInfo.DeclaringType.FullName;
                }
                // 在ILRuntime中执行
                else
                {
                    var definition = definitionField.GetValue(fieldInfo);
                    if (definition != null)
                    {
                        var defType = definition.GetType();

                        var properties = typeof(ILRuntime.Mono.Cecil.FieldDefinition).GetProperties().ToList();
                        var declaringTypeField = properties.Find(p => p.Name.Equals("DeclaringType"));

                        var declaringType = declaringTypeField.GetValue(definition);
                        var fullNameField = typeof(ILRuntime.Mono.Cecil.TypeReference).GetProperty("FullName");
                        var fullName = fullNameField.GetValue(declaringType);
                        dFullName = fullName.ToString();
                    }
                }
                // 对比字段定义类名的长度，最长的就是最后定义的
                if (dFullName.Length > targetDefTypeFullName.Length)
                {
                    targetFieldInfo = fieldInfo;
                    targetDefTypeFullName = dFullName;
                }
            }
            return targetFieldInfo;
        }
    }
}