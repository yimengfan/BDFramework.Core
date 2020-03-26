using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BDFramework.Test.hotfix
{

    /// <summary>
    /// 执行所有的runner
    /// </summary>
   static public class TestRunner
    {
        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunAll()
        {

            if (ILRuntimeHelper.IsRunning)
            {
                //ILR模式
                TestForILR();
            }
            else
            {
                //普通模式 
                TestForMono();
            }
        }

        static void TestForMono()
        {
            var assembly = typeof(BDLauncherBridge).Assembly;
            var attribute = typeof(HotfixTest);
            //测试用例类
            List<Type> testClassList =new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                var attrs = type.GetCustomAttributes(attribute,false);
                if (attrs.Length > 0)
                {
                    testClassList.Add(type);
                }
            }

            //开始执行
            foreach (var type in testClassList)
            {
                var methods = type.GetMethods(  BindingFlags.Static | BindingFlags.Public |
                                              BindingFlags.NonPublic);
                
                var attrs = type.GetCustomAttributes(attribute,false);
                var attr = attrs[0] as HotfixTest;
                Debug.LogFormat("<color=green>-------------------执行测试:{0}--------------------</color>",attr.Des);
                //开始执行方法 
                foreach (var method in methods)
                {
                    var mattrs = method.GetCustomAttributes(attribute, false);
                    var mattr = mattrs[0] as HotfixTest;
                    Debug.LogFormat("<color=green>---->执行:{0} </color>",mattr.Des);
                    method.Invoke(null,null);
                }
                Debug.Log("<color=green>------------------------执行完毕--------------------</color>");
            }
        }


        static void TestForILR()
        {
            
        }

}
}