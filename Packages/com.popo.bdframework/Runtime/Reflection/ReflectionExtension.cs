using System;
using System.Reflection;
using UnityEngine;

namespace BDFramework.Hotfix.Reflection
{
    /// <summary>
    /// 反射扩展
    /// </summary>
    static public class ReflectionExtension
    {
        /// <summary>
        /// 在ILR中获取Attrbute
        /// T需要是热更类型的Attribute，不然is判断会报错
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public T GetAttributeInILRuntime<T>(this MemberInfo memberInfo) where T : Attribute
        {
#if UNITY_EDITOR
            //Editor下的判断
            // if (ILRuntimeHelper.IsRunning)
            // {
            //     if (!typeof(T).Namespace.Contains(".ILRuntimeType"))
            //     {
            //         Debug.Log("热更中获取 主工程Attribute，某些版本ILRuntime会出错![v1.6.6]");
            //     }
            // }

            try
            {
#endif
                var attrs = memberInfo.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is T t)
                    {
                        return t;
                    }
                }
#if UNITY_EDITOR
            }
            catch (Exception e)
            {
                Debug.LogError("获取[Attribute]失败:" + memberInfo.Name + "\n 请注意该Attribute构造过程中是否报错!");
            }
#endif
            return null;
        }

        /// <summary>
        /// 在ILR中获取Attrbute
        /// T需要是热更类型的Attribute，不然is判断会报错
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public T[] GetAttributeInILRuntimes<T>(this MemberInfo memberInfo) where T : Attribute
        {
#if UNITY_EDITOR

            try
            {
#endif
                var attrs = memberInfo.GetCustomAttributes(false);
                T[] ts = new T[attrs.Length];

                for (int i = 0; i < attrs.Length; i++)
                {
                    var attr = attrs[i];
                    if (attr is T t)
                    {
                        ts[i] = t;
                    }
                }

                return ts;

#if UNITY_EDITOR
            }
            catch (Exception e)
            {
                Debug.LogError("获取[Attribute]失败:" + memberInfo.Name + "\n 请注意该Attribute构造过程中是否报错!");
            }
#endif
            return null;
        }
    }
}
