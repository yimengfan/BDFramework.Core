using System;

namespace BDFramework.UnitTest
{
    /// <summary>
    /// 单元测试基类属性
    /// </summary>
    abstract public class UnitTestBaseAttribute : Attribute
    {
        /// <summary>
        /// 执行顺序
        /// </summary>
        public int Order { get; private set; } = 0;

        /// <summary>
        /// 描述
        /// </summary>
        public string Des { get; private set; } = "";


        public UnitTestBaseAttribute(int order, string des)
        {
            this.Order = order;
            this.Des = des;
        }
    }
}