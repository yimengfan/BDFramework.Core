using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 自动初始化ui组件
    /// 只要是UI组件都能被初始化
    /// </summary>
    public class TransformPathAttribute : AutoAssignAttribute
    {
        public string Path;

        public TransformPathAttribute(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// 设置字段
        /// </summary>
        /// <param name="winComponent"></param>
        /// <param name="fieldInfo"></param>
        public override void AutoSetField(IComponent winComponent, FieldInfo fieldInfo)
        {
            var value = GetTypeValue(winComponent, fieldInfo.FieldType, fieldInfo);
            if (value != null)
            {
                fieldInfo.SetValue(winComponent, value);
            }
        }


        /// <summary>
        /// 设置property
        /// </summary>
        /// <param name="winComponent"></param>
        /// <param name="propertyInfo"></param>
        public override void AutoSetProperty(IComponent winComponent, PropertyInfo propertyInfo)
        {
            var value = GetTypeValue(winComponent, propertyInfo.PropertyType, propertyInfo);
            if (value != null)
            {
                propertyInfo.SetValue(winComponent, value);
            }
        }


        /// <summary>
        /// 获取类型值
        /// </summary>
        /// <param name="winComponent"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object GetTypeValue(IComponent winComponent, Type type, MemberInfo memberInfo)
        {
            if (!winComponent.Transform)
            {
                throw new Exception($"【窗口:{winComponent.Transform.name}】 Transform Root为空:{this.Path} type:{memberInfo.Name}");
            }

            var memberInfoTransformNode = winComponent.Transform.Find(this.Path);
            if (!memberInfoTransformNode)
            {
                BDebug.LogError($"【窗口:{winComponent.Transform.name}】 不存在节点:{this.Path}");
                return null;
            }

            //是否为数组 或者泛型
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                var retArray = Array.CreateInstance(elementType, memberInfoTransformNode.childCount);

                for (int i = 0; i < memberInfoTransformNode.childCount; i++)
                {
                    var childNode = memberInfoTransformNode.GetChild(i);
                    object elementValue = null;

                    if (elementType == typeof(Transform))
                    {
                        elementValue = memberInfoTransformNode;
                    }
                    else if (elementType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        var uicom = childNode.GetComponent(elementType);
                        if (uicom)
                        {
                            elementValue = uicom;
                        }
                        else
                        {
                            BDebug.LogError($"【窗口:{winComponent.Transform.name}】 子节点:{this.Path} 不存在UI组件:{type.FullName}");
                        }
                    }
                    else
                    {
                        throw new Exception($"【窗口:{winComponent.Transform.name}】{memberInfo.Name} 不是UI组件 : => {type.Name}");
                    }

                    retArray.SetValue(elementValue, i);
                }


                //返回数组
                if (type.IsArray)
                {
                    return retArray;
                }
                else
                {
                    //返回list
                    var retList = Activator.CreateInstance(type) as IList;
                    foreach (var item in retArray)
                    {
                        retList.Add(item);
                    }

                    return retList;
                }
            }
            //非数组
            else
            {
                if (type == typeof(Transform))
                {
                    return memberInfoTransformNode;
                }
                else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    var uicom = memberInfoTransformNode.GetComponent(type);
                    if (uicom)
                    {
                        return uicom;
                    }
                    else
                    {
                        BDebug.LogError($"【窗口:{winComponent.Transform.name}】 子节点:{this.Path} 不存在UI组件:{type.FullName}");
                        return null;
                    }
                }
                else
                {
                    throw new Exception($"【窗口:{winComponent.Transform.name}】 自动赋值字段值 不是UI组件 :{memberInfo.Name} => {type.Name} - {this.Path}");
                }
            }

            return null;
        }
    }
}
