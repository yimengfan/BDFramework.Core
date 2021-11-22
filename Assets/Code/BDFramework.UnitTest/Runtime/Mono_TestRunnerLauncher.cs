using System;
using System.Collections;
using Game.ILRuntime;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// Hotfix的TestRunner
    /// </summary>
    public class Mono_TestRunnerLauncher : MonoBehaviour
    {
        private void Start()
        {
            this.StartCoroutine(RunUnitTest());
        }

        /// <summary>
        /// 执行所有的TestRunner
        /// </summary>
        public IEnumerator RunUnitTest()
        {
            BDLauncher.Inst.Launch(this.GetType().Assembly.GetTypes(), GameLogicCLRBinding.Bind);

            yield return new WaitForSeconds(2f);
            
            if (BDLauncher.Inst.GameConfig.CodeRoot!= AssetLoadPathType.Editor && ILRuntimeHelper.IsRunning)
            {
                //执行热更单元测试
                ILRuntimeHelper.AppDomain.Invoke("BDFramework.UnitTest.TestRunner", "RunHotfixUnitTest", null, new object[] { });
            }
            else
            {
                
#if UNITY_EDITOR
                TestRunner.RunMonoCLRUnitTest();
#endif
            }
        }
    }
}