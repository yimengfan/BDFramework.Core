using System;

namespace BDFramework.UI
{
    public class BBindData : Attribute
    {
        public string Name;

        public BBindData(string name)
        {
            this.Name = name;
        }
    }
}