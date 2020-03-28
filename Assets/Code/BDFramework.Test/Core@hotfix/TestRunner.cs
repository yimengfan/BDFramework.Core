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


        private static Dictionary<Type, List<TestMethodData>> testMethodMap;

        public class TestMethodData
        {
            public HotfixTest TestAttribute;
            public MethodInfo MethodInfo;
        }

        static void TestForMono()
        {
            testMethodMap = new Dictionary<Type, List<TestMethodData>>();
            //
            var assembly = typeof(BDLauncherBridge).Assembly;
            var attribute = typeof(HotfixTest);
            //测试用例类
            List<Type> testClassList = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                var attrs = type.GetCustomAttributes(attribute, false);
                if (attrs.Length > 0)
                {
                    testClassList.Add(type);
                }
            }

            //搜集Test信息
            foreach (var type in testClassList)
            {
                var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                // var attrs = type.GetCustomAttributes(attribute,false);
                // var attr = attrs[0] as HotfixTest;
                var testMethodDataList = new List<TestMethodData>();
                testMethodMap[type] = testMethodDataList;
                //获取uit test并排序
                foreach (var method in methods)
                {
                    var mattrs = method.GetCustomAttributes(attribute, false);
                    var mattr = mattrs[0] as HotfixTest;

                    //数据
                    var newMethodData = new TestMethodData() {MethodInfo = method, TestAttribute = mattr,};


                    //添加整合排序
                    bool isAdd = false;
                    for (int i = 0; i < testMethodDataList.Count; i++)
                    {
                        var tdata = testMethodDataList[i];

                        if (newMethodData.TestAttribute.Order < tdata.TestAttribute.Order)
                        {
                            testMethodDataList.Insert(i, newMethodData);
                            isAdd = true;
                            break;
                        }
                    }

                    if (!isAdd)
                    {
                        testMethodDataList.Add(newMethodData);
                    }
                }
            }

            foreach (var item in testMethodMap)
            {
                 Debug.LogFormat("<color=yellow>---->执行:{0} </color>",item.Key.FullName);

                 foreach (var methodData in item.Value)
                 {
                     try
                     {
                         methodData.MethodInfo.Invoke(null,null);
                         Debug.LogFormat("<color=green>----->{0} 成功!</color>",methodData.MethodInfo.Name);
                     }
                     catch (Exception e)
                     {
                         Debug.Log(e.StackTrace);
                         Debug.LogErrorFormat("<color=red>----->{0} 失败!</color>",methodData.MethodInfo.Name);
                     }
                   
                 }
            }
            
        }


        static void TestForILR()
        {
        }
    }
}