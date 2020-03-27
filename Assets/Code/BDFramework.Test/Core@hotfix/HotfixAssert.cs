using System;
using UnityEngine;

namespace BDFramework.Test.hotfix
{
    static public class HotfixAssert
    {
        
        /// <summary>
        /// 比较值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="obj2"></param>
        static public bool Equals(object obj, object obj2)
        {
           var ret=  obj.Equals(obj2);
           if (ILRuntimeHelper.IsRunning)
           {
               
           }
           else
           {
               if (!ret)
               {
                   throw new Exception("测试失败");
               }
           }

           return ret;
        }
    }
}