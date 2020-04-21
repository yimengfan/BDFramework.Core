using System;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// Hotfix的TestRunner
    /// </summary>
    public class EditorTestRunner : MonoBehaviour
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

           

            //提前注册好生命周期 相关测试
            BDLauncher.OnBDFrameInitializedForTest = () =>
            {
                //搜集测试用例
                TestRunner.CollectTestClassData(TestRunner.TestType.MonoOrCLR);
                //1.执行普通的测试
                TestRunner.ExcuteTest<UnitTestAttribute>();
            };
           
            BDLauncher.Inst.Launch();
       
           
        }
    }
}