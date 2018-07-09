using System;

namespace BDFramework.ScreenView
{
    public class ScreenViewAttribute: Attribute
    {

        public string Name = "null";
        public bool isDefault = false;
        public ScreenViewAttribute(string name, bool isDefault =false)
        {
            this.Name = name;
            this.isDefault = isDefault;
        }
    }
}