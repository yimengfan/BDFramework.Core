using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFramework.Mgr;

namespace BDFramework.UFlux
{
    public class UIAttribute : ManagerAtrribute
    {
        public string ResourcePath { get; private set; }
       
        public UIAttribute(int intTag, string resPath):base(intTag)
        {
            this.ResourcePath = resPath;
        }
    }
}
