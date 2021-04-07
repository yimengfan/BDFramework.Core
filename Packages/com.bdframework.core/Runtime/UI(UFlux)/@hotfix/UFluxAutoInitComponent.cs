using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Core;
using BDFramework.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 组件信息缓存
    /// </summary>
    public class ComponentClassCache
    {
        public FieldInfo[]    FieldInfos;
        public PropertyInfo[] PropertyInfos;
        public MethodInfo[]   MethodInfos;
    }

    /// <summary>
    /// Uflux辅助类
    /// </summary>
    static public partial class UFlux
    {
        /// <summary>
        /// Component 类数据缓存
        /// </summary>
        private static Dictionary<string, ComponentClassCache> ComponentClassCacheMap = new Dictionary<string, ComponentClassCache>();

        #region 自动设置值

        /// <summary>
        /// 初始化Component内容
        /// </summary>
        /// <param name="o"></param>
        static public void InitComponentContent(IComponent component)
        {
            var                 comType    = component.GetType();
            ComponentClassCache classCache = null;
            //反射获取信息
            if (!ComponentClassCacheMap.TryGetValue(comType.FullName, out classCache))
            {
                var fields     = comType.GetFields(BindingFlags.NonPublic     | BindingFlags.Instance | BindingFlags.Public);
                var properties = comType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                var methodes   = comType.GetMethods(BindingFlags.NonPublic    | BindingFlags.Instance | BindingFlags.Public);

                //筛选有属性的,且为自动赋值的
                classCache = new ComponentClassCache();
                classCache.FieldInfos = fields.Where((f) =>
                {
                    var attrs = f.GetCustomAttributes(false);
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        if (attrs[i] is AutoInitComponentContentAttribute) return true;
                    }
                    return false;
                }).ToArray();
                classCache.PropertyInfos = properties.Where((p) =>
                {
                    var attrs = p.GetCustomAttributes(false);
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        if (attrs[i] is AutoInitComponentContentAttribute) return true;
                    }
                    return false;
                }).ToArray();
                classCache.MethodInfos = methodes.Where((m) =>
                {
                    var attrs = m.GetCustomAttributes(false);
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        if (attrs[i] is AutoInitComponentContentAttribute) return true;
                    }
                    return false;
                }).ToArray();
                //缓存cls data
                ComponentClassCacheMap[comType.FullName] = classCache;
            }

            //开始初始化component内容
            foreach (var f in classCache.FieldInfos)
            {
                var attrs = f.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as AutoInitComponentContentAttribute)?.AutoSetField(component, f);
                }
            }

            foreach (var p in classCache.PropertyInfos)
            {
                var attrs = p.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as AutoInitComponentContentAttribute)?.AutoSetProperty(component, p);
                }
            }

            foreach (var m in classCache.MethodInfos)
            {
                var attrs = m.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as AutoInitComponentContentAttribute)?.AutoSetMethod(component, m);
                }
            }

            #endregion
        }
    }
}