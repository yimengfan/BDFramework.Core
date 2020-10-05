using System;

namespace BDFramework.UnitTest
{
    /// <summary>
 /// 通用单元测试属性
 /// </summary>
    public class UnitTestAttribute : UnitTestBaseAttribute
    {
        public UnitTestAttribute(int order=1, string des="") : base(order, des)
        {
        }
    }
}