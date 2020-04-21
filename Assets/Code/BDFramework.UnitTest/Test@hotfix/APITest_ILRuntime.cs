using System.Collections.Generic;
using BDFramework.UnitTest;
using Code.Game;
using LitJson;
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
            Dictionary<string, object> testmap = new Dictionary<string, object>();
            testmap["t"] = null;

            object o = "str";
            if (testmap.TryGetValue("t", out o))
            {
                int test = 110;
            }

            Assert.IsTrue(o == null, "map TryGet API 失败");
        }


        public class LitjsonTest
        {
            public int            id     = 1;
            public string         s      = "tt";
            public List<int>      list   = new List<int>() {1};
            public SubLitjsonTest subObj = new SubLitjsonTest();
        }

        public class SubLitjsonTest
        {
            public int       id   = 1;
            public string    s    = "tt";
            public List<int> list = new List<int>() {1};
        }

        /// <summary>
        /// 测试litjson
        /// </summary>
        [HotfixUnitTest(Des = "测试litjson")]
        public static void LitJsonTest()
        {
            var obj  = new LitjsonTest();
            var json = JsonMapper.ToJson(obj);
            var obj2 = JsonMapper.ToObject<LitjsonTest>(json);

            Assert.IsPass(
                          //基础class
                          obj.id == obj2.id 
                          && obj.s == obj2.s 
                          && obj.list.Count == obj2.list.Count && obj.list[0] == obj2.list[0] 
                          //嵌套
                          && obj.subObj.id == obj2.subObj.id 
                          && obj.subObj.s == obj2.subObj.s 
                          && obj.subObj.list.Count == obj2.subObj.list.Count && obj.subObj.list[0] == obj2.subObj.list[0],
                          "litjson 测试失败");
        }
    }
}