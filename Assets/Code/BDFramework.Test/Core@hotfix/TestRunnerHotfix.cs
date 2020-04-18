using System;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// Hotfix的TestRunner
    /// </summary>
    public class TestRunnerHotfix : MonoBehaviour
    {
        private void Start()
        {
            RunUnitTest();
        }

        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        public void RunUnitTest()
        {
            //执行普通的测试
            TestRunner.ExcuteTest<HotfixTest>();
            //执行流程性测试
            BDLauncher.OnBDFrameInitialized = () =>
            {
                //当框架初始化完成的测试
                TestRunner.ExcuteTest<HotfixTestOnFrameInitialized>();
            };
        }
    }
}