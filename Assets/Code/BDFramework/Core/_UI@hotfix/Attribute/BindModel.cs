using System;

namespace BDFramework.UI
{
    public class BindModel : Attribute
    {
        public string Name;

        public BindModel(string name)
        {
            this.Name = name;
        }
    }
}