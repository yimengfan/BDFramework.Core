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
        static public void SetTransformPath(IUFluxComponent component)
        {
            var vt = component.GetType();
            var fields = vt.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
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
    }
}