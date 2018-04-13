using System;

namespace BDFramework.UI
{
    public class BindData : Attribute
    {
        public string Name;

        public BindData(string name)
        {
            this.Name = name;
        }
    }
}