using System;
using System.Collections.Generic;
using System.Reflection;

namespace Utils.mslibEx
{
    static public class TypeEx
    {
        public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        
    }
}
