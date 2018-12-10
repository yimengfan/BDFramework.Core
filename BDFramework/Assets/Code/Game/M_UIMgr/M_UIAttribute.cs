using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Mgr;

namespace Game.UI
{
    public class M_UIAttribute : M_ManagerAtrribute//ManagerAtrribute
    {
        public string ResourcePath { get; private set; }
       
        public M_UIAttribute(int tag, string resPath):base(tag.ToString())
        {
            this.ResourcePath = resPath;
        }
    }
}
