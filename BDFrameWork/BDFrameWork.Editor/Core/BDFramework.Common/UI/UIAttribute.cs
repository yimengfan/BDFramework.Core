using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDFramework.UI
{
    public class UIAttribute : Attribute
    {
        public int Type { get; private set; }
        public string ResourcePath { get; private set; }

        public UIAttribute(int UIType, string UIResourcePath)
        {
            this.Type = UIType;
            this.ResourcePath = UIResourcePath;
        }
    }
}
