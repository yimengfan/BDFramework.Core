using System;
using UnityEngine;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// Hotfix的TestRunner
    /// </summary>
    public class HotfixTestRunner : MonoBehaviour
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
            
           
            BDLauncher.Inst.Launch();
       
           
        }
    }
}