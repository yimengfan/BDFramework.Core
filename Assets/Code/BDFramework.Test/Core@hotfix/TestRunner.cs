using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Game.demo6_UFlux;
using ILRuntime.Runtime.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// 执行所有的runner
    /// </summary>
    static public class TestRunner
    {
        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        static public void RunUnitTest()
        {
            if (ILRuntimeHelper.IsRunning)
            {
                //启动场景
                EditorSceneManager.LoadSceneAsync("Assets/Code/BDFramework.Test/BDFrame.UnitTest.ILRuntime.unity", LoadSceneMode.Single);
                //执行
                EditorApplication.ExecuteMenuItem("Edit/Play");

            }
            else
            {
                // //热更模式
                // CollectTestClassData(TestType.ILRuntime);
                //
                // //执行普通的测试
                // ExcuteTest<HotfixTest>();
                // //执行流程性测试
                // BDLauncher.OnBDFrameInitialized = () =>
                // {
                //     //当框架初始化完成的测试
                //     ExcuteTest<HotfixTestOnFrameInitialized>();
                // };
            
                //启动场景
                EditorSceneManager.OpenScene("Assets/Code/BDFramework.Test/BDFrame.UnitTest.Mono.unity");
                //执行
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }
            
  
            
        }




        private static Dictionary<Type, List<TestMethodData>> testMethodDataMap;

        /// <summary>
        /// Test方法的数据
        /// </summary>
        public class TestMethodData
        {
            public HotfixTestBase TestData;
            public MethodInfo MethodInfo;
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
                 var assembly = typeof(BDLauncherBridge).Assembly;
                 types = assembly.GetTypes().ToList();
             }
             else    if (testType == TestType.ILRuntime)
             {
                 types = ILRuntimeHelper.GetHotfixTypes();
             }
          
            var attribute = typeof(HotfixTestBase);
            //测试用例类
            List<Type> testClassList = new List<Type>();
            foreach (var type in types)
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
                testMethodDataMap[type] = testMethodDataList;
                //获取uit test并排序
                foreach (var method in methods)
                {
                    var mattrs = method.GetCustomAttributes(attribute, false);
                    var mattr = mattrs[0] as HotfixTestBase;

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
        static  public void ExcuteTest<T>() where T: HotfixTestBase
        {
            foreach (var item in testMethodDataMap)
            {
                Debug.LogFormat("<color=yellow>---->执行:{0} </color>",item.Key.FullName);

                foreach (var methodData in item.Value)
                {
                    //判断当前执行的测试类型
                    if (!(methodData.TestData is T))
                    {
                        continue;
                    }
                    //开始执行测试
                    try
                    {
                        methodData.MethodInfo.Invoke(null,null);
                        Debug.LogFormat("<color=green>执行:{0}: 成功! - {1}</color>",methodData.TestData.Des,methodData.MethodInfo.Name);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("<color=red>执行{0}: {1}</color>",methodData.MethodInfo.Name,e.InnerException.Message);
                        Debug.Log(e.StackTrace);
                    }
                   
                }
            }
        }

  
    }
}