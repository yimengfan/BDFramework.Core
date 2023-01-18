using System;

namespace BDFramework.UnitTest.Runtime
{
    [UnitTest(des:  "异常测试")]
    public class APITest_Throw
    {


        [HotfixOnlyUnitTest(des: "异常")]
        static public void TestThrow()
        {
            throw new Exception("测试抛出异常");
        }
        
    }
}