using System;
using BDFramework.DataListener;
using BDFramework.UnitTest;

namespace Tests
{
    [UnitTest(Des = "数据监听测试")]
    static public class APITest_DataListener
    {
        [UnitTest(Des = "添加监听测试")]
        public static void AddListener()
        {
            int count = 0;
            int compareValue = 100;
            var service = DataListenerServer.Create("t");
            service.AddData("t");
            service.AddListener("t", (o) =>
            {
                //每次自增
                count++;
            });
           
            for (int i = 0; i < compareValue; i++)
            {
                service.TriggerEvent("t");
            }

            DataListenerServer.DelService("t");
            Assert.Equals(count, 100);
        }

        [UnitTest(Des = "触发次数测试")]
        public static void AddListener_TriggerCount()
        {
            int count = 0;
            var service = DataListenerServer.Create("t");
            service.AddData("t");
            service.AddListener("t", triggerNum: 10, callback: (o) =>
            {
                //每次自增
                count++;
            });

         
            //次数到了之后不会再执行
            for (int i = 0; i < 20; i++)
            {
                service.TriggerEvent("t");
            }

            DataListenerServer.DelService("t");
            Assert.Equals(count, 10);
        }

        [UnitTest(Des = "添加顺序测试")]
        public static void AddListener_Order()
        {
            int count = 0;
            int count2 = 0;
            int count3 = 0;
            var service = DataListenerServer.Create("t");
            service.AddData("t");
            service.AddListener("t", order: 10, callback: (o) =>
            {
                //每次自增
                count2++;
            });
            service.AddListener("t", order: 1, callback: (o) =>
            {
                //每次自增
                if (count2 == 0)
                {
                    count++;
                }
            });
            service.AddListener("t", order: 11, callback: (o) =>
            {
                //每次自增
                if (count == 1 && count2 == 1)
                {
                    count3++;
                }
            });

            //次数到了之后不会再执行
            service.TriggerEvent("t");
            DataListenerServer.DelService("t");
            //两个必须等于1
            Assert.Equals(count, 1);
            Assert.Equals(count2, 1);
            Assert.Equals(count3, 1);
        }
        

        [UnitTest(Des = "删除监听测试")]
        public static void DeleteListener()
        {
            int count = 0;
            var service = DataListenerServer.Create("t");
            service.AddData("t");
            Action<object> callback = (o) =>
            {
                //每次自增
                count++;
            };
            //初始化数据
            service.AddListener("t", triggerNum:10,order: 10, callback: callback);

            //测试
            for (int i = 0; i < 10; i++)
            {
                service.TriggerEvent("t");
                service.RemoveListener("t", callback);
            }
            DataListenerServer.DelService("t");
            //验证结果
            Assert.Equals(count, 1);
        }
    }
}