using System;
using System.Collections.Generic;
using System.Reflection;

namespace BDFramework.UFlux
{
    static public class StateFactory
    {
        static Dictionary<Type, Dictionary<string, MemberInfo>> StateCahce = new Dictionary<Type, Dictionary<string, MemberInfo>>();

        /// <summary>
        /// 获取cache
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Dictionary<string, MemberInfo> GetCache(Type t)
        {
            Dictionary<string, MemberInfo> map = null;
            StateCahce.TryGetValue(t, out map);
            return map;
        }

        /// <summary>
        /// 添加cache
        /// </summary>
        /// <param name="t"></param>
        /// <param name="map"></param>
        public static void AddCache(Type t, Dictionary<string, MemberInfo> map)
        {
            StateCahce[t] = map;
        }
    }
}