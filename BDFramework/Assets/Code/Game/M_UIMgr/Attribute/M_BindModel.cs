using System;

namespace Game.UI
{
    public class M_BindModel : Attribute
    {
        public string Name;

        public M_BindModel(string name)
        {
            this.Name = name;
        }
    }
}