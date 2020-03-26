using System;

namespace BDFramework.Test.hotfix
{
    public class HotfixTest : Attribute
    {
        /// <summary>
        /// 执行顺序
        /// </summary>
        public int Order = -1;
        /// <summary>
        /// 描述
        /// </summary>
        public string Des = "";
        
    }
}