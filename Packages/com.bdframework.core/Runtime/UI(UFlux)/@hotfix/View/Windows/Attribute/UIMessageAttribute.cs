using System;

namespace BDFramework.UFlux
{
    /// <summary>
    /// ui消息对象
    /// </summary>
    public class UIMessageAttribute : Attribute
    {
        public int MessageName;

        public UIMessageAttribute(int name)
        {
            this.MessageName = name;
        }
    }
}