using System;
using Game.ILRuntime;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// Hotfix的TestRunner
    /// </summary>
    public class Mono_HotfixTestRunner : MonoBehaviour
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
            BDLauncher.Inst.Launch(this.GetType().Assembly.GetTypes(), GameLogicILRBinding.Bind);
        }
    }
}