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
    public class ComponentClassCache
    {
        public FieldInfo[] FieldInfos;
        public PropertyInfo[] PropertyInfos;
        public MethodInfo[] MethodInfos;
    }
    static public partial class UFlux
    {
        /// <summary>
        /// Component 类数据缓存
        /// </summary>
        private static Dictionary<string, ComponentClassCache> ComponentClassCacheMap = new Dictionary<string, ComponentClassCache>();

        #region 自动设置值

        /// <summary>
        /// 绑定Windows的值
        /// </summary>
        /// <param name="o"></param>
        static public void SetTransformPath(IComponent component)
        {
            var comType = component.GetType();

            ComponentClassCache classCache = null;

            if (!ComponentClassCacheMap.TryGetValue(comType.FullName, out classCache))
            {
                var fields = comType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                var properties =
                    comType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                var methodes = comType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                //筛选有属性的,且为自动赋值的
                classCache = new ComponentClassCache();
                classCache.FieldInfos = fields.Where((f) =>
                    {
                        if (f.GetCustomAttributesData().Count > 0)
                        {
                            var attrs = f.GetCustomAttributes(false);
                            for (int i = 0; i < attrs.Length; i++)
                            {
                                if (attrs[i] is UFluxAutoInitComponentAttribute)
                                    return true;
                            }
                        }
                        return false;
                    })
                    .ToArray();
                classCache.PropertyInfos = properties.Where((p) =>
                    {
                        if (p.GetCustomAttributesData().Count > 0)
                        {
                            var attrs = p.GetCustomAttributes(false);
                            for (int i = 0; i < attrs.Length; i++)
                            {
                                if (attrs[i] is UFluxAutoInitComponentAttribute)
                                    return true;
                            }
                        }
                        return false;
                    })
                    .ToArray();
                classCache.MethodInfos = methodes.Where((m) =>
                    {
                        if (m.GetCustomAttributesData().Count > 0)
                        {
                            var attrs = m.GetCustomAttributes(false);
                            for (int i = 0; i < attrs.Length; i++)
                            {
                                if (attrs[i] is UFluxAutoInitComponentAttribute)
                                    return true;
                            }
                        }

                        return false;
                    })
                    .ToArray();
                //缓存cls data
                ComponentClassCacheMap[comType.FullName] = classCache;
            }

            //开始赋值逻辑
            foreach (var f in classCache.FieldInfos)
            {
                var attrs = f.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as UFluxAutoInitComponentAttribute)?.AutoSetField(component, f);
                }
            }
            foreach (var p in classCache.PropertyInfos)
            {
                var attrs = p.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as UFluxAutoInitComponentAttribute)?.AutoSetProperty(component, p);
                }
            }
            foreach (var m in classCache.MethodInfos)
            {
                var attrs = m.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as UFluxAutoInitComponentAttribute)?.AutoSetMethod(component, m);
                }
            }
            #endregion
        }
    }
}