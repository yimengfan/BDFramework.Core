using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace  BDFramework.Logic.Item
{
    public  class EventAttribute: Attribute
    {
        public int Type { get; private set; }
        public string Name { get; private set; }
        public EventAttribute(int type,string name)
        {
            this.Type = type;
            this.Name = name;
        }
    }
}
