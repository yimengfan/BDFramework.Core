using System.IO;
using Google.Protobuf;
using Test;
using UnityEngine;

namespace BDFramework.UnitTest
{
    [UnitTestAttribute(des:  "ILRuntime测试")]
    static public class APITest_Protobuf
    {
        /// <summary>
        /// 测试litjson
        /// </summary>
        [HotfixOnlyUnitTest(des:  "测试Protobuf")]
        public static void Protobuf()
        {
            one_item testItem = new one_item {Id = 1000, Name = "测试道具1", Amount = 2};

            rsp_getItem testItems = new rsp_getItem {UserName = "测试名称"};
            testItems.Items.Add(testItem);

            //测试字节数组 
            byte[] dataBytes = testItems.ToByteArray();
            //解析字节数组
            var parseByteItems = rsp_getItem.Parser.ParseFrom(dataBytes);

            //测试内存数据流
            var mem = new MemoryStream();
            parseByteItems.WriteTo(mem);
            mem.Position = 0;
            //解析内存数据流
            rsp_getItem parseMemItems = rsp_getItem.Parser.ParseFrom(mem);
            
            Assert.IsPass(
                //基础class
                parseMemItems.UserName == testItems.UserName
                && parseMemItems.Items.Count == testItems.Items.Count
                //嵌套
                && parseMemItems.Items[0].Id == testItems.Items[0].Id
                && parseMemItems.Items[0].Name == testItems.Items[0].Name
                && parseMemItems.Items[0].Amount == testItems.Items[0].Amount,
                "Protobuf 测试失败");
        }
    }
}