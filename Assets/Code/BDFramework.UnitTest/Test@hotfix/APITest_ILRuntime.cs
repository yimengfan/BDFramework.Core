using System.Collections.Generic;
using BDFramework.UnitTest;
using Code.Game;
using UnityEngine;

namespace Tests
{
    [UnitTestAttribute(Des = "框架流程测试")]
    static public class APITest_ILRuntime
    {


        /// <summary>
        /// 测试启动逻辑
        /// </summary>
        [HotfixUnitTest(Des = "测试Map tryget 为null")]
        public static void MapTryGetTest()
        {
            Dictionary<string,object> testmap = new Dictionary<string, object>();
            testmap["t"] = null;

            object o =  "str";
            if (testmap.TryGetValue("t", out o))
            {
                int test = 110;
            }
            Assert.IsTrue(o==null,"map TryGet API Wrong");
        }
    }
}