using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Reflection;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// 执行所有的runner
    /// </summary>
    static public class HotfixTestRunner
    {
        #region 对外的函数接口
        
        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunHotfixUnitTest()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("<color=red>----------------------开始测试-----------------------</color>");
            //搜集测试用例
            CollectTestClassData(TestType.ILRuntime);
            //1.执行普通的测试
            ExcuteTest<UnitTestAttribute>();
            //2.执行hotfix的测试
            ExcuteTest<HotfixUnitTestAttribute>();
        }

        #endregion


        /// <summary>
        /// 测试函数的集合
        /// </summary>
        private static Dictionary<Type, List<TestMethodData>> testMethodDataMap;

        /// <summary>
        /// Test方法的数据
        /// </summary>
        public class TestMethodData
        {
            public UnitTestBaseAttribute TestData;
            public MethodInfo            MethodInfo;
        }


        /// <summary>
        /// 测试类型
        /// </summary>
        public enum TestType
        {
            MonoOrCLR,
            ILRuntime
        }

        /// <summary>
        /// 收集Test的数据
        /// </summary>
        static public void CollectTestClassData(TestType testType)
        {
            testMethodDataMap = new Dictionary<Type, List<TestMethodData>>();
            List<Type> types = new List<Type>();
            //判断不同的模式
            if (testType == TestType.MonoOrCLR)
            {
                var assembly = typeof(BDLauncher).Assembly;
                types = assembly.GetTypes().ToList();
            }
            else if (testType == TestType.ILRuntime)
            {
                types = ILRuntimeHelper.GetHotfixTypes();
            }
            
            //测试用例类
            List<Type> testClassList = new List<Type>();
            foreach (var type in types)
            {
                var attr = type.GetAttributeInILRuntime<UnitTestBaseAttribute>();
                if (attr!=null)
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
                testMethodDataMap[type] = testMethodDataList;
                //获取uit test并排序
                foreach (var method in methods)
                {
                    var mattr = method.GetAttributeInILRuntime<UnitTestBaseAttribute>();

                    //数据
                    var newMethodData = new TestMethodData() {MethodInfo = method, TestData = mattr,};

                    //添加整合排序
                    bool isAdd = false;
                    for (int i = 0; i < testMethodDataList.Count; i++)
                    {
                        var tdata = testMethodDataList[i];

                        if (newMethodData.TestData.Order < tdata.TestData.Order)
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
        }

        /// <summary>
        /// 执行正常测试
        /// </summary>
        static public void ExcuteTest<T>() where T : UnitTestBaseAttribute
        {
            foreach (var item in testMethodDataMap)
            {
                //判断当前执行的测试类型
                var md = item.Value.FindAll((_item) => _item.TestData is T);
                if(md.Count>0)
                {
                    Debug.LogFormat("<color=yellow>---->执行:{0} </color>", item.Key.FullName);
                }
                foreach (var methodData in md)
                {
                    //开始执行测试
                    try
                    {
                        methodData.MethodInfo.Invoke(null, null);
                        Debug.LogFormat("<color=green>执行 {0}: 成功! - {1}</color>", methodData.TestData.Des,
                                        methodData.MethodInfo.Name);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("<color=red>执行 {0}: 失败! - {1}</color>", methodData.TestData.Des,
                                             methodData.MethodInfo.Name);

                        if (e.InnerException!=null)
                            Debug.LogError(e.InnerException.Message + "\n" + e.InnerException.StackTrace);
                        else
                            Debug.LogError(e.Message + "\n" + e.StackTrace);
                    }
                }
            }
        }
    }
}
