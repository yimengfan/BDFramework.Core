using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFramework.Mgr;

namespace BDFramework.UI
{
    public class UIAttribute : ManagerAtrribute
    {
        public string ResourcePath { get; private set; }
       
        public UIAttribute(int tag, string resPath):base(tag.ToString())
        {
            this.ResourcePath = resPath;
        }
    }
}
