using System;
using System.Reflection;

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

            var attrs = memberInfo.GetCustomAttributes(false);

            foreach (var attr in attrs)
            {
                if (attr is T)
                {
                    return (attr as T);
                }
            }
            
            return null;
        }
    }
}