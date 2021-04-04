using System;
using System.Reflection;
using UnityEngine;

namespace BDFramework.Reflection
{
    static public class ReflectionExtension
    {
        /// <summary>
        /// 在ILR中获取Attrbute
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public T GetAttributeInILRuntime<T>(this MemberInfo memberInfo) where T : Attribute
        {
#if UNITY_EDITOR
            try
            {
#endif
                var attrs = memberInfo.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is T)
                    {
                        return (attr as T);
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
    }
}