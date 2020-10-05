using System;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// 热更单元测试属性
    /// </summary>
    public class HotfixUnitTestAttribute : UnitTestBaseAttribute
    {
        public HotfixUnitTestAttribute(int order=0, string des="") : base(order, des)
        {
        }
    }
}