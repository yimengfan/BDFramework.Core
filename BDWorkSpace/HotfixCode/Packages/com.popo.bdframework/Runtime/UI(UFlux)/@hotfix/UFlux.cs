using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.ResourceMgr;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.UFlux
{
    static public partial class UFlux
    {
        #region 组件初始化

        /// <summary>
        /// 组件类缓存
        /// </summary>
        public class ComponentClassCache
        {
            public FieldInfo[] FieldInfos;
            public PropertyInfo[] PropertyInfos;
            public MethodInfo[] MethodInfos;
        }

        /// <summary>
        /// Component 类数据缓存
        /// </summary>
        static Dictionary<string, ComponentClassCache> ComponentClassCacheMap =
            new Dictionary<string, ComponentClassCache>();

        /// <summary>
        /// 绑定Windows的值
        /// </summary>
        /// <param name="o"></param>
        static public void InitComponent(IComponent component)
        {
            var comType = component.GetType();

            ComponentClassCache classCache = null;

            //缓存各种Component的class数据
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
                        var attrs = f.GetCustomAttributes(false);
                        for (int i = 0; i < attrs.Length; i++)
                        {
                            if (attrs[i] is AutoInitComponentAttribute)
                                return true;
                        }
                        return false;
                    })
                    .ToArray();
                classCache.PropertyInfos = properties.Where((p) =>
                    {
                        var attrs = p.GetCustomAttributes(false);
                        for (int i = 0; i < attrs.Length; i++)
                        {
                            if (attrs[i] is AutoInitComponentAttribute)
                                return true;
                        }
                        return false;
                    })
                    .ToArray();
                classCache.MethodInfos = methodes.Where((m) =>
                    {
                        var attrs = m.GetCustomAttributes(false);
                        for (int i = 0; i < attrs.Length; i++)
                        {
                            if (attrs[i] is AutoInitComponentAttribute)
                                return true;
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
                    (attrs[i] as AutoInitComponentAttribute)?.AutoSetField(component, f);
                }
            }

            foreach (var p in classCache.PropertyInfos)
            {
                var attrs = p.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as AutoInitComponentAttribute)?.AutoSetProperty(component, p);
                }
            }

            foreach (var m in classCache.MethodInfos)
            {
                var attrs = m.GetCustomAttributes(false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    (attrs[i] as AutoInitComponentAttribute)?.AutoSetMethod(component, m);
                }
            }
        }

        #endregion

        #region 组件值绑定

        /// <summary>
        /// 设置Component Props
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="aState"></param>
        static public void SetComponentProps(Transform trans, APropsBase props)
        {
            ComponentBindAdaptorManager.Inst.SetTransformProps(trans, props);
        }

        #endregion

        #region 资源相关操作

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        static public T Load<T>(string path) where T : Object
        {
            return BResources.Load<T>(path);
        }

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        static public void AsyncLoad<T>(string path, Action<T> callback) where T : Object
        {
            BResources.AsyncLoad<T>(path, callback);
        }

        /// <summary>
        /// 删除接口
        /// </summary>
        /// <param name="go"></param>
        static public void Destroy(GameObject go)
        {
            BResources.Destroy(go);
        }

        /// <summary>
        /// 卸载，ab中需要
        /// </summary>
        /// <param name="path"></param>
        static public void Unload(string path)
        {
            BResources.UnloadAsset(path);
        }

        #endregion
    }
}