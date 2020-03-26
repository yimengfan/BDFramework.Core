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
                   Debug.Log("<color=green>测试成功!</color>");
               }
               else
               {
                   Debug.LogErrorFormat("测试失败! value1:{0}  value2:{1}",obj,obj2);
               }
           }
        }
    }
}