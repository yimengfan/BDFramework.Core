using System;

namespace BDFramework.UFlux
{
    public class UIMessageAttribute : Attribute
    {
        public int MessageName;

        public UIMessageAttribute(int name)
        {
            this.MessageName = name;
        }
    }
}