using System.Collections.Generic;
using System.Net;
using BDFramework.UnitTest;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UnitTest
{
    [UnitTestAttribute(des:  "ILRuntime测试")]
    static public class APITest_LitJson
    {
        /// <summary>
        /// 测试启动逻辑
        /// </summary>
        [HotfixOnlyUnitTest(des:  "测试Map try get 为null")]
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
        
        public class A
        {
            
        }
        public class B:A
        {
            
        }
        public class C:B
        {
            
        }
        
        /// <summary>
        /// 测试litjson
        /// </summary>
        [HotfixOnlyUnitTest(des: "测试类型判断")]
        public static void TypeTest()
        {
            var b = new B();
            var c =new C();
            Assert.IsPass(b is A, "b父类判断");
            Assert.IsPass(b is B, "b父类2判断");
            Assert.IsFalse(b is C, "类型容错判断");
            Assert.IsPass(c is A, "c父类判断");
            Assert.IsPass(c is B, "c父类2判断");
            Assert.IsPass(c is C, "c本身判断");

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
        [HotfixOnlyUnitTest(des:  "测试litjson")]
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

        
                
        /// <summary>
        /// 测试litjson
        /// </summary>
        [HotfixOnlyUnitTest(des: "测试Await")]
        public static async void AwaitAsyncTest()
        {
            WebClient wc  =new WebClient();
            var       ret = await wc.DownloadStringTaskAsync("http://www.baidu.com");
            Debug.Log("Async Await测试:" + ret);
            Assert.IsPass(true, "测试Await Async");
        }
    }
}