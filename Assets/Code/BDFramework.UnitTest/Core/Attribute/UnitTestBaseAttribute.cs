using System;

namespace BDFramework.UnitTest
{
   abstract public class UnitTestBaseAttribute : Attribute
    {
        /// <summary>
        /// 执行顺序
        /// </summary>
        public int Order = 0;
        /// <summary>
        /// 描述
        /// </summary>
        public string Des = "";
        
    }
}