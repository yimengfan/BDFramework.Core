using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.Hotfix.Reflection;
using DotNetExtension;
using LitJson;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// 执行所有的runner
    /// </summary>
    static public class TestRunner
    {
        /// <summary>
        /// Test方法的数据
        /// </summary>
        public class TestMethodData
        {
            public UnitTestBaseAttribute TestData;
            public MethodInfo MethodInfo;
        }

        static string monoUniTestResultPath = BApplication.BDEditorCachePath + "/Unitest_Mono";
        static string hotfixUniTestResultPath = BApplication.BDEditorCachePath + "/Unitest_Hotfix";

        /// <summary>
        /// 测试方法Map
        /// </summary>
        static public Dictionary<Type, List<TestMethodData>> TestMethodDataMap { get; private set; }

        /// <summary>
        /// 测试结果列表
        /// </summary>
        static public Dictionary<string, List<UniTestMethodResult>> TestResultMap { get; private set; }

        #region 对外的函数接口

        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunMonoCLRUnitTest()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("<color=red>----------------------开始测试MonoCLR-----------------------</color>");
            //热更模式
            TestMethodDataMap = CollectTestClassData(TestType.MonoOrCLR);
            //执行普通的测试
            TestResultMap = ExcuteTest<UnitTestAttribute>();
            
            //保存本地
            SaveTestDataToLocal(TestResultMap,false);
        }


        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunHotfixUnitTest()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("<color=red>----------------------开始测试ILR-----------------------</color>");
            //搜集测试用例
            TestMethodDataMap = CollectTestClassData(TestType.ILRuntime);
            //1.执行普通的测试
            ExcuteTest<UnitTestAttribute>();
            //2.执行hotfix的测试
            TestResultMap = ExcuteTest<HotfixOnlyUnitTestAttribute>();
            //保存本地
            SaveTestDataToLocal(TestResultMap,true);
        }

        #endregion


        /// <summary>
        /// 保存测试数据
        /// </summary>
        /// <param name="ret"></param>
        static public void SaveTestDataToLocal(object testRet,bool ishotFix)
        {
            var path = "";
            var hotfix = ishotFix?"_hotfix" : "";
            if (Application.isEditor)
            {
                path = IPath.Combine(BApplication.DevOpsPath, $"TestRenner/UnitTest{hotfix}_{DateTimeEx.GetTotalSeconds()}");
            }
            else
            {
                path = IPath.Combine(Application.persistentDataPath, $"TestRenner/UnitTest{hotfix}_{DateTimeEx.GetTotalSeconds()}");
            }

            var json = JsonMapper.ToJson(testRet, true);
            
            FileHelper.WriteAllText(path,json);
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
        static public Dictionary<Type, List<TestMethodData>> CollectTestClassData(TestType testType)
        {
            var retMap = new Dictionary<Type, List<TestMethodData>>();
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
                retMap[type] = testMethodDataList;
                //获取uit test并排序
                foreach (MethodInfo method in methods)
                {
                    UnitTestBaseAttribute mattr = null;
                    if (ILRuntimeHelper.IsRunning)
                    {
                        mattr = method.GetAttributeInILRuntime<UnitTestBaseAttribute>();
                    }
                    else
                    {
                        var mattrs = method.GetCustomAttributes(attribute, false);
                        mattr = mattrs[0] as UnitTestBaseAttribute;
                    }

                    if (mattr == null)
                    {
                        continue;
                    }

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

            return retMap;
        }


        public class UniTestMethodResult
        {
            /// <summary>
            /// 方法名
            /// </summary>
            public string MedthodName = "";

            /// <summary>
            /// 方法描述
            /// </summary>
            public string MethodDes = "";

            /// <summary>
            /// 是否失败
            /// </summary>
            public bool isFail = false;

            /// <summary>
            /// 失败信息
            /// </summary>
            public string failMsg = "";

            /// <summary>
            /// 耗时
            /// </summary>
            public float time = 0;
        }

        /// <summary>
        /// 执行正常测试
        /// </summary>
        static public Dictionary<string, List<UniTestMethodResult>> ExcuteTest<T>( string onlyTestClassName = "") where T : UnitTestBaseAttribute
        {
            Dictionary<string, List<UniTestMethodResult>> retMap = new Dictionary<string, List<UniTestMethodResult>>();
            foreach (var item in TestMethodDataMap)
            {
                //只执行某一个
                if (!string.IsNullOrEmpty(onlyTestClassName))
                {
                    if (item.Key.FullName != onlyTestClassName)
                    { 
                        continue;
                    }
                }
                
                //判断当前执行的测试类型
                var md = item.Value.FindAll((_item) => _item.TestData is T);
                if (md.Count > 0)
                {
                    Debug.LogFormat("<color=yellow>---->执行:{0} </color>", item.Key.FullName);
                }

                List<UniTestMethodResult> resultList = new List<UniTestMethodResult>();
                retMap[typeof(T).FullName] = resultList;
                //
                foreach (var methodData in md)
                {
                    var result = new UniTestMethodResult();
                    //开始执行测试
                    try
                    {
                        methodData.MethodInfo.Invoke(null, null);
                        //采用最简单的状态模式，防止ilr下爆栈
                        Assert.GetAssertStaus(out result.isFail, out result.failMsg, out result.time);
                        Assert.ClearStatus();
                    }
                    catch (Exception e)
                    {
                        result.isFail = true;
                        if (e.InnerException != null)
                        {
                            Debug.LogError(e.InnerException);
                        }
                        else
                        {
                            Debug.LogError(e);
                        }
                    }


                    var color = "";
                    if (!result.isFail)
                    {
                        color = "green";
                    }
                    else
                    {
                        color = "red";
                    }

                    if (result.time == 0)
                    {
                        if (result.isFail)
                        {
                            Debug.LogError($"<color={color}>执行 {methodData.TestData.Des}: {(result.isFail ? "失败" : "成功")}! - {methodData.MethodInfo.Name} </color>");
                        }
                        else
                        {
                            Debug.Log($"<color={color}>执行 {methodData.TestData.Des}: {(result.isFail ? "失败" : "成功")}! - {methodData.MethodInfo.Name} </color>");
                        }
                    }
                    else
                    {
                        if (result.isFail)
                        {
                            Debug.LogError($"<color={color}>执行 {methodData.TestData.Des}: {(result.isFail ? "失败" : "成功")}! - {methodData.MethodInfo.Name}, 耗时：<color=yellow>{result.time} ms</color>. </color>");
                        }
                        else
                        {
                            Debug.Log($"<color={color}>执行 {methodData.TestData.Des}: {(result.isFail ? "失败" : "成功")}! - {methodData.MethodInfo.Name}, 耗时：<color=yellow>{result.time} ms</color>. </color>");
                        }
                    }

                    resultList.Add(result);
                }
            }

            return retMap;
        }
    }
}
