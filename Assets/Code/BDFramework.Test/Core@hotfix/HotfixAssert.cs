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
        static public void Equals(object obj, object obj2)
        {
           var ret=  obj.Equals(obj2);
           if (ILRuntimeHelper.IsRunning)
           {
               
           }
           else
           {
               if (ret)
               {
                   Debug.Log("测试成功!");
               }
               else
               {
                   Debug.LogError("测试失败");
               }
           }
        }
    }
}