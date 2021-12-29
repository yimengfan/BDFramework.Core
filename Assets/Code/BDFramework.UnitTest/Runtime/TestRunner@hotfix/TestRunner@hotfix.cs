using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Hotfix.Reflection;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// 执行所有的runner
    /// </summary>
    static public class TestRunner
    {
        #region 对外的函数接口

        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunMonoCLRUnitTest()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("<color=red>----------------------开始测试MonoCLR-----------------------</color>");
            //热更模式
            CollectTestClassData(TestType.MonoOrCLR);
            //执行普通的测试
            ExcuteTest<UnitTestAttribute>();
        }

        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunHotfixUnitTest()
        {
            Debug.Log("<color=red>----------------------开始测试ILR-----------------------</color>");
            //搜集测试用例
            CollectTestClassData(TestType.ILRuntime);
            //1.执行普通的测试
            ExcuteTest<UnitTestAttribute>();
            //2.执行hotfix的测试
            ExcuteTest<HotfixOnlyUnitTestAttribute>();
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
                var assembly = typeof(ILRuntimeDelegateHelper).Assembly;
                types = assembly.GetTypes().ToList();
            }
            else if (testType == TestType.ILRuntime)
            {
                types = ILRuntimeHelper.GetHotfixTypes();
            }

            var attribute = typeof(UnitTestBaseAttribute);
            //测试用例类
            List<Type> testClassList = new List<Type>();
            foreach (var type in types)
            {
                var attr = type.GetAttributeInILRuntime<UnitTestBaseAttribute>();
                if (attr != null)
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
                    var mattrs = method.GetCustomAttributes(attribute, false);
                    var mattr  = mattrs[0] as UnitTestBaseAttribute;

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
        // static public void ExcuteTest<T>() where T : UnitTestBaseAttribute
        // {
        //     foreach (var item in testMethodDataMap)
        //     {
        //         //判断当前执行的测试类型
        //         var md = item.Value.FindAll((_item) => _item.TestData is T);
        //         if (md.Count > 0)
        //         {
        //             Debug.LogFormat("<color=yellow>---->执行:{0} </color>", item.Key.FullName);
        //         }
        //
        //         foreach (var methodData in md)
        //         {
        //             //开始执行测试
        //             try
        //             {
        //                 methodData.MethodInfo.Invoke(null, null);
        //                 Debug.LogFormat("<color=green>执行 {0}: 成功! - {1}</color>", methodData.TestData.Des, methodData.MethodInfo.Name);
        //             }
        //             catch (Exception e)
        //             {
        //                 Debug.LogErrorFormat("<color=red>执行 {0}: 失败! - {1}</color>", methodData.TestData.Des, methodData.MethodInfo.Name);
        //
        //                 // if (!ILRuntimeHelper.IsRunning)
        //                 // {
        //                 //
        //                 //     //Debug.LogError(JsonMapper.ToJson(e.Data.Keys));
        //                 // }
        //             }
        //         }
        //     }
        // }
        static public void ExcuteTest<T>() where T : UnitTestBaseAttribute
        {
            foreach (var item in testMethodDataMap)
            {
                //判断当前执行的测试类型
                var md = item.Value.FindAll((_item) => _item.TestData is T);
                if (md.Count > 0)
                {
                    Debug.LogFormat("<color=yellow>---->执行:{0} </color>", item.Key.FullName);
                }

                foreach (var methodData in md)
                {
                    bool   isFail  = false;
                    string failMsg = "";
                    //开始执行测试
                    try
                    {
                        methodData.MethodInfo.Invoke(null, null);
                        //采用最简单的状态模式，防止ilr下爆栈
                        Assert.GetAssertStaus(out isFail, out failMsg);
                        Assert.ClearStatus();
                    }
                    catch (Exception e)
                    {
                        isFail  = true;
                        Debug.LogError(e);
                    }

                    if (!isFail)
                    {
                        Debug.LogFormat("<color=green>执行 {0}: 成功! - {1}</color>", methodData.TestData.Des, methodData.MethodInfo.Name);
                    }
                    else
                    {
                        Debug.LogFormat("<color=red>执行 {0}: 失败! - {1}</color>", methodData.TestData.Des, methodData.MethodInfo.Name);
                     
                    }
                }
            }
        }
    }
}